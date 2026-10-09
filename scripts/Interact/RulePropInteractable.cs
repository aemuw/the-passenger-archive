using Godot;

namespace PassengerArchive;

/// <summary>The object the carriage rule forbids touching. Touching it summons the killer.</summary>
public partial class RulePropInteractable : Interactable
{
	public string Label = "Об'єкт";
	public override string Prompt => $"[E] {Label}";

	public override void Interact(Game game) => game.ViolateRule();
}
