using Godot;

namespace PassengerArchive;

/// <summary>Dust hanging in the lamplight: a few tiny motes drifting slowly.</summary>
public partial class Dust : Node3D
{
	[Export] public Vector3 Extents { get; set; } = new(1.6f, 1.2f, 6.8f);
	[Export] public int Count { get; set; } = 18;
	readonly System.Collections.Generic.List<(MeshInstance3D m, Vector3 v)> _motes = new();

	public override void _Ready()
	{
		foreach (var child in GetChildren())
			if (child is MeshInstance3D m) _motes.Add((m, new Vector3(0.01f, 0.008f, 0.015f)));
	}

	public void Author()
	{
		var rng = new RandomNumberGenerator { Seed = 8 };
		for (int i = 0; i < Count; i++)
		{
			var m = Geo.MeshBox(new Vector3(rng.RandfRange(-Extents.X, Extents.X), rng.RandfRange(-Extents.Y, Extents.Y), rng.RandfRange(-Extents.Z, Extents.Z)),
				new Vector3(0.006f, 0.006f, 0.006f), new Color(0.45f, 0.42f, 0.33f), 0.15f, 0f);
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


