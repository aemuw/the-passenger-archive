using Godot;

namespace PassengerArchive;

/// <summary>
/// One spoken line. <see cref="Fx"/> triggers game effects (several separated by ';', see Game.ApplyFx).
/// <see cref="Speaker"/>: a name, or "@id" = the current display name of that person (alias until they are known).
/// </summary>
public sealed record Line(string Text, string Fx = null, string Speaker = null);

/// <summary>How the story of a carriage relates to reality — shown on the card at the carriage end.</summary>
public enum StoryKind { Fiction, Inspired, Documented, Nightmare, Hero }

/// <summary>Idle "tell" of a character. Repeated tells are how the player recognises people across carriages and memories.</summary>
public enum Gesture { None, Sleeve, Tap }

public enum RuleKind { None, PhoneRing, OfferGuide }

public sealed record Outfit(
	Color Coat,
	bool Hat = false,
	Color? HatColor = null,
	bool Scarf = false,
	Color? ScarfColor = null,
	bool LongCoat = false,
	bool Collar = false,
	bool Paper = false,
	bool Flowers = false,
	bool Faceless = false);   // a face nobody ever saw (used in reconstructed memories)

/// <summary>
/// A person on the train (or in a memory). Everyone is "Незнайомець / Незнайомка …" (<see cref="Alias"/>, a description
/// of what the hero sees) until a conversation reveals the real <see cref="Name"/>. Name == null: never revealed.
/// </summary>
public sealed record PersonData(
	string Id,
	string Name,
	string Alias,
	int Row, int Side, int Slot,
	Outfit Look,
	Convo Dialog = null,             // null = background passenger, cannot be spoken to
	Gesture Idle = Gesture.None,
	Vector3? StandAt = null,         // set = standing at this spot instead of sitting
	float StandYaw = 0f,
	bool IsKiller = false,           // killer of THIS carriage's victim
	bool IsHeroKiller = false);      // the hero's own killer (the "Попутник")

/// <summary>The hidden rule of a carriage. It is never shown to the player — it is learned from the victim's story.</summary>
public sealed record RuleDef(
	RuleKind Kind,
	string Reason,
	string VictimId,
	string VictimName,      // instrumental case, e.g. "Віктором"
	string Method,
	string KillText,
	string KillerId);

public sealed record CarriageData(
	string Title,
	StoryKind Kind,
	string CardNote,
	string RealWorld,
	PersonData[] People,
	bool StuckWorld = false,
	int[] DeadLamps = null,
	Line[] Overheard = null,         // auto-plays when the player comes near OverheardAt
	Vector3 OverheardAt = default,
	RuleDef Rule = null,
	bool DoorStuck = false);         // the door sticks until the man with the newspaper helps (his node "door")
