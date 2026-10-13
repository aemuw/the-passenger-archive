using Godot;

namespace PassengerArchive;

/// <summary>
/// A played memory: you walk through someone's past, you watch, you cannot change anything.
///   route  — Viktor's way home, four evenings in a row; each evening adds one more sign.
///   ride   — Halyna's imagined last night of her husband: a rainy road, a phone call, a man without a face.
///   walk   — Solomiya's evening: say "no" twice; the third time the word will not come out. Lights die behind you.
///   depart — the hero boards the train (hurrying, missed calls from Marta, a man at the door).
///   arrive — the terminus. Marta calls. A man behind you says «До кінця — то до кінця.» The hero dies.
/// Cold-blue palette is applied by Game while a memory is active.
/// </summary>
public partial class MemoryScene : Node3D
{
	[Export] public string Id { get; set; }
	public Game GameRef;
	[Export] public Vector3 StartLocal { get; set; }
	[Export] public float StartYaw { get; set; }

	static readonly string[] RouteCaptions =
	{
		"Вівторок. Той самий шлях додому.",
		"Середа. На зупинці стоїть чоловік у кепці.",
		"Четвер. Той самий чоловік. І ця машина — вона вже стояла вчора.",
		"П'ятниця. У кишені дзвонить телефон.",
	};

	int _loop;
	bool _busy, _done, _phoneStarted, _fired1, _fired2;
	double _phoneT, _vmT, _ringT;
	Node3D _extras;

	// walk
	Passenger _taras;
	bool _walking, _archAsked;
	readonly System.Collections.Generic.List<(float z, OmniLight3D light, MeshInstance3D fixture)> _lamps = new();

	// arrive
	bool _callStarted, _callDone, _approach;
	Passenger _pop;

	public override void _Ready()
	{
		_extras = GetNode<Node3D>("Extras");
		foreach (var child in _extras.GetChildren())
		{
			if (child is not Passenger passenger) continue;
			passenger.GameRef = GameRef;
			passenger.Data = Id switch
			{
				"ride" => new PersonData("hitch", null, "Незнайомець у капюшоні, мокрий до нитки", 0, 0, 0, Carriages.HitchLook, Carriages.HitchTalk),
				"walk" => new PersonData("taras", null, "Незнайомець у світлому светрі", 0, 0, 0, Carriages.TarasLook),
				_ => new PersonData("pop", null, "Незнайомець у довгому пальті", 0, 0, 0, Carriages.PopOriginal, Idle: Gesture.Sleeve),
			};
			passenger.Build();
			if (Id == "walk") _taras = passenger;
		}
		foreach (var child in GetChildren())
			if (child is OmniLight3D light && light.HasMeta("memory_fixture"))
				_lamps.Add((light.Position.Z, light, GetNode<MeshInstance3D>(light.GetMeta("memory_fixture").AsString())));
			else if (child is OmniLight3D finalLight && finalLight.HasMeta("final_lamp"))
				_lamps.Add((finalLight.Position.Z, finalLight, null));
		if (GetNodeOrNull<ActionProp>("BoardTrain") is { } board)
		{
			board.PromptFn = () => "[E] Сісти у вагон";
			board.OnUse = game => FinishAt("Двері зачиняються.", 2.8);
		}
	}

	public void Author()
	{
		ResetExtras();
		switch (Id)
		{
			case "route": BuildRoute(); break;
			case "ride": BuildRide(); break;
			case "walk": BuildWalk(); break;
			case "depart": BuildStation(false); break;
			case "arrive": BuildStation(true); break;
		}
	}

	public Node3D AuthorEvening(int evening)
	{
		_loop = evening;
		SpawnLoop();
		RemoveChild(_extras);
		var result = _extras;
		_extras = null;
		return result;
	}

	public void Intro()
	{
		switch (Id)
		{
			case "route": GameRef.Hud.Caption(RouteCaptions[0], 2.5f); break;
			case "ride": GameRef.Hud.Caption("Дощ. Дорога порожня.\nТи повертаєшся додому.", 3f); break;
			case "walk": GameRef.Hud.Caption("П'ятниця. Автобус не прийшов.", 3f); break;
			case "depart": GameRef.Hud.Caption("Центральний вокзал. 23:41.\nПодарунок для Марти — лише сьогодні вночі.", 3.5f); break;
			case "arrive": GameRef.Hud.Caption("Кінцева. Нікого.", 2.5f); break;
		}
	}

