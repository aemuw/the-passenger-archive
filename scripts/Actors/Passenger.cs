using Godot;

namespace PassengerArchive;

/// <summary>
/// A person, seated or standing, built from boxes. Their speech comes from <see cref="PersonData.Dialog"/>.
/// They breathe, glance out of the window, fidget, sleep; when they speak they nod and gesture; when you linger
/// near them they mutter (<see cref="PersonData.Barks"/>). Tells (<see cref="Gesture"/>) repeat across carriages and memories.
/// A carriage killer slowly turns their head to follow the player — but only while the player is not looking at them.
/// </summary>
public partial class Passenger : Interactable
{
	public static bool Frozen;          // everyone freezes when the rule is broken

	public PersonData Data;
	public Game GameRef;
	[Export] public bool Standing { get; set; }
	[Export] public string PersonId { get; set; } = "";

	FigureParts _parts;
	AnimationPlayer _animation;
	bool _attacking;
	Player _player;
	float _yaw;
	float _t;
	float _habit = 3f;
	float _phase;
	float _winT = 5f;
	bool _winLook;
	float _fidT = 4f, _fid;
	float _sleeveK = -1f;
	float _near, _barkCool = 6f;
	int _barkI;
	float _reactionTime;
	Vector3 _reactionPoint;
	public void ReactTo(Vector3 point, float seconds) { _reactionPoint=point; _reactionTime=seconds; }

	/// <summary>"Незнайомець …" until a conversation reveals the real name.</summary>
	public string DisplayName => Game.DisplayOf(Data, GameRef);

	public override string Prompt => Data.Dialog == null ? "" : $"{DisplayName}\n[E] Заговорити";

	public Vector3 HeadWorld => _parts != null ? _parts.Head.GlobalPosition : GlobalPosition + new Vector3(0, 1.4f, 0);

	public void Build()
	{
		_phase = GD.Randf() * 6.28f;
		if (HasNode("Upper")) _parts = SceneAssets.BindFigure(this);
		else if (SceneAssets.Baking) _parts = FigureBuilder.Build(this, Data.Look, !Standing);
		else
		{
			var model = SceneAssets.Load<Node3D>(SceneAssets.FigurePath(Data.Look, !Standing));
			model.Name = "Visual";
			AddChild(model);
			_parts = SceneAssets.BindFigure(model);
		}
		_animation = GetNodeOrNull<AnimationPlayer>("Animations") ?? GetNodeOrNull<AnimationPlayer>("Visual/Animations");
		if (SceneAssets.Baking)
		{
			if (!HasNode("InteractionShape"))
			{
				if (Standing) SetBox(new Vector3(0.5f, 1.8f, 0.4f), new Vector3(0, 0.9f, 0));
				else SetBox(new Vector3(0.5f, 1.4f, 0.45f), new Vector3(0, 0.45f, -0.05f));
			}
			CollisionLayer = GameConfig.LayerWorld | GameConfig.LayerInteract;
		}
	}

	public override void Interact(Game game)
	{
		if (Data.Dialog == null) return;
		game.StartTalk(Data.Dialog, Data.Id, () => DisplayName, HeadWorld);
	}
	public void BeginAttack(string method)
	{
		_attacking = true;
		string weapon = method.Contains("нож") ? "Knife" : "BluntProp";
		var prop = SceneAssets.Load<Node3D>($"res://scenes/props/{weapon}.tscn");
		prop.Position = new Vector3(0, -0.62f, -0.04f);
		_parts.RightArm.AddChild(prop);
		_animation?.Play("attack");
	}

