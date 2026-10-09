using System.Collections.Generic;
using System.Linq;
using Godot;

namespace PassengerArchive;

/// <summary>
/// Root script: builds the low-res 3D view + HUD, loads carriages, owns the rules of the game
/// (clues, accusation, rule violation → death, advancing to the next carriage).
/// </summary>
public partial class Game : Node
{
	Player _player;
	Hud _hud;
	TrainAudio _audio;
	SubViewport _vp;
	Node3D _worldRoot;

	int _index;
	bool _solved, _dead, _deathShown, _busy, _ended;
	readonly List<string> _clues = new();
	Interactable _focus;

	CarriageData Current => Carriages.All[_index];
	public bool CanAccuse => _clues.Count >= Current.MinClues;

	public override void _Ready()
	{
		RegisterInput();
		BuildView();

		_hud = new Hud();
		AddChild(_hud);
		_audio = new TrainAudio();
		AddChild(_audio);

		Input.MouseMode = Input.MouseModeEnum.Captured;
		LoadCarriage(first: true);
	}

	// ───────────────────────── setup ─────────────────────────
	static void RegisterInput()
	{
		Bind("move_forward", Key.W, Key.Up);
		Bind("move_back", Key.S, Key.Down);
		Bind("move_left", Key.A, Key.Left);
		Bind("move_right", Key.D, Key.Right);
		Bind("interact", Key.E);
		Bind("accuse", Key.F);
		Bind("notebook", Key.Tab);
		Bind("restart", Key.R);
	}

	static void Bind(string action, params Key[] keys)
	{
		if (!InputMap.HasAction(action)) InputMap.AddAction(action);
		foreach (var k in keys)
			InputMap.ActionAddEvent(action, new InputEventKey { PhysicalKeycode = k });
	}

	void BuildView()
	{
		var container = new SubViewportContainer
		{
			Stretch = true,
			StretchShrink = GameConfig.RenderShrink,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/post_ps1.gdshader") },
		};
		container.SetAnchorsPreset(Control.LayoutPreset.FullRect);
		AddChild(container);

		_vp = new SubViewport
		{
			OwnWorld3D = true,
			Msaa3D = Viewport.Msaa.Disabled,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		container.AddChild(_vp);

		var env = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			BackgroundColor = new Color(0.02f, 0.03f, 0.06f),
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			AmbientLightColor = new Color(0.35f, 0.4f, 0.6f),
			AmbientLightEnergy = 0.45f,
			FogEnabled = true,
			FogLightColor = new Color(0.03f, 0.04f, 0.07f),
			FogDensity = 0.018f,
		};
		_vp.AddChild(new WorldEnvironment { Environment = env });

		_player = new Player();
		_vp.AddChild(_player);
	}

	// ───────────────────────── carriage flow ─────────────────────────
	void LoadCarriage(bool first = false)
	{
		_worldRoot?.QueueFree();
		_worldRoot = CarriageBuilder.Build(Current, this);
		_vp.AddChild(_worldRoot);

		_solved = false;
		_dead = false;
		_deathShown = false;
		_clues.Clear();
		_player.Teleport(new Vector3(0, 0.05f, 6.0f), 0f);
		_audio.RestoreClacks();
		_hud.HideDeath();
		_hud.HideDialogue();

		_hud.SetFadeInstant(1f);
		var tw = _hud.Fade(0f, first ? 2.5f : 1.4f);
		tw.Finished += () => { _player.InputEnabled = !_hud.BookOpen; _busy = false; };

		_hud.Caption($"{Current.Title}\n{Current.Rule}", first ? 3.5f : 2.5f);
		if (first)
			_hud.Notify("WASD — рух · E — взаємодія · F — звинуватити · Tab — блокнот · Esc — курсор", 7f);
	}

	public void TryAdvance()
	{
		if (!_solved)
		{
			Notify("Двері зачинені. Хтось у цьому вагоні ще має відповісти за смерть.");
			return;
		}
		GoNext();
	}

	async void GoNext()
	{
		if (_busy) return;
		_busy = true;
		_player.InputEnabled = false;
		await ToSignal(_hud.Fade(1f, 0.9f), Tween.SignalName.Finished);

		_index++;
		if (_index >= Carriages.All.Length)
		{
			_ended = true;
			_hud.ShowDeath("КІНЕЦЬ ПРОТОТИПУ\n\nПотяг їде далі.\nПункт призначення — досі стертий.\n\n[R] — почати спочатку", ending: true);
			_busy = false;
			return;
		}
		LoadCarriage();
	}

