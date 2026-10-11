using System;
using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>
/// Root script: builds the low-res 3D view + HUD and runs the flow of the train:
/// carriage → (played memory) → carriage … doors lock behind you, the rule is never announced,
/// and breaking it makes the carriage's killer do to you what he did to the victim.
/// The conversation system lives in GameTalk.cs.
/// </summary>
public partial class Game : Node
{
	enum Mode { Carriage, Memory, Ending }

	public Hud Hud { get; private set; }
	public TrainAudio Audio { get; private set; }
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
	readonly HashSet<string> _flags = new();      // live until the carriage ends / you die
	readonly HashSet<string> _kept = new();       // "k:…" flags: live for the whole run
	readonly HashSet<string> _known = new();      // people whose real name you have learned (this carriage)
	readonly List<string> _remembered = new();    // shown on the ticket
	double _carriageTime;
	float _stress;
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
		_player.OnStep = () => Audio.Step();

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
		Bind("confirm", Key.Space, Key.Enter);
		Bind("notebook", Key.Tab);
		Bind("restart", Key.R);
		Bind("opt1", Key.Key1, Key.Kp1);
		Bind("opt2", Key.Key2, Key.Kp2);
		Bind("opt3", Key.Key3, Key.Kp3);
		Bind("opt4", Key.Key4, Key.Kp4);
		Bind("opt5", Key.Key5, Key.Kp5);
		Bind("opt6", Key.Key6, Key.Kp6);
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
		_known.Clear();
		ResetTalk();
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
		PhoneRinging = false;
		Audio.SetRing(false);
		Audio.RestoreClacks();
		Audio.SetOutdoor(false);
		Audio.SetPad(false);
		Audio.Heartbeat(false);
		Audio.AmbientOn = true;
		SetStress(0f);
		_mode = Mode.Carriage;
		ApplyLook(false);
		Hud.ClearGore();

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
		if (first) Hud.Notify("WASD — рух · E — діяти · Tab — квиток · Esc — курсор", 6f);
	}

	public void TryDoor()
	{
		if (_mode != Mode.Carriage || _busy || _inTalk) return;
		if (_index >= Carriages.All.Length - 1 || !HasFlag("story_done")) { Notify("Двері зачинені."); return; }
		if (Current.DoorStuck && !HasFlag("unstuck"))
		{
			// The man with the newspaper offers to help (his node "door").
			var pop = _worldRoot.GetNodeOrNull<Passenger>("p_pop");
			if (pop?.Data.Dialog != null) StartTalk(pop.Data.Dialog, pop.Data.Id, () => pop.DisplayName, pop.HeadWorld, force: "door");
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

	/// <summary>The end of the run: the memory of the terminus has played out and the hero is dead.</summary>
	void EndGame()
	{
		_mode = Mode.Ending;
		_busy = false;
		_player.InputEnabled = false;
		Audio.PlayMotif(-26f);                                                                  // almost inaudible, as designed
		Hud.ShowDeath(
			"TRAIN / FINAL MEMORY\n\n" +
			"Ти бачиш ту саму мить ще раз — іншими очима.\n\n" +
			"Продавця з «Зенітом» не було. Був Максим. Він написав тобі першим.\n" +
			"Він сів у той самий потяг і їхав поруч — щоб ти приїхав сам.\n\n" +
			"Вагон 1 — у довгому пальті, біля заднього виходу.\n" +
			"Вагон 2 — із газетою, яку він ніколи не перегортав. Він відчинив тобі двері.\n" +
			"Вагон 3 — біля виходу, ніби теж чекає зупинки.\n\n" +
			"Марта сказала вголос те, що ти не захотів почути: ніч, готівка, сам, «тільки сьогодні».\n" +
			"Винен той, хто це підлаштував. Але почути її було можливо.\n\n" +
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
		Audio.Heartbeat(false);
		SetStress(0f);
		Audio.SetOutdoor(true, id == "ride");
		if (id != "arrive") { Audio.SetPad(true, -26f); GetTree().CreateTimer(3.0).Timeout += () => { if (_mode == Mode.Memory) Audio.PlayMotif(-15f); }; }
		else Audio.SetPad(true, -20f);
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

		if (id == "arrive") { Audio.SetPad(false); Audio.StopMotif(); EndGame(); return; }
		Audio.SetOutdoor(false);
		Audio.SetPad(false);

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

		// A memory that tells the whole story finishes the carriage (the door lamp turns green).
		if (id == "route" || id == "ride" || id == "walk") ApplyFx("done");
	}

	public void SetStress(float v)
	{
		_stress = v;
		_post?.SetShaderParameter("stress", v);
	}

	public void TeleportPlayer(Vector3 pos, float yaw) => _player.Teleport(pos, yaw);
	public void LockPlayer() => _player.InputEnabled = false;
	public void CollapsePlayer() => _player.Collapse();

	public void SetUse(string prompt, Action action) { _pendingPrompt = prompt; _pendingUse = action; }
	public void ClearUse() { _pendingPrompt = null; _pendingUse = null; }

	// ───────────────────────── speech & effects ─────────────────────────
	public bool HasFlag(string f) => _flags.Contains(f) || _kept.Contains(f);
	public void Notify(string text) => Hud.Notify(text);

	/// <summary>Remember something for the whole run (it may change what people say much later).</summary>
	public void Keep(string flag) => _kept.Add("k:" + flag);

	/// <summary>Flags separated by ',' — "k:x" lives for the run, "x" for the carriage.</summary>
	void ApplySet(string set)
	{
		if (string.IsNullOrEmpty(set)) return;
		foreach (var raw in set.Split(','))
		{
			var f = raw.Trim();
			if (f.Length == 0) continue;
			if (f.StartsWith("k:")) _kept.Add(f); else _flags.Add(f);
		}
	}

	/// <summary>All of <paramref name="need"/> set, none of <paramref name="unless"/> set (both comma separated).</summary>
	public bool Check(string need, string unless)
	{
		if (!string.IsNullOrEmpty(need))
			foreach (var f in need.Split(',')) if (!HasFlag(f.Trim())) return false;
		if (!string.IsNullOrEmpty(unless))
			foreach (var f in unless.Split(',')) if (HasFlag(f.Trim())) return false;
		return true;
	}

	/// <summary>"Незнайомець …" until a conversation reveals the real name.</summary>
	public static string DisplayOf(PersonData p, Game g)
		=> p.Name != null && g != null && g.IsKnown(p.Id) ? p.Name : (p.Alias ?? p.Name ?? "");

	public bool IsKnown(string id) => _known.Contains(id);

	void Know(string id)
	{
		if (string.IsNullOrEmpty(id) || !_known.Add(id)) return;
		var p = _mode == Mode.Carriage ? Carriages.Find(Current, id) : null;
		if (p?.Name == null) return;
		string entry = $"{p.Name} — {Current.Title}";
		if (!_remembered.Contains(entry)) _remembered.Add(entry);
	}

	string ResolveSpeaker(string speaker)
	{
		if (speaker == null || !speaker.StartsWith("@")) return speaker;
		var p = Carriages.Find(Current, speaker.Substring(1));
		return p == null ? "—" : DisplayOf(p, this);
	}

	/// <summary>One-off line (scripts, overheard talk). Conversations use ShowNodeLine instead.</summary>
	public void Say(string speaker, Line line)
	{
		Hud.ShowDialogue(ResolveSpeaker(line.Speaker) ?? speaker, line.Text);
		ApplyFx(line.Fx);
	}

	/// <summary>Effects, separated by ';': done · unstick · know[:id] · flag:x · memory:id · memend · walkgo · callend · kill · violate</summary>
	void ApplyFx(string fx)
	{
		if (string.IsNullOrEmpty(fx)) return;
		if (fx.Contains(';'))
		{
			foreach (var part in fx.Split(';')) ApplyFx(part.Trim());
			return;
		}

		if (fx == "done")
		{
			if (_flags.Add("story_done"))
			{
				Audio.Click();
				Audio.PlayMotif(-21f);                                                          // the truth lands: the motif, very quietly
				_worldRoot?.GetNodeOrNull<DoorInteractable>("door_front")?.SetUnlocked(true);   // the lamp over the door turns green
			}
		}
		else if (fx == "unstick") { _flags.Add("unstuck"); Audio.Click(); }
		else if (fx == "know") Know(_ownerId);
		else if (fx.StartsWith("know:")) Know(fx.Substring(5));
		else if (fx.StartsWith("flag:")) ApplySet(fx.Substring(5));
		else if (fx.StartsWith("memory:"))
		{
			if (_mode != Mode.Carriage || _memoryQueued) return;
			_memoryQueued = true;
			_busy = true;
			_player.InputEnabled = false;
			string id = fx.Substring(7);
			GetTree().CreateTimer(0.8).Timeout += () => EnterMemory(id);
		}
		else if (fx == "memend") _memory?.Conclude();
		else if (fx == "walkgo") _memory?.StartWalk();
		else if (fx == "callend") _memory?.CallEnded();
		else if (fx == "kill") _memory?.Kill();
		else if (fx == "violate") ViolateRule();
	}

	/// <summary>Plays lines one after another (overheard talk…). Pauses while the player is in a conversation.</summary>
	public void PlayScript(Line[] lines, Action done = null)
	{
		_script = lines;
		_scriptI = 0;
		_scriptT = 0.4;
		_scriptDone = done;
	}

	void TickScript(double dt)
	{
		if (_script == null || _inTalk) return;
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

	/// <summary>Lights die; a moment later Taras starts a conversation: "Темно. Давайте я вас проведу."</summary>
	async void StartOffer()
	{
		_offerStarted = true;
		int g = _gen;
		foreach (var n in GetTree().GetNodesInGroup("lamps")) (n as FlickerLight)?.Extinguish();
		await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen || _dead) return;

		// Never cut into another conversation, a memory or a transition.
		while (_inTalk || _busy || _mode != Mode.Carriage)
		{
			await ToSignal(GetTree().CreateTimer(0.5), SceneTreeTimer.SignalName.Timeout);
			if (g != _gen || _dead) return;
		}

		var taras = _worldRoot.GetNodeOrNull<Passenger>("p_taras");
		if (taras == null) return;
		StartTalk(taras.Data.Dialog, taras.Data.Id, () => taras.DisplayName, taras.HeadWorld, force: "offer", onEnd: () =>
		{
			if (!HasFlag("offer_declined")) return;
			foreach (var n in GetTree().GetNodesInGroup("lamps")) (n as FlickerLight)?.Revive();
		});
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
		ResetTalk();
		_script = null;
		_scriptDone = null;
		ClearUse();
		Passenger.Frozen = true;
		Audio.CutClacks();
		Audio.SetRing(false);
		Audio.AmbientOn = false;
		Audio.Heartbeat(true);
		SetStress(1f);
		PhoneRinging = false;
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
			Data = killer with { Dialog = null, StandAt = null, Idle = Gesture.None },
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
		await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen) return;

		// The blow. Blood on the lens, blood on the floor, the world tips over.
		Audio.Heartbeat(false);
		Audio.Hit();
		Hud.BloodFlash();
		Hud.Splatter(16);
		_player.Collapse();
		var floor = new Vector3(_player.GlobalPosition.X, 0f, _player.GlobalPosition.Z);
		Gore.Pool(_worldRoot, floor + fwd * 0.3f, 1.2f, 6f);
		Gore.Spatter(_worldRoot, floor, 14, 1.5f);
		_env.AmbientLightColor = new Color(0.55f, 0.1f, 0.1f);
		_env.AmbientLightEnergy = 0.35f;
		await ToSignal(GetTree().CreateTimer(2.4), SceneTreeTimer.SignalName.Timeout);
		if (g != _gen) return;

		string victim = IsKnown(rule.VictimId) ? rule.VictimName : "жертвою цього вагона";
		Hud.ShowDeath(
			$"{reason}\n\n{DisplayOf(killer, this)} робить із тобою те саме, що зробив із {victim}:\n{rule.Method}.\n\n{rule.KillText}\n\n[R] — пройти вагон ще раз");
		_deathShown = true;
	}

	// ───────────────────────── per-frame ─────────────────────────
	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("restart") && (_deathShown || _mode == Mode.Ending))
		{
			if (_mode == Mode.Ending)
			{
				_index = 0;
				_kept.Clear();
				_remembered.Clear();
			}
			LoadCarriage();
			return;
		}

		TickScript(delta);
		if (_dead || _busy || _mode == Mode.Ending)
		{
			Hud.SetPrompt(null);
			return;
		}

		if (_inTalk)
		{
			Hud.SetPrompt(null);
			TickTalk();
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
		if (_pendingUse != null) prompt = _pendingPrompt;
		Hud.SetPrompt(prompt);

		if (_player.InputEnabled && Input.IsActionJustPressed("interact"))
		{
			if (_pendingUse != null)
			{
				var a = _pendingUse;
				ClearUse();
				a();
			}
			else _focus?.Interact(this);
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

		// Something in the carriage is looking at you: the picture bleeds, the pulse rises.
		float target = 0f;
		if (rule != null && !_dead)
		{
			var k = _worldRoot.GetNodeOrNull<Node3D>("p_" + rule.KillerId);
			if (k != null)
			{
				float dist = k.GlobalPosition.DistanceTo(_player.GlobalPosition);
				target = Mathf.Clamp((3.4f - dist) / 2.4f, 0f, 1f) * 0.55f;
			}
		}
		_stress = Mathf.MoveToward(_stress, target, (float)delta * 0.7f);
		_post.SetShaderParameter("stress", _stress);
		Audio.Heartbeat(_stress > 0.25f);

		if (rule?.Kind == RuleKind.OfferGuide && !_offerStarted && _carriageTime >= 35 && !_inTalk && !_busy && _script == null)
			StartOffer();

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
		else if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } && _inTalk)
			_clickPending = true;
		else if (e is InputEventMouseButton { Pressed: true } && Input.MouseMode != Input.MouseModeEnum.Captured)
			Input.MouseMode = Input.MouseModeEnum.Captured;
		else if (e.IsActionPressed("ui_cancel"))
			Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
				? Input.MouseModeEnum.Visible
				: Input.MouseModeEnum.Captured;
	}

	/// <summary>The ticket. Its destination is erased and slowly comes back as the train goes on. Below: who you have got to know.</summary>
	string TicketText()
	{
		const string dest = "КІНЦЕВА";
		int reveal = Mathf.Min(_index * 2, dest.Length);
		string shown = dest.Substring(0, reveal) + new string('▒', dest.Length - reveal);
		string text =
			"КВИТОК\n\n" +
			"  Відправлення:  ЦЕНТРАЛЬНИЙ ВОКЗАЛ · 23:41\n" +
			$"  Призначення:   {shown}\n" +
			$"  Вагон:         {Current.Title}";
		if (_remembered.Count > 0)
		{
			text += "\n\nТи запам'ятав:";
			foreach (var r in _remembered) text += "\n  · " + r;
		}
		return text;
	}
}
