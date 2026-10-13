using Godot;

namespace PassengerArchive;

/// <summary>A strap/curtain that sways with the train.</summary>
public partial class Swayer : Node3D
{
	[Export] public float Amp { get; set; } = 0.05f;
	[Export] public float Speed { get; set; } = 1.2f;
	[Export] public float Phase { get; set; }
	public override void _Process(double delta)
	{
		Rotation = new Vector3(Mathf.Sin(Time.GetTicksMsec() * 0.001f * Speed + Phase) * Amp * 0.4f, 0, Mathf.Sin(Time.GetTicksMsec() * 0.001f * Speed * 0.7f + Phase) * Amp);
	}
}


