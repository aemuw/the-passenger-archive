using System;
using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>
/// Root script: builds the low-res 3D view + HUD and runs the flow of the train:
/// carriage → (played memory) → carriage … doors lock behind you, the rule is never announced,
/// and breaking it makes the carriage's killer do to you what he did to the victim.
/// </summary>
public partial class Game : Node
{
	enum Mode { Carriage, Memory, Ending }

	public Hud Hud { get; private set; }
	public TrainAudio Audio { get; private set; }
	public string OfferTarget { get; private set; }
	public bool PhoneRinging { get; private set; }
	public Vector3 PlayerPosition => _player.GlobalPosition;

	static readonly Vector3 CarriageTint = new(1f, 0.98f, 0.9f);
	static readonly Vector3 MemoryTint = new(0.78f, 0.92f, 1.2f);

	Mode _mode;
	Player _player;
	SubViewport _vp;
	Node3D _worldRoot;
	MemoryScene _memory;
	Godot.Environment _env;
	ShaderMaterial _post;

	int _index;
	int _gen;                         // bumps on every carriage load; stale async continuations check it
	bool _dead, _deathShown, _busy, _memoryQueued;
	readonly HashSet<string> _flags = new();
	double _carriageTime, _offerTimer;
	bool _offerStarted, _overheardStarted;
	Vector3 _savedPos;
	float _savedYaw;
	Interactable _focus;

	Action _pendingUse;
	string _pendingPrompt;

	Line[] _script;
	int _scriptI;
	double _scriptT;
	Action _scriptDone;

	CarriageData Current => Carriages.All[_index];

	// ───────────────────────── setup ─────────────────────────
	public override void _Ready()
	{
		RegisterInput();
		BuildView();

		Hud = new Hud();
		AddChild(Hud);
		Audio = new TrainAudio();
		AddChild(Audio);

		Input.MouseMode = Input.MouseModeEnum.Captured;
		LoadCarriage(first: true);
	}

