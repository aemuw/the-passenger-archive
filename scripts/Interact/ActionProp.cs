using System;
using Godot;

namespace PassengerArchive;

/// <summary>A prop whose prompt and action are supplied by the code that creates it.</summary>
public partial class ActionProp : Interactable
{
	public Func<string> PromptFn;
	public Action<Game> OnUse;

	public override string Prompt => PromptFn?.Invoke() ?? "";
	public override void Interact(Game game) => OnUse?.Invoke(game);
}
