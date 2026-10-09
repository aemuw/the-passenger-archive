using Godot;

namespace PassengerArchive;

/// <summary>Door at either end of the carriage. The back door never opens: the train never goes back.</summary>
public partial class DoorInteractable : Interactable
{
	public bool Forward;
	public override string Prompt => Forward ? "[E] Двері вперед" : "[E] Двері назад";

	public override void Interact(Game game)
	{
		if (Forward) game.TryDoor();
		else game.Notify("Двері заблоковано. Назад не повернутися.");
	}
}
