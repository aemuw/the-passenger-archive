using Godot;

namespace PassengerArchive;

/// <summary>
/// A seated passenger built from boxes. Talking cycles through their lines.
/// The killer slowly turns their head to follow the player — but only while the player isn't looking.
/// </summary>
public partial class Passenger : Interactable
{
	public PassengerData Data;
	public Game GameRef;

	Node3D _head;
	Node3D _rightArm;
	Player _player;
	int _next;
	float _yaw;
	float _habitTimer = 4f;

	public override string Prompt => $"[E] Заговорити — {Data.Name}";
	public override string AccusePrompt => GameRef.CanAccuse
		? "[F] Звинуватити"
		: "[F] Звинуватити (доказів замало)";

	public void Build()
	{
		var coat = Data.Coat;
		var trousers = new Color(0.1f, 0.1f, 0.12f);
		var skin = new Color(0.55f, 0.43f, 0.36f);
		var dark = new Color(0.04f, 0.03f, 0.03f);

		Part(new Vector3(0, 0.09f, -0.18f), new Vector3(0.42f, 0.16f, 0.46f), trousers);      // thighs
		Part(new Vector3(0, -0.28f, -0.4f), new Vector3(0.38f, 0.55f, 0.16f), trousers);      // shins
		Part(new Vector3(0, 0.45f, 0.02f), new Vector3(0.5f, 0.62f, 0.28f), coat);            // torso
		Part(new Vector3(-0.31f, 0.42f, -0.06f), new Vector3(0.12f, 0.5f, 0.14f), coat);      // left arm

		_rightArm = new Node3D { Position = new Vector3(0.31f, 0.68f, -0.06f) };              // pivot at shoulder
		AddChild(_rightArm);
		_rightArm.AddChild(Mesh(new Vector3(0, -0.26f, 0), new Vector3(0.12f, 0.5f, 0.14f), coat));

		if (Data.Scarf) Part(new Vector3(0, 0.74f, -0.02f), new Vector3(0.3f, 0.1f, 0.3f), new Color(0.6f, 0.08f, 0.08f));

		_head = new Node3D { Position = new Vector3(0, 0.9f, 0.02f) };
		AddChild(_head);
		_head.AddChild(Mesh(new Vector3(0, -0.05f, 0), new Vector3(0.1f, 0.1f, 0.1f), skin));            // neck
		_head.AddChild(Mesh(new Vector3(0, 0.12f, 0), new Vector3(0.24f, 0.28f, 0.24f), skin));          // head
		_head.AddChild(Mesh(new Vector3(0, 0.28f, 0.01f), new Vector3(0.26f, 0.07f, 0.26f), new Color(0.12f, 0.09f, 0.07f))); // hair
		_head.AddChild(Mesh(new Vector3(0, 0.17f, -0.125f), new Vector3(0.25f, 0.1f, 0.02f), dark));     // eyes in shadow
		_head.AddChild(Mesh(new Vector3(0, 0.03f, -0.125f), new Vector3(0.09f, 0.025f, 0.02f), new Color(0.25f, 0.08f, 0.08f))); // mouth

		if (Data.Hat)
		{
			var hc = Data.HatColor ?? dark;
			_head.AddChild(Mesh(new Vector3(0, 0.34f, 0), new Vector3(0.3f, 0.14f, 0.3f), hc));          // crown
			_head.AddChild(Mesh(new Vector3(0, 0.27f, 0), new Vector3(0.44f, 0.03f, 0.44f), hc));        // brim
		}

		SetBox(new Vector3(0.6f, 1.5f, 0.55f), new Vector3(0, 0.45f, -0.05f));
	}

	public override void Interact(Game game)
	{
		var line = Data.Lines[Mathf.Min(_next, Data.Lines.Length - 1)];
		_next++;
		game.Say(Data.Name, line);
	}

	public override void Accuse(Game game) => game.Accuse(Data);

	public override void _Process(double delta)
	{
		if (_head == null) return;
		float dt = (float)delta;
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;
		if (_player == null) return;

		Vector3 to = _player.GlobalPosition + new Vector3(0, 1.5f, 0) - _head.GlobalPosition;
		float dist = to.Length();
		Vector3 camFwd = -_player.Cam.GlobalTransform.Basis.Z;
		Vector3 fromCam = (_head.GlobalPosition - _player.Cam.GlobalPosition).Normalized();
		bool seen = camFwd.Dot(fromCam) > 0.6f;

		float target = 0f;
		float rate = 3f;
		if (Data.IsKiller)
		{
			// Watches you only when you are not watching.
			if (!seen && dist < 9f) { target = Mathf.Clamp(Mathf.Atan2(-to.X, -to.Z), -1.35f, 1.35f); rate = 1.1f; }
		}
		else if (dist < 2.6f)
		{
			target = Mathf.Clamp(Mathf.Atan2(-to.X, -to.Z), -1.2f, 1.2f);
			rate = 4f;
		}

		_yaw = Mathf.LerpAngle(_yaw, target, 1f - Mathf.Exp(-rate * dt));
		_head.Rotation = new Vector3(0, _yaw, 0);

		// Small habit: adjusts a sleeve now and then.
		if (Data.SleeveHabit)
		{
			_habitTimer -= dt;
			if (_habitTimer <= 0f)
			{
				_habitTimer = GD.Randf() * 4f + 6f;
				var tw = CreateTween();
				tw.TweenProperty(_rightArm, "rotation:x", -0.7f, 0.25f);
				tw.TweenProperty(_rightArm, "rotation:x", -0.5f, 0.2f);
				tw.TweenProperty(_rightArm, "rotation:x", -0.8f, 0.2f);
				tw.TweenProperty(_rightArm, "rotation:x", 0.0f, 0.4f);
			}
		}
	}

	void Part(Vector3 pos, Vector3 size, Color c) => AddChild(Mesh(pos, size, c));

	static MeshInstance3D Mesh(Vector3 pos, Vector3 size, Color c) => new()
	{
		Mesh = new BoxMesh { Size = size },
		Position = pos,
		MaterialOverride = Ps1Materials.Get(c, 0f, 0.25f),
	};
}
