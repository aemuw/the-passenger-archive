using Godot;

namespace PassengerArchive;

/// <summary>One reusable railway bench, saved as a scene before any carriage is authored.</summary>
public static class BenchAuthoring
{
	public static Node3D Create()
	{
		var root = new Node3D { Name = "Bench" };
		var wood = new Color(0.24f, 0.16f, 0.1f);
		var fabric = new Color(0.24f, 0.35f, 0.27f);
		var metal = new Color(0.16f, 0.18f, 0.16f);
		foreach (float x in new[] { -0.49f, 0.49f })
		foreach (float z in new[] { -0.3f, 0.3f })
			Part(root, "Leg", new CylinderMesh { TopRadius = 0.025f, BottomRadius = 0.04f, Height = 0.44f, RadialSegments = 6 }, new Vector3(x, 0.22f, z), metal, Surface.Metal);
		Part(root, "SeatFrame", new BoxMesh { Size = new Vector3(1.24f, 0.09f, 0.88f) }, new Vector3(0, 0.44f, 0), wood, Surface.Wood);
		Part(root, "Upholstery", Chamfer(new Vector3(1.24f, 0.15f, 0.88f), 0.07f), new Vector3(0, 0.52f, 0), fabric, Surface.Fabric);
		Part(root, "Backrest", Chamfer(new Vector3(1.24f, 0.72f, 0.16f), 0.035f), new Vector3(0, 0.98f, 0.37f), fabric, Surface.Fabric);
		Part(root, "TopRail", new BoxMesh { Size = new Vector3(1.28f, 0.055f, 0.19f) }, new Vector3(0, 1.37f, 0.37f), wood, Surface.Wood);
		Part(root, "BackSeam", new BoxMesh { Size = new Vector3(0.009f, 0.62f, 0.005f) }, new Vector3(0, 0.99f, 0.287f), fabric.Darkened(0.2f), Surface.None);
		foreach (float x in new[] { -0.31f, 0.31f })
			Part(root, "UpholsteryButton", new CylinderMesh { TopRadius = 0.013f, BottomRadius = 0.013f, Height = 0.007f, RadialSegments = 8 }, new Vector3(x, 1.13f, 0.282f), fabric.Darkened(0.15f), Surface.None);
		foreach (float x in new[] { -0.61f, 0.61f })
		{
			Part(root, "ArmSupport", new BoxMesh { Size = new Vector3(0.045f, 0.4f, 0.045f) }, new Vector3(x, 0.65f, -0.16f), metal, Surface.Metal);
			Part(root, "Armrest", Chamfer(new Vector3(0.08f, 0.06f, 0.63f), 0.015f), new Vector3(x, 0.85f, 0), wood, Surface.Wood);
		}
		Part(root, "CentreSeam", new BoxMesh { Size = new Vector3(0.012f, 0.004f, 0.72f) }, new Vector3(0, 0.597f, -0.02f), fabric.Darkened(0.25f), Surface.None);
		Geo.Barrier(root, new Vector3(0, 0.3f, 0), new Vector3(1.24f, 0.6f, 0.86f));
		Geo.Barrier(root, new Vector3(0, 0.98f, 0.37f), new Vector3(1.24f, 0.72f, 0.16f));
		return root;
	}
	static void Part(Node parent, string name, Mesh mesh, Vector3 position, Color color, Surface surface)
	{
		parent.AddChild(new MeshInstance3D { Name = name, Mesh = mesh, Position = position, MaterialOverride = Ps1Materials.Get(color, 0, 0.18f, surface) });
	}
	// Eight hand-defined corners, flat-shaded triangles. No external/generated bitmap model.
	public static ArrayMesh Chamfer(Vector3 size, float bevel)
	{
		float x = size.X / 2, z = size.Z / 2, y = size.Y / 2;
		var outline = new Vector2[] { new(-x + bevel, -z), new(x - bevel, -z), new(x, -z + bevel), new(x, z - bevel), new(x - bevel, z), new(-x + bevel, z), new(-x, z - bevel), new(-x, -z + bevel) };
		var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
		void Triangle(Vector3 a, Vector3 b, Vector3 c) { surface.AddVertex(a); surface.AddVertex(b); surface.AddVertex(c); }
		for (int i = 0; i < 8; i++)
		{
			Vector2 a = outline[i], b = outline[(i + 1) % 8];
			Vector3 at = new(a.X, y, a.Y), bt = new(b.X, y, b.Y), ab = new(a.X, -y, a.Y), bb = new(b.X, -y, b.Y);
			Triangle(Vector3.Up * y, bt, at); Triangle(Vector3.Down * y, ab, bb);
			Triangle(at, bt, bb); Triangle(at, bb, ab);
		}
		surface.GenerateNormals(); return surface.Commit();
	}
}
