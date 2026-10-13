using System;
using System.Linq;
using Godot;

namespace PassengerArchive;

public partial class Game
{
	async System.Threading.Tasks.Task Until(Func<bool> condition, float seconds)
	{
		for(int i=0;i<seconds*10;i++) { if(condition()) return; await ToSignal(GetTree().CreateTimer(.1),SceneTreeTimer.SignalName.Timeout); }
		throw new InvalidOperationException("Horror stage timed out.");
	}
	async void VerifyHorror(bool capture)
	{
		float setting=UserSettings.Scares;
		try
		{
			Resume(); UserSettings.Scares=1; _index=3; LoadCarriage();
			await Until(()=>!_busy,4);
			_horror.FirstDelay=0; _horror.WindowDelay=0; _horror.AisleDelay=0;
			await Until(()=>_horror.CompletedBeats==1,5);
			var watcher=_horror.GetNode<Node3D>("WindowWatcher");
			await Until(()=>watcher.Visible,3);
			Require(_worldRoot.GetNode<Scenery>("WindowLandscape").Speed==0,"Landscape stops during impossible window encounter");
			if(capture) {
				_player.FaceTowards(watcher.GlobalPosition+Vector3.Up*1.65f);
				await ToSignal(GetTree().CreateTimer(.45),SceneTreeTimer.SignalName.Timeout);
				GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/horror_window.png");
			}
			await Until(()=>_horror.CompletedBeats==2,8);
			var figure=_horror.GetNode<Node3D>("AisleFigure");
			await Until(()=>figure.Visible,4);
			if(capture) {
				_player.FaceTowards(figure.GlobalPosition+Vector3.Up*1.75f);
				await ToSignal(GetTree().CreateTimer(.45),SceneTreeTimer.SignalName.Timeout);
				GetViewport().GetTexture().GetImage().SavePng("res://.godot/qa/horror_aisle.png");
			}
			await Until(()=>_horror.CompletedBeats==3,4);
			Require(!watcher.Visible&&!figure.Visible,"Apparitions disappear");
			Require(_worldRoot.GetNode<Scenery>("WindowLandscape").Speed>0,"Landscape restored");
			foreach(var n in _worldRoot.GetChildren()) if(n is FlickerLight light) Require(light.Dead==light.Permanent,"Lamp state restored");
			// Restart during a cue, then enter dialogue: the encounter must release its visuals and sound.
			LoadCarriage(); await Until(()=>!_busy,4); _horror.FirstDelay=0;
			await Until(()=>_horror.Active,2);
			var speaker=Current.People.First(person=>person.Dialog!=null);
			StartTalk(speaker.Dialog,speaker.Id,()=>speaker.Alias);
			await ToSignal(GetTree().CreateTimer(.2),SceneTreeTimer.SignalName.Timeout);
			Require(!_horror.Active&&_inTalk,"Encounter cancelled on actual dialogue entry");
			EndTalk(); UserSettings.Scares=0;
			_horror.WindowDelay=0;
			await ToSignal(GetTree().CreateTimer(.3),SceneTreeTimer.SignalName.Timeout);
			Require(!_horror.Active&&!_horror.GetNode<Node3D>("WindowWatcher").Visible,"Scare setting disables appearances");
			UserSettings.Scares=setting;
			GD.Print("PASS horror cues, window encounter, aisle apparition, restoration, cancellation and scare setting"); QuitGame();
		}
		catch(Exception ex) { UserSettings.Scares=setting; GD.PushError(ex.ToString()); QuitGame(1); }
	}
}
