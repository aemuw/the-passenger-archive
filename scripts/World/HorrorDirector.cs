using Godot;

namespace PassengerArchive;

/// <summary>Finite, staged encounters. All geometry and spatial sounds live in the saved scene.</summary>
public partial class HorrorDirector : Node3D
{
	[Export] public int Carriage { get; set; }
	[Export] public float FirstDelay { get; set; } = 9;
	[Export] public float WindowDelay { get; set; } = 24;
	[Export] public float AisleDelay { get; set; } = 55;
	public Game GameRef;
	public int CompletedBeats { get; private set; }
	public bool Active => _active;
	Node3D _watcher, _figure;
	AudioStreamPlayer3D _knock, _breath, _sting, _scrape;
	Scenery _landscape;
	float _time, _beat, _seen, _speed;
	bool _active, _revealed;
	readonly System.Collections.Generic.List<FlickerLight> _darkened = new();
	public override void _Ready()
	{
		_watcher = GetNode<Node3D>("WindowWatcher"); _figure = GetNode<Node3D>("AisleFigure");
		_knock=GetNode<AudioStreamPlayer3D>("Knock"); _breath=GetNode<AudioStreamPlayer3D>("Breath");
		_sting=GetNode<AudioStreamPlayer3D>("Sting"); _scrape=GetNode<AudioStreamPlayer3D>("Scrape");
		_landscape=GetParent().GetNode<Scenery>("WindowLandscape"); _speed=_landscape.Speed;
		_watcher.Hide(); _figure.Hide();
	}
	public override void _Process(double delta)
	{
		if(GameRef==null) return;
		if(UserSettings.Scares<=0) { if(_active) Cancel(); return; }
		if(!GameRef.HorrorAvailable) { if(_active) Cancel(); return; }
		float dt=(float)delta; _time+=dt;
		if(!_active)
		{
			float threshold = CompletedBeats==0?FirstDelay:CompletedBeats==1?WindowDelay:AisleDelay;
			if(CompletedBeats>=3||_time<threshold) return;
			Begin();
		}
		_beat+=dt;
		if(CompletedBeats==0)
		{
			if(_beat>1.7f && !_revealed) { _revealed=true; _knock.Position=new Vector3(0,1.3f,-6.7f); _knock.Play(); }
			if(_beat>3.5f) Finish();
		}
		else if(CompletedBeats==1)
		{
			if(_beat>1.5f && !_revealed) { _revealed=true; _watcher.Show(); _landscape.Speed=0; _breath.Play(); }
			if(_revealed && GameRef.SeesHorrorPoint(_watcher.GlobalPosition+Vector3.Up*1.5f)) _seen+=dt;
			if(_seen>.65f) { _scrape.Play(); _watcher.Hide(); }
			if(_beat>7f || _seen>1.5f) Finish();
		}
		else
		{
			if(_beat>2.2f && !_revealed)
			{
				_revealed=true; _figure.Show(); _sting.VolumeDb=Mathf.Lerp(-30,-15,UserSettings.Scares); _sting.GlobalPosition=_figure.GlobalPosition+Vector3.Up; _sting.Play();
				foreach(var n in GetParent().GetChildren()) if(n is FlickerLight light && !light.Permanent && !light.Dead) { _darkened.Add(light); light.Extinguish(); }
			}
			if(_beat>3.05f) { _figure.Hide(); RestoreLights(); }
			if(_beat>4.5f) Finish();
		}
	}
	void Begin()
	{
		_active=true; _beat=0; _seen=0; _revealed=false;
		if(CompletedBeats==0) { _knock.Position=new Vector3(0,1.3f,6.8f); _knock.Play(); React(_knock.GlobalPosition); }
		else if(CompletedBeats==1) {
			float side=Carriage%2==0?1:-1;
			int window=Mathf.Clamp(Mathf.FloorToInt((GameRef.PlayerPosition.Z-1.2f+5.6f)/1.6f),0,7);
			_watcher.Position=new Vector3(side*2.02f,-.22f,CarriageBuilder.RowZ(window));
			_watcher.Rotation=new Vector3(0,side*Mathf.Pi/2,0);
			_breath.Position=_watcher.Position;
		}
		else {
			float direction=GameRef.PlayerPosition.Z>0?-1:1;
			_figure.Position=new Vector3(0,0,Mathf.Clamp(GameRef.PlayerPosition.Z+direction*2.6f,-6.1f,6.1f));
			_figure.Rotation=new Vector3(0,direction<0?Mathf.Pi:0,0);
			_breath.Position=_figure.Position; _breath.Play();
			_scrape.Position=new Vector3(0,0,6.5f); _scrape.Play();
		}
	}
	void React(Vector3 point)
	{
		Passenger nearest=null; float distance=float.MaxValue;
		foreach(var n in GetParent().GetChildren()) if(n is Passenger person && person.Data?.Idle!=Gesture.Sleep) {
			float d=person.GlobalPosition.DistanceTo(point); if(d<distance) { nearest=person; distance=d; } }
		nearest?.ReactTo(point,3);
	}
	void RestoreLights() { foreach(var light in _darkened) if(GodotObject.IsInstanceValid(light)) light.Revive(); _darkened.Clear(); }
	void Finish() { CompletedBeats++; _active=false; _watcher.Hide(); _figure.Hide(); _landscape.Speed=_speed; RestoreLights(); _breath.Stop(); }
	public void Cancel() { if(_active) Finish(); _knock?.Stop(); _scrape?.Stop(); _sting?.Stop(); }
	public override void _ExitTree() { foreach(var n in GetChildren()) if(n is AudioStreamPlayer3D p) { p.Stop(); p.Stream=null; } }
}
