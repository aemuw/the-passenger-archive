using Godot;

namespace PassengerArchive;

/// <summary>The carriage's victim. Examining gives clues about how they died.</summary>
public partial class VictimBody : Interactable
{
	public Line[] Lines;
	int _next;

	public override string Prompt => "[E] Оглянути тіло";

	public override void Interact(Game game)
	{
		var line = Lines[Mathf.Min(_next, Lines.Length - 1)];
		_next++;
		game.Say("—", line);
	}
}
