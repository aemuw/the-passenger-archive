using System;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace PassengerArchive;

/// <summary>Small, deliberately drawn pixel faces and faceted head meshes. Saved assets only.</summary>
public static class FaceAuthoring
{
	static Color Skin(Outfit look) => look.Skin ?? new Color(0.55f, 0.43f, 0.36f);
	static Color Hair(Outfit look) => look.Hair ?? new Color(0.12f, 0.09f, 0.07f);
	static string Path(Outfit look)
	{
		string key = $"{Skin(look).ToHtml()}:{Hair(look).ToHtml()}:{look.Glasses}:{look.Beard}:{look.Faceless}:{look.HairStyle}:{look.Age}";
		string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..12].ToLowerInvariant();
		return $"res://assets/textures/faces/{hash}.png";
	}
	public static void Draw(Outfit look)
	{
		var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgb8);
		Color skin = Skin(look), hair = Hair(look), ink = new(0.12f, 0.10f, 0.085f);
		image.Fill(skin);
		void Rect(int x, int y, int w, int h, Color c)
		{
			for (int py = y; py < y + h; py++) for (int px = x; px < x + w; px++) image.SetPixel(px, py, c);
		}
		for (int y = 0; y < 64; y++)
		for (int x = 0; x < 64; x++)
		{
			float edge = Mathf.Abs(x - 31.5f) / 32;
			image.SetPixel(x, y, skin.Darkened(edge > 0.78f ? 0.17f : edge > 0.56f ? 0.07f : 0f));
		}
		Rect(0, 0, 64, 8, hair); Rect(0, 8, 7, 18, hair); Rect(57, 8, 7, 18, hair);
		foreach (int x in new[] { 17, 39 })
		{
			Rect(x, 23, 10, 2, hair.Darkened(0.1f));
			Rect(x, 27, 10, 5, skin.Darkened(.22f));
			Rect(x + 1, 29, 8, 2, skin.Lightened(0.12f));
			Rect(x + 4, 29, 2, 2, ink);
			Rect(x + 1, 33, 9, 1, skin.Darkened(0.14f));
			Rect(x-1, 35, 7, 1, skin.Darkened(.08f));
		}
		Rect(31, 29, 2, 12, skin.Lightened(0.12f)); Rect(33, 32, 2, 10, skin.Darkened(0.22f));
		Rect(28, 41, 8, 2, skin.Darkened(0.3f));
		Rect(24, 48, 16, 2, skin.Darkened(0.35f)); Rect(27, 50, 10, 1, skin.Lightened(0.08f));
		if(look.Age>55) { Rect(24,16,16,1,skin.Darkened(.08f)); Rect(22,19,20,1,skin.Darkened(.06f)); }
		if (look.Beard)
			for (int y = 43; y < 64; y++) for (int x = 10; x < 54; x++)
				if ((x < 23 || x > 40 || y > 52) && (x + y) % 3 != 0) image.SetPixel(x, y, skin.Lerp(hair, 0.65f));
		if (look.Glasses)
		{
			foreach (int x in new[] { 14, 37 })
			{
				Rect(x, 26, 15, 1, ink); Rect(x, 34, 15, 1, ink);
				Rect(x, 26, 1, 9, ink); Rect(x + 14, 26, 1, 9, ink);
				Rect(x + 2, 27, 4, 1, skin.Lightened(0.35f));
			}
			Rect(29, 29, 8, 1, ink);
		}
		if (look.Faceless) image.Fill(new Color(0.045f, 0.055f, 0.065f));
		var error = image.SavePng(Path(look));
		if (error != Error.Ok) throw new InvalidOperationException("Could not save face texture: " + error);
		image.Dispose();
	}
	public static void Build(Node3D root, Outfit look)
	{
		Color skin = Skin(look), hair = Hair(look);
		var outline = new Vector2[] { new(0, -0.01f), new(-0.08f, 0.025f), new(-0.12f, 0.095f), new(-0.13f, 0.17f), new(-0.1f, 0.265f), new(0, 0.29f), new(0.1f, 0.265f), new(0.13f, 0.17f), new(0.12f, 0.095f), new(0.08f, 0.025f) };
		var front = new SurfaceTool(); front.Begin(Mesh.PrimitiveType.Triangles);
		var sides = new SurfaceTool(); sides.Begin(Mesh.PrimitiveType.Triangles);
		Vector3 Point(Vector2 point, bool back) => new(point.X, point.Y, back ? 0.095f : point.Y > 0.25f || point.Y < 0.04f ? -0.085f : -0.12f);
		void FaceVertex(Vector3 vertex)
		{
			front.SetUV(new Vector2(vertex.X / 0.26f + 0.5f, 1f - (vertex.Y + 0.01f) / 0.30f)); front.AddVertex(vertex);
		}
		void Triangle(Vector3 a, Vector3 b, Vector3 c) { sides.AddVertex(a); sides.AddVertex(b); sides.AddVertex(c); }
		for (int i = 0; i < outline.Length; i++)
		{
			var a = Point(outline[i], false); var b = Point(outline[(i + 1) % outline.Length], false);
			var ab = Point(outline[i], true); var bb = Point(outline[(i + 1) % outline.Length], true);
			FaceVertex(new Vector3(0, 0.145f, -0.125f)); FaceVertex(b); FaceVertex(a);
			Triangle(a, b, bb); Triangle(a, bb, ab);
			Triangle(new Vector3(0, 0.145f, 0.095f), ab, bb);
		}
		front.GenerateNormals(); sides.GenerateNormals();
		var material = new StandardMaterial3D
		{
			AlbedoTexture = GD.Load<Texture2D>(Path(look)), TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
			Roughness = 1f, SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled, CullMode = BaseMaterial3D.CullModeEnum.Disabled,
		};
		root.AddChild(new MeshInstance3D { Name = "Face", Mesh = front.Commit(), MaterialOverride = material });
		var nose = new SurfaceTool(); nose.Begin(Mesh.PrimitiveType.Triangles);
		foreach(var v in new[] { new Vector3(-.015f,.11f,-.122f),new Vector3(0,.10f,-.155f),new Vector3(0,.18f,-.124f),new Vector3(0,.18f,-.124f),new Vector3(0,.10f,-.155f),new Vector3(.015f,.11f,-.122f) }) nose.AddVertex(v);
		nose.GenerateNormals();
		root.AddChild(new MeshInstance3D { Name="Nose", Mesh=nose.Commit(), MaterialOverride = new StandardMaterial3D { AlbedoColor=skin, Roughness=1, CullMode=BaseMaterial3D.CullModeEnum.Disabled } });
		root.AddChild(new MeshInstance3D { Name = "Skull", Mesh = sides.Commit(), MaterialOverride = Ps1Materials.Get(skin, 0, 0.04f) });
		root.AddChild(new MeshInstance3D
		{
			Name = "Hair", Position = new Vector3(0, 0.245f, 0.015f),
			Mesh = new SphereMesh { Radius = 0.137f, Height = 0.145f, RadialSegments = 10, Rings = 4 },
			MaterialOverride = Ps1Materials.Get(hair, 0, 0.05f),
		});
		foreach (float side in new[] { -1f, 1f })
			root.AddChild(new MeshInstance3D { Name = side < 0 ? "LeftEar" : "RightEar", Position = new Vector3(side * 0.13f, 0.12f, 0),
				Mesh = new SphereMesh { Radius = 0.032f, Height = 0.065f, RadialSegments = 6, Rings = 3 }, Scale = new Vector3(0.45f, 1, 0.6f), MaterialOverride = Ps1Materials.Get(skin, 0, 0.05f) });
		if(look.HairStyle=="bob") foreach(float side in new[] { -1f,1f })
			root.AddChild(new MeshInstance3D { Name=side<0?"HairLeft":"HairRight",Position=new Vector3(side*.123f,.16f,.035f), Mesh=new CylinderMesh { TopRadius=.053f,BottomRadius=.062f,Height=.22f,RadialSegments=6,Rings=1 }, MaterialOverride=Ps1Materials.Get(hair,0,.04f) });
		if(look.HairStyle=="tied") root.AddChild(new MeshInstance3D { Name="HairBun",Position=new Vector3(0,.19f,.12f), Mesh=new SphereMesh { Radius=.075f,Height=.13f,RadialSegments=8,Rings=4 }, MaterialOverride=Ps1Materials.Get(hair,0,.04f) });
		if (look.Hat)
		{
			Color cap = look.HatColor ?? new Color(0.08f, 0.09f, 0.09f);
			root.AddChild(new MeshInstance3D { Name = "CapCrown", Position = new Vector3(0, 0.3f, 0.015f), Mesh = new CylinderMesh { TopRadius = 0.115f, BottomRadius = 0.15f, Height = 0.10f, RadialSegments = 10, Rings = 1 }, MaterialOverride = Ps1Materials.Get(cap, 0, 0.1f, Surface.Fabric) });
			root.AddChild(new MeshInstance3D { Name = "CapBrim", Position = new Vector3(0, 0.265f, -0.065f), Mesh = new CylinderMesh { TopRadius = 0.17f, BottomRadius = 0.17f, Height = 0.014f, RadialSegments = 10, Rings = 1 }, Scale = new Vector3(1, 1, 0.7f), MaterialOverride = Ps1Materials.Get(cap, 0, 0.1f) });
		}
	}
}
