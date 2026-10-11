using Godot;

namespace PassengerArchive;

/// <summary>A strap/curtain that sways with the train.</summary>
public partial class Swayer : Node3D
{
	public float Amp = 0.05f, Speed = 1.2f, Phase;
	public override void _Process(double delta)
	{
		Rotation = new Vector3(Mathf.Sin(Time.GetTicksMsec() * 0.001f * Speed + Phase) * Amp * 0.4f, 0, Mathf.Sin(Time.GetTicksMsec() * 0.001f * Speed * 0.7f + Phase) * Amp);
	}
}

/// <summary>Dust hanging in the lamplight: a few tiny motes drifting slowly.</summary>
public partial class Dust : Node3D
{
	public Vector3 Extents = new(1.6f, 1.2f, 6.8f);
	public int Count = 46;
	readonly System.Collections.Generic.List<(MeshInstance3D m, Vector3 v)> _motes = new();

	public override void _Ready()
	{
		var rng = new RandomNumberGenerator { Seed = 8 };
		for (int i = 0; i < Count; i++)
		{
			var m = Geo.MeshBox(new Vector3(rng.RandfRange(-Extents.X, Extents.X), rng.RandfRange(-Extents.Y, Extents.Y), rng.RandfRange(-Extents.Z, Extents.Z)),
				new Vector3(0.014f, 0.014f, 0.014f), new Color(0.9f, 0.85f, 0.65f), 1.2f, 0f);
			AddChild(m);
			_motes.Add((m, new Vector3(rng.RandfRange(-0.04f, 0.04f), rng.RandfRange(-0.025f, 0.02f), rng.RandfRange(-0.05f, 0.05f))));
		}
	}

	public override void _Process(double delta)
	{
		float dt = (float)delta;
		foreach (var (m, v) in _motes)
		{
			var p = m.Position + v * dt;
			if (p.X > Extents.X) p.X = -Extents.X; else if (p.X < -Extents.X) p.X = Extents.X;
			if (p.Y > Extents.Y) p.Y = -Extents.Y; else if (p.Y < -Extents.Y) p.Y = Extents.Y;
			if (p.Z > Extents.Z) p.Z = -Extents.Z; else if (p.Z < -Extents.Z) p.Z = Extents.Z;
			m.Position = p;
		}
	}
}

/// <summary>
/// Builds one carriage procedurally from boxes. The train travels towards -Z;
/// the player enters at the +Z end and the next carriage is through the -Z door.
/// </summary>
public static class CarriageBuilder
{
	public const float HalfLen = 7f;
	public const float HalfWidth = 1.75f;
	public const float Height = 2.6f;
	public const float SeatY = 0.57f;

	// Palette: dirty yellow / grey-green / brown / cold blue (see docs/DESIGN.md)
	static readonly Color Floor = new(0.22f, 0.16f, 0.11f);
	static readonly Color Runner = new(0.17f, 0.10f, 0.08f);
	static readonly Color WoodLow = new(0.30f, 0.19f, 0.12f);
	static readonly Color WallUp = new(0.52f, 0.50f, 0.36f);
	static readonly Color Ceiling = new(0.58f, 0.56f, 0.45f);
	static readonly Color Metal = new(0.32f, 0.33f, 0.30f);
	static readonly Color SeatGreen = new(0.22f, 0.34f, 0.26f);
	static readonly Color SeatFrame = new(0.22f, 0.15f, 0.10f);

	static readonly string[] Posters =
	{
		"НЕ ВІДВОЛІКАЙТЕ\nМАШИНІСТА",
		"СТОП-КРАН\nТІЛЬКИ ПРИ НЕБЕЗПЕЦІ",
		"ГУЧНО НЕ\nРОЗМОВЛЯТИ",
		"ПАСАЖИР!\nПЕРЕВІРЯЙ СВОЇ РЕЧІ",
		"ВАГОН НЕ\nПРОВІТРЮЄТЬСЯ",
		"ХТО ЇДЕ ПОРУЧ\nЗ ТОБОЮ?",
	};

	public static float RowZ(int row) => -5.6f + 1.6f * row;

	public static Vector3 SeatPos(int row, int side, int slot) =>
		new(side * (slot == 0 ? 0.78f : 1.40f), SeatY, RowZ(row));

