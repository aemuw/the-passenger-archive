using System;
using System.Text.Json;
using Godot;

namespace PassengerArchive;

public sealed class Checkpoint
{
	public int Version { get; set; } = 1;
	public int Carriage { get; set; }
	public string[] Flags { get; set; } = Array.Empty<string>();
	public string[] Remembered { get; set; } = Array.Empty<string>();
	public static string StoragePath = "user://checkpoint.json";
	public static Checkpoint Read()
	{
		if (!FileAccess.FileExists(StoragePath)) return null;
		try
		{
			using var file = FileAccess.Open(StoragePath, FileAccess.ModeFlags.Read);
			var result = JsonSerializer.Deserialize<Checkpoint>(file.GetAsText());
			return result?.Version == 1 && result.Carriage >= 0 && result.Carriage < Carriages.All.Length
				&& result.Flags != null && result.Remembered != null ? result : null;
		}
		catch (Exception ex) { GD.PushWarning("Checkpoint could not be loaded: " + ex.Message); return null; }
	}
	public void Write()
	{
		string temp = StoragePath + ".tmp";
		using (var file = FileAccess.Open(temp, FileAccess.ModeFlags.Write))
		{
			if (file == null) { GD.PushWarning("Checkpoint is not writable."); return; }
			file.StoreString(JsonSerializer.Serialize(this));
			file.Flush();
		}
		// Rename in the same directory avoids exposing partially written JSON.
		var error = DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(temp), ProjectSettings.GlobalizePath(StoragePath));
		if (error != Error.Ok) GD.PushWarning("Checkpoint replacement failed: " + error);
	}
}
