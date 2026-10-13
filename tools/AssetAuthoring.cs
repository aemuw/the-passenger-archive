using System;
using System.Collections.Generic;
using Godot;

namespace PassengerArchive;

/// <summary>Explicit development command. Its output is checked in and loaded as normal scenes.</summary>
public static class AssetAuthoring
{
	static void Folder(string path) => DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(path));
	public static void AuthorMedia()
	{
		Folder("res://assets/textures"); Folder("res://assets/audio");
		Ps1Materials.AuthorTextures(); AudioAuthoring.AuthorAudio();
		GD.Print("Authored editable PNG textures and WAV files.");
	}
	static HashSet<Outfit> Looks()
	{
		var looks = new HashSet<Outfit> { Carriages.KepkaLook, Carriages.TarasLook, Carriages.HitchLook, Carriages.PopOriginal };
		foreach (var carriage in Carriages.All)
			foreach (var person in carriage.People) looks.Add(person.Look);
		return looks;
	}
	public static void AuthorFaces()
	{
		Folder("res://assets/textures/faces");
		foreach (var look in Looks()) FaceAuthoring.Draw(look);
		GD.Print("Saved hand-drawn pixel face textures.");
	}
	public static void Run(bool visualsOnly = false)
	{
		SceneAssets.Baking = true;
		Folder("res://scenes/carriages"); Folder("res://scenes/memories"); Folder("res://scenes/characters");
		Folder("res://scenes/ui"); Folder("res://scenes/actors");
		Folder("res://scenes/props");
		Folder("res://scenes/effects");
		HorrorAuthoring.Save();
		var bench = BenchAuthoring.Create(); Save(bench, "res://scenes/props/Bench.tscn"); bench.Free();
		if (!visualsOnly)
		{
			Folder("res://data/dialogues");
			foreach (var (name, dialogue) in DialogueAssets.Templates())
			{
				DialogueAssets.Validate(dialogue, name);
				using var file = FileAccess.Open($"res://data/dialogues/{name}.json", FileAccess.ModeFlags.Write);
				file.StoreString(System.Text.Json.JsonSerializer.Serialize(dialogue, new System.Text.Json.JsonSerializerOptions
				{
					WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				}));
			}
			var hud = new Hud { Name = "Hud" }; hud.Author(); Save(hud, "res://scenes/ui/Hud.tscn"); hud.Free();
			var player = new Player { Name = "Player" }; player.Author(); Save(player, "res://scenes/actors/Player.tscn"); player.Free();
			Folder("res://scenes/audio");
			var audio = new TrainAudio { Name = "TrainAudio" }; audio.AuthorLayout(); Save(audio, "res://scenes/audio/TrainAudio.tscn"); audio.Free();
			var menu = MenuAuthoring.Create(); Save(menu, "res://scenes/ui/ArchiveMenu.tscn"); menu.Free();
		}
		foreach (var look in Looks())
		foreach (bool seated in new[] { false, true })
		{
			var model = new Node3D { Name = "PassengerModel" };
			FigureBuilder.Build(model, look, seated);
			Save(model, SceneAssets.FigurePath(look, seated));
			model.Free();
		}
		for (int i = 0; i < Carriages.All.Length; i++)
		{
			var carriage = CarriageBuilder.Build(Carriages.All[i], null);
			Save(carriage, $"res://scenes/carriages/Carriage{i + 1:00}.tscn");
			carriage.Free();
		}
		foreach (string id in new[] { "route", "ride", "walk", "depart", "arrive" })
		{
			var memory = new MemoryScene { Name = "Memory", Id = id };
			memory.Author();
			MemoryArt.Dress(memory,id);
			Save(memory, $"res://scenes/memories/{id}.tscn");
			if (id == "route")
				for (int evening = 0; evening < 4; evening++)
				{
					var props = memory.AuthorEvening(evening);
					Save(props, $"res://scenes/memories/route_evening{evening}.tscn");
					props.Free();
				}
			memory.Free();
		}
		SceneAssets.Baking = false;
		GD.Print("Authored 4 carriages, 5 memories, 4 evening variants and character models.");
	}
	public static void Save(Node root, string path)
	{
		OwnChildren(root, root);
		var scene = new PackedScene();
		var error = scene.Pack(root);
		if (error != Error.Ok) throw new InvalidOperationException($"Pack {path}: {error}");
		error = ResourceSaver.Save(scene, path);
		if (error != Error.Ok) throw new InvalidOperationException($"Save {path}: {error}");
	}
	static void OwnChildren(Node node, Node root)
	{
		int index = 0;
		foreach (var child in node.GetChildren())
		{
			if (child.Name.ToString().StartsWith("@")) child.Name = child.GetClass().ToString() + (++index).ToString("00");
			child.Owner = root;
			if (string.IsNullOrEmpty(child.SceneFilePath)) OwnChildren(child, root);
		}
	}
}

