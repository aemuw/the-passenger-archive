using Godot;

namespace PassengerArchive;

public sealed class FigureParts
{
	public Node3D Head;
	public Node3D Upper;      // torso + head + arms: breathes as one
	public Node3D RightArm;
	public Node3D LeftArm;
}

/// <summary>
/// Authors a low-poly human with faceted volumes, either seated (origin = top of the seat cushion)
/// or standing (origin = floor). Faces are half in shadow by design; silhouettes (hats, collars, bags, glasses,
/// beards, hair) are what make people recognisable from across the carriage.
/// </summary>
public static class FigureBuilder
{
	static readonly Color Trousers = new(0.1f, 0.1f, 0.12f);
	static readonly Color DefaultSkin = new(0.55f, 0.43f, 0.36f);
	static readonly Color DefaultHair = new(0.12f, 0.09f, 0.07f);
	static readonly Color Dark = new(0.04f, 0.03f, 0.03f);
	static readonly Color Shoe = new(0.05f, 0.04f, 0.04f);

	static MeshInstance3D B(Node3D parent, Vector3 pos, Vector3 size, Color c, Surface surf = Surface.None, float grime = 0.25f)
	{
		var m = Geo.MeshBox(pos, size, c, 0, grime, surf);
		// Tapered, faceted volumes for anatomy and clothing; flat sheets remain boxes.
		if (size.Y > 0.12f && size.X > 0.08f && size.Z > 0.08f)
		{
			m.Mesh = new CylinderMesh { TopRadius = 0.43f, BottomRadius = 0.5f, Height = 1f, RadialSegments = 8, Rings = 1 };
			m.Scale = size;
		}
		parent.AddChild(m);
		return m;
	}

