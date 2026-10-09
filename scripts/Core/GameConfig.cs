namespace PassengerArchive;

/// <summary>Central tweakables for the prototype.</summary>
public static class GameConfig
{
	/// <summary>Window size is divided by this to get the internal (PS1-like) render resolution.</summary>
	public const int RenderShrink = 3;

	public const float PlayerSpeed = 2.6f;
	public const float MouseSensitivity = 0.0022f;
	public const float InteractDistance = 2.4f;
	public const float Fov = 75f;

	/// <summary>If true, accusing the wrong passenger is punished like breaking the carriage rule.</summary>
	public const bool WrongAccusationKills = true;

	// Collision layers
	public const uint LayerWorld = 1;
	public const uint LayerInteract = 2;
	public const uint LayerPlayer = 4;
}