	public override void _Process(double delta)
	{
		if (_parts == null) return;
		if (_attacking) return;
		if (Frozen) { _animation?.Pause(); return; }
		float dt = (float)delta;
		_t += dt;
		_player ??= GetTree().GetFirstNodeInGroup("player") as Player;

		bool asleep = Data.Idle == Gesture.Sleep;
		bool talking = GameRef != null && GameRef.NpcSpeakingNow(Data.Id);
		string clip = talking ? "talk" : Data.Idle == Gesture.Sleeve && Mathf.PosMod(_t + _phase, 8f) < 1.5f ? "sleeve" : "idle";
		if (_animation != null && (_animation.CurrentAnimation != clip || !_animation.IsPlaying())) _animation.Play(clip);

		// ── breathing ──
		float breath = Mathf.Sin(_t * (asleep ? 1.1f : 1.6f) + _phase) * (asleep ? 0.009f : 0.005f);
		if (_animation == null) _parts.Upper.Position = new Vector3(0, breath, 0);

		// ── arms / head pose from the idle gesture ──
		float rArm = 0f, lArm = 0f, pitch = 0f;

		if (Data.Idle == Gesture.Tap)
		{
			rArm = -0.55f + Mathf.Sin(_t * 11f) * 0.12f;
		}
		else if (Data.Idle == Gesture.Sleeve)
		{
			_habit -= dt;
			if (_habit <= 0f && _sleeveK < 0f) { _sleeveK = 0f; _habit = GD.Randf() * 4f + 6f; }
			if (_sleeveK >= 0f)
			{
				_sleeveK += dt / 1.05f;
				float k = _sleeveK;
				rArm = -(0.35f + 0.4f * Mathf.Sin(k * Mathf.Pi)) + Mathf.Sin(k * Mathf.Tau * 3f) * 0.08f;
				if (k >= 1f) _sleeveK = -1f;
			}
		}
		else if (Data.Idle == Gesture.Fidget)
		{
			_fidT -= dt;
			if (_fidT <= 0f) { _fid = 1.4f; _fidT = GD.Randf() * 5f + 3f; }
			if (_fid > 0f)
			{
				_fid -= dt;
				lArm = -0.35f - Mathf.Sin(_fid * 9f) * 0.12f;
				rArm = -0.25f + Mathf.Sin(_fid * 7f) * 0.1f;
			}
		}
		else if (asleep) pitch = 0.55f;

		if (talking)
		{
			pitch += Mathf.Sin(_t * 9f) * 0.045f;
			rArm = -0.4f + Mathf.Sin(_t * 5.3f) * 0.22f;
			lArm = -0.2f + Mathf.Sin(_t * 4.1f + 1f) * 0.12f;
		}

		if (_animation == null)
		{
			_parts.RightArm.Rotation = new Vector3(rArm, 0, 0);
			_parts.LeftArm.Rotation = new Vector3(lArm, 0, 0);
		}

		// ── barks: they mutter when you stand close and say nothing ──
		if (Data.Barks != null && Data.Barks.Length > 0 && _player != null && GameRef != null)
		{
			_barkCool -= dt;
			bool close = GlobalPosition.DistanceTo(_player.GlobalPosition) < 2.4f;
			if (close && !talking && !GameRef.InTalk && GameRef.BarksAllowed) _near += dt; else _near = 0f;
			if (_near > 5f && _barkCool <= 0f)
			{
				_near = 0f;
				_barkCool = 28f;
				string who = Data.Dialog != null ? DisplayName : (Data.Alias ?? "—");
				GameRef.Say(who, new Line(Data.Barks[_barkI++ % Data.Barks.Length]));
			}
		}

		// ── head ──
		if (_player == null) { _parts.Head.Rotation = new Vector3(pitch, _yaw, 0); return; }
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
		else if (asleep) { target = 0f; }
		else if (Data.Dialog != null && dist < 2.8f)
		{
			target = look;
			rate = 4f;
		}
		else if (Data.Side != 0 && Data.Idle == Gesture.Window)
		{
			_winT -= dt;
			if (_winT <= 0f) { _winLook = !_winLook; _winT = _winLook ? GD.Randf() * 2f + 2.5f : GD.Randf() * 6f + 5f; }
			if (_winLook) { target = -Data.Side * 1.1f; rate = 1.4f; }
		}

		if(_reactionTime>0) {
			_reactionTime-=dt; var toward=ToLocal(_reactionPoint)-_parts.Head.Position;
			target=Mathf.Clamp(Mathf.Atan2(-toward.X,-toward.Z),-1.2f,1.2f); rate=2.5f;
		}
		_yaw = Mathf.LerpAngle(_yaw, target, 1f - Mathf.Exp(-rate * dt));
		_parts.Head.Rotation = new Vector3(pitch, _yaw, 0);
	}
}