	public static Node3D Build(CarriageData d, Game game)
	{
		var root = new Node3D { Name = "Carriage" };

		BuildShell(root);
		BuildSeats(root);
		BuildDetails(root, d);
		BuildLamps(root, d);
		BuildDoorsAndSign(root, d);
		BuildProps(root, d, game);
		BuildPeople(root, d, game);

		root.AddChild(new Dust { Position = new Vector3(0, 1.3f, 0) });
		root.AddChild(new Scenery { StuckHouse = d.StuckWorld });
		return root;
	}

	static void BuildShell(Node3D root)
	{
		float len = HalfLen * 2f + 0.2f;
		Geo.Box(root, new Vector3(0, -0.05f, 0), new Vector3(3.6f, 0.1f, len), Floor, 0, true, 0.45f, Surface.Floor);
		Geo.Box(root, new Vector3(0, 0.003f, 0), new Vector3(0.9f, 0.01f, len - 0.4f), Runner, 0, false, 0.4f, Surface.Fabric);   // runner
		Geo.Box(root, new Vector3(0, Height + 0.05f, 0), new Vector3(3.6f, 0.1f, len), Ceiling, 0, false, 0.4f, Surface.Plaster);
		Geo.Box(root, new Vector3(0, Height / 2f, -HalfLen - 0.05f), new Vector3(3.6f, Height, 0.1f), WoodLow, 0, true, 0.35f, Surface.Wood);
		Geo.Box(root, new Vector3(0, Height / 2f, HalfLen + 0.05f), new Vector3(3.6f, Height, 0.1f), WoodLow, 0, true, 0.35f, Surface.Wood);

		foreach (int side in new[] { -1, 1 })
		{
			float x = side * (HalfWidth + 0.05f);
			Geo.Box(root, new Vector3(x, 0.45f, 0), new Vector3(0.1f, 0.9f, len), WoodLow, 0, false, 0.35f, Surface.Wood);
			Geo.Box(root, new Vector3(x, 2.3f, 0), new Vector3(0.1f, 0.6f, len), WallUp, 0, false, 0.4f, Surface.Plaster);

			// Window pillars (the gaps between them are the windows)
			for (int k = 0; k <= 8; k++)
				Geo.Box(root, new Vector3(side * HalfWidth, 1.45f, -6.4f + 1.6f * k), new Vector3(0.12f, 1.1f, 0.3f), Metal, 0, false, 0.35f, Surface.Metal);

			// Window sills + the odd curtain
			Geo.Box(root, new Vector3(side * (HalfWidth - 0.04f), 0.93f, 0), new Vector3(0.14f, 0.05f, len - 0.3f), WoodLow, 0, false, 0.3f, Surface.Wood);
			for (int k = 0; k < 8; k += 2)
			{
				var curtain = new Swayer { Position = new Vector3(side * (HalfWidth - 0.1f), 1.95f, -5.6f + 1.6f * k), Amp = 0.03f, Speed = 0.9f, Phase = k };
				curtain.AddChild(Geo.MeshBox(new Vector3(0, -0.5f, 0), new Vector3(0.05f, 0.95f, 0.5f), new Color(0.32f, 0.12f, 0.1f), 0, 0.3f, Surface.Fabric));
				root.AddChild(curtain);
			}

			// Invisible wall so you cannot walk out through the windows.
			Geo.Barrier(root, new Vector3(x, Height / 2f, 0), new Vector3(0.1f, Height, len));

			// Luggage shelf
			Geo.Box(root, new Vector3(side * 1.3f, 1.85f, 0), new Vector3(0.5f, 0.05f, 13f), Metal, 0, false, 0.35f, Surface.Metal);
		}
	}

