using System;
using Godot;

namespace PassengerArchive;

public partial class Game
{
	static void Require(bool condition, string message)
	{
		if (!condition) throw new InvalidOperationException(message);
	}
	async void RunVerification()
	{
		try
		{
			Resume();
			foreach (var (name, dialogue) in DialogueAssets.Templates()) DialogueAssets.Resolve(dialogue);
			GD.Print("PASS dialogue graph validation");
			DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/qa"));
			var originalPath = Checkpoint.StoragePath;
			Checkpoint.StoragePath = "res://.godot/qa/checkpoint.json";
			var checkpoint = new Checkpoint { Carriage = 2, Flags = new[] { "k:pop_named" }, Remembered = new[] { "Віктор" } };
			checkpoint.Write(); checkpoint.Carriage = 3; checkpoint.Write();
			var restored = Checkpoint.Read();
			Require(restored.Carriage == 3 && restored.Flags[0] == "k:pop_named" && restored.Remembered[0] == "Віктор", "Checkpoint replacement and round trip");
			Checkpoint.StoragePath = originalPath;
			GD.Print("PASS checkpoint round trip");
			for (int carriage = 0; carriage < Carriages.All.Length; carriage++)
			{
				_index = carriage; LoadCarriage();
				await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
				Require(_worldRoot.GetNode<DoorInteractable>("door_front").Forward, "Forward door binding");
				foreach (var person in Current.People)
				{
					var passenger = _worldRoot.GetNode<Passenger>("p_" + person.Id);
					Require(passenger.Data == person, "Passenger data binding: " + person.Id);
					Require(passenger.HasNode("Upper/Head") && passenger.HasNode("Animations"), "Saved character model/animations");
					Require(passenger.HasNode("InteractionShape"), "Saved NPC collision");
				}
				GD.Print("PASS carriage " + (carriage + 1));
			}
			foreach (string id in new[] { "route", "ride", "walk", "depart", "arrive" })
			{
				EnterMemory(id);
				await ToSignal(GetTree().CreateTimer(2.5), SceneTreeTimer.SignalName.Timeout);
				Require(_mode == Mode.Memory && _memory.Id == id && !_busy, "Memory entry: " + id);
				Require(_player.GlobalPosition.DistanceTo(_memory.GlobalPosition + _memory.StartLocal) < 0.15f, "Memory spawn: " + id);
				if (id == "route")
				{
					for (int evening = 0; evening < 4; evening++)
					{
						var props = SceneAssets.Load<Node3D>($"res://scenes/memories/route_evening{evening}.tscn");
						props.Free();
					}
				}
				EndMemory(id);
				await ToSignal(GetTree().CreateTimer(2.5), SceneTreeTimer.SignalName.Timeout);
				Require(id == "arrive" ? _mode == Mode.Ending : _mode == Mode.Carriage, "Memory exit: " + id);
				GD.Print("PASS memory " + id);
			}
			_index = 0; LoadCarriage();
			await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
			ViolateRule();
			await ToSignal(GetTree().CreateTimer(5.5), SceneTreeTimer.SignalName.Timeout);
			Require(_dead && _deathShown, "Death scene");
			Restore(_entry); LoadCarriage();
			await ToSignal(GetTree().CreateTimer(1.6), SceneTreeTimer.SignalName.Timeout);
			Require(!_dead && !_deathShown && !Passenger.Frozen, "Retry reset");
			Require(_worldRoot.GetNode<ActionProp>("Telephone").PromptFn != null, "Saved telephone binding");
			var viktor = _worldRoot.GetNode<Passenger>("p_viktor");
			StartTalk(viktor.Data.Dialog, "viktor", () => viktor.DisplayName, force: "name");
			for (int i = 0; i < 2; i++) { Hud.FinishTyping(); AdvanceTalk(); }
			Require(IsKnown("viktor"), "Name revealed through dialogue");
			EndTalk();
			StartTalk(viktor.Data.Dialog, "viktor", () => viktor.DisplayName, force: "route");
			for (int i = 0; i < 2; i++) { Hud.FinishTyping(); AdvanceTalk(); }
			Require(_choosing && _opts.Count == 2, "Dialogue choices");
			Choose(0); Hud.FinishTyping(); AdvanceTalk(); Hud.FinishTyping(); AdvanceTalk();
			await ToSignal(GetTree().CreateTimer(3.3), SceneTreeTimer.SignalName.Timeout);
			Require(_mode == Mode.Memory && _memory.Id == "route", "Dialogue starts played memory");
			EndMemory("route");
			await ToSignal(GetTree().CreateTimer(2.5), SceneTreeTimer.SignalName.Timeout);
			Require(HasFlag("story_done"), "Played memory unlocks next carriage");
			GD.Print("PASS name, choices, played memory and story completion");
			GD.Print("PASS death and retry; verification complete");
			QuitGame();
		}
		catch (Exception ex) { GD.PushError(ex.ToString()); QuitGame(1); }
	}
	async void CapturePreview()
	{
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://.godot/qa"));
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/menu.png");
		_menu.GetNode<Control>("Root/Layout/Scroll/Controls/Settings").Show();
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/settings.png");
		_menu.GetNode<Control>("Root/Layout/Scroll/Controls/Settings").Hide();
		Resume();
		await ToSignal(GetTree().CreateTimer(4), SceneTreeTimer.SignalName.Timeout);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/carriage.png");
		_player.Teleport(new Vector3(0, 0.05f, -2.3f), Mathf.Pi);
		_player.FaceTowards(_worldRoot.GetNode<Passenger>("p_viktor").HeadWorld);
		await ToSignal(GetTree().CreateTimer(0.6), SceneTreeTimer.SignalName.Timeout);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/passenger.png");
		EnterMemory("walk");
		await ToSignal(GetTree().CreateTimer(2.6), SceneTreeTimer.SignalName.Timeout);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/memory.png");
		EndMemory("walk");
		await ToSignal(GetTree().CreateTimer(2.5), SceneTreeTimer.SignalName.Timeout);
		ViolateRule();
		await ToSignal(GetTree().CreateTimer(2.2), SceneTreeTimer.SignalName.Timeout);
		GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/attack.png");
		await ToSignal(GetTree().CreateTimer(3.5), SceneTreeTimer.SignalName.Timeout);
		GD.Print("Saved preview images.");
		QuitGame();
	}
}
