using Godot;

namespace PassengerArchive;

/// <summary>
/// A played memory: you walk through someone's past, you watch, you cannot change anything.
///   route  — Viktor's way home, four evenings in a row; each evening adds one more sign.
///   depart — the hero boards the train (hurrying, missed calls, a man at the door).
///   arrive — the hero gets off at an empty terminus.
/// Cold-blue palette is applied by Game while a memory is active.
/// </summary>
public partial class MemoryScene : Node3D
{
	public string Id;
	public Game GameRef;
	public Vector3 StartLocal;
	public float StartYaw;

	static readonly string[] RouteCaptions =
	{
		"Вівторок. Той самий шлях додому.",
		"Середа. На зупинці стоїть чоловік у кепці.",
		"Четвер. Той самий чоловік. І ця машина — вона вже стояла вчора.",
		"П'ятниця. У кишені дзвонить телефон.",
	};

	int _loop;
	bool _busy, _done, _phoneStarted, _fired1, _fired2;
	double _phoneT;
	Node3D _extras;

	public override void _Ready()
	{
		ResetExtras();
		switch (Id)
		{
			case "route": BuildRoute(); break;
			case "depart": BuildStation(false); break;
			case "arrive": BuildStation(true); break;
		}
	}

	public void Intro()
	{
		switch (Id)
		{
			case "route": GameRef.Hud.Caption(RouteCaptions[0], 2.5f); break;
			case "depart": GameRef.Hud.Caption("Центральний вокзал. Ти запізнюєшся.", 2.5f); break;
			case "arrive": GameRef.Hud.Caption("Кінцева. Нікого.", 2.5f); break;
		}
	}

	// ───────────────────────── per-frame ─────────────────────────
	public override void _Process(double delta)
	{
		if (_busy || _done || GameRef == null) return;
		Vector3 p = GameRef.PlayerPosition - GlobalPosition;
		switch (Id)
		{
			case "route": TickRoute(p, delta); break;
			case "depart": TickDepart(p); break;
			case "arrive": TickArrive(p); break;
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

	void TickDepart(Vector3 p)
	{
		if (!_fired1 && p.Z < 10f)
		{
			_fired1 = true;
			GameRef.Hud.Notify("Телефон вібрує в кишені. Пропущені: Мама (3). Нема часу.", 6f);
		}
		if (!_fired2 && p.Z < -1f)
		{
			_fired2 = true;
			GameRef.PlayScript(new[]
			{
				new Line("Встигнете. Двері ще відчинені.", Speaker: "Чоловік у довгому пальті"),
				new Line(Carriages.HeroPhrase, Speaker: "Чоловік у довгому пальті"),
			});
		}
	}

	void TickArrive(Vector3 p)
	{
		if (!_fired1 && p.Z < -3f)
		{
			_fired1 = true;
			Spawn(Carriages.PopOriginal, Gesture.Sleeve, new Vector3(0.5f, 0f, 6f), 0f);
		}
		if (!_fired2 && p.Z < -9f)
		{
			_fired2 = true;
			GameRef.Hud.Notify("Позаду — кроки.", 3.5f);
		}
		if (p.Z < -15f) FinishAt("Ти не обертаєшся.", 3.4);
	}

	// ───────────────────────── flow ─────────────────────────
	async void NextLoop()
	{
		_busy = true;
		await ToSignal(GameRef.Hud.Fade(1f, 0.5f), Tween.SignalName.Finished);
		_loop++;
		SpawnLoop();
		GameRef.TeleportPlayer(GlobalPosition + StartLocal, StartYaw);
		GameRef.Hud.Caption(RouteCaptions[_loop], 3f);
		await ToSignal(GameRef.Hud.Fade(0f, 0.7f), Tween.SignalName.Finished);
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
		GetTree().CreateTimer(afterSeconds).Timeout += () => GameRef.EndMemory(Id);
	}

	void ResetExtras()
	{
		_extras?.QueueFree();
		_extras = new Node3D();
		AddChild(_extras);
	}

	void SpawnLoop()
	{
		ResetExtras();
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

	void Spawn(Outfit look, Gesture g, Vector3 pos, float yaw)
	{
		var data = new PersonData(Id: "mem", Name: "", Row: 0, Side: 0, Slot: 0, Look: look, Idle: g);
		var n = new Passenger { Data = data, GameRef = GameRef, Standing = true, Position = pos, Rotation = new Vector3(0, yaw, 0) };
		_extras.AddChild(n);
		n.Build();
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
		AddChild(new OmniLight3D { Position = pos, LightColor = c, OmniRange = range, LightEnergy = energy });
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