	static void BuildSeats(Node3D root)
	{
		for (int r = 0; r < 8; r++)
		foreach (int side in new[] { -1, 1 })
		{
			float z = RowZ(r);
			float x = side * 1.09f;
			Geo.Box(root, new Vector3(x, 0.225f, z), new Vector3(1.24f, 0.45f, 0.9f), SeatFrame, 0, true, 0.35f, Surface.Wood);
			Geo.Box(root, new Vector3(x, 0.51f, z), new Vector3(1.24f, 0.12f, 0.9f), SeatGreen, 0, false, 0.35f, Surface.Fabric);
			Geo.Box(root, new Vector3(x, SeatY + 0.4f, z + 0.38f), new Vector3(1.24f, 0.8f, 0.14f), SeatGreen, 0, true, 0.35f, Surface.Fabric);
			Geo.Box(root, new Vector3(x, SeatY + 0.83f, z + 0.38f), new Vector3(1.24f, 0.06f, 0.16f), SeatFrame, 0, false, 0.3f, Surface.Wood);   // headrest rail
		}
	}

	/// <summary>Everything that makes it a lived-in carriage: rails, straps, posters, an extinguisher, a scuffed stripe.</summary>
	static void BuildDetails(Node3D root, CarriageData d)
	{
		// ceiling handrails
		foreach (float x in new[] { -0.62f, 0.62f })
			Geo.Box(root, new Vector3(x, 2.28f, 0), new Vector3(0.035f, 0.035f, 13.2f), Metal, 0, false, 0.3f, Surface.Metal);

		// hanging straps
		for (int i = 0; i < 6; i++)
		{
			float z = -5.8f + i * 2.3f;
			float x = (i % 2 == 0) ? -0.62f : 0.62f;
			var strap = new Swayer { Position = new Vector3(x, 2.27f, z), Amp = 0.07f, Speed = 1.15f, Phase = i * 1.7f };
			strap.AddChild(Geo.MeshBox(new Vector3(0, -0.18f, 0), new Vector3(0.025f, 0.36f, 0.025f), new Color(0.12f, 0.1f, 0.08f), 0, 0.2f, Surface.Fabric));
			strap.AddChild(Geo.MeshBox(new Vector3(0, -0.38f, 0), new Vector3(0.12f, 0.025f, 0.04f), new Color(0.25f, 0.25f, 0.22f), 0, 0.2f, Surface.Metal));
			root.AddChild(strap);
		}

		// posters on the upper wall (different set of three per carriage)
		int seed = d.Title.GetHashCode() & 0x7fffffff;
		for (int i = 0; i < 3; i++)
		{
			string text = Posters[(seed + i * 2) % Posters.Length];
			int side = (i % 2 == 0) ? 1 : -1;
			root.AddChild(new Label3D
			{
				Text = text,
				Position = new Vector3(side * (HalfWidth - 0.03f), 2.3f, -4.2f + i * 3.6f),
				RotationDegrees = new Vector3(0, side == 1 ? -90 : 90, 0),
				FontSize = 22,
				PixelSize = 0.0036f,
				Modulate = new Color(0.9f, 0.82f, 0.55f, 0.85f),
				OutlineSize = 4,
				OutlineModulate = new Color(0.1f, 0.07f, 0.04f),
				Shaded = false,
			});
		}

		// emergency handle (red) and an extinguisher at the back door
		Geo.Box(root, new Vector3(-1.62f, 1.7f, 5.4f), new Vector3(0.05f, 0.28f, 0.1f), new Color(0.6f, 0.06f, 0.05f), 0.35f, false, 0.1f);
		Geo.Box(root, new Vector3(1.55f, 0.95f, 6.8f), new Vector3(0.16f, 0.46f, 0.16f), new Color(0.55f, 0.05f, 0.04f), 0.2f, false, 0.2f, Surface.Metal);
		Geo.Box(root, new Vector3(1.55f, 1.22f, 6.8f), new Vector3(0.06f, 0.08f, 0.08f), Metal, 0, false, 0.2f, Surface.Metal);
	}

	static void BuildLamps(Node3D root, CarriageData d)
	{
		for (int k = 0; k < 5; k++)
		{
			float z = -5.6f + 2.8f * k;
			bool dead = d.DeadLamps != null && System.Array.IndexOf(d.DeadLamps, k) >= 0;

			var fixture = Geo.Box(root, new Vector3(0, 2.54f, z), new Vector3(0.4f, 0.06f, 1.0f),
				dead ? new Color(0.15f, 0.15f, 0.12f) : new Color(1f, 0.9f, 0.55f), dead ? 0f : 1f, false, 0f);

			var light = new FlickerLight
			{
				Position = new Vector3(0, 2.35f, z),
				LightColor = new Color(1f, 0.86f, 0.5f),
				OmniRange = 6.5f,
				OmniAttenuation = 1.2f,
				BaseEnergy = 1.15f,
				Fixture = fixture,
				Dead = dead,
				Permanent = dead,
			};
			if (!dead)
			{
				var halo = Geo.Halo(new Color(1f, 0.85f, 0.45f, 0.5f), 1.7f);
				halo.Position = new Vector3(0, 0.12f, 0);
				light.Glow = halo;
				light.AddChild(halo);
			}
			root.AddChild(light);
		}
	}

