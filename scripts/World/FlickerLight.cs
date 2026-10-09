using Godot;

namespace PassengerArchive;

/// <summary>A dirty fluorescent lamp: slow hum-flicker, occasional dropout, can be killed.</summary>
public partial class FlickerLight : OmniLight3D
{
	public float BaseEnergy = 1.1f;
	public MeshInstance3D Fixture;
	public bool Dead;

	readonly RandomNumberGenerator _rng = new();
	float _timer;
	float _dropout;

	public override void _Ready()
	{
		AddToGroup("lamps");
		_rng.Randomize();
		_timer = _rng.RandfRange(0.5f, 4f);
	}

	public override void _Process(double delta)
	{
		if (Dead) { LightEnergy = 0f; return; }

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
	}

	public void Extinguish()
	{
		Dead = true;
		LightEnergy = 0f;
		if (Fixture != null) Fixture.MaterialOverride = Ps1Materials.Get(new Color(0.15f, 0.15f, 0.12f));
	}
}
