using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>
/// The night outside the windows: poles and trees streaming past, plus (optionally) a lit house
/// that never moves — the world refusing to match the train's motion.
/// </summary>
public partial class Scenery : Node3D
{
	[Export] public float Speed { get; set; } = 14f;
	[Export] public bool StuckHouse { get; set; }

	const float Span = 100f;
	readonly List<Node3D> _movers = new();

	public override void _Ready()
	{
		foreach (var n in GetChildren())
			if (n is Node3D mover && mover.Name.ToString().StartsWith("Moving")) _movers.Add(mover);
	}

	public void Author()
	{
		var rng = new RandomNumberGenerator { Seed = 42 };

		// Ground + moon
		Prim(this, new BoxMesh { Size = new Vector3(300, 0.2f, 300) }, new Vector3(0, -1.3f, 0), new Color(0.04f, 0.06f, 0.05f));
		Prim(this, new BoxMesh { Size = new Vector3(4, 4, 0.5f) }, new Vector3(-35, 28, -70), new Color(0.75f, 0.8f, 0.9f), 1.0f);

		int count = 36;
		for (int i = 0; i < count; i++)
		{
			float z = -Span / 2f + Span * i / count;
			float side = (i % 2 == 0) ? 1f : -1f;
			var node = new Node3D { Name = "Moving" + i, Position = new Vector3(side * rng.RandfRange(4.5f, 16f), 0, z) };

			if (i % 3 == 0)
			{
				// telegraph pole
				Prim(node, new BoxMesh { Size = new Vector3(0.15f, 5f, 0.15f) }, new Vector3(0, 1.2f, 0), new Color(0.12f, 0.09f, 0.07f));
				Prim(node, new BoxMesh { Size = new Vector3(1.4f, 0.1f, 0.1f) }, new Vector3(0, 3.4f, 0), new Color(0.12f, 0.09f, 0.07f));
			}
			else
			{
				Prim(node, new BoxMesh { Size = new Vector3(0.3f, 2f, 0.3f) }, new Vector3(0, 0f, 0), new Color(0.10f, 0.07f, 0.05f));
				Prim(node, new CylinderMesh { TopRadius = 0f, BottomRadius = 1.3f, Height = 3.4f, RadialSegments = 5 },
					new Vector3(0, 2.2f, 0), new Color(0.05f, 0.11f, 0.08f));
			}
			AddChild(node);
			_movers.Add(node);
		}

		if (StuckHouse)
		{
			var house = new Node3D { Position = new Vector3(7.5f, 0, 0.8f) };
			Prim(house, new BoxMesh { Size = new Vector3(3.2f, 2.2f, 3.0f) }, new Vector3(0, -0.1f, 0), new Color(0.2f, 0.17f, 0.12f));
			Prim(house, new CylinderMesh { TopRadius = 0f, BottomRadius = 2.6f, Height = 1.4f, RadialSegments = 4 },
				new Vector3(0, 1.7f, 0), new Color(0.12f, 0.07f, 0.06f));
			Prim(house, new BoxMesh { Size = new Vector3(0.05f, 0.6f, 0.5f) }, new Vector3(-1.62f, 0.2f, 0), new Color(1f, 0.8f, 0.4f), 1.2f);
			AddChild(house);
		}
	}

	public override void _Process(double delta)
	{
		float dz = Speed * (float)delta;
		foreach (var n in _movers)
		{
			var p = n.Position;
			p.Z += dz;
			if (p.Z > Span / 2f) p.Z -= Span;
			n.Position = p;
		}
	}

	static void Prim(Node parent, Mesh mesh, Vector3 pos, Color c, float emission = 0f)
	{
		parent.AddChild(new MeshInstance3D
		{
			Mesh = mesh,
			Position = pos,
			MaterialOverride = Ps1Materials.Get(c, emission, 0.15f),
		});
	}
}