	static void BuildDoorsAndSign(Node3D root, CarriageData d)
	{
		var front = new DoorInteractable { Name = "door_front", Forward = true, Position = new Vector3(0, 1.0f, -HalfLen + 0.03f) };
		front.AddChild(Geo.MeshBox(Vector3.Zero, new Vector3(0.95f, 2.0f, 0.06f), new Color(0.16f, 0.12f, 0.09f), 0, 0.3f, Surface.Wood));
		front.AddChild(Geo.MeshBox(new Vector3(0, 0.45f, 0.04f), new Vector3(0.55f, 0.6f, 0.02f), new Color(0.02f, 0.03f, 0.05f)));
		front.AddChild(Geo.MeshBox(new Vector3(0.34f, -0.1f, 0.05f), new Vector3(0.04f, 0.14f, 0.04f), Metal, 0, 0.2f, Surface.Metal));   // handle
		front.SetBox(new Vector3(0.95f, 2.0f, 0.3f), Vector3.Zero);
		front.AddLamp(false);
		root.AddChild(front);

		var back = new DoorInteractable { Forward = false, Position = new Vector3(0, 1.0f, HalfLen - 0.03f) };
		back.AddChild(Geo.MeshBox(Vector3.Zero, new Vector3(0.95f, 2.0f, 0.06f), new Color(0.16f, 0.12f, 0.09f), 0, 0.3f, Surface.Wood));
		back.SetBox(new Vector3(0.95f, 2.0f, 0.3f), Vector3.Zero);
		root.AddChild(back);

		// Only the carriage number. The game never prints the rule.
		root.AddChild(new Label3D
		{
			Text = d.Title,
			Position = new Vector3(0, 2.28f, -HalfLen + 0.05f),
			FontSize = 40,
			PixelSize = 0.0042f,
			Modulate = new Color(0.95f, 0.78f, 0.35f),
			OutlineSize = 6,
			Shaded = false,
		});
	}

	static void BuildProps(Node3D root, CarriageData d, Game game)
	{
		if (d.Rule != null && d.Rule.Kind == RuleKind.PhoneRing)
		{
			var phone = new ActionProp
			{
				Position = new Vector3(HalfWidth - 0.1f, 1.4f, -6.3f),
				PromptFn = () => game.PhoneRinging ? "[E] Відповісти" : "[E] Слухавка",
				OnUse = g =>
				{
					if (g.PhoneRinging) g.ViolateRule();
					else g.Notify("Гудків немає.");
				},
			};
			phone.AddChild(Geo.MeshBox(Vector3.Zero, new Vector3(0.12f, 0.3f, 0.2f), new Color(0.05f, 0.05f, 0.05f)));
			phone.AddChild(Geo.MeshBox(new Vector3(-0.08f, 0.05f, 0), new Vector3(0.05f, 0.22f, 0.07f), new Color(0.1f, 0.1f, 0.1f)));
			phone.SetBox(new Vector3(0.5f, 0.6f, 0.6f), Vector3.Zero);
			root.AddChild(phone);
		}
	}

	static void BuildPeople(Node3D root, CarriageData d, Game game)
	{
		foreach (var p in d.People)
		{
			var node = new Passenger { Data = p, GameRef = game, Name = "p_" + p.Id };
			if (p.StandAt.HasValue)
			{
				node.Standing = true;
				node.Position = p.StandAt.Value;
				node.Rotation = new Vector3(0, p.StandYaw, 0);
			}
			else
			{
				node.Position = SeatPos(p.Row, p.Side, p.Slot);
			}
			root.AddChild(node);
			node.Build();
		}
	}
}