	// ───────────────────────── per-frame ─────────────────────────
	public override void _Process(double delta)
	{
		if (_busy || _done || GameRef == null || !GameRef.MemoryIsActive(this)) return;
		Vector3 p = GameRef.PlayerPosition - GlobalPosition;
		switch (Id)
		{
			case "route": TickRoute(p, delta); break;
			case "ride": TickRide(p); break;
			case "walk": TickWalk(p, delta); break;
			case "depart": TickDepart(p, delta); break;
			case "arrive": TickArrive(p, delta); break;
		}
	}

	void TickRoute(Vector3 p, double dt)
	{
		if (_loop == 3)
		{
			if (!_phoneStarted && p.Z < -6f)
			{
				_phoneStarted = true;
				GameRef.Audio.SetRing(true);
				GameRef.SetUse("[E] Відповісти", () => FinishRoute(true));
				GameRef.Hud.Notify("Телефон дзвонить у кишені.", 4f);
			}
			if (_phoneStarted)
			{
				_phoneT += dt;
				if (_phoneT > 14.0) FinishRoute(false);
			}
		}
		else if (p.Z < -16f)
		{
			NextLoop();
		}
	}

	void TickDepart(Vector3 p, double dt)
	{
		if (!_fired1 && p.Z < 10f)
		{
			_fired1 = true;
			GameRef.Hud.Notify("Телефон вібрує. Нові повідомлення. Пропущені: Марта (3).", 5f);
			GameRef.SetUse("[E] Телефон", () => GameRef.StartTalk(Carriages.HeroPhone, "phone", () => "Телефон"));
		}
		if (_fired1 && !_fired2) { _vmT += dt; if (_vmT > 14.0) GameRef.ClearUse(); }
		if (!_fired2 && p.Z < -1f)
		{
			_fired2 = true;
			GameRef.ClearUse();
			GameRef.PlayScript(new[]
			{
				new Line("Встигнете. Двері ще відчинені.", Speaker: "Незнайомець у довгому пальті"),
				new Line(Carriages.HeroPhrase, Speaker: "Незнайомець у довгому пальті"),
			});
		}
	}

	void TickArrive(Vector3 p, double dt)
	{
		// 1. Marta calls. The phone rings in your pocket — the same sound that ended Viktor.
		if (!_callStarted && p.Z < 8f)
		{
			_callStarted = true;
			GameRef.Audio.SetRing(true);
			GameRef.SetUse("[E] Відповісти", () =>
			{
				GameRef.Audio.SetRing(false);
				GameRef.ClearUse();
				GameRef.StartTalk(Carriages.MartaCall, "marta", () => "Марта");
			});
		}
		if (_callStarted && !_callDone && !GameRef.InTalk)
		{
			_ringT += dt;
			if (_ringT > 12.0)
			{
				GameRef.Audio.SetRing(false);
				GameRef.ClearUse();
				GameRef.Hud.Notify("Дзвінок обривається.", 3f);
				_callDone = true;
			}
		}
		if (_callStarted && !_callDone && p.Z < -14f) { GameRef.Audio.SetRing(false); GameRef.ClearUse(); _callDone = true; }

		// 2. Footsteps behind you; he walks up and talks.
		if (!_fired1 && _callDone && p.Z < -4f)
		{
			_fired1 = true;
			GameRef.Hud.Caption("Позаду — кроки.", 2.5f);
			_pop = Spawn(Carriages.PopOriginal, Gesture.Sleeve, new Vector3(p.X, 0f, p.Z + 9f), 0f);
			_approach = true;
		}
		if (_approach && _pop != null)
		{
			var pos = _pop.Position;
			pos.X = Mathf.Lerp(pos.X, p.X, 0.02f);
			pos.Z -= 2.1f * (float)dt;
			_pop.Position = pos;
			if (pos.Z - p.Z < 1.9f)
			{
				_approach = false;
				GameRef.StartTalk(Carriages.KillerTalk, "pop", () => "Незнайомець у довгому пальті", _pop.HeadWorld);
			}
		}
	}

	// ───────────────────────── hooks called by conversations ─────────────────────────
	public void CallEnded() => _callDone = true;

