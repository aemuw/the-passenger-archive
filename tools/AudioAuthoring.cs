using Godot;

namespace PassengerArchive;

/// <summary>Development-only sound authoring. Writes replaceable WAV assets.</summary>
public static class AudioAuthoring
{
	public static void AuthorAudio()
	{
		var clips = new System.Collections.Generic.Dictionary<string, AudioStreamWav>
		{
			["rumble"] = MakeRumble(), ["clack"] = MakeClack(), ["ring"] = MakeRing(),
			["click"] = MakeClick(), ["hit"] = MakeHit(), ["wind"] = MakeWind(),
			["rain"] = MakeRain(), ["pad"] = MakePad(), ["heart"] = MakeHeart(),
			["creak"] = MakeCreak(), ["thud"] = MakeThud(), ["chime"] = MakeChime(),
			["motif"] = MakeMotif(0f, false), ["motif_dark"] = MakeMotif(-4f, true),
			["step1"] = MakeStep(1), ["step2"] = MakeStep(2), ["step3"] = MakeStep(3),
		};
		foreach (var (name, clip) in clips)
		{
			clip.SaveToWav($"res://assets/audio/{name}.wav");
			// Import flags persist in source control and are editable through Godot's Import tab.
			using var file = FileAccess.Open($"res://assets/audio/{name}.wav.import", FileAccess.ModeFlags.Write);
			file.StoreString($"[remap]\nimporter=\"wav\"\ntype=\"AudioStreamWAV\"\n\n[deps]\nsource_file=\"res://assets/audio/{name}.wav\"\n\n[params]\nedit/loop_mode={(clip.LoopMode == AudioStreamWav.LoopModeEnum.Forward ? 2 : 0)}\nedit/loop_begin=0\nedit/loop_end=-1\n");
		}
	}
	// ───────────── synthesis ─────────────
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

