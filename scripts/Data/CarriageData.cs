using Godot;

namespace PassengerArchive;

/// <summary>One spoken line. If <see cref="Clue"/> is set, hearing it adds a note to the notebook.</summary>
public sealed record Line(string Text, string Clue = null);

public enum RuleProp { Suitcase, Window }

public sealed record PassengerData(
	string Id,
	string Name,
	int Row, int Side, int Slot,
	Color Coat,
	bool IsKiller,
	Line[] Lines,
	bool Hat = false,
	Color? HatColor = null,
	bool Scarf = false,
	bool SleeveHabit = false,
	bool Puddle = false);

public sealed record CarriageData(
	string Title,
	string Rule,
	string VictimName,
	string VictimMethod,
	string KillDescription,
	int VictimRow, int VictimSide, int VictimSlot,
	Color VictimCoat,
	bool VictimScarf,
	bool VictimWet,
	Line[] VictimExamine,
	RuleProp Prop,
	Vector3 PropPos,
	bool StuckWorld,
	int[] DeadLamps,
	int MinClues,
	PassengerData[] Passengers);
