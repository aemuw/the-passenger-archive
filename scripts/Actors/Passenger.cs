using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>
/// A person, seated or standing, built from boxes. Their speech comes from <see cref="PersonData.Talk"/>.
/// Tells (<see cref="Gesture"/>) repeat across carriages and memories. A carriage killer slowly turns
/// their head to follow the player — but only while the player is not looking at them.
/// </summary>
public partial class Passenger : Interactable
{
	public static bool Frozen;          // everyone freezes when the rule is broken

	public PersonData Data;
	public Game GameRef;
	public bool Standing;

	FigureParts _parts;
	readonly Dictionary<int, int> _idx = new();
	Player _player;
	float _yaw;
	float _t;
	float _habit = 3f;

	bool Offering => GameRef != null && GameRef.OfferTarget == Data.Id;

	public override string Prompt =>
		Data.Talk == null ? "" :
		Offering ? $"[E] Відмовитись — {Data.Name}" : $"[E] Заговорити — {Data.Name}";

	public override string AltPrompt => Offering ? "[F] Піти з ним" : null;

	public void Build()
	{
		_parts = FigureBuilder.Build(this, Data.Look, !Standing);
		if (Data.Talk != null)
		{
			if (Standing) SetBox(new Vector3(0.6f, 1.85f, 0.5f), new Vector3(0, 0.92f, 0));
			else SetBox(new Vector3(0.6f, 1.5f, 0.55f), new Vector3(0, 0.45f, -0.05f));
		}
	}

	public override void Interact(Game game)
	{
		if (Data.Talk == null) return;
		if (Offering) { game.DeclineOffer(Data.Name); return; }

		int si = Data.Talk.Length - 1;
		while (si > 0 && Data.Talk[si].Requires != null && !game.HasFlag(Data.Talk[si].Requires)) si--;
		var set = Data.Talk[si];

		_idx.TryGetValue(si, out int i);
		Line line = i < set.Lines.Length ? set.Lines[i] : (set.Loop ? set.Lines[i % set.Lines.Length] : set.Lines[^1]);
		_idx[si] = i + 1;
		game.Say(Data.Name, line);
	}

	public override void Alt(Game game) => game.ViolateRule();

	public override void _Process(double delta)
	{
		if (_parts == null || Frozen) return;
		float dt = (float)delta;
		_t += dt;
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;

		// Tells
		if (Data.Idle == Gesture.Tap)
		{
			_parts.RightArm.Rotation = new Vector3(-0.55f + Mathf.Sin(_t * 11f) * 0.12f, 0, 0);
		}
		else if (Data.Idle == Gesture.Sleeve)
		{
			_habit -= dt;
			if (_habit <= 0f)
			{
				_habit = GD.Randf() * 4f + 6f;
				var tw = CreateTween();
				tw.TweenProperty(_parts.RightArm, "rotation:x", -0.7f, 0.25f);
				tw.TweenProperty(_parts.RightArm, "rotation:x", -0.5f, 0.2f);
				tw.TweenProperty(_parts.RightArm, "rotation:x", -0.8f, 0.2f);
				tw.TweenProperty(_parts.RightArm, "rotation:x", 0.0f, 0.4f);
			}
		}

		// Head
		if (_player == null) return;
		Vector3 local = ToLocal(_player.GlobalPosition + new Vector3(0, 1.5f, 0)) - _parts.Head.Position;
		float dist = local.Length();
		Vector3 camFwd = -_player.Cam.GlobalTransform.Basis.Z;
		Vector3 fromCam = (_parts.Head.GlobalPosition - _player.Cam.GlobalPosition).Normalized();
		bool seen = camFwd.Dot(fromCam) > 0.6f;

		float target = 0f;
		float rate = 3f;
		float look = Mathf.Clamp(Mathf.Atan2(-local.X, -local.Z), -1.35f, 1.35f);
		if (Data.IsKiller)
		{
			if (!seen && dist < 9f) { target = look; rate = 1.1f; }   // watches you only when you are not watching
		}
		else if (Data.Talk != null && dist < 2.8f)
		{
			target = look;
			rate = 4f;
		}

		_yaw = Mathf.LerpAngle(_yaw, target, 1f - Mathf.Exp(-rate * dt));
		_parts.Head.Rotation = new Vector3(0, _yaw, 0);
	}
}
