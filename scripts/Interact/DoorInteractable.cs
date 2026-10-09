using Godot;

namespace PassengerArchive;

/// <summary>Door at either end of the carriage. Front leads on once the carriage is solved.</summary>
public partial class DoorInteractable : Interactable
{
	public bool Forward;
	public override string Prompt => Forward ? "[E] Двері вперед" : "[E] Двері назад";

	public override void Interact(Game game)
	{
		if (Forward) game.TryAdvance();
		else game.Notify("Двері не піддаються. Назад не повернутися.");
	}
}
