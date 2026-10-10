namespace PassengerArchive;

/// <summary>
/// One thing the hero can say. Picking it sets flags, may fire effects, and jumps to node <see cref="To"/> (null = end of talk).
/// Flags: "x" lives until the carriage ends; "k:x" is kept for the whole run (it can change what people say much later).
/// <see cref="Need"/> = all of these flags must be set, <see cref="Unless"/> = none of these may be set (comma separated).
/// </summary>
public sealed record Opt(string Text, string To = null, string Need = null, string Unless = null, string Set = null, string Fx = null);

/// <summary>
/// A beat of conversation. Lines play in order (E advances). Then: options, or the conditional jump
/// (<see cref="IfNeed"/> → <see cref="IfTo"/>), or <see cref="Next"/>, or the talk ends.
/// An empty <see cref="Say"/> is allowed — useful for "hub" nodes that only show options.
/// </summary>
public sealed record DNode(
	string Id,
	Line[] Say,
	Opt[] Opts = null,
	string Next = null,
	string Set = null,
	string Fx = null,
	string IfNeed = null,
	string IfTo = null);

/// <summary>Which node a conversation starts at. The LAST entry whose conditions hold wins.</summary>
public sealed record Entry(string Node, string Need = null, string Unless = null);

public sealed record Convo(Entry[] Entries, DNode[] Nodes)
{
	public DNode Find(string id)
	{
		foreach (var n in Nodes) if (n.Id == id) return n;
		return null;
	}
}
