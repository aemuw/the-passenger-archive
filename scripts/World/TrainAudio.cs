using Godot;

namespace PassengerArchive;

/// <summary>
/// Saved WAV audio resources. The train never goes fully silent: rumble + wheel clacks + now and then a creak,
/// a distant thud, the PA chime. Footsteps follow the surface. Music is ONE five-note motif (D F A G F) that comes back
/// in different forms: soft piano in a memory, a faint echo for a revelation, detuned and crushed at the end.
/// Swap for real recordings later — keep the same API.
/// </summary>
public partial class TrainAudio : Node
{
	AudioStreamPlayer _rumble, _clack, _ring, _click, _hit;
	AudioStreamPlayer _wind, _rain, _pad, _heart, _creak, _thud, _chime;
	AudioStreamPlayer _motif, _motifDark;
	AudioStreamPlayer[] _steps;
	int _stepI;
	Tween _tween, _padTween;
	double _ambT = 9;

	public bool Outdoor;          // gravel instead of metal underfoot
	public bool AmbientOn = true; // creaks, thuds, chimes

	readonly RandomNumberGenerator _rng = new();

	AudioStreamPlayer Mk(AudioStream s, float db)
	{
		string name = System.IO.Path.GetFileNameWithoutExtension(s.ResourcePath);
		string bus = name.StartsWith("motif") || name == "pad" ? "Music" : name is "rumble" or "clack" or "wind" or "rain" or "creak" or "thud" or "chime" ? "Ambience" : "Effects";
		var p = new AudioStreamPlayer { Name = name, Stream = s, VolumeDb = db, Bus = bus };
		AddChild(p);
		return p;
	}

	public override void _Ready()
	{
		_rng.Randomize();
		_rumble = GetNode<AudioStreamPlayer>("rumble"); _clack = GetNode<AudioStreamPlayer>("clack");
		_ring = GetNode<AudioStreamPlayer>("ring"); _click = GetNode<AudioStreamPlayer>("click"); _hit = GetNode<AudioStreamPlayer>("hit");
		_wind = GetNode<AudioStreamPlayer>("wind"); _rain = GetNode<AudioStreamPlayer>("rain"); _pad = GetNode<AudioStreamPlayer>("pad");
		_heart = GetNode<AudioStreamPlayer>("heart"); _creak = GetNode<AudioStreamPlayer>("creak"); _thud = GetNode<AudioStreamPlayer>("thud");
		_chime = GetNode<AudioStreamPlayer>("chime"); _motif = GetNode<AudioStreamPlayer>("motif"); _motifDark = GetNode<AudioStreamPlayer>("motif_dark");
		_steps = new[] { GetNode<AudioStreamPlayer>("step1"), GetNode<AudioStreamPlayer>("step2"), GetNode<AudioStreamPlayer>("step3") };
		_rumble.Play(); _clack.Play();
	}

	public void AuthorLayout()
	{
		_rng.Randomize();
		_rumble = Mk(Clip("rumble"), -9f);
		_clack = Mk(Clip("clack"), -12f);
		_ring = Mk(Clip("ring"), -8f);
		_click = Mk(Clip("click"), -4f);
		_hit = Mk(Clip("hit"), -2f);
		_wind = Mk(Clip("wind"), -60f);
		_rain = Mk(Clip("rain"), -60f);
		_pad = Mk(Clip("pad"), -60f);
		_heart = Mk(Clip("heart"), -60f);
		_creak = Mk(Clip("creak"), -20f);
		_thud = Mk(Clip("thud"), -22f);
		_chime = Mk(Clip("chime"), -24f);
		_motif = Mk(Clip("motif"), -15f);
		_motifDark = Mk(Clip("motif_dark"), -12f);
		_steps = new[]
		{
			Mk(Clip("step1"), -13f), Mk(Clip("step2"), -13f), Mk(Clip("step3"), -13f),
		};
	}

