using Godot;

namespace PassengerArchive;

/// <summary>Tiny helpers for building boxy PS1 geometry in code.</summary>
public static class Geo
{
	public static MeshInstance3D MeshBox(Vector3 pos, Vector3 size, Color c, float emission = 0f, float grime = 0.3f) => new()
	{
		Mesh = new BoxMesh { Size = size },
		Position = pos,
		MaterialOverride = Ps1Materials.Get(c, emission, grime),
	};

	public static MeshInstance3D Box(Node parent, Vector3 pos, Vector3 size, Color c, float emission = 0f, bool collide = false, float grime = 0.35f)
	{
		var mi = MeshBox(pos, size, c, emission, grime);
		parent.AddChild(mi);
		if (collide)
		{
			var body = new StaticBody3D { CollisionLayer = GameConfig.LayerWorld, CollisionMask = 0 };
			body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
			mi.AddChild(body);
		}
		return mi;
	}

	/// <summary>Invisible solid box (walls you cannot see but cannot cross).</summary>
	public static void Barrier(Node parent, Vector3 pos, Vector3 size)
	{
		var body = new StaticBody3D { CollisionLayer = GameConfig.LayerWorld, CollisionMask = 0, Position = pos };
		body.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size } });
		parent.AddChild(body);
	}
}