	static void RegisterInput()
	{
		Bind("move_forward", Key.W, Key.Up);
		Bind("move_back", Key.S, Key.Down);
		Bind("move_left", Key.A, Key.Left);
		Bind("move_right", Key.D, Key.Right);
		Bind("interact", Key.E);
		Bind("alt", Key.F);
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
		_post = new ShaderMaterial { Shader = GD.Load<Shader>("res://shaders/post_ps1.gdshader") };
		var container = new SubViewportContainer
		{
			Stretch = true,
			StretchShrink = GameConfig.RenderShrink,
			TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Material = _post,
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

		_env = new Godot.Environment
		{
			BackgroundMode = Godot.Environment.BGMode.Color,
			AmbientLightSource = Godot.Environment.AmbientSource.Color,
			FogEnabled = true,
		};
		ApplyLook(false);
		_vp.AddChild(new WorldEnvironment { Environment = _env });

		_player = new Player();
		_vp.AddChild(_player);
	}

	/// <summary>Reality = yellow-green and warm. Memory = cold blue.</summary>
	void ApplyLook(bool memory)
	{
		_env.BackgroundColor = memory ? new Color(0.03f, 0.05f, 0.10f) : new Color(0.02f, 0.03f, 0.06f);
		_env.AmbientLightColor = memory ? new Color(0.30f, 0.42f, 0.70f) : new Color(0.35f, 0.40f, 0.60f);
		_env.AmbientLightEnergy = memory ? 0.70f : 0.45f;
		_env.FogLightColor = memory ? new Color(0.05f, 0.08f, 0.16f) : new Color(0.03f, 0.04f, 0.07f);
		_env.FogDensity = memory ? 0.03f : 0.018f;
		_post.SetShaderParameter("tint", memory ? MemoryTint : CarriageTint);
	}

	// ───────────────────────── carriage flow ─────────────────────────
	void LoadCarriage(bool first = false)
	{
		_gen++;
		_memory?.QueueFree();
		_memory = null;
		_worldRoot?.QueueFree();

		_flags.Clear();
		_script = null;
		_scriptDone = null;
		ClearUse();
		Passenger.Frozen = false;
		_dead = false;
		_deathShown = false;
		_memoryQueued = false;
		_carriageTime = 0;
		_offerStarted = false;
		_overheardStarted = false;
		_offerTimer = 0;
		OfferTarget = null;
		PhoneRinging = false;
		Audio.SetRing(false);
		Audio.RestoreClacks();
		_mode = Mode.Carriage;
		ApplyLook(false);

		_worldRoot = CarriageBuilder.Build(Current, this);
		_vp.AddChild(_worldRoot);
		_player.Teleport(new Vector3(0, 0.05f, 6.0f), 0f);

		Hud.HideDeath();
		Hud.HideDialogue();
		_busy = true;
		_player.InputEnabled = false;
		Hud.SetFadeInstant(1f);
		var tw = Hud.Fade(0f, first ? 2.5f : 1.4f);
		tw.Finished += () => { _busy = false; _player.InputEnabled = !Hud.BookOpen; };

		Hud.Caption(Current.Title, 2.5f);
		if (first)
			Hud.Notify("WASD — рух · E — взаємодія · F — альтернативна дія · Tab — квиток · Esc — курсор", 7f);
	}

	public void TryDoor()
	{
		if (_mode != Mode.Carriage || _busy) return;
		if (_index >= Carriages.All.Length - 1 || !HasFlag("story_done")) { Notify("Двері не відчиняються."); return; }
		if (Current.DoorStuck && !HasFlag("unstuck"))
		{
			if (_script == null && Current.HelperScript != null) PlayScript(Current.HelperScript);
			else Notify("Двері заклинило.");
			return;
		}
		GoNext();
	}

	async void GoNext()
	{
		if (_busy) return;
		_busy = true;
		_player.InputEnabled = false;
		Audio.Click();
		int g = _gen;
		await ToSignal(Hud.Fade(1f, 0.9f), Tween.SignalName.Finished);
		if (g != _gen) return;

		var c = Current;
		string tag = c.Kind switch
		{
			StoryKind.Fiction => "FICTIONAL",
			StoryKind.Inspired => "INSPIRED",
			StoryKind.Documented => "DOCUMENTED",
			StoryKind.Nightmare => "NIGHTMARE",
			_ => "MEMORY",
		};
		string card = $"{c.Title} · {tag}\n\n{c.CardNote}";
		if (c.RealWorld != null) card += $"\n\nREAL-WORLD CONTEXT\n{c.RealWorld}";

		_index++;
		Hud.ShowDeath(card, ending: true);
		await ToSignal(GetTree().CreateTimer(6.0), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen) return;
		Hud.HideDeath();
		LoadCarriage();
	}

	void EndGame()
	{
		_mode = Mode.Ending;
		_busy = false;
		_player.InputEnabled = false;
		Hud.ShowDeath(
			"TRAIN / FINAL MEMORY\n\n" +
			"Ти бачиш ту саму мить ще раз — іншими очима.\n" +
			"Чоловік у довгому пальті. Чоловік із газетою. Чоловік біля дверей.\n\n" +
			"Він не переслідував тебе.\nВін їхав разом із тобою.\n\n" +
			"Кінець прототипу  ·  [R] — почати спочатку", ending: true);
	}

	// ───────────────────────── memories ─────────────────────────
	async void EnterMemory(string id)
	{
		if (_mode != Mode.Carriage) return;
		int g = _gen;
		_busy = true;
		_player.InputEnabled = false;
		Hud.HideDialogue();
		await ToSignal(Hud.Fade(1f, 0.9f), Tween.SignalName.Finished);
		if (g != _gen) return;

		_savedPos = _player.GlobalPosition;
		_savedYaw = _player.Rotation.Y;
		_worldRoot.Visible = false;
		_worldRoot.ProcessMode = Node.ProcessModeEnum.Disabled;
		_script = null;
		_scriptDone = null;
		PhoneRinging = false;
		Audio.SetRing(false);
		Audio.CutClacks(0.3f);
		ApplyLook(true);

		_memory = new MemoryScene { Id = id, GameRef = this, Position = new Vector3(300, 0, 0) };
		_vp.AddChild(_memory);
		_mode = Mode.Memory;
		_player.Teleport(_memory.GlobalPosition + _memory.StartLocal, _memory.StartYaw);

		await ToSignal(Hud.Fade(0f, 1.2f), Tween.SignalName.Finished);
		_player.InputEnabled = true;
		_busy = false;
		_memory.Intro();
	}

	public async void EndMemory(string id)
	{
		if (_mode != Mode.Memory || _busy) return;
		int g = _gen;
		_busy = true;
		_player.InputEnabled = false;
		ClearUse();
		Audio.SetRing(false);
		await ToSignal(Hud.Fade(1f, 0.9f), Tween.SignalName.Finished);
		if (g != _gen) return;

		_memory?.QueueFree();
		_memory = null;
		_flags.Add("mem_" + id);

		if (id == "arrive") { EndGame(); return; }

		_worldRoot.Visible = true;
		_worldRoot.ProcessMode = Node.ProcessModeEnum.Inherit;
		_mode = Mode.Carriage;
		_memoryQueued = false;
		ApplyLook(false);
		Audio.RestoreClacks();
		_player.Teleport(_savedPos, _savedYaw);

		await ToSignal(Hud.Fade(0f, 1.4f), Tween.SignalName.Finished);
		_player.InputEnabled = true;
		_busy = false;

		if (id == "route") ApplyFx("done");
	}

	public void TeleportPlayer(Vector3 pos, float yaw) => _player.Teleport(pos, yaw);

	public void SetUse(string prompt, Action action) { _pendingPrompt = prompt; _pendingUse = action; }
	public void ClearUse() { _pendingPrompt = null; _pendingUse = null; }

	// ───────────────────────── speech & effects ─────────────────────────
	public bool HasFlag(string f) => _flags.Contains(f);
	public void Notify(string text) => Hud.Notify(text);

	public void Say(string speaker, Line line)
	{
		Hud.ShowDialogue(line.Speaker ?? speaker, line.Text);
		ApplyFx(line.Fx);
	}

	void ApplyFx(string fx)
	{
		if (string.IsNullOrEmpty(fx)) return;

		if (fx == "done")
		{
			if (_flags.Add("story_done"))
			{
				Audio.Click();
				Notify("Попереду щось клацнуло.");
			}
		}
		else if (fx == "unstick")
		{
			_flags.Add("unstuck");
			Audio.Click();
		}
		else if (fx.StartsWith("flag:"))
		{
			_flags.Add(fx.Substring(5));
		}
		else if (fx.StartsWith("memory:"))
		{
			if (_mode != Mode.Carriage || _memoryQueued) return;
			_memoryQueued = true;
			string id = fx.Substring(7);
			GetTree().CreateTimer(2.2).Timeout += () => EnterMemory(id);
		}
	}

	/// <summary>Plays lines one after another (overheard talk, someone helping with a door…).</summary>
	public void PlayScript(Line[] lines, Action done = null)
	{
		_script = lines;
		_scriptI = 0;
		_scriptT = 0.4;
		_scriptDone = done;
	}

	void TickScript(double dt)
	{
		if (_script == null) return;
		_scriptT -= dt;
		if (_scriptT > 0) return;
		if (_scriptI >= _script.Length)
		{
			var d = _scriptDone;
			_script = null;
			_scriptDone = null;
			d?.Invoke();
			return;
		}
		var l = _script[_scriptI++];
		Say("—", l);
		_scriptT = 2.6 + l.Text.Length * 0.05;
	}

	// ───────────────────────── rule & death ─────────────────────────
	public void ViolateRule()
	{
		if (_mode == Mode.Carriage && Current.Rule != null) KillPlayer(Current.Rule.Reason);
	}

	async void StartOffer()
	{
		_offerStarted = true;
		int g = _gen;
		foreach (var n in GetTree().GetNodesInGroup("lamps")) (n as FlickerLight)?.Extinguish();
		await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen || _dead) return;

		OfferTarget = "taras";
		_offerTimer = 22;
		Say("Тарас", new Line("Темно. Давайте я вас проведу до дверей."));

		await ToSignal(GetTree().CreateTimer(4.5), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen || _dead) return;
		foreach (var n in GetTree().GetNodesInGroup("lamps")) (n as FlickerLight)?.Revive();
	}