	public static FigureParts Build(Node3D root, Outfit o, bool seated)
	{
		var parts = new FigureParts();
		Color skin = o.Skin ?? DefaultSkin;
		Color hair = o.Hair ?? DefaultHair;
		float w = o.Build;                       // width multiplier
		Color pants = o.Coat.Darkened(0.6f).Lerp(Trousers, 0.5f);

		var up = new Node3D { Name = "Upper" };
		root.AddChild(up);
		parts.Upper = up;

		float headY, shoulderY, neckY;

		if (seated)
		{
			// legs (not part of the breathing group)
			foreach (float sx in new[] { -0.1f, 0.1f })
			{
				CharacterGeometry.Limb(root, "Thigh", new Vector3(sx*w,.10f,.02f), new Vector3(sx*w,.03f,-.37f), .085f, pants);
				CharacterGeometry.Limb(root, "Shin", new Vector3(sx*w,.03f,-.37f), new Vector3(sx*w,-.48f,-.40f), .064f, pants);
				B(root, new Vector3(sx * w, -0.51f, -0.46f), new Vector3(0.14f, 0.10f, 0.27f), Shoe);
			}
			up.AddChild(new MeshInstance3D { Name = "TailoredCoat", Position = new Vector3(0,.43f,.02f), Mesh = CharacterGeometry.Torso(w,o.LongCoat), MaterialOverride = CharacterGeometry.Cloth(o.Coat) });
			B(up, new Vector3(0, 0.17f, 0.02f), new Vector3(0.52f * w, 0.05f, 0.3f), Dark);   // belt
			if (o.LongCoat) B(root, new Vector3(0, 0.03f, -0.18f), new Vector3(0.54f * w, 0.1f, 0.5f), o.Coat, Surface.Fabric);
			if (o.Paper) B(up, new Vector3(0, 0.55f, -0.3f), new Vector3(0.4f, 0.32f, 0.02f), new Color(0.78f, 0.75f, 0.66f), Surface.Plaster, 0.1f);
			if (o.Flowers)
			{
				B(up, new Vector3(0.05f, 0.22f, -0.22f), new Vector3(0.2f, 0.08f, 0.38f), new Color(0.8f, 0.78f, 0.74f), Surface.None, 0.1f);
				B(up, new Vector3(0.05f, 0.28f, -0.34f), new Vector3(0.18f, 0.06f, 0.14f), new Color(0.7f, 0.1f, 0.12f), Surface.None, 0.1f);
			}
			if (o.Bag) B(root, new Vector3(0.0f, 0.2f, -0.52f), new Vector3(0.34f, 0.22f, 0.2f), o.Coat.Darkened(0.35f), Surface.Fabric);   // on the knees
			shoulderY = 0.68f;
			headY = 0.79f;
			neckY = 0.74f;
		}
		else
		{
			foreach (float sx in new[] { -0.11f, 0.11f })
			{
				CharacterGeometry.Limb(root,"Thigh",new Vector3(sx*w,.81f,0),new Vector3(sx*w,.43f,-.02f),.082f,pants);
				CharacterGeometry.Limb(root,"Shin",new Vector3(sx*w,.43f,-.02f),new Vector3(sx*w,.09f,0),.061f,pants);
				B(root, new Vector3(sx * w, 0.04f, -0.05f), new Vector3(0.17f, 0.08f, 0.3f), Shoe);
			}
			up.AddChild(new MeshInstance3D { Name = "TailoredCoat", Position = new Vector3(0,1.11f,0), Mesh = CharacterGeometry.Torso(w,o.LongCoat), MaterialOverride = CharacterGeometry.Cloth(o.Coat) });
			B(up, new Vector3(0, 0.84f, 0), new Vector3(0.52f * w, 0.05f, 0.3f), Dark);        // belt
			if (o.LongCoat) B(root, new Vector3(0, 0.55f, 0), new Vector3(0.54f * w, 0.62f, 0.3f), o.Coat, Surface.Fabric);
			if (o.Paper) B(up, new Vector3(0, 1.2f, -0.3f), new Vector3(0.4f, 0.32f, 0.02f), new Color(0.78f, 0.75f, 0.66f), Surface.Plaster, 0.1f);
			if (o.Bag) B(up, new Vector3(-0.4f * w, 0.85f, 0), new Vector3(0.12f, 0.3f, 0.34f), o.Coat.Darkened(0.35f), Surface.Fabric);
			shoulderY = 1.38f;
			headY = 1.47f;
			neckY = 1.4f;
		}

		// coat front: two lapels and a few buttons (readable from the front only)
		if (!o.LongCoat)
		{
			float by = seated ? 0.45f : 1.11f;
			for (int i = 0; i < 3; i++) B(up, new Vector3(0, by + 0.18f - i * 0.17f, -0.145f), new Vector3(0.035f, 0.035f, 0.01f), Dark, Surface.None, 0f);
		}

		// arms: pivot at the shoulder, so they can swing
		float armY = seated ? -0.26f : -0.28f;
		float armH = seated ? 0.5f : 0.56f;
		float armZ = seated ? -0.06f : 0f;
		parts.RightArm = new Node3D { Name = "RightArm", Position = new Vector3(0.235f * w, shoulderY, armZ) };
		parts.LeftArm = new Node3D { Name = "LeftArm", Position = new Vector3(-0.235f * w, shoulderY, armZ) };
		up.AddChild(parts.RightArm);
		up.AddChild(parts.LeftArm);
		foreach (var arm in new[] { parts.RightArm, parts.LeftArm })
		{
			var elbow = new Vector3(0,-.26f,seated ? -.07f:0);
			var wrist = new Vector3(arm == parts.RightArm ? -.025f:.025f, seated ? -.36f:-.54f,seated ? -.29f:-.02f);
			CharacterGeometry.Limb(arm,"UpperSleeve",Vector3.Zero,elbow,.071f,o.Coat);
			CharacterGeometry.Limb(arm,"ForeSleeve",elbow,wrist,.055f,o.Coat);
			B(arm,wrist+new Vector3(0,-.045f,-.018f),new Vector3(.074f,.10f,.07f),skin,Surface.Skin,.03f);
		}

		if (o.Collar) B(up, new Vector3(0, neckY + 0.02f, 0.02f), new Vector3(0.36f * w, 0.14f, 0.32f), o.Coat.Darkened(0.2f), Surface.Fabric);
		if (o.Scarf) B(up, new Vector3(0, neckY, -0.02f), new Vector3(0.3f * w, 0.1f, 0.3f), o.ScarfColor ?? new Color(0.6f, 0.08f, 0.08f), Surface.Fabric, 0.2f);

		B(up, new Vector3(0, neckY + 0.035f, 0.02f), new Vector3(0.11f, 0.11f, 0.11f), skin, Surface.Skin, 0.04f);
		// head
		parts.Head = new Node3D { Name = "Head", Position = new Vector3(0, headY, 0.02f) };
		up.AddChild(parts.Head);
		var h = parts.Head;
		FaceAuthoring.Build(h, o);
		h.Scale = new Vector3(.91f,.90f,.95f);
		var animations = new AnimationPlayer { Name = "Animations" };
		var library = new AnimationLibrary();
		foreach (string name in new[] { "idle", "talk", "sleeve", "attack" })
		{
			var animation = new Animation { Length = name == "attack" ? 1.1f : name == "sleeve" ? 1.5f : 3f, LoopMode = name is "sleeve" or "attack" ? Animation.LoopModeEnum.None : Animation.LoopModeEnum.Linear };
			int breath = animation.AddTrack(Animation.TrackType.Value);
			animation.TrackSetPath(breath, "Upper:position");
			animation.TrackInsertKey(breath, 0, Vector3.Zero);
			animation.TrackInsertKey(breath, animation.Length / 2, new Vector3(0, 0.007f, 0));
			animation.TrackInsertKey(breath, animation.Length, Vector3.Zero);
			int arm = animation.AddTrack(Animation.TrackType.Value);
			animation.TrackSetPath(arm, "Upper/RightArm:rotation");
			animation.TrackInsertKey(arm, 0, Vector3.Zero);
			animation.TrackInsertKey(arm, animation.Length / 3, new Vector3(name == "idle" ? -0.025f : -0.25f, 0, name == "sleeve" ? -0.3f : 0));
			animation.TrackInsertKey(arm, animation.Length * 2 / 3, new Vector3(name == "idle" ? 0 : -0.12f, 0, 0));
			animation.TrackInsertKey(arm, animation.Length, Vector3.Zero);
			if (name == "attack")
			{
				animation.TrackSetKeyValue(arm, 0, new Vector3(-1.3f, 0, -0.2f));
				animation.TrackSetKeyValue(arm, 1, new Vector3(-1.6f, 0, -0.1f));
				animation.TrackSetKeyValue(arm, 2, new Vector3(0.7f, 0, 0.2f));
				animation.TrackSetKeyValue(breath, 1, new Vector3(0, -0.06f, -0.18f));
			}
			library.AddAnimation(name, animation);
		}
		animations.AddAnimationLibrary("", library);
		root.AddChild(animations);
		return parts;
	}
}