	static AudioStream Clip(string name) => GD.Load<AudioStream>($"res://assets/audio/{name}.wav");
	public override void _ExitTree()
	{
		Shutdown();
	}
	public void Shutdown()
	{
		AmbientOn = false; SetProcess(false);
		_tween?.Kill(); _padTween?.Kill();
		foreach (var child in GetChildren())
			if (child is AudioStreamPlayer player) { player.Stop(); player.Stream = null; }
	}

	public override void _Process(double delta)
	{
		if (!AmbientOn) return;
		_ambT -= delta;
		if (_ambT > 0) return;
		_ambT = _rng.RandfRange(7f, 19f);
		float r = _rng.Randf();
		if (Outdoor) return;
		if (r < 0.45f) { _creak.PitchScale = _rng.RandfRange(0.85f, 1.2f); _creak.Play(); }
		else if (r < 0.85f) { _thud.PitchScale = _rng.RandfRange(0.8f, 1.1f); _thud.Play(); }
		else if (!_chime.Playing) _chime.Play();
	}

	// ───────────── train bed ─────────────
	/// <summary>The wheels stop clacking. The rumble stays — the train never fully goes silent.</summary>
	public void CutClacks(float fade = 0.6f)
	{
		_tween?.Kill();
		_tween = CreateTween();
		_tween.TweenProperty(_clack, "volume_db", -60f, fade);
	}

	public void RestoreClacks()
	{
		_tween?.Kill();
		_clack.VolumeDb = -12f;
	}

	public void SetRing(bool on)
	{
		if (on && !_ring.Playing) _ring.Play();
		else if (!on && _ring.Playing) _ring.Stop();
	}

	/// <summary>Door lock / unlock.</summary>
	public void Click() => _click.Play();

	/// <summary>A heavy blow: low thump + a wet crack of noise.</summary>
	public void Hit() => _hit.Play();

	// ───────────── places ─────────────
	/// <summary>Outside the train (memories): wind, optionally rain; the wheels fall silent.</summary>
	public void SetOutdoor(bool on, bool rain = false)
	{
		Outdoor = on;
		Fade(_wind, on, -22f);
		Fade(_rain, on && rain, -18f);
		Fade(_rumble, !on, -9f);
	}

	void Fade(AudioStreamPlayer p, bool on, float db)
	{
		if (on && !p.Playing) { p.VolumeDb = -60f; p.Play(); }
		var tw = CreateTween();
		tw.TweenProperty(p, "volume_db", on ? db : -60f, 1.2f);
		if (!on) tw.TweenCallback(Callable.From(() => { if (p.VolumeDb <= -59f) p.Stop(); }));
	}

	// ───────────── music ─────────────
	/// <summary>The motif once (≈ 8 s). dark = detuned, bit-crushed, wobbling.</summary>
	public void PlayMotif(float db = -15f, bool dark = false)
	{
		var p = dark ? _motifDark : _motif;
		p.VolumeDb = db;
		p.Play();
	}

	public void StopMotif()
	{
		_motif.Stop();
		_motifDark.Stop();
	}

	/// <summary>A low drone under memories and the finale.</summary>
	public void SetPad(bool on, float db = -24f)
	{
		_padTween?.Kill();
		if (on && !_pad.Playing) { _pad.VolumeDb = -60f; _pad.Play(); }
		_padTween = CreateTween();
		_padTween.TweenProperty(_pad, "volume_db", on ? db : -60f, 2.0f);
		if (!on) _padTween.TweenCallback(Callable.From(() => { if (_pad.VolumeDb <= -59f) _pad.Stop(); }));
	}

	public void Heartbeat(bool on)
	{
		if (on && !_heart.Playing) { _heart.VolumeDb = -9f; _heart.Play(); }
		else if (!on && _heart.Playing) _heart.Stop();
	}

	// ───────────── footsteps ─────────────
	public void Step()
	{
		var p = _steps[_stepI++ % _steps.Length];
		p.PitchScale = Outdoor ? _rng.RandfRange(1.05f, 1.25f) : _rng.RandfRange(0.88f, 1.02f);
		p.Play();
	}

}
