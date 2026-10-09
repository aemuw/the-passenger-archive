using Godot;

namespace PassengerArchive;

/// <summary>
/// Procedural train ambience (no audio assets needed): a low rumble loop + wheel clacks.
/// Swap for real recordings later — keep the same API.
/// </summary>
public partial class TrainAudio : Node
{
	AudioStreamPlayer _rumble;
	AudioStreamPlayer _clack;

	public override void _Ready()
	{
		_rumble = new AudioStreamPlayer { Stream = MakeRumble(), VolumeDb = -9f };
		_clack = new AudioStreamPlayer { Stream = MakeClack(), VolumeDb = -12f };
		AddChild(_rumble);
		AddChild(_clack);
		_rumble.Play();
		_clack.Play();
	}

	/// <summary>The wheels stop clacking (used on death / supernatural beats). The rumble stays — the train never fully goes silent.</summary>
	public void CutClacks(float fade = 0.6f)
	{
		var tw = CreateTween();
		tw.TweenProperty(_clack, "volume_db", -60f, fade);
	}

	public void RestoreClacks()
	{
		_clack.VolumeDb = -12f;
	}

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
		return Wav(data, n);
	}

	static AudioStreamWav MakeClack()
	{
		float len = 1.6f;
		int n = (int)(Rate * len);
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
		return Wav(data, n);
	}

	static void Write(byte[] data, int i, float v)
	{
		short s = (short)(Mathf.Clamp(v, -1f, 1f) * 30000f);
		data[i * 2] = (byte)(s & 0xFF);
		data[i * 2 + 1] = (byte)((s >> 8) & 0xFF);
	}

	static AudioStreamWav Wav(byte[] data, int samples) => new()
	{
		Format = AudioStreamWav.FormatEnum.Format16Bits,
		MixRate = Rate,
		Stereo = false,
		Data = data,
		LoopMode = AudioStreamWav.LoopModeEnum.Forward,
		LoopBegin = 0,
		LoopEnd = samples,
	};
}
