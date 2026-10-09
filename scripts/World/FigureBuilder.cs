using Godot;

namespace PassengerArchive;

public sealed class FigureParts
{
	public Node3D Head;
	public Node3D RightArm;
}

/// <summary>
/// Builds a low-poly human from boxes, either seated (origin = top of the seat cushion)
/// or standing (origin = floor). Faces are half in shadow by design.
/// </summary>
public static class FigureBuilder
{
	static readonly Color Trousers = new(0.1f, 0.1f, 0.12f);
	static readonly Color Skin = new(0.55f, 0.43f, 0.36f);
	static readonly Color Dark = new(0.04f, 0.03f, 0.03f);

	public static FigureParts Build(Node3D root, Outfit o, bool seated)
	{
		var parts = new FigureParts();
		float headY;

		if (seated)
		{
			root.AddChild(Geo.MeshBox(new Vector3(0, 0.09f, -0.18f), new Vector3(0.42f, 0.16f, 0.46f), Trousers, 0, 0.25f));
			root.AddChild(Geo.MeshBox(new Vector3(0, -0.28f, -0.4f), new Vector3(0.38f, 0.55f, 0.16f), Trousers, 0, 0.25f));
			root.AddChild(Geo.MeshBox(new Vector3(0, 0.45f, 0.02f), new Vector3(0.5f, 0.62f, 0.28f), o.Coat, 0, 0.25f));
			root.AddChild(Geo.MeshBox(new Vector3(-0.31f, 0.42f, -0.06f), new Vector3(0.12f, 0.5f, 0.14f), o.Coat, 0, 0.25f));
			parts.RightArm = new Node3D { Position = new Vector3(0.31f, 0.68f, -0.06f) };
			root.AddChild(parts.RightArm);
			parts.RightArm.AddChild(Geo.MeshBox(new Vector3(0, -0.26f, 0), new Vector3(0.12f, 0.5f, 0.14f), o.Coat, 0, 0.25f));
			if (o.LongCoat) root.AddChild(Geo.MeshBox(new Vector3(0, 0.03f, -0.18f), new Vector3(0.54f, 0.1f, 0.5f), o.Coat, 0, 0.25f));
			if (o.Paper) root.AddChild(Geo.MeshBox(new Vector3(0, 0.55f, -0.3f), new Vector3(0.4f, 0.32f, 0.02f), new Color(0.78f, 0.75f, 0.66f), 0, 0.1f));
			if (o.Flowers)
			{
				root.AddChild(Geo.MeshBox(new Vector3(0.05f, 0.22f, -0.22f), new Vector3(0.2f, 0.08f, 0.38f), new Color(0.8f, 0.78f, 0.74f), 0, 0.1f));
				root.AddChild(Geo.MeshBox(new Vector3(0.05f, 0.28f, -0.34f), new Vector3(0.18f, 0.06f, 0.14f), new Color(0.7f, 0.1f, 0.12f), 0, 0.1f));
			}
			headY = 0.9f;
		}
		else
		{
			root.AddChild(Geo.MeshBox(new Vector3(-0.11f, 0.4f, 0), new Vector3(0.17f, 0.8f, 0.19f), Trousers, 0, 0.25f));
			root.AddChild(Geo.MeshBox(new Vector3(0.11f, 0.4f, 0), new Vector3(0.17f, 0.8f, 0.19f), Trousers, 0, 0.25f));
			root.AddChild(Geo.MeshBox(new Vector3(0, 1.11f, 0), new Vector3(0.5f, 0.62f, 0.28f), o.Coat, 0, 0.25f));
			root.AddChild(Geo.MeshBox(new Vector3(-0.31f, 1.08f, 0), new Vector3(0.12f, 0.56f, 0.14f), o.Coat, 0, 0.25f));
			parts.RightArm = new Node3D { Position = new Vector3(0.31f, 1.38f, 0) };
			root.AddChild(parts.RightArm);
			parts.RightArm.AddChild(Geo.MeshBox(new Vector3(0, -0.28f, 0), new Vector3(0.12f, 0.56f, 0.14f), o.Coat, 0, 0.25f));
			if (o.LongCoat) root.AddChild(Geo.MeshBox(new Vector3(0, 0.55f, 0), new Vector3(0.54f, 0.62f, 0.3f), o.Coat, 0, 0.25f));
			if (o.Paper) root.AddChild(Geo.MeshBox(new Vector3(0, 1.2f, -0.3f), new Vector3(0.4f, 0.32f, 0.02f), new Color(0.78f, 0.75f, 0.66f), 0, 0.1f));
			headY = 1.55f;
		}

		float neckY = seated ? 0.74f : 1.4f;
		if (o.Collar) root.AddChild(Geo.MeshBox(new Vector3(0, neckY + 0.02f, 0.02f), new Vector3(0.36f, 0.14f, 0.32f), o.Coat.Darkened(0.2f), 0, 0.25f));
		if (o.Scarf) root.AddChild(Geo.MeshBox(new Vector3(0, neckY, -0.02f), new Vector3(0.3f, 0.1f, 0.3f), o.ScarfColor ?? new Color(0.6f, 0.08f, 0.08f), 0, 0.2f));

		parts.Head = new Node3D { Position = new Vector3(0, headY, 0.02f) };
		root.AddChild(parts.Head);
		var h = parts.Head;
		h.AddChild(Geo.MeshBox(new Vector3(0, -0.05f, 0), new Vector3(0.1f, 0.1f, 0.1f), Skin, 0, 0.25f));
		h.AddChild(Geo.MeshBox(new Vector3(0, 0.12f, 0), new Vector3(0.24f, 0.28f, 0.24f), Skin, 0, 0.25f));
		h.AddChild(Geo.MeshBox(new Vector3(0, 0.28f, 0.01f), new Vector3(0.26f, 0.07f, 0.26f), new Color(0.12f, 0.09f, 0.07f), 0, 0.25f));
		h.AddChild(Geo.MeshBox(new Vector3(0, 0.17f, -0.125f), new Vector3(0.25f, 0.1f, 0.02f), Dark, 0, 0.1f));          // eyes in shadow
		h.AddChild(Geo.MeshBox(new Vector3(0, 0.03f, -0.125f), new Vector3(0.09f, 0.025f, 0.02f), new Color(0.25f, 0.08f, 0.08f), 0, 0.1f)); // mouth
		if (o.Hat)
		{
			var hc = o.HatColor ?? Dark;
			h.AddChild(Geo.MeshBox(new Vector3(0, 0.34f, 0), new Vector3(0.3f, 0.14f, 0.3f), hc, 0, 0.25f));
			h.AddChild(Geo.MeshBox(new Vector3(0, 0.27f, 0), new Vector3(0.44f, 0.03f, 0.44f), hc, 0, 0.25f));
		}
		return parts;
	}
}
