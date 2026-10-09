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
		BuildProps(root, d, game);
		BuildPeople(root, d, game);

		root.AddChild(new Scenery { StuckHouse = d.StuckWorld });
		return root;
	}

	static void BuildShell(Node3D root)
	{
		float len = HalfLen * 2f + 0.2f;
		Geo.Box(root, new Vector3(0, -0.05f, 0), new Vector3(3.6f, 0.1f, len), Floor, 0, true);
		Geo.Box(root, new Vector3(0, Height + 0.05f, 0), new Vector3(3.6f, 0.1f, len), Ceiling);
		Geo.Box(root, new Vector3(0, Height / 2f, -HalfLen - 0.05f), new Vector3(3.6f, Height, 0.1f), WoodLow, 0, true);
		Geo.Box(root, new Vector3(0, Height / 2f, HalfLen + 0.05f), new Vector3(3.6f, Height, 0.1f), WoodLow, 0, true);

		foreach (int side in new[] { -1, 1 })
		{
			float x = side * (HalfWidth + 0.05f);
			Geo.Box(root, new Vector3(x, 0.45f, 0), new Vector3(0.1f, 0.9f, len), WoodLow);
			Geo.Box(root, new Vector3(x, 2.3f, 0), new Vector3(0.1f, 0.6f, len), WallUp);

			// Window pillars (the gaps between them are the windows)
			for (int k = 0; k <= 8; k++)
				Geo.Box(root, new Vector3(side * HalfWidth, 1.45f, -6.4f + 1.6f * k), new Vector3(0.12f, 1.1f, 0.3f), Metal);

			// Invisible wall so you cannot walk out through the windows.
			Geo.Barrier(root, new Vector3(x, Height / 2f, 0), new Vector3(0.1f, Height, len));

			// Luggage shelf
			Geo.Box(root, new Vector3(side * 1.3f, 1.85f, 0), new Vector3(0.5f, 0.05f, 13f), Metal);
		}
	}

	static void BuildSeats(Node3D root)
	{
		for (int r = 0; r < 8; r++)
		foreach (int side in new[] { -1, 1 })
		{
			float z = RowZ(r);
			float x = side * 1.09f;
			Geo.Box(root, new Vector3(x, 0.225f, z), new Vector3(1.24f, 0.45f, 0.9f), SeatFrame, 0, true);
			Geo.Box(root, new Vector3(x, 0.51f, z), new Vector3(1.24f, 0.12f, 0.9f), SeatGreen);
			Geo.Box(root, new Vector3(x, SeatY + 0.4f, z + 0.38f), new Vector3(1.24f, 0.8f, 0.14f), SeatGreen, 0, true);
		}
	}

	static void BuildLamps(Node3D root, CarriageData d)
	{
		for (int k = 0; k < 5; k++)
		{
			float z = -5.6f + 2.8f * k;
			bool dead = d.DeadLamps != null && System.Array.IndexOf(d.DeadLamps, k) >= 0;

			var fixture = Geo.Box(root, new Vector3(0, 2.54f, z), new Vector3(0.4f, 0.06f, 1.0f),
				dead ? new Color(0.15f, 0.15f, 0.12f) : new Color(1f, 0.9f, 0.55f), dead ? 0f : 1f, false, 0f);

			root.AddChild(new FlickerLight
			{
				Position = new Vector3(0, 2.35f, z),
				LightColor = new Color(1f, 0.86f, 0.5f),
				OmniRange = 6.5f,
				OmniAttenuation = 1.2f,
				BaseEnergy = 1.15f,
				Fixture = fixture,
				Dead = dead,
				Permanent = dead,
			});
		}
	}

	static void BuildDoorsAndSign(Node3D root, CarriageData d)
	{
		var front = new DoorInteractable { Forward = true, Position = new Vector3(0, 1.0f, -HalfLen + 0.03f) };
		front.AddChild(Geo.MeshBox(Vector3.Zero, new Vector3(0.95f, 2.0f, 0.06f), new Color(0.16f, 0.12f, 0.09f)));
		front.AddChild(Geo.MeshBox(new Vector3(0, 0.45f, 0.04f), new Vector3(0.55f, 0.6f, 0.02f), new Color(0.02f, 0.03f, 0.05f)));
		front.SetBox(new Vector3(0.95f, 2.0f, 0.3f), Vector3.Zero);
		root.AddChild(front);

		var back = new DoorInteractable { Forward = false, Position = new Vector3(0, 1.0f, HalfLen - 0.03f) };
		back.AddChild(Geo.MeshBox(Vector3.Zero, new Vector3(0.95f, 2.0f, 0.06f), new Color(0.16f, 0.12f, 0.09f)));
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