	public void StartWalk() => _walking = true;

	/// <summary>The talk with the hitch-hiker / at the arch is over — the memory ends the way it always ended.</summary>
	public void Conclude()
	{
		if (_done) return;
		GameRef.LockPlayer();
		if (Id == "ride") FinishAt("Він сідає ззаду.\nУ дзеркалі — лише капюшон.", 4.2);
		else if (Id == "walk")
		{
			GameRef.Audio.Hit();
			GameRef.Hud.BloodFlash(0.55f);
			FinishAt("Вона сказала «ні» біля арки.\nТоді вже було пізно казати «так».", 5.0);
		}
	}

	/// <summary>The hero's death. Called by the last line of the man's conversation.</summary>
	public async void Kill()
	{
		if (_done) return;
		_done = true;
		GameRef.LockPlayer();
		GameRef.Audio.SetRing(false);
		_pop?.BeginAttack("нож");
		foreach (var l in _lamps) { l.light.LightEnergy = 0.15f; }
		await ToSignal(GetTree().CreateTimer(0.8), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;

		GameRef.Audio.Hit();
		GameRef.Hud.BloodFlash();
		GameRef.Hud.Splatter(10);
		await ToSignal(GetTree().CreateTimer(0.9), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;

		GameRef.Audio.Hit();
		GameRef.Hud.BloodFlash(0.9f);
		GameRef.Hud.Splatter(16);
		GameRef.CollapsePlayer();
		var local = GameRef.PlayerPosition - GlobalPosition;
		var floor = new Vector3(local.X, 0f, local.Z);
		Gore.Pool(this, floor + new Vector3(0, 0, -0.2f), 1.4f, 7f);
		Gore.Spatter(this, floor, 18, 1.8f);
		await ToSignal(GetTree().CreateTimer(2.2), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;

		GameRef.Hud.Caption("Лезо входить під ребра. Холодно.\nПотім — гаряче.", 3.2f);
		await ToSignal(GetTree().CreateTimer(4.2), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;
		GameRef.Hud.Caption(Carriages.HeroPhrase, 2.2f);
		await ToSignal(GetTree().CreateTimer(3.4), SceneTreeTimer.SignalName.Timeout);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;
		GameRef.EndMemory("arrive");
	}

	// ───────────────────────── ride & walk ─────────────────────────
	void TickRide(Vector3 p)
	{
		if (!_fired1 && p.Z < 14f)
		{
			_fired1 = true;
			GameRef.PlayScript(new[]
			{
				new Line("Ти де? Дощ такий. Я хвилююсь.", Speaker: "Галя"),
				new Line("Їду, Галю. Тут на узбіччі чоловік, мокрий до нитки. Підвезу — не залишати ж.", Speaker: "Ти"),
				new Line("Андрію… Може, не треба? Уже пізно.", Speaker: "Галя"),
				new Line("Люди ж, Галю.", Speaker: "Ти"),
				new Line("Ну… дивись сам. Подзвони, як доїдеш.", Speaker: "Галя"),
			});
		}
	}

	void TickWalk(Vector3 p, double dt)
	{
		// He waits at the bus stop and speaks first.
		if (!_fired1 && p.Z < 20f && _taras != null)
		{
			_fired1 = true;
			GameRef.StartTalk(Carriages.WalkTalk, "taras", () => "Незнайомець у світлому светрі", _taras.HeadWorld);
		}
		if (!_walking || _taras == null) return;

		// He walks ahead, always a few steps in front of you. Every lamp you pass goes out behind you.
		var pos = _taras.Position;
		if (pos.Z > -22f && pos.Z > p.Z - 3.2f) pos.Z -= 1.5f * (float)dt;
		pos.X = Mathf.Lerp(pos.X, 0.6f, 0.02f);
		_taras.Position = pos;
		if (!_archAsked) _taras.Rotation = Vector3.Zero;

		foreach (var l in _lamps)
			if (p.Z < l.z - 1.0f && l.light.LightEnergy > 0f)
			{
				l.light.LightEnergy = 0f;
				l.fixture.MaterialOverride = Ps1Materials.Get(new Color(0.12f, 0.12f, 0.12f));
			}

		if (!_archAsked && pos.Z <= -22f && p.Z < pos.Z + 4.5f)
		{
			_archAsked = true;
			_taras.Rotation = new Vector3(0, Mathf.Pi, 0);
			GameRef.StartTalk(Carriages.ArchTalk, "taras", () => "Незнайомець у світлому светрі", _taras.HeadWorld);
		}
	}

	void BuildRide()
	{
		StartLocal = new Vector3(0f, 0.05f, 26f);
		StartYaw = 0f;
		var rng = new RandomNumberGenerator { Seed = 41 };

		Geo.Box(this, new Vector3(0, -0.1f, 0), new Vector3(30f, 0.2f, 80f), new Color(0.06f, 0.08f, 0.07f), 0, true);
		Geo.Box(this, new Vector3(0, 0.01f, 0), new Vector3(4.2f, 0.02f, 80f), new Color(0.04f, 0.045f, 0.05f));            // asphalt
		for (int k = 0; k < 14; k++)
			Geo.Box(this, new Vector3(0, 0.025f, 30f - k * 5f), new Vector3(0.12f, 0.01f, 2.2f), new Color(0.5f, 0.48f, 0.36f)); // dashes
		Geo.Barrier(this, new Vector3(0, 2f, -32f), new Vector3(30f, 4f, 0.4f));
		Geo.Barrier(this, new Vector3(0, 2f, 34f), new Vector3(30f, 4f, 0.4f));
		Geo.Barrier(this, new Vector3(-9f, 2f, 0), new Vector3(0.4f, 4f, 80f));
		Geo.Barrier(this, new Vector3(9f, 2f, 0), new Vector3(0.4f, 4f, 80f));

		for (int i = 0; i < 26; i++)
		{
			float x = (i % 2 == 0 ? -1 : 1) * rng.RandfRange(4f, 8f);
			float z = 32f - i * 2.6f;
			float h = rng.RandfRange(3f, 5.5f);
			Geo.Box(this, new Vector3(x, h / 2f, z), new Vector3(0.3f, h, 0.3f), new Color(0.07f, 0.06f, 0.05f), 0, true);
			Geo.Box(this, new Vector3(x, h, z), new Vector3(1.6f, 1.4f, 1.6f), new Color(0.06f, 0.1f, 0.08f), 0, true);
		}

		// His car, headlights on, right behind you.
		var car = new Node3D { Position = new Vector3(-0.9f, 0f, 29f) };
		Geo.Box(car, new Vector3(0, 0.55f, 0), new Vector3(1.8f, 0.9f, 4.2f), new Color(0.2f, 0.12f, 0.1f), 0, true);
		Geo.Box(car, new Vector3(0, 1.2f, 0.4f), new Vector3(1.6f, 0.6f, 2.2f), new Color(0.14f, 0.1f, 0.09f));
		Geo.Box(car, new Vector3(-0.6f, 0.6f, -2.12f), new Vector3(0.3f, 0.2f, 0.05f), new Color(1f, 0.95f, 0.7f), 2f);
		Geo.Box(car, new Vector3(0.6f, 0.6f, -2.12f), new Vector3(0.3f, 0.2f, 0.05f), new Color(1f, 0.95f, 0.7f), 2f);
		car.AddChild(new OmniLight3D { Position = new Vector3(0, 0.8f, -4f), OmniRange = 9f, LightEnergy = 1.2f, LightColor = new Color(1f, 0.9f, 0.6f) });
		AddChild(car);

		// One sodium lamp where he stands.
		Geo.Box(this, new Vector3(3.2f, 2.2f, -6f), new Vector3(0.12f, 4.4f, 0.12f), new Color(0.1f, 0.1f, 0.12f));
		Geo.Box(this, new Vector3(2.7f, 4.4f, -6f), new Vector3(0.9f, 0.1f, 0.3f), new Color(1f, 0.7f, 0.3f), 1.4f, false, 0f);
		AddChild(new OmniLight3D { Position = new Vector3(2.4f, 4.0f, -6f), OmniRange = 9f, LightEnergy = 1.4f, LightColor = new Color(1f, 0.7f, 0.35f) });

		// The man by the road. Nobody saw his face — not even Halyna's imagination.
		Spawn(Carriages.HitchLook, Gesture.None, new Vector3(2.4f, 0f, -6f), Mathf.Pi * 0.5f,
			Carriages.HitchTalk, "hitch", "Незнайомець у капюшоні, мокрий до нитки");
	}

	void BuildWalk()
	{
		StartLocal = new Vector3(0f, 0.05f, 24f);
		StartYaw = 0f;
		var rng = new RandomNumberGenerator { Seed = 77 };

		Geo.Box(this, new Vector3(0, -0.1f, 0), new Vector3(14f, 0.2f, 62f), new Color(0.07f, 0.09f, 0.13f), 0, true);
		Geo.Box(this, new Vector3(0, 0.01f, 0), new Vector3(2.6f, 0.02f, 62f), new Color(0.04f, 0.05f, 0.07f));
		float[] heights = { 6f, 8f, 7f, 5f, 4f, 3f };
		foreach (int side in new[] { -1, 1 })
		for (int k = 0; k < 6; k++)
		{
			float cz = 24f - 8f * k;
			float h = heights[k];
			Geo.Box(this, new Vector3(side * 6f, h / 2f, cz), new Vector3(6f, h, 7.6f), new Color(0.11f, 0.12f, 0.17f), 0, true);
			if (k < 4) for (int j = 0; j < 2; j++)
				Geo.Box(this, new Vector3(side * 2.97f, rng.RandfRange(2f, h - 1f), cz + rng.RandfRange(-3f, 3f)),
					new Vector3(0.04f, 1f, 0.8f), new Color(1f, 0.82f, 0.45f), 0.8f, false, 0f);
		}
		Geo.Barrier(this, new Vector3(0, 2f, 28f), new Vector3(14f, 4f, 0.4f));
		Geo.Barrier(this, new Vector3(0, 2f, -30f), new Vector3(14f, 4f, 0.4f));

		// Lamps along the way; they go out one by one behind you.
		float[] lampZ = { 14f, 6f, -2f, -10f, -18f };
		for (int i = 0; i < lampZ.Length; i++)
		{
			float x = (i % 2 == 0) ? -2.7f : 2.7f;
			Geo.Box(this, new Vector3(x, 1.8f, lampZ[i]), new Vector3(0.12f, 3.6f, 0.12f), new Color(0.1f, 0.1f, 0.12f));
			var fix = Geo.Box(this, new Vector3(x, 3.65f, lampZ[i]), new Vector3(0.5f, 0.12f, 0.3f), new Color(0.72f, 0.84f, 1.0f), 1.2f, false, 0f);
			var light = new OmniLight3D { Position = new Vector3(x * 0.8f, 3.4f, lampZ[i]), LightColor = new Color(0.72f, 0.84f, 1.0f), OmniRange = 8f, LightEnergy = 1.3f };
			AddChild(light);
			_lamps.Add((lampZ[i], light, fix));
			fix.Name = "MemoryFixture" + i;
			light.SetMeta("memory_fixture", fix.Name.ToString());
		}

		// Bus stop (the bus never comes).
		Geo.Box(this, new Vector3(2.3f, 2.3f, 22f), new Vector3(1.6f, 0.1f, 2.6f), new Color(0.12f, 0.12f, 0.14f));
		Geo.Box(this, new Vector3(2.85f, 1.15f, 22f), new Vector3(0.1f, 2.3f, 2.6f), new Color(0.14f, 0.16f, 0.2f));
		Geo.Box(this, new Vector3(2.5f, 0.45f, 22f), new Vector3(0.5f, 0.1f, 2.0f), new Color(0.2f, 0.14f, 0.1f));

		// The arch to the wasteland behind the station.
		var stone = new Color(0.09f, 0.1f, 0.12f);
		Geo.Box(this, new Vector3(-2.2f, 2.2f, -24f), new Vector3(1.4f, 4.4f, 1.6f), stone, 0, true);
		Geo.Box(this, new Vector3(2.2f, 2.2f, -24f), new Vector3(1.4f, 4.4f, 1.6f), stone, 0, true);
		Geo.Box(this, new Vector3(0, 4.5f, -24f), new Vector3(5.8f, 1.2f, 1.6f), stone, 0, true);

		// Light sweater, friendly, waiting by the stop.
		_taras = Spawn(Carriages.TarasLook, Gesture.None, new Vector3(1.2f, 0f, 17f), Mathf.Pi);
	}

	// ───────────────────────── flow ─────────────────────────
	async void NextLoop()
	{
		_busy = true;
		await ToSignal(GameRef.Hud.Fade(1f, 0.5f), Tween.SignalName.Finished);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;
		_loop++;
		SpawnLoop();
		GameRef.TeleportPlayer(GlobalPosition + StartLocal, StartYaw);
		GameRef.Hud.Caption(RouteCaptions[_loop], 3f);
		await ToSignal(GameRef.Hud.Fade(0f, 0.7f), Tween.SignalName.Finished);
		if (!IsInsideTree() || IsQueuedForDeletion() || !GameRef.MemoryIsActive(this)) return;
		_busy = false;
	}

	void FinishRoute(bool answered)
	{
		if (_done) return;
		GameRef.Audio.SetRing(false);
		GameRef.ClearUse();
		FinishAt(answered ? "— Алло?\n\nТиша. Хтось дихає. І позаду — кроки." : "Дзвінок обривається.\n\nПозаду — кроки.", 4.4);
	}

	void FinishAt(string caption, double afterSeconds)
	{
		if (_done) return;
		_done = true;
		GameRef.Hud.Caption(caption, 3f);
		GetTree().CreateTimer(afterSeconds).Timeout += () => { if (IsInsideTree() && !IsQueuedForDeletion() && GameRef.MemoryIsActive(this)) GameRef.EndMemory(Id); };
	}

	void ResetExtras()
	{
		if (GodotObject.IsInstanceValid(_extras))
		{
			if (_extras.GetParent() == this) RemoveChild(_extras);
			if (SceneAssets.Baking) _extras.Free(); else _extras.QueueFree();
		}
		_extras = new Node3D { Name = "Extras" };
		AddChild(_extras);
	}

	void SpawnLoop()
	{
		ResetExtras();
		if (!SceneAssets.Baking)
		{
			var props = SceneAssets.Load<Node3D>($"res://scenes/memories/route_evening{_loop}.tscn");
			_extras.AddChild(props);
			foreach (var child in props.GetChildren())
				if (child is Passenger passenger)
				{
					passenger.Data = new PersonData("kepka", null, "Незнайомець у кепці", 0, 0, 0, Carriages.KepkaLook, Idle: Gesture.Tap);
					passenger.GameRef = GameRef;
					passenger.Build();
				}
			return;
		}
		if (_loop == 1)
		{
			Spawn(Carriages.KepkaLook, Gesture.Tap, new Vector3(2.0f, 0f, 4.0f), Mathf.Pi * 0.5f);
		}
		else if (_loop >= 2)
		{
			Spawn(Carriages.KepkaLook, Gesture.Tap, new Vector3(-2.2f, 0f, -6.5f), Mathf.Pi);
			BuildCar(new Vector3(0f, 0f, -1.5f));
		}
	}

	Passenger Spawn(Outfit look, Gesture g, Vector3 pos, float yaw, Convo dialog = null, string id = "mem", string alias = null)
	{
		var data = new PersonData(Id: id, Name: null, Alias: alias, Row: 0, Side: 0, Slot: 0, Look: look, Dialog: dialog, Idle: g);
		var n = new Passenger { Data = data, GameRef = GameRef, Standing = true, Position = pos, Rotation = new Vector3(0, yaw, 0) };
		_extras.AddChild(n);
		n.Build();
		return n;
	}

	void BuildCar(Vector3 at)
	{
		var car = new Node3D { Position = at };
		Geo.Box(car, new Vector3(0, 0.55f, 0), new Vector3(1.8f, 0.9f, 4.2f), new Color(0.12f, 0.14f, 0.2f), 0, true);
		Geo.Box(car, new Vector3(0, 1.2f, -0.4f), new Vector3(1.6f, 0.6f, 2.2f), new Color(0.1f, 0.12f, 0.18f));
		Geo.Box(car, new Vector3(-0.6f, 0.6f, 2.12f), new Vector3(0.3f, 0.2f, 0.05f), new Color(1f, 0.95f, 0.7f), 2f);
		Geo.Box(car, new Vector3(0.6f, 0.6f, 2.12f), new Vector3(0.3f, 0.2f, 0.05f), new Color(1f, 0.95f, 0.7f), 2f);
		car.AddChild(new OmniLight3D { Position = new Vector3(0, 0.8f, 2.8f), OmniRange = 6f, LightEnergy = 1.0f, LightColor = new Color(1f, 0.9f, 0.6f) });
		_extras.AddChild(car);
	}

	// ───────────────────────── places ─────────────────────────
	static readonly Color Ground = new(0.07f, 0.09f, 0.13f);
	static readonly Color Building = new(0.11f, 0.12f, 0.17f);
	static readonly Color ColdLight = new(0.72f, 0.84f, 1.0f);

	void Lamp(Vector3 pos, Color c, float range, float energy)
	{
		var l = new OmniLight3D { Position = pos, LightColor = c, OmniRange = range, LightEnergy = energy };
		AddChild(l);
		if (Id == "arrive") _lamps.Add((pos.Z, l, null));
		if (Id == "arrive") l.SetMeta("final_lamp", true);
	}

	void BuildRoute()
	{
		StartLocal = new Vector3(0f, 0.05f, 16f);
		StartYaw = 0f;
		var rng = new RandomNumberGenerator { Seed = 99 };

		Geo.Box(this, new Vector3(0, -0.1f, 0), new Vector3(14f, 0.2f, 44f), Ground, 0, true);
		Geo.Box(this, new Vector3(0, 0.01f, 0), new Vector3(2.2f, 0.02f, 44f), new Color(0.04f, 0.05f, 0.07f));      // road

		float[] heights = { 6f, 8f, 7f, 9f, 6f };
		foreach (int side in new[] { -1, 1 })
		for (int k = 0; k < 5; k++)
		{
			float cz = -16f + 8f * k;
			float h = heights[k];
			Geo.Box(this, new Vector3(side * 6f, h / 2f, cz), new Vector3(6f, h, 7.6f), Building, 0, true);
			for (int j = 0; j < 3; j++)
			{
				Geo.Box(this, new Vector3(side * 2.97f, rng.RandfRange(2f, h - 1f), cz + rng.RandfRange(-3f, 3f)),
					new Vector3(0.04f, 1.0f, 0.8f), new Color(1f, 0.82f, 0.45f), 0.8f, false, 0f);
			}
		}

		Geo.Box(this, new Vector3(0, 3f, -18.2f), new Vector3(14f, 6f, 0.4f), Building, 0, true);
		Geo.Barrier(this, new Vector3(0, 2f, 19f), new Vector3(14f, 4f, 0.4f));

		// Entrance at the end of the route
		Geo.Box(this, new Vector3(0, 1.1f, -17.9f), new Vector3(1.4f, 2.2f, 0.2f), new Color(0.2f, 0.15f, 0.1f));
		Geo.Box(this, new Vector3(0, 2.55f, -17.8f), new Vector3(0.5f, 0.1f, 0.2f), new Color(1f, 0.8f, 0.45f), 1.2f, false, 0f);
		Lamp(new Vector3(0, 2.3f, -17f), new Color(1f, 0.8f, 0.5f), 5f, 0.9f);

		// Street lamps
		float[] lampZ = { 13f, 5f, -3f, -11f };
		for (int i = 0; i < lampZ.Length; i++)
		{
			float x = (i % 2 == 0) ? -2.7f : 2.7f;
			Geo.Box(this, new Vector3(x, 1.8f, lampZ[i]), new Vector3(0.12f, 3.6f, 0.12f), new Color(0.1f, 0.1f, 0.12f));
			Geo.Box(this, new Vector3(x, 3.65f, lampZ[i]), new Vector3(0.5f, 0.12f, 0.3f), ColdLight, 1.2f, false, 0f);
			Lamp(new Vector3(x * 0.8f, 3.4f, lampZ[i]), ColdLight, 8f, 1.3f);
		}

		// Bus stop
		Geo.Box(this, new Vector3(2.3f, 2.3f, 4f), new Vector3(1.6f, 0.1f, 2.6f), new Color(0.12f, 0.12f, 0.14f));
		Geo.Box(this, new Vector3(2.85f, 1.15f, 4f), new Vector3(0.1f, 2.3f, 2.6f), new Color(0.14f, 0.16f, 0.2f));
		Geo.Box(this, new Vector3(2.5f, 0.45f, 4f), new Vector3(0.5f, 0.1f, 2.0f), new Color(0.2f, 0.14f, 0.1f));
	}

	void BuildStation(bool arrive)
	{
		StartLocal = new Vector3(0.5f, 0.05f, 16f);
		StartYaw = 0f;

		Geo.Box(this, new Vector3(0, -0.1f, 0), new Vector3(8f, 0.2f, 40f), new Color(0.16f, 0.16f, 0.18f), 0, true);
		Geo.Box(this, new Vector3(0, 4.6f, 0), new Vector3(8f, 0.2f, 40f), new Color(0.1f, 0.1f, 0.12f));
		Geo.Barrier(this, new Vector3(4.2f, 2f, 0), new Vector3(0.4f, 4f, 40f));
		Geo.Barrier(this, new Vector3(0, 2f, 19.5f), new Vector3(8f, 4f, 0.4f));
		Geo.Barrier(this, new Vector3(-4.2f, 2f, 0), new Vector3(0.4f, 4f, 40f));

		float[] lz = { 13f, 4f, -5f, -14f };
		foreach (float z in lz)
		{
			Geo.Box(this, new Vector3(0, 4.4f, z), new Vector3(0.7f, 0.1f, 0.3f), ColdLight, 1.2f, false, 0f);
			Lamp(new Vector3(0, 3.9f, z), ColdLight, 9f, 1.1f);
		}

		Geo.Box(this, new Vector3(0, 2f, -20.2f), new Vector3(8f, 4f, 0.4f), new Color(0.2f, 0.2f, 0.22f), 0, true);

		if (!arrive)
		{
			var green = new Color(0.2f, 0.27f, 0.23f);
			Geo.Box(this, new Vector3(-3.7f, 1.6f, 4.5f), new Vector3(0.5f, 3.2f, 25f), green, 0, true);
			Geo.Box(this, new Vector3(-3.7f, 1.6f, -13.5f), new Vector3(0.5f, 3.2f, 7f), green, 0, true);
			Geo.Box(this, new Vector3(-3.7f, 3.0f, -9f), new Vector3(0.5f, 0.4f, 2f), green, 0, true);
			Geo.Box(this, new Vector3(-4.1f, 1.1f, -9f), new Vector3(0.15f, 2.2f, 2f), new Color(0.9f, 0.75f, 0.4f), 0.8f, true, 0f);

			var board = new ActionProp
			{
				Name = "BoardTrain",
				Position = new Vector3(-3.6f, 1.1f, -9f),
				PromptFn = () => "[E] Сісти у вагон",
				OnUse = g => FinishAt("Двері зачиняються.", 2.8),
			};
			board.SetBox(new Vector3(1.4f, 2.2f, 2.2f), Vector3.Zero);
			AddChild(board);

			AddChild(new Label3D { Text = "ЦЕНТРАЛЬНИЙ ВОКЗАЛ\n23:41", Position = new Vector3(3.6f, 3.0f, 10f), RotationDegrees = new Vector3(0, -90, 0), FontSize = 40, PixelSize = 0.006f, Modulate = new Color(0.9f, 0.9f, 0.7f), OutlineSize = 6, Shaded = false });
			AddChild(new Label3D { Text = "Наступний потяг: 05:10", Position = new Vector3(3.6f, 2.3f, 4f), RotationDegrees = new Vector3(0, -90, 0), FontSize = 32, PixelSize = 0.005f, Modulate = new Color(0.8f, 0.5f, 0.4f), OutlineSize = 6, Shaded = false });

			Spawn(Carriages.PopOriginal, Gesture.Sleeve, new Vector3(-2.7f, 0f, -6f), Mathf.Pi);
		}
		else
		{
			AddChild(new Label3D { Text = "КІНЦЕВА", Position = new Vector3(3.6f, 3.0f, 8f), RotationDegrees = new Vector3(0, -90, 0), FontSize = 56, PixelSize = 0.008f, Modulate = new Color(0.9f, 0.9f, 0.7f), OutlineSize = 6, Shaded = false });
			Geo.Box(this, new Vector3(0, 1.1f, -19.9f), new Vector3(1.4f, 2.2f, 0.2f), new Color(1f, 0.85f, 0.5f), 0.8f, false, 0f);
			Lamp(new Vector3(0, 2f, -18.5f), new Color(1f, 0.8f, 0.5f), 6f, 1.0f);
		}
	}
}
