using Godot;

namespace PassengerArchive;

/// <summary>Low-poly blood: flat boxes on the floor. Pools slowly spread. Used for deaths (see docs/DESIGN.md → Violence).</summary>
public static class Gore
{
	static readonly Color Blood = new(0.42f, 0.02f, 0.03f);

	/// <summary>A pool that spreads from a point on the floor.</summary>
	public static void Pool(Node3D parent, Vector3 floorPos, float radius = 1.1f, float seconds = 7f)
	{
		if (UserSettings.Gore <= 0f) return;
		var mi = SceneAssets.Load<MeshInstance3D>("res://scenes/effects/BloodPool.tscn");
		mi.Position = floorPos + new Vector3(0, 0.014f, 0);
		mi.Scale = new Vector3(0.06f, 1f, 0.06f);
		parent.AddChild(mi);
		var tw = mi.CreateTween();
		tw.TweenProperty(mi, "scale", new Vector3(radius * 2f, 1f, radius * 1.6f), seconds)
			.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
	}

	/// <summary>Drops and streaks scattered around a point on the floor.</summary>
	public static void Spatter(Node3D parent, Vector3 floorPos, int count, float spread = 1.4f, int seed = 3)
	{
		count = (int)(count * UserSettings.Gore);
		var rng = new RandomNumberGenerator { Seed = (ulong)seed };
		for (int i = 0; i < count; i++)
		{
			float w = rng.RandfRange(0.05f, 0.28f);
			var pos = floorPos + new Vector3(rng.RandfRange(-spread, spread), 0.016f + i * 0.0004f, rng.RandfRange(-spread, spread));
			var mi = SceneAssets.Load<MeshInstance3D>("res://scenes/effects/BloodPool.tscn");
			mi.Position = pos;
			mi.Scale = new Vector3(w, 1f, w * rng.RandfRange(0.5f, 2.2f));
			mi.Rotation = new Vector3(0, rng.RandfRange(0f, Mathf.Pi), 0);
			parent.AddChild(mi);
		}
	}
}
