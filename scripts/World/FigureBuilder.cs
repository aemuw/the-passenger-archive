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
/// Builds a low-poly human from boxes, either seated (origin = top of the seat cushion)
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

		var up = new Node3D();
		root.AddChild(up);
		parts.Upper = up;

		float headY, shoulderY, neckY;

		if (seated)
		{
			// legs (not part of the breathing group)
			B(root, new Vector3(0, 0.09f, -0.18f), new Vector3(0.42f * w, 0.16f, 0.46f), pants, Surface.Fabric);
			foreach (float sx in new[] { -0.1f, 0.1f })
			{
				B(root, new Vector3(sx * w, -0.28f, -0.4f), new Vector3(0.15f, 0.55f, 0.15f), pants, Surface.Fabric);
				B(root, new Vector3(sx * w, -0.55f, -0.46f), new Vector3(0.15f, 0.08f, 0.27f), Shoe);
			}
			B(up, new Vector3(0, 0.45f, 0.02f), new Vector3(0.5f * w, 0.62f, 0.28f), o.Coat, Surface.Fabric);
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
			headY = 0.9f;
			neckY = 0.74f;
		}
		else
		{
			foreach (float sx in new[] { -0.11f, 0.11f })
			{
				B(root, new Vector3(sx * w, 0.4f, 0), new Vector3(0.17f, 0.8f, 0.19f), pants, Surface.Fabric);
				B(root, new Vector3(sx * w, 0.04f, -0.05f), new Vector3(0.17f, 0.08f, 0.3f), Shoe);
			}
			B(up, new Vector3(0, 1.11f, 0), new Vector3(0.5f * w, 0.62f, 0.28f), o.Coat, Surface.Fabric);
			B(up, new Vector3(0, 0.84f, 0), new Vector3(0.52f * w, 0.05f, 0.3f), Dark);        // belt
			if (o.LongCoat) B(root, new Vector3(0, 0.55f, 0), new Vector3(0.54f * w, 0.62f, 0.3f), o.Coat, Surface.Fabric);
			if (o.Paper) B(up, new Vector3(0, 1.2f, -0.3f), new Vector3(0.4f, 0.32f, 0.02f), new Color(0.78f, 0.75f, 0.66f), Surface.Plaster, 0.1f);
			if (o.Bag) B(up, new Vector3(-0.4f * w, 0.85f, 0), new Vector3(0.12f, 0.3f, 0.34f), o.Coat.Darkened(0.35f), Surface.Fabric);
			shoulderY = 1.38f;
			headY = 1.55f;
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
		parts.RightArm = new Node3D { Position = new Vector3(0.31f * w, shoulderY, armZ) };
		parts.LeftArm = new Node3D { Position = new Vector3(-0.31f * w, shoulderY, armZ) };
		up.AddChild(parts.RightArm);
		up.AddChild(parts.LeftArm);
		foreach (var arm in new[] { parts.RightArm, parts.LeftArm })
		{
			B(arm, new Vector3(0, armY, 0), new Vector3(0.12f, armH, 0.14f), o.Coat, Surface.Fabric);
			B(arm, new Vector3(0, armY - armH / 2f - 0.04f, 0), new Vector3(0.1f, 0.1f, 0.1f), skin, Surface.Skin);   // hand
		}

		if (o.Collar) B(up, new Vector3(0, neckY + 0.02f, 0.02f), new Vector3(0.36f * w, 0.14f, 0.32f), o.Coat.Darkened(0.2f), Surface.Fabric);
		if (o.Scarf) B(up, new Vector3(0, neckY, -0.02f), new Vector3(0.3f * w, 0.1f, 0.3f), o.ScarfColor ?? new Color(0.6f, 0.08f, 0.08f), Surface.Fabric, 0.2f);

		// head
		parts.Head = new Node3D { Position = new Vector3(0, headY, 0.02f) };
		up.AddChild(parts.Head);
		var h = parts.Head;
		B(h, new Vector3(0, -0.05f, 0), new Vector3(0.1f, 0.1f, 0.1f), skin, Surface.Skin);                         // neck
		B(h, new Vector3(0, 0.12f, 0), new Vector3(0.24f, 0.28f, 0.24f), skin, Surface.Skin);                       // skull
		B(h, new Vector3(0, 0.28f, 0.01f), new Vector3(0.26f, 0.07f, 0.26f), hair);                                  // hair on top
		B(h, new Vector3(0, 0.18f, 0.1f), new Vector3(0.26f, 0.24f, 0.07f), hair);                                   // hair at the back
		B(h, new Vector3(-0.13f, 0.12f, 0), new Vector3(0.03f, 0.07f, 0.04f), skin, Surface.Skin);                  // ears
		B(h, new Vector3(0.13f, 0.12f, 0), new Vector3(0.03f, 0.07f, 0.04f), skin, Surface.Skin);
		B(h, new Vector3(0, 0.1f, -0.135f), new Vector3(0.04f, 0.06f, 0.04f), skin.Lightened(0.06f), Surface.Skin);  // nose
		B(h, new Vector3(0, 0.17f, -0.125f), new Vector3(0.25f, 0.1f, 0.02f), Dark, Surface.None, 0.1f);            // eyes in shadow
		B(h, new Vector3(0, 0.03f, -0.125f), new Vector3(0.09f, 0.025f, 0.02f), new Color(0.25f, 0.08f, 0.08f), Surface.None, 0.1f); // mouth
		if (o.Beard) B(h, new Vector3(0, 0.02f, -0.12f), new Vector3(0.22f, 0.1f, 0.05f), hair.Lerp(Dark, 0.3f));
		if (o.Glasses)
		{
			B(h, new Vector3(-0.065f, 0.17f, -0.138f), new Vector3(0.09f, 0.065f, 0.012f), Dark, Surface.None, 0f);
			B(h, new Vector3(0.065f, 0.17f, -0.138f), new Vector3(0.09f, 0.065f, 0.012f), Dark, Surface.None, 0f);
			B(h, new Vector3(0, 0.175f, -0.138f), new Vector3(0.04f, 0.015f, 0.012f), Dark, Surface.None, 0f);
		}
		if (o.Faceless) B(h, new Vector3(0, 0.12f, -0.14f), new Vector3(0.26f, 0.3f, 0.02f), Dark, Surface.None, 0.1f);   // nothing to remember
		if (o.Hat)
		{
			var hc = o.HatColor ?? Dark;
			B(h, new Vector3(0, 0.34f, 0), new Vector3(0.3f, 0.14f, 0.3f), hc, Surface.Fabric);
			B(h, new Vector3(0, 0.27f, 0), new Vector3(0.44f, 0.03f, 0.44f), hc, Surface.Fabric);
		}
		return parts;
	}
}