	public void DeclineOffer(string who)
	{
		OfferTarget = null;
		_flags.Add("offer_declined");
		Say(who, new Line("Як знаєте. Тоді сидіть тихо."));
	}

	async void KillPlayer(string reason)
	{
		if (_dead || _mode != Mode.Carriage) return;
		int g = _gen;
		var rule = Current.Rule;
		_dead = true;
		_player.InputEnabled = false;
		Hud.HideDialogue();
		Hud.CloseBook();
		_script = null;
		_scriptDone = null;
		ClearUse();
		Passenger.Frozen = true;
		Audio.CutClacks();
		Audio.SetRing(false);
		PhoneRinging = false;
		OfferTarget = null;
		foreach (var n in GetTree().GetNodesInGroup("lamps")) (n as FlickerLight)?.Extinguish();

		await ToSignal(GetTree().CreateTimer(1.8), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen) return;

		// One flash: the killer, standing right in front of you.
		var killer = Carriages.Find(Current, rule.KillerId);
		_worldRoot.GetNodeOrNull<Node3D>("p_" + rule.KillerId)?.Hide();
		Vector3 fwd = -_player.GlobalTransform.Basis.Z;
		fwd.Y = 0;
		fwd = fwd.Normalized();
		var figure = new Passenger
		{
			Data = killer with { Talk = null, StandAt = null, Idle = Gesture.None },
			GameRef = this,
			Standing = true,
			Position = _player.GlobalPosition + fwd * 1.1f,
			Rotation = new Vector3(0, _player.Rotation.Y + Mathf.Pi, 0),
		};
		_worldRoot.AddChild(figure);
		figure.Build();

		_env.AmbientLightEnergy = 1.4f;
		await ToSignal(GetTree().CreateTimer(0.22), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen) return;
		_env.AmbientLightEnergy = 0.05f;
		await ToSignal(GetTree().CreateTimer(1.4), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen) return;

		Hud.ShowDeath(
			$"{reason}\n\n{killer.Name} робить із тобою те саме, що зробив із {rule.VictimName}:\n{rule.Method}.\n\n{rule.KillText}\n\n[R] — пройти вагон ще раз");
		_deathShown = true;
	}

