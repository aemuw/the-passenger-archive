using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Godot;

namespace PassengerArchive;

/// <summary>The JSON files are the editable runtime dialogue source.</summary>
public static class DialogueAssets
{
	static readonly Dictionary<Convo, Convo> Cache = new();
	public static IEnumerable<(string name, Convo dialogue)> Templates()
	{
		foreach (var field in typeof(Carriages).GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
			if (field.FieldType == typeof(Convo)) yield return (field.Name, (Convo)field.GetValue(null));
	}
	public static Convo Resolve(Convo template)
	{
		if (Cache.TryGetValue(template, out var found)) return found;
		foreach (var (name, dialogue) in Templates())
		{
			if (!ReferenceEquals(dialogue, template)) continue;
			using var file = FileAccess.Open($"res://data/dialogues/{name}.json", FileAccess.ModeFlags.Read);
			if (file == null) throw new InvalidOperationException("Missing dialogue asset: " + name);
			found = JsonSerializer.Deserialize<Convo>(file.GetAsText());
			Validate(found, name);
			Cache[template] = found;
			return found;
		}
		throw new InvalidOperationException("Unregistered dialogue template");
	}
	public static void Validate(Convo dialogue, string name)
	{
		if (dialogue?.Nodes == null || dialogue.Entries == null) throw new InvalidOperationException("Invalid dialogue: " + name);
		var ids = new HashSet<string>();
		foreach (var node in dialogue.Nodes)
			if (string.IsNullOrEmpty(node.Id) || node.Say == null || !ids.Add(node.Id)) throw new InvalidOperationException("Duplicate/invalid node in " + name);
		void Target(string id) { if (id != null && !ids.Contains(id)) throw new InvalidOperationException($"Broken dialogue link {name}/{id}"); }
		foreach (var entry in dialogue.Entries) Target(entry.Node);
		foreach (var node in dialogue.Nodes)
		{
			Target(node.Next); Target(node.IfTo);
			if (node.Opts != null) foreach (var option in node.Opts) Target(option.To);
		}
	}
}
