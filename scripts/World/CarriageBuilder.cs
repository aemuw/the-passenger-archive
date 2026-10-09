using Godot;

namespace PassengerArchive;

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
	static readonly Color Floor = new(0.20f, 0.14f, 0.10f);
	static readonly Color WoodLow = new(0.30f, 0.19f, 0.12f);
	static readonly Color WallUp = new(0.52f, 0.50f, 0.36f);
	static readonly Color Ceiling = new(0.58f, 0.56f, 0.45f);
	static readonly Color Metal = new(0.32f, 0.33f, 0.30f);
	static readonly Color SeatGreen = new(0.22f, 0.34f, 0.26f);
	static readonly Color SeatFrame = new(0.22f, 0.15f, 0.10f);

	public static float RowZ(int row) => -5.6f + 1.6f * row;

	public static Vector3 SeatPos(int row, int side, int slot) =>
		new(side * (slot == 0 ? 0.78f : 1.40f), SeatY, RowZ(row));

	public static Node3D Build(CarriageData d, Game game)
	{
		var root = new Node3D { Name = "Carriage" };

		BuildShell(root);
		BuildSeats(root);
		BuildLamps(root, d);
		BuildDoorsAndSign(root, d);
		BuildProp(root, d);
		BuildPeople(root, d, game);

		var scenery = new Scenery { StuckHouse = d.StuckWorld };
		root.AddChild(scenery);
		return root;
	}

	// ───────────────────────── shell ─────────────────────────
	static void BuildShell(Node3D root)
	{
		float len = HalfLen * 2f + 0.2f;
		Box(root, new Vector3(0, -0.05f, 0), new Vector3(3.6f, 0.1f, len), Floor, 0, true);
		Box(root, new Vector3(0, Height + 0.05f, 0), new Vector3(3.6f, 0.1f, len), Ceiling);
		Box(root, new Vector3(0, Height / 2f, -HalfLen - 0.05f), new Vector3(3.6f, Height, 0.1f), WoodLow, 0, true);
		Box(root, new Vector3(0, Height / 2f, HalfLen + 0.05f), new Vector3(3.6f, Height, 0.1f), WoodLow, 0, true);

		foreach (int side in new[] { -1, 1 })
		{
			float x = side * (HalfWidth + 0.05f);
			Box(root, new Vector3(x, 0.45f, 0), new Vector3(0.1f, 0.9f, len), WoodLow);
			Box(root, new Vector3(x, 2.3f, 0), new Vector3(0.1f, 0.6f, len), WallUp);

			// Window pillars (the gaps between them are the windows)
			for (int k = 0; k <= 8; k++)
			{
				float z = -6.4f + 1.6f * k;
				Box(root, new Vector3(side * HalfWidth, 1.45f, z), new Vector3(0.12f, 1.1f, 0.3f), Metal);
			}

			// Invisible wall so you cannot walk out through the windows.
			var body = new StaticBody3D { CollisionLayer = GameConfig.LayerWorld, CollisionMask = 0 };
			body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.1f, Height, len) } });
			body.Position = new Vector3(x, Height / 2f, 0);
			root.AddChild(body);

			// Luggage shelf
			Box(root, new Vector3(side * 1.3f, 1.85f, 0), new Vector3(0.5f, 0.05f, 13f), Metal);
		}
	}

	static void BuildSeats(Node3D root)
	{
		for (int r = 0; r < 8; r++)
		foreach (int side in new[] { -1, 1 })
		{
			float z = RowZ(r);
			float x = side * 1.09f;
			Box(root, new Vector3(x, 0.225f, z), new Vector3(1.24f, 0.45f, 0.9f), SeatFrame, 0, true);
			Box(root, new Vector3(x, 0.51f, z), new Vector3(1.24f, 0.12f, 0.9f), SeatGreen);
			Box(root, new Vector3(x, SeatY + 0.4f, z + 0.38f), new Vector3(1.24f, 0.8f, 0.14f), SeatGreen, 0, true);
		}
	}

	static void BuildLamps(Node3D root, CarriageData d)
	{
		for (int k = 0; k < 5; k++)
		{
			float z = -5.6f + 2.8f * k;
			bool dead = System.Array.IndexOf(d.DeadLamps, k) >= 0;

			var fixture = Box(root, new Vector3(0, 2.54f, z), new Vector3(0.4f, 0.06f, 1.0f),
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
			};
			root.AddChild(light);
		}
	}

	static void BuildDoorsAndSign(Node3D root, CarriageData d)
	{
		var front = new DoorInteractable { Forward = true, Position = new Vector3(0, 1.0f, -HalfLen + 0.03f) };
		front.AddChild(MeshBox(Vector3.Zero, new Vector3(0.95f, 2.0f, 0.06f), new Color(0.16f, 0.12f, 0.09f)));
		front.AddChild(MeshBox(new Vector3(0, 0.45f, 0.04f), new Vector3(0.55f, 0.6f, 0.02f), new Color(0.02f, 0.03f, 0.05f)));
		front.SetBox(new Vector3(0.95f, 2.0f, 0.3f), Vector3.Zero);
		root.AddChild(front);

		var back = new DoorInteractable { Forward = false, Position = new Vector3(0, 1.0f, HalfLen - 0.03f) };
		back.AddChild(MeshBox(Vector3.Zero, new Vector3(0.95f, 2.0f, 0.06f), new Color(0.16f, 0.12f, 0.09f)));
		back.SetBox(new Vector3(0.95f, 2.0f, 0.3f), Vector3.Zero);
		root.AddChild(back);

		// The rule, painted above the front door.
		root.AddChild(new Label3D
		{
			Text = $"{d.Title}\n{d.Rule}",
			Position = new Vector3(0, 2.28f, -HalfLen + 0.05f),
			FontSize = 40,
			PixelSize = 0.0042f,
			Modulate = new Color(0.95f, 0.78f, 0.35f),
			OutlineSize = 6,
			Shaded = false,
		});
	}

	static void BuildProp(Node3D root, CarriageData d)
	{
		if (d.Prop == RuleProp.Suitcase)
		{
			var s = new RulePropInteractable { Label = "Відчинити валізу", Position = d.PropPos };
			s.AddChild(MeshBox(Vector3.Zero, new Vector3(0.6f, 0.32f, 0.4f), new Color(0.25f, 0.13f, 0.07f)));
			s.AddChild(MeshBox(new Vector3(0, 0.0f, -0.2f), new Vector3(0.1f, 0.06f, 0.02f), new Color(0.7f, 0.6f, 0.2f), 0.4f));
			s.SetBox(new Vector3(0.7f, 0.5f, 0.55f), Vector3.Zero);
			root.AddChild(s);
		}
		else
		{
			var w = new RulePropInteractable { Label = "Відчинити вікно", Position = d.PropPos };
			w.AddChild(MeshBox(Vector3.Zero, new Vector3(0.04f, 0.95f, 1.25f), new Color(0.35f, 0.4f, 0.45f), 0.0f));
			w.SetBox(new Vector3(0.4f, 1.0f, 1.3f), new Vector3(-0.15f, 0, 0));
			root.AddChild(w);
		}
	}

	static void BuildPeople(Node3D root, CarriageData d, Game game)
	{
		// Victim
		var victim = new VictimBody { Lines = d.VictimExamine, Position = SeatPos(d.VictimRow, d.VictimSide, d.VictimSlot) };
		var coat = d.VictimCoat;
		var trousers = new Color(0.1f, 0.1f, 0.12f);
		var skin = new Color(0.5f, 0.45f, 0.42f);
		if (d.VictimWet) coat = coat.Darkened(0.4f);
		victim.AddChild(MeshBox(new Vector3(0, 0.09f, -0.18f), new Vector3(0.42f, 0.16f, 0.46f), trousers));
		victim.AddChild(MeshBox(new Vector3(0, -0.28f, -0.4f), new Vector3(0.38f, 0.55f, 0.16f), trousers));
		victim.AddChild(MeshBox(new Vector3(0.0f, 0.4f, 0.02f), new Vector3(0.5f, 0.6f, 0.28f), coat)); // torso slumped
		victim.AddChild(MeshBox(new Vector3(0.0f, 0.86f, -0.12f), new Vector3(0.24f, 0.28f, 0.24f), skin)); // head dropped forward
		if (d.VictimScarf)
			victim.AddChild(MeshBox(new Vector3(0, 0.72f, -0.08f), new Vector3(0.3f, 0.1f, 0.3f), new Color(0.6f, 0.08f, 0.08f)));
		victim.SetBox(new Vector3(0.6f, 1.4f, 0.55f), new Vector3(0, 0.4f, -0.05f));
		root.AddChild(victim);

		if (d.VictimWet)
			root.AddChild(MeshBox(new Vector3(victim.Position.X, 0.575f, victim.Position.Z), new Vector3(0.7f, 0.01f, 0.7f), new Color(0.08f, 0.14f, 0.25f)));

		// Living passengers
		foreach (var p in d.Passengers)
		{
			var node = new Passenger { Data = p, GameRef = game, Position = SeatPos(p.Row, p.Side, p.Slot) };
			root.AddChild(node);
			node.Build();

			if (p.Puddle)
				root.AddChild(MeshBox(new Vector3(node.Position.X, 0.005f, node.Position.Z - 0.4f),
					new Vector3(0.6f, 0.01f, 0.5f), new Color(0.08f, 0.14f, 0.25f)));
		}
	}

	// ───────────────────────── helpers ─────────────────────────
	static MeshInstance3D MeshBox(Vector3 pos, Vector3 size, Color c, float emission = 0f) => new()
	{
		Mesh = new BoxMesh { Size = size },
		Position = pos,
		MaterialOverride = Ps1Materials.Get(c, emission, 0.3f),
	};

	static MeshInstance3D Box(Node parent, Vector3 pos, Vector3 size, Color c, float emission = 0f, bool collide = false, float grime = 0.35f)
	{
		var mi = new MeshInstance3D
		{
			Mesh = new BoxMesh { Size = size },
			Position = pos,
			MaterialOverride = Ps1Materials.Get(c, emission, grime),
		};
		parent.AddChild(mi);
		if (collide)
		{
			var body = new StaticBody3D { CollisionLayer = GameConfig.LayerWorld, CollisionMask = 0 };
			body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
			mi.AddChild(body);
		}
		return mi;
	}
}