	// ───────────────────────── gameplay API (used by interactables) ─────────────────────────
	public void Notify(string text) => _hud.Notify(text);

	public void Say(string speaker, Line line)
	{
		_hud.ShowDialogue(speaker, line.Text);
		if (line.Clue != null && !_clues.Contains(line.Clue))
		{
			_clues.Add(line.Clue);
			_hud.Notify($"У блокнот записано ({_clues.Count}/{Current.MinClues}): {line.Clue}", 4f);
		}
	}

	public void Accuse(PassengerData p)
	{
		if (_solved) { Notify("Вагон уже мовчить."); return; }
		if (!CanAccuse) { Notify("Доказів замало. Ти ще не певен."); return; }

		if (p.IsKiller)
		{
			_solved = true;
			_hud.ShowDialogue(p.Name, "...Гаразд. Ідіть далі. Тільки не озирайтесь.");
			_hud.Notify("Двері попереду відчинено.", 5f);
		}
		else if (GameConfig.WrongAccusationKills)
		{
			KillPlayer("Ти звинуватив невинного.");
		}
		else
		{
			Notify($"{p.Name} дивиться на тебе. Це була не та людина.");
		}
	}

	public void ViolateRule() => KillPlayer("Ти порушив правило вагона.");

	async void KillPlayer(string reason)
	{
		if (_dead) return;
		_dead = true;
		_player.InputEnabled = false;
		_hud.HideDialogue();
		_hud.CloseBook();
		_audio.CutClacks();
		foreach (var n in GetTree().GetNodesInGroup("lamps"))
			(n as FlickerLight)?.Extinguish();

		await ToSignal(GetTree().CreateTimer(2.2), SceneTreeTimer.SignalName.Timeout);

		string killer = Current.Passengers.First(x => x.IsKiller).Name;
		_hud.ShowDeath(
			$"{reason}\n\n{killer} вбиває тебе так само, як {Current.VictimName}:\n{Current.VictimMethod}.\n\n{Current.KillDescription}\n\n[R] — спробувати знову");
		_deathShown = true;
	}

	// ───────────────────────── per-frame ─────────────────────────
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("restart") && (_deathShown || _ended))
		{
			if (_ended) { _ended = false; _index = 0; }
			LoadCarriage();
			return;
		}

		if (_dead || _busy || _ended)
		{
			_hud.SetPrompt(null);
			return;
		}

		if (Input.IsActionJustPressed("notebook"))
		{
			_hud.ToggleBook(NotebookText());
			_player.InputEnabled = !_hud.BookOpen;
		}

		_focus = _player.InputEnabled ? _player.Focus() : null;
		string prompt = _focus?.Prompt;
		if (_focus?.AccusePrompt != null) prompt += "\n" + _focus.AccusePrompt;
		_hud.SetPrompt(prompt);

		if (_focus != null && _player.InputEnabled)
		{
			if (Input.IsActionJustPressed("interact")) _focus.Interact(this);
			else if (Input.IsActionJustPressed("accuse") && _focus.AccusePrompt != null) _focus.Accuse(this);
		}
	}

	public override void _Input(InputEvent e)
	{
		if (e is InputEventMouseMotion mm && Input.MouseMode == Input.MouseModeEnum.Captured)
			_player.Look(mm.Relative);
		else if (e is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Captured)
			Input.MouseMode = Input.MouseModeEnum.Captured;
		else if (e.IsActionPressed("ui_cancel"))
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
	}

	string NotebookText()
	{
		var c = Current;
		string notes = _clues.Count == 0
			? "  (порожньо — поговори з пасажирами, огляни тіло)"
			: string.Join("\n", _clues.Select(x => "  • " + x));
		return
			"КВИТОК\n" +
			"  Станція відправлення: ██████████\n" +
			"  Пункт призначення:    ░░░░░░░░░░  (стерто)\n\n" +
			$"{c.Title}\n" +
			$"  Правило вагона: {c.Rule}\n" +
			$"  Жертва: {c.VictimName}\n\n" +
			$"НОТАТКИ ({_clues.Count}/{c.MinClues} потрібно, щоб звинувачувати)\n{notes}\n\n" +
			"Tab — закрити";
	}
}
