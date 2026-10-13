using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Godot;

namespace PassengerArchive;

/// <summary>Loads authored assets. Missing assets are errors, never an invisible procedural fallback.</summary>
public static class SceneAssets
{
	public static bool Baking;
	public static T Load<T>(string path) where T : Node
	{
		using var scene = GD.Load<PackedScene>(path);
		if (scene == null) throw new InvalidOperationException($"Missing scene: {path}");
		return scene.Instantiate<T>();
	}
	public static string FigurePath(Outfit look, bool seated)
	{
		// Stable across machines/locales; scene filenames never depend on string.GetHashCode().
		var old = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
			var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(look.ToString())))[..12].ToLowerInvariant();
			return $"res://scenes/characters/{hash}_{(seated ? "seated" : "standing")}.tscn";
		}
		finally { CultureInfo.CurrentCulture = old; }
	}
	public static FigureParts BindFigure(Node root) => new()
	{
		Upper = root.GetNode<Node3D>("Upper"),
		Head = root.GetNode<Node3D>("Upper/Head"),
		RightArm = root.GetNode<Node3D>("Upper/RightArm"),
		LeftArm = root.GetNode<Node3D>("Upper/LeftArm"),
	};
}