	// ───────────────────────── per-frame ─────────────────────────
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("restart") && (_deathShown || _mode == Mode.Ending))
		{
			if (_mode == Mode.Ending) _index = 0;
			LoadCarriage();
			return;
		}

		TickScript(delta);
		if (_dead || _busy || _mode == Mode.Ending)
		{
			Hud.SetPrompt(null);
			return;
		}

		if (_mode == Mode.Carriage) TickCarriage(delta);

		if (Input.IsActionJustPressed("notebook"))
		{
			Hud.ToggleBook(TicketText());
			_player.InputEnabled = !Hud.BookOpen;
		}

		_focus = _player.InputEnabled ? _player.Focus() : null;
		string prompt = _focus?.Prompt;
		if (_focus?.AltPrompt != null) prompt += "\n" + _focus.AltPrompt;
		if (_pendingUse != null) prompt = _pendingPrompt;
		Hud.SetPrompt(prompt);

		if (_player.InputEnabled)
		{
			if (Input.IsActionJustPressed("interact"))
			{
				if (_pendingUse != null)
				{
					var a = _pendingUse;
					ClearUse();
					a();
				}
				else _focus?.Interact(this);
			}
			else if (Input.IsActionJustPressed("alt") && _focus?.AltPrompt != null)
			{
				_focus.Alt(this);
			}
		}
	}

	void TickCarriage(double delta)
	{
		_carriageTime += delta;
		var rule = Current.Rule;

		if (rule?.Kind == RuleKind.PhoneRing)
		{
			bool ring = _carriageTime >= 14 && ((_carriageTime - 14) % 30) < 12;
			if (ring != PhoneRinging)
			{
				PhoneRinging = ring;
				Audio.SetRing(ring);
			}
		}

		if (rule?.Kind == RuleKind.OfferGuide)
		{
			if (!_offerStarted && _carriageTime >= 35) StartOffer();
			if (OfferTarget != null)
			{
				_offerTimer -= delta;
				if (_offerTimer <= 0)
				{
					OfferTarget = null;
					_flags.Add("offer_declined");
				}
			}
		}

		if (Current.Overheard != null && !_overheardStarted)
		{
			var d = new Vector2(PlayerPosition.X - Current.OverheardAt.X, PlayerPosition.Z - Current.OverheardAt.Z);
			if (d.Length() < 3.2f)
			{
				_overheardStarted = true;
				PlayScript(Current.Overheard);
			}
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

	/// <summary>The ticket. Its destination is erased and slowly comes back as the train goes on.</summary>
	string TicketText()
	{
		const string dest = "КІНЦЕВА";
		int reveal = Mathf.Min(_index * 2, dest.Length);
		string shown = dest.Substring(0, reveal) + new string('▒', dest.Length - reveal);
		return
			"КВИТОК\n\n" +
			"  Відправлення:  ЦЕНТРАЛЬНИЙ ВОКЗАЛ · 23:41\n" +
			$"  Призначення:   {shown}\n" +
			$"  Вагон:         {Current.Title}\n\n" +
			"Tab — закрити";
	}
}
