using Godot;

namespace PassengerArchive;

/// <summary>A dirty fluorescent lamp: hum-flicker, occasional dropout; can be switched off and (unless permanently dead) back on.</summary>
public partial class FlickerLight : OmniLight3D
{
	[Export] public float BaseEnergy { get; set; } = 1.1f;
	[Export] public MeshInstance3D Fixture { get; set; }
	[Export] public MeshInstance3D Glow { get; set; }
	[Export] public bool Dead { get; set; }
	[Export] public bool Permanent { get; set; }

	readonly RandomNumberGenerator _rng = new();
	Material _fixtureMat;
	float _timer;
	float _dropout;

	public override void _Ready()
	{
		AddToGroup("lamps");
		_rng.Randomize();
		_timer = _rng.RandfRange(0.5f, 4f);
		_fixtureMat = Fixture?.MaterialOverride;
	}

	public override void _Process(double delta)
	{
		if (Dead) { LightEnergy = 0f; if (Glow != null) Glow.Visible = false; return; }

		float dt = (float)delta;
		_timer -= dt;
		if (_timer <= 0f)
		{
			_dropout = _rng.RandfRange(0.04f, 0.18f);
			_timer = _rng.RandfRange(2f, 9f);
		}

		float e = BaseEnergy * (0.97f + 0.03f * UserSettings.Effects * Mathf.Sin(Time.GetTicksMsec() * 0.045f));
		if (_dropout > 0f) { _dropout -= dt; e *= Mathf.Lerp(1f,0.15f,UserSettings.Effects); }
		LightEnergy = e;
		if (Glow != null) Glow.Visible = e > BaseEnergy * 0.4f;
	}

	public void Extinguish()
	{
		Dead = true;
		LightEnergy = 0f;
		if (Fixture != null) Fixture.MaterialOverride = Ps1Materials.Get(new Color(0.15f, 0.15f, 0.12f));
	}

	public void Revive()
	{
		if (Permanent) return;
		Dead = false;
		if (Fixture != null && _fixtureMat != null) Fixture.MaterialOverride = _fixtureMat;
	}
}
