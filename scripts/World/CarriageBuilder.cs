using Godot;

namespace PassengerArchive;

/// <summary>
/// Development-only carriage authoring, packed into an editable scene. The train travels towards -Z;
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

		if (!SceneAssets.Baking) throw new System.InvalidOperationException("CarriageBuilder is an authoring tool, not a runtime world factory.");
		BuildShell(root);
		BuildSeats(root);
		BuildDetails(root, d);
		BuildLamps(root, d);
		BuildDoorsAndSign(root, d);
		BuildProps(root, d, game);
		BuildPeople(root, d, game);

		var dust = new Dust { Name = "LamplightDust", Position = new Vector3(0, 1.3f, 0) };
		root.AddChild(dust);
		dust.Author();
		var scenery = new Scenery { Name = "WindowLandscape", StuckHouse = d.StuckWorld };
		root.AddChild(scenery);
		scenery.Author();
		root.AddChild(HorrorAuthoring.Create(System.Array.IndexOf(Carriages.All,d)));
		return root;
	}

	static void BuildShell(Node3D root)
	{
		float len = HalfLen * 2f + 0.2f;
		Geo.Box(root, new Vector3(0, -0.05f, 0), new Vector3(3.6f, 0.1f, len), Floor, 0, true, 0.45f, Surface.Floor);
		Geo.Box(root, new Vector3(0, 0.003f, 0), new Vector3(0.9f, 0.01f, len - 0.4f), Runner, 0, false, 0.4f, Surface.Fabric);   // runner
		Geo.Box(root, new Vector3(0, Height + 0.05f, 0), new Vector3(2.5f, 0.1f, len), Ceiling, 0, false, 0.12f, Surface.Plaster);
		foreach (int side in new[] { -1, 1 })
		{
			var shoulder = Geo.Box(root, new Vector3(side * 1.51f, 2.47f, 0), new Vector3(0.63f, 0.08f, len), Ceiling, 0, false, 0.12f, Surface.Plaster);
			shoulder.RotationDegrees = new Vector3(0, 0, side * -27f);
			for (int rib = 0; rib < 9; rib++)
			{
				float z = -6.4f + rib * 1.6f;
				Geo.Box(root, new Vector3(0, 2.585f, z), new Vector3(2.47f, 0.025f, 0.035f), Metal, 0, false, 0.1f);
				var trim = Geo.Box(root, new Vector3(side * 1.5f, 2.43f, z), new Vector3(0.59f, 0.025f, 0.035f), Metal, 0, false, 0.1f);
				trim.RotationDegrees = new Vector3(0, 0, side * -27f);
			}
		}
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

			// Separate saved panes and divided wooden frames; scenery remains visible through glass.
			for (int k = 0; k < 8; k++)
			{
				float z = RowZ(k);
				foreach (float y in new[] { 1.01f, 1.97f })
					Geo.Box(root, new Vector3(side * 1.70f, y, z), new Vector3(0.08f, 0.055f, 1.31f), WoodLow, 0, false, 0.12f, Surface.Wood);
				Geo.Box(root, new Vector3(side * 1.70f, 1.74f, z), new Vector3(0.045f, 0.028f, 1.31f), Metal, 0, false, 0.08f);
				root.AddChild(new MeshInstance3D { Name = $"Glass_{side}_{k}", Position = new Vector3(side * 1.76f, 1.49f, z),
					Mesh = new BoxMesh { Size = new Vector3(0.008f, 0.90f, 1.29f) },
					MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.20f, 0.31f, 0.37f, 0.14f), Transparency = BaseMaterial3D.TransparencyEnum.Alpha, Roughness = 0.3f },
					CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
			}
			// Window sills + the odd curtain
			Geo.Box(root, new Vector3(side * (HalfWidth - 0.04f), 0.93f, 0), new Vector3(0.14f, 0.05f, len - 0.3f), WoodLow, 0, false, 0.3f, Surface.Wood);
			for (int k = 0; k < 8; k += 2)
			{
				var curtain = new Swayer { Position = new Vector3(side * (HalfWidth - 0.1f), 1.95f, -5.6f + 1.6f * k), Amp = 0.03f, Speed = 0.9f, Phase = k };
				for (int fold = 0; fold < 6; fold++)
					curtain.AddChild(Geo.MeshBox(new Vector3((fold % 2) * 0.025f, -0.45f, -0.55f + fold * 0.045f), new Vector3(0.045f, 0.87f, 0.05f), new Color(0.28f + (fold % 2) * 0.04f, 0.15f, 0.11f), 0, 0.12f, Surface.Fabric));
				root.AddChild(curtain);
			}

			// Invisible wall so you cannot walk out through the windows.
			Geo.Barrier(root, new Vector3(x, Height / 2f, 0), new Vector3(0.1f, Height, len));

			// Luggage shelf
			for (int rail = 0; rail < 5; rail++)
				Rod(root, new Vector3(side * (1.12f + rail * 0.115f), 2.08f, 0), 0.012f, 12.9f, new Vector3(90, 0, 0));
			for (int rack = 0; rack < 8; rack++)
			{
				float z = RowZ(rack);
				Rod(root, new Vector3(side * 1.36f, 2.08f, z), 0.016f, 0.57f, new Vector3(0, 0, 90));
				Rod(root, new Vector3(side * 1.67f, 2.19f, z), 0.014f, 0.22f, Vector3.Zero);
			}
		}
	}
	static void Rod(Node3D root, Vector3 position, float radius, float length, Vector3 rotation)
	{
		root.AddChild(new MeshInstance3D { Position = position, RotationDegrees = rotation,
			Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = length, RadialSegments = 6, Rings = 1 },
			MaterialOverride = Ps1Materials.Get(Metal, 0, 0.06f, Surface.Metal) });
	}

	static void BuildSeats(Node3D root)
	{
		for (int r = 0; r < 8; r++)
		foreach (int side in new[] { -1, 1 })
		{
			float z = RowZ(r);
			float x = side * 1.09f;
			var bench = SceneAssets.Load<Node3D>("res://scenes/props/Bench.tscn");
			bench.Name = $"Bench_{r}_{(side < 0 ? "Left" : "Right")}";
			bench.Position = new Vector3(x, 0, z);
			root.AddChild(bench);
		}
	}

	/// <summary>Everything that makes it a lived-in carriage: rails, straps, posters, an extinguisher, a scuffed stripe.</summary>
	static void BuildDetails(Node3D root, CarriageData d)
	{
		// A restrained set of plausible passenger belongings, with deliberate placement.
		foreach (var place in new[] { new Vector3(1.35f, 2.20f, -3.9f), new Vector3(-1.35f, 2.20f, 2.4f) })
		{
			var luggage = new Node3D { Name = "Luggage", Position = place }; root.AddChild(luggage);
			luggage.AddChild(new MeshInstance3D { Name = "Case", Mesh = BenchAuthoring.Chamfer(new Vector3(0.40f, 0.22f, 0.63f), 0.045f), MaterialOverride = Ps1Materials.Get(new Color(0.25f, 0.17f, 0.10f), 0, 0.14f, Surface.Fabric) });
			foreach (float z in new[] { -0.21f, 0.21f })
				Geo.Box(luggage, new Vector3(0, 0.115f, z), new Vector3(0.4f, 0.009f, 0.026f), new Color(0.12f, 0.08f, 0.05f), 0, false, 0.05f);
			Geo.Box(luggage, new Vector3(-0.21f, 0, 0), new Vector3(0.022f, 0.035f, 0.15f), Metal, 0, false, 0.04f);
		}
		// ceiling handrails
		foreach (float x in new[] { -0.62f, 0.62f })
			Rod(root, new Vector3(x, 2.28f, 0), 0.018f, 13.2f, new Vector3(90, 0, 0));

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
		int seed = 0;
		foreach (char ch in d.Title) seed = (seed * 31 + ch) & 0x7fffffff;
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
		foreach (int side in new[] { -1, 1 })
			root.AddChild(new OmniLight3D { Name = side < 0 ? "NightWindowLeft" : "NightWindowRight", Position = new Vector3(side * 1.65f, 1.6f, side * 3.2f), LightColor = new Color(0.36f, 0.48f, 0.67f), LightEnergy = 0.38f, OmniRange = 3.2f, OmniAttenuation = 1.4f });
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
					OmniRange = 4.5f,
				OmniAttenuation = 1.2f,
					BaseEnergy = 0.95f,
				ShadowEnabled = k % 2 == 0,
				ShadowBias = 0.035f,
				Fixture = fixture,
				Dead = dead,
				Permanent = dead,
			};
			if (!dead)
			{
				var halo = Geo.Halo(new Color(1f, 0.85f, 0.45f, 0.14f), 0.8f);
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
				Name = "Telephone",
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
			var node = new Passenger { Data = p, GameRef = game, PersonId = p.Id, Name = "p_" + p.Id };
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
