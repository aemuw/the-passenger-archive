using Godot;

namespace PassengerArchive;

/// <summary>First-person controller with subtle head-bob and train sway.</summary>
public partial class Player : CharacterBody3D
{
	public Camera3D Cam { get; private set; }
	public bool InputEnabled = true;
	public System.Action OnStep;     // fired once per footfall
	int _stepCount;

	Node3D _head;
	RayCast3D _ray;
	float _pitch;
	float _bobTime;
	float _swayTime;
	bool _collapsed;

	const float EyeHeight = 1.6f;

	public override void _Ready()
	{
		AddToGroup("player");
		CollisionLayer = GameConfig.LayerPlayer;
		CollisionMask = GameConfig.LayerWorld;

		AddChild(new CollisionShape3D
		{
			Shape = new CapsuleShape3D { Radius = 0.25f, Height = 1.7f },
			Position = new Vector3(0, 0.85f, 0),
		});

		_head = new Node3D { Position = new Vector3(0, EyeHeight, 0) };
		AddChild(_head);

		Cam = new Camera3D { Fov = GameConfig.Fov, Near = 0.05f, Far = 150f, Current = true };
		_head.AddChild(Cam);

		_ray = new RayCast3D
		{
			TargetPosition = new Vector3(0, 0, -GameConfig.InteractDistance),
			CollisionMask = GameConfig.LayerWorld | GameConfig.LayerInteract,
			CollideWithBodies = true,
			CollideWithAreas = false,
		};
		Cam.AddChild(_ray);
	}

	public void Look(Vector2 relative)
	{
		if (!InputEnabled) return;
		RotateY(-relative.X * GameConfig.MouseSensitivity);
		_pitch = Mathf.Clamp(_pitch - relative.Y * GameConfig.MouseSensitivity, -1.45f, 1.45f);
		_head.Rotation = new Vector3(_pitch, 0, 0);
	}

	public void Teleport(Vector3 pos, float yaw)
	{
		GlobalPosition = pos;
		Velocity = Vector3.Zero;
		Rotation = new Vector3(0, yaw, 0);
		_pitch = 0;
		_head.Rotation = Vector3.Zero;
		_collapsed = false;
		_head.Position = new Vector3(0, EyeHeight, 0);
		Cam.Rotation = Vector3.Zero;
	}

	/// <summary>Smoothly turns the view towards a point (used when a conversation starts).</summary>
	public void FaceTowards(Vector3 worldPoint)
	{
		Vector3 d = worldPoint - (GlobalPosition + new Vector3(0, EyeHeight, 0));
		float yaw = Mathf.Atan2(-d.X, -d.Z);
		float pitch = Mathf.Clamp(Mathf.Atan2(d.Y, new Vector2(d.X, d.Z).Length()), -0.9f, 0.9f);
		float target = Rotation.Y + Mathf.AngleDifference(Rotation.Y, yaw);

		var tw = CreateTween().SetParallel(true);
		tw.TweenProperty(this, "rotation:y", target, 0.4f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		tw.TweenMethod(Callable.From<float>(v => { _pitch = v; _head.Rotation = new Vector3(v, 0, 0); }), _pitch, pitch, 0.4f);
	}

	/// <summary>The hero drops to the floor: the camera falls and tilts, the world goes sideways.</summary>
	public void Collapse(float seconds = 1.4f)
	{
		InputEnabled = false;
		_collapsed = true;
		var tw = CreateTween().SetParallel(true);
		tw.TweenProperty(_head, "position", new Vector3(0, 0.22f, 0), seconds).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
		tw.TweenProperty(Cam, "rotation:z", 1.35f, seconds).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
	}

	public Interactable Focus()
	{
		_ray.ForceRaycastUpdate();
		return _ray.IsColliding() ? _ray.GetCollider() as Interactable : null;
	}

	public override void _PhysicsProcess(double delta)
	{
		float dt = (float)delta;
		var vel = Velocity;
		Vector2 input = InputEnabled
			? Input.GetVector("move_left", "move_right", "move_forward", "move_back")
			: Vector2.Zero;

		var dir = (GlobalTransform.Basis * new Vector3(input.X, 0, input.Y));
		dir.Y = 0;
		dir = dir.Normalized();

		float accel = 14f;
		vel.X = Mathf.MoveToward(vel.X, dir.X * GameConfig.PlayerSpeed, accel * dt);
		vel.Z = Mathf.MoveToward(vel.Z, dir.Z * GameConfig.PlayerSpeed, accel * dt);
		vel.Y = IsOnFloor() ? 0f : vel.Y - 9.8f * dt;
		Velocity = vel;
		MoveAndSlide();

		if (_collapsed) return;
		// Head bob when walking + constant gentle train sway.
		bool moving = new Vector2(Velocity.X, Velocity.Z).Length() > 0.3f;
		_bobTime += dt * (moving ? 8.5f : 0f);
		_swayTime += dt;
		int steps = (int)(_bobTime / Mathf.Pi);
		if (moving && steps != _stepCount) { _stepCount = steps; OnStep?.Invoke(); }
		float bob = moving ? Mathf.Sin(_bobTime) * 0.025f : 0f;
		_head.Position = new Vector3(Mathf.Sin(_bobTime * 0.5f) * 0.012f, EyeHeight + bob, 0);
		Cam.Rotation = new Vector3(0, 0, Mathf.Sin(_swayTime * 1.3f) * 0.004f);
	}
}
