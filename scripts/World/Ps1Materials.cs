using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>Saved 32×32 surface textures (nearest-filtered, world-space mapped by the shader).</summary>
public enum Surface { None, Wood, Fabric, Plaster, Metal, Floor, Skin }

/// <summary>Cached factory for PS1-shader materials (one per colour/emission/surface combo).</summary>
public static class Ps1Materials
{
	static Shader _shader;
	static Texture2D _grime;
	static readonly Dictionary<Surface, Texture2D> Surfaces = new();
	static readonly Dictionary<string, ShaderMaterial> Cache = new();
	public static void Release()
	{
		foreach (var material in Cache.Values) material.Dispose();
		Cache.Clear();
		foreach (var texture in Surfaces.Values) texture.Dispose();
		Surfaces.Clear();
		_grime?.Dispose(); _grime = null;
		_shader?.Dispose(); _shader = null;
	}

	public static ShaderMaterial Get(Color color, float emission = 0f, float grime = 0.35f, Surface surface = Surface.None, float surfStrength = 0.28f)
	{
		string key = $"{color.ToHtml()}|{emission}|{grime}|{surface}|{surfStrength}";
		if (Cache.TryGetValue(key, out var m)) return m;

		_shader ??= GD.Load<Shader>("res://shaders/ps1.gdshader");
		_grime ??= GD.Load<Texture2D>("res://assets/textures/grime.png");

		m = new ShaderMaterial { Shader = _shader };
		m.SetShaderParameter("albedo", color);
		m.SetShaderParameter("grime_tex", _grime);
		m.SetShaderParameter("grime_strength", grime);
		m.SetShaderParameter("emission_strength", emission);
		m.SetShaderParameter("surf_tex", GetSurface(surface));
		m.SetShaderParameter("surf_strength", surface == Surface.None ? 0f : surfStrength);
		m.SetShaderParameter("surf_scale", surface == Surface.Floor ? 2.4f : surface == Surface.Skin ? 4f : 1.4f);
		Cache[key] = m;
		return m;
	}

	static Texture2D GetSurface(Surface s)
	{
		if (Surfaces.TryGetValue(s, out var t)) return t;
		t = GD.Load<Texture2D>($"res://assets/textures/{s.ToString().ToLowerInvariant()}.png");
		Surfaces[s] = t;
		return t;
	}

	public static void AuthorTextures()
	{
		MakeGrime().GetImage().SavePng("res://assets/textures/grime.png");
		foreach (Surface surface in System.Enum.GetValues<Surface>())
			MakeSurface(surface).GetImage().SavePng($"res://assets/textures/{surface.ToString().ToLowerInvariant()}.png");
	}

	static ImageTexture MakeGrime()
	{
		const int size = 32;
		var rng = new RandomNumberGenerator { Seed = 1998 };
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);
		for (int y = 0; y < size; y++)
		for (int x = 0; x < size; x++)
		{
			float v = rng.RandfRange(0.55f, 1.0f);
			if (rng.Randf() < 0.04f) v *= 0.6f; // occasional dark speck
			img.SetPixel(x, y, new Color(v, v, v));
		}
		return ImageTexture.CreateFromImage(img);
	}

	static ImageTexture MakeSurface(Surface s)
	{
		const int size = 32;
		var rng = new RandomNumberGenerator { Seed = 3000 + (ulong)(int)s };
		var img = Image.CreateEmpty(size, size, false, Image.Format.Rgb8);

		var colShade = new float[size];
		var rowShade = new float[size];
		for (int i = 0; i < size; i++) { colShade[i] = rng.RandfRange(0.72f, 1.0f); rowShade[i] = rng.RandfRange(0.8f, 1.0f); }
		var tileShade = new float[16];
		for (int i = 0; i < 16; i++) tileShade[i] = rng.RandfRange(0.78f, 1.0f);

		for (int y = 0; y < size; y++)
		for (int x = 0; x < size; x++)
		{
			float v = 1f;
			float n = rng.RandfRange(-0.06f, 0.06f);
			switch (s)
			{
				case Surface.Wood:       // long grain with the odd dark knot
					v = colShade[x] * (0.92f + 0.08f * Mathf.Sin(y * 0.4f + x)) + n;
					if ((x - 20) * (x - 20) + (y - 12) * (y - 12) < 6) v *= 0.55f;
					break;
				case Surface.Fabric:     // coarse weave
					v = (((x + y) & 1) == 0 ? 1.0f : 0.78f) * (((x / 2 + y / 2) & 1) == 0 ? 1f : 0.9f) + n;
					break;
				case Surface.Plaster:    // blotchy stains
					v = 0.82f + 0.12f * Mathf.Sin(x * 0.55f) * Mathf.Cos(y * 0.43f) + n * 2f;
					if (rng.Randf() < 0.02f) v *= 0.7f;
					break;
				case Surface.Metal:      // brushed streaks + pitting
					v = rowShade[y] * 0.95f + n;
					if (rng.Randf() < 0.03f) v *= 0.55f;
					break;
				case Surface.Floor:      // 8-px lino tiles with dark grout
				{
					bool grout = (x % 8 == 0) || (y % 8 == 0);
					v = grout ? 0.45f : tileShade[(x / 8) + (y / 8) * 4] + n;
					break;
				}
				case Surface.Skin:       // slight blotch so faces are not flat
					v = 0.9f + n * 1.5f;
					break;
			}
			v = Mathf.Clamp(v, 0f, 1.1f);
			img.SetPixel(x, y, new Color(v, v, v));
		}
		return ImageTexture.CreateFromImage(img);
	}
}
