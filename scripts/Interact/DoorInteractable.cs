using Godot;

namespace PassengerArchive;

/// <summary>
/// Door at either end of the carriage. The back door never opens: the train never goes back.
/// The front door has a small lamp over it: red = locked, green = the story here is finished and you may go on.
/// </summary>
public partial class DoorInteractable : Interactable
{
	public bool Forward;
	MeshInstance3D _lamp;

	public override string Prompt => "[E] Двері";

	public void AddLamp(bool unlocked)
	{
		_lamp = Geo.MeshBox(new Vector3(0, 1.12f, 0.05f), new Vector3(0.14f, 0.14f, 0.04f), Colors.Red, 1.4f, 0f);
		AddChild(_lamp);
		SetUnlocked(unlocked);
	}

	public void SetUnlocked(bool on)
	{
		if (_lamp == null) return;
		_lamp.MaterialOverride = on
			? Ps1Materials.Get(new Color(0.35f, 1f, 0.45f), 1.6f, 0f)
			: Ps1Materials.Get(new Color(0.9f, 0.1f, 0.08f), 1.2f, 0f);
	}

	public override void Interact(Game game)
	{
		if (Forward) game.TryDoor();
		else game.Notify("Двері заблоковано. Назад не повернутися.");
	}
}