	/// <summary>A footstep: a dull knock plus a short scuff of filtered noise. Three variants.</summary>
	static AudioStreamWav MakeStep(int seed)
	{
		int n = (int)(Rate * 0.2f);
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = (ulong)(100 + seed) };
		float lp = 0f;
		float f0 = 62f + seed * 9f;
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			lp += (rng.RandfRange(-1f, 1f) - lp) * 0.35f;
			float v = Mathf.Sin(t * Mathf.Tau * f0) * Mathf.Exp(-t * 38f) + lp * Mathf.Exp(-t * 26f) * 0.5f;
			Write(data, i, v * 0.8f);
		}
		return Wav(data, n, false);
	}

	/// <summary>An old carriage creaking: a slow saw sweep through a one-pole low-pass.</summary>
	static AudioStreamWav MakeCreak()
	{
		int n = (int)(Rate * 1.5f);
		var data = new byte[n * 2];
		float phase = 0f, lp = 0f;
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float f = 140f + 50f * Mathf.Sin(t * 2.2f) + t * 20f;
			phase += f / Rate;
			float saw = (phase - Mathf.Floor(phase)) * 2f - 1f;
			lp += (saw - lp) * 0.12f;
			float env = Mathf.Sin(Mathf.Pi * t / 1.5f);
			Write(data, i, lp * env * 0.6f);
		}
		return Wav(data, n, false);
	}

	/// <summary>Something heavy, far away, in another carriage.</summary>
	static AudioStreamWav MakeThud()
	{
		int n = (int)(Rate * 0.7f);
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 33 };
		float lp = 0f;
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			lp += (rng.RandfRange(-1f, 1f) - lp) * 0.06f;
			float v = Mathf.Sin(t * Mathf.Tau * 52f) * Mathf.Exp(-t * 8f) + lp * Mathf.Exp(-t * 14f) * 1.2f;
			Write(data, i, v * 0.8f);
		}
		return Wav(data, n, false);
	}

	/// <summary>Train PA chime: two soft tones, descending. Nobody ever announces anything after it.</summary>
	static AudioStreamWav MakeChime()
	{
		int n = (int)(Rate * 2.6f);
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float v = 0f;
			v += Mathf.Sin(t * Mathf.Tau * 659.25f) * Mathf.Exp(-t * 3.2f);
			float t2 = t - 0.55f;
			if (t2 > 0f) v += Mathf.Sin(t2 * Mathf.Tau * 523.25f) * Mathf.Exp(-t2 * 2.6f);
			Write(data, i, v * 0.35f);
		}
		return Wav(data, n, false);
	}

	static AudioStreamWav MakeWind()
	{
		int n = Rate * 4;
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 55 };
		float lp = 0f;
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			lp += (rng.RandfRange(-1f, 1f) - lp) * 0.025f;
			float gust = 0.6f + 0.4f * Mathf.Sin(t * Mathf.Tau / 4f);
			Write(data, i, lp * 5f * gust);
		}
		return Wav(data, n, true);
	}

	static AudioStreamWav MakeRain()
	{
		int n = Rate * 2;
		var data = new byte[n * 2];
		var rng = new RandomNumberGenerator { Seed = 66 };
		float lp = 0f;
		for (int i = 0; i < n; i++)
		{
			float x = rng.RandfRange(-1f, 1f);
			lp += (x - lp) * 0.3f;
			Write(data, i, (x - lp) * 0.35f);   // high-passed hiss
		}
		return Wav(data, n, true);
	}

	/// <summary>Low drone; every component completes whole cycles in 4 s so the loop is seamless.</summary>
	static AudioStreamWav MakePad()
	{
		int n = Rate * 4;
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float v = Mathf.Sin(t * Mathf.Tau * 55f) + Mathf.Sin(t * Mathf.Tau * 82.5f) * 0.6f + Mathf.Sin(t * Mathf.Tau * 110f) * 0.3f + Mathf.Sin(t * Mathf.Tau * 164.5f) * 0.12f;
			float trem = 0.75f + 0.25f * Mathf.Sin(t * Mathf.Tau * 0.5f);
			Write(data, i, v * trem * 0.28f);
		}
		return Wav(data, n, true);
	}

	static AudioStreamWav MakeHeart()
	{
		int n = (int)(Rate * 0.9f);
		var data = new byte[n * 2];
		for (int i = 0; i < n; i++)
		{
			float t = (float)i / Rate;
			float v = Mathf.Sin(t * Mathf.Tau * 55f) * Mathf.Exp(-t * 20f);
			float t2 = t - 0.25f;
			if (t2 > 0f) v += Mathf.Sin(t2 * Mathf.Tau * 48f) * Mathf.Exp(-t2 * 22f) * 0.8f;
			Write(data, i, v * 0.9f);
		}
		return Wav(data, n, true);
	}

	/// <summary>
	/// The game's motif — five notes, D F A G F — as a soft piano with a ghostly echo. dark = a tone lower,
	/// bit-crushed and wobbling (the nightmare / the ending).
	/// </summary>
	static AudioStreamWav MakeMotif(float semitones, bool dark)
	{
		float[] notes = { 293.66f, 349.23f, 440.00f, 392.00f, 349.23f };
		float[] onsets = { 0.0f, 1.1f, 2.2f, 3.4f, 4.5f };
		float shift = Mathf.Pow(2f, semitones / 12f);
		int n = (int)(Rate * 9.5f);
		var dry = new float[n];

		for (int k = 0; k < notes.Length; k++)
		{
			float f = notes[k] * shift;
			int start = (int)(onsets[k] * Rate);
			for (int i = start; i < n; i++)
			{
				float t = (float)(i - start) / Rate;
				if (t > 4.5f) break;
				float attack = Mathf.Min(1f, t / 0.006f);
				float v = 0f;
				for (int h = 1; h <= 4; h++)
					v += Mathf.Sin(t * Mathf.Tau * f * h * (1f + (h - 1) * 0.0007f)) / (h * 1.3f) * Mathf.Exp(-t * (1.1f + h * 0.9f));
				dry[i] += v * attack * 0.5f;
			}
		}

		var data = new byte[n * 2];
		int d1 = (int)(Rate * 0.37f), d2 = (int)(Rate * 0.81f);
		for (int i = 0; i < n; i++)
		{
			float v = dry[i];
			if (i >= d1) v += dry[i - d1] * 0.35f;
			if (i >= d2) v += dry[i - d2] * 0.18f;
			if (dark)
			{
				v *= 0.75f + 0.25f * Mathf.Sin(i * Mathf.Tau * 5.5f / Rate);       // wobble
				v = Mathf.Round(v * 9f) / 9f;                                       // crush
			}
			float fadeOut = Mathf.Clamp((n - i) / (Rate * 1.2f), 0f, 1f);
			Write(data, i, v * fadeOut);
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

