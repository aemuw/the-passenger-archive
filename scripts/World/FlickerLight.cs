using Godot;

namespace PassengerArchive;

/// <summary>A dirty fluorescent lamp: hum-flicker, occasional dropout; can be switched off and (unless permanently dead) back on.</summary>
public partial class FlickerLight : OmniLight3D
{
	public float BaseEnergy = 1.1f;
	public MeshInstance3D Fixture;
	public MeshInstance3D Glow;     // haze around the tube; hidden when the lamp is out
	public bool Dead;
	public bool Permanent;      // permanently broken lamp: Revive() does nothing

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

		float e = BaseEnergy * (0.94f + 0.06f * Mathf.Sin(Time.GetTicksMsec() * 0.045f));
		if (_dropout > 0f) { _dropout -= dt; e *= 0.15f; }
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
