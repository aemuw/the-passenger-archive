using Godot;

namespace PassengerArchive;

/// <summary>
/// Procedural audio (no asset files): rumble + wheel clacks, a ringing phone, a door-lock click.
/// Swap for real recordings later — keep the same API.
/// </summary>
public partial class TrainAudio : Node
{
	AudioStreamPlayer _rumble, _clack, _ring, _click, _hit;
	Tween _tween;

	public override void _Ready()
	{
		_rumble = new AudioStreamPlayer { Stream = MakeRumble(), VolumeDb = -9f };
		_clack = new AudioStreamPlayer { Stream = MakeClack(), VolumeDb = -12f };
		_ring = new AudioStreamPlayer { Stream = MakeRing(), VolumeDb = -8f };
		_click = new AudioStreamPlayer { Stream = MakeClick(), VolumeDb = -4f };
		_hit = new AudioStreamPlayer { Stream = MakeHit(), VolumeDb = -2f };
		AddChild(_hit);
		AddChild(_rumble);
		AddChild(_clack);
		AddChild(_ring);
		AddChild(_click);
		_rumble.Play();
		_clack.Play();
	}

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

	const int Rate = 22050;

	static AudioStreamWav MakeRumble()
	{
		int n = Rate * 3;
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 7 };
		float lp = 0f;
		for (int i = 0; i < n; i++)
		{
			lp += (rng.RandfRange(-1f, 1f) - lp) * 0.015f;
			float v = lp * 2.2f + Mathf.Sin(i * Mathf.Tau * 48f / Rate) * 0.08f;
			Write(data, i, v * 0.5f);
		}
		return Wav(data, n, true);
	}

	static AudioStreamWav MakeClack()
	{
		int n = (int)(Rate * 1.6f);
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 11 };
		float[] hits = { 0.0f, 0.15f, 0.8f, 0.95f };
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float v = 0f;
			foreach (float h in hits)
			{
				float dt = t - h;
				if (dt < 0f) continue;
				float env = Mathf.Exp(-dt * 38f);
				v += (rng.RandfRange(-1f, 1f) * 0.45f + Mathf.Sin(dt * Mathf.Tau * 85f) * 0.8f) * env;
			}
			Write(data, i, v * 0.6f);
		}
		return Wav(data, n, true);
	}

	/// <summary>Old two-tone phone: 0.9 s of ringing, 1.1 s of silence, looped.</summary>
	static AudioStreamWav MakeRing()
	{
		int n = Rate * 2;
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float v = 0f;
			if (t < 0.9f)
			{
				float burst = (Mathf.Sin(t * Mathf.Tau * 22f) > 0f) ? 1f : 0.3f; // warble
				v = (Mathf.Sin(t * Mathf.Tau * 440f) + Mathf.Sin(t * Mathf.Tau * 480f)) * 0.25f * burst;
			}
			Write(data, i, v);
		}
		return Wav(data, n, true);
	}

	static AudioStreamWav MakeClick()
	{
		int n = (int)(Rate * 0.35f);
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 5 };
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float v = (rng.RandfRange(-1f, 1f) * 0.5f + Mathf.Sin(t * Mathf.Tau * 140f)) * Mathf.Exp(-t * 28f);
			Write(data, i, v * 0.8f);
		}
		return Wav(data, n, false);
	}

	static AudioStreamWav MakeHit()
	{
		int n = (int)(Rate * 0.7f);
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 21 };
		float lp = 0f;
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			lp += (rng.RandfRange(-1f, 1f) - lp) * 0.25f;
			float v = Mathf.Sin(t * Mathf.Tau * (70f - t * 60f)) * Mathf.Exp(-t * 7f) + lp * Mathf.Exp(-t * 22f) * 0.8f;
			Write(data, i, v * 0.9f);
		}
		return Wav(data, n, false);
	}

	static void Write(byte[] data, int i, float v)
	{
		short s = (short)(Mathf.Clamp(v, -1f, 1f) * 30000f);
		data[i * 2] = (byte)(s & 0xFF);
		data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
	}

	static AudioStreamWav Wav(byte[] data, int samples, bool loop) => new()
	{
		Format = AudioStreamWav.FormatEnum.Format16Bits,
		MixRate = Rate,
		Stereo = false,
		Data = data,
		LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
		LoopBegin = 0,
		LoopEnd = samples,
	};
}
