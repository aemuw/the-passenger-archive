using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>Cached factory for PS1-shader materials (one per colour/emission combo).</summary>
public static class Ps1Materials
{
	static Shader _shader;
	static ImageTexture _grime;
	static readonly Dictionary<string, ShaderMaterial> Cache = new();

	public static ShaderMaterial Get(Color color, float emission = 0f, float grime = 0.35f)
	{
		string key = $"{color.ToHtml()}|{emission}|{grime}";
		if (Cache.TryGetValue(key, out var m)) return m;

		_shader ??= GD.Load<Shader>("res://shaders/ps1.gdshader");
		_grime ??= MakeGrime();

		m = new ShaderMaterial { Shader = _shader };
		m.SetShaderParameter("albedo", color);
		m.SetShaderParameter("grime_tex", _grime);
		m.SetShaderParameter("grime_strength", grime);
		m.SetShaderParameter("emission_strength", emission);
		Cache[key] = m;
		return m;
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
}
