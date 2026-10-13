using Godot;

namespace PassengerArchive;

public static class UserSettings
{
	public static float Sensitivity = 1f;
	public static float Effects = 0.45f;
	public static float Gore = 1f;
	public static float TextScale = 1f;
	public static float Motion = 0.6f;
	public static float Volume = 0.8f;
	public static float Scares = 1f;
	public static void Load()
	{
		var config = new ConfigFile();
		if (config.Load("user://settings.cfg") != Error.Ok) return;
		Sensitivity = Mathf.Clamp((float)config.GetValue("accessibility", "sensitivity", 1f), 0.2f, 3f);
		Effects = Mathf.Clamp((float)config.GetValue("accessibility", "effects", 0.45f), 0f, 1f);
		Gore = Mathf.Clamp((float)config.GetValue("accessibility", "gore", 1f), 0f, 1f);
		TextScale = Mathf.Clamp((float)config.GetValue("accessibility", "text_scale", 1f), 0.8f, 1.4f);
		Motion = Mathf.Clamp((float)config.GetValue("accessibility", "motion", 0.6f), 0f, 1f);
		Volume = Mathf.Clamp((float)config.GetValue("audio", "volume", 0.8f), 0f, 1f);
		ApplyVolume();
		Scares = Mathf.Clamp((float)config.GetValue("accessibility", "scares", 1f), 0f, 1f);
	}
	public static void ApplyVolume() => AudioServer.SetBusVolumeDb(0, Volume <= 0f ? -80f : Mathf.LinearToDb(Volume));
	public static void Save()
	{
		var config = new ConfigFile();
		config.SetValue("accessibility", "sensitivity", Sensitivity);
		config.SetValue("accessibility", "effects", Effects);
		config.SetValue("accessibility", "gore", Gore);
		config.SetValue("accessibility", "text_scale", TextScale);
		config.SetValue("accessibility", "motion", Motion);
		config.SetValue("audio", "volume", Volume);
		config.SetValue("accessibility", "scares", Scares);
		config.Save("user://settings.cfg");
	}
}
