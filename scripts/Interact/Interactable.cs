using Godot;

namespace PassengerArchive;

/// <summary>Base class for anything the player can look at and press [E] on.</summary>
public partial class Interactable : StaticBody3D
{
	public virtual string Prompt => "[E] Взаємодіяти";
	public virtual string AccusePrompt => null;

	public virtual void Interact(Game game) { }
	public virtual void Accuse(Game game) { }

	/// <summary>Adds a box collider on the interaction layer.</summary>
	public void SetBox(Vector3 size, Vector3 center)
	{
		CollisionLayer = GameConfig.LayerInteract;
		CollisionMask = 0;
		AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size }, Position = center });
	}
}
