using Godot;

namespace PassengerArchive;

/// <summary>Explicit cross sections for authored anatomy and tailored clothing.</summary>
public static class CharacterGeometry
{
	public static ArrayMesh Torso(float width, bool longCoat)
	{
		var levels = new[] { (-0.31f, .35f, .24f), (-.18f, .36f, .25f), (.08f, .46f, .28f), (.23f, .49f, .25f), (.31f, .20f, .17f) };
		var surface = new SurfaceTool(); surface.Begin(Mesh.PrimitiveType.Triangles);
		Vector3 Point(int level, int edge)
		{
			var (y, w, depth) = levels[level];
			var ring = new Vector2[] { new(-.37f,-.5f), new(.37f,-.5f), new(.5f,-.25f), new(.5f,.25f), new(.37f,.5f), new(-.37f,.5f), new(-.5f,.25f), new(-.5f,-.25f) };
			return new Vector3(ring[edge].X * w * width, y, ring[edge].Y * depth);
		}
		void V(Vector3 p) { surface.SetUV(new Vector2(p.X / (.5f * width) + .5f, .5f - p.Y / .62f)); surface.AddVertex(p); }
		for (int level = 0; level < levels.Length-1; level++)
		for (int edge = 0; edge < 8; edge++)
		{
			var a = Point(level,edge); var b = Point(level,(edge+1)%8); var c = Point(level+1,edge); var d = Point(level+1,(edge+1)%8);
			V(a); V(c); V(d); V(a); V(d); V(b);
		}
		surface.GenerateNormals(); return surface.Commit();
	}
	public static Material Cloth(Color color) => new StandardMaterial3D {
		AlbedoColor = color.Lightened(.13f), AlbedoTexture = GD.Load<Texture2D>("res://assets/textures/tailoring.png"),
		TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest, Roughness = 1, CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled };
	public static MeshInstance3D Limb(Node3D parent, string name, Vector3 a, Vector3 b, float radius, Color color)
	{
		var mesh = new MeshInstance3D { Name = name, Position = (a+b)/2,
			Mesh = new CylinderMesh { TopRadius = radius*.85f, BottomRadius = radius, Height = a.DistanceTo(b), RadialSegments = 8, Rings = 1 },
			MaterialOverride = Ps1Materials.Get(color, 0, .06f, Surface.Fabric) };
		var direction = (a-b).Normalized();
		mesh.Quaternion = new Quaternion(Vector3.Up, direction);
		parent.AddChild(mesh); return mesh;
	}
}
