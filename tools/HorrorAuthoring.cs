using Godot;

namespace PassengerArchive;

public static class HorrorAuthoring
{
	public static void Media()
	{
		const int rate=22050;
		foreach(string name in new[] { "knock", "breath", "scrape", "sting" })
		{
			float duration=name=="breath"?4:name=="scrape"?2.2f:1.4f;
			int n=(int)(rate*duration); var data=new byte[n*2];
			var rng=new RandomNumberGenerator { Seed=918 }; float lp=0;
			for(int i=0;i<n;i++)
			{
				float t=(float)i/rate; float noise=rng.RandfRange(-1,1); lp+=(noise-lp)*.03f;
				float v=0;
				if(name=="knock") foreach(float hit in new[] { .08f,.39f,.96f }) { float d=t-hit; if(d>=0) v+=(Mathf.Sin(d*Mathf.Tau*110)*.45f+noise*.16f)*Mathf.Exp(-d*32); }
				else if(name=="breath") v=lp*Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.Pi*.55f)),2)*.9f;
				else if(name=="scrape") v=(Mathf.Sin(t*Mathf.Tau*(370+22*Mathf.Sin(t*8)))*.18f+lp*.3f)*Mathf.Sin(t/duration*Mathf.Pi);
				else v=(Mathf.Sin(t*Mathf.Tau*47)*.4f+Mathf.Sin(t*Mathf.Tau*181)*.18f+lp*.4f)*Mathf.Exp(-t*4)*Mathf.Min(1,t*80);
				v*=Mathf.Min(1,(duration-t)*60); short sample=(short)(Mathf.Clamp(v,-.85f,.85f)*32767);
				data[i*2]=(byte)(sample&255); data[i*2+1]=(byte)(sample>>8);
			}
			using var clip=new AudioStreamWav { Format=AudioStreamWav.FormatEnum.Format16Bits,MixRate=rate,Data=data };
			clip.SaveToWav($"res://assets/audio/horror_{name}.wav");
		}
	}
	public static void Save()
	{
		var figure=new Node3D { Name="HollowPassenger" }; FigureBuilder.Build(figure,Carriages.HitchLook,false);
		var head=figure.GetNode<Node3D>("Upper/Head"); head.RotationDegrees=new Vector3(9,0,14);
		head.GetNode<MeshInstance3D>("Face").MaterialOverride=Ps1Materials.Get(new Color(.62f,.63f,.54f),.07f,.08f,Surface.Plaster);
		foreach(float side in new[] { -1f,1f })
			Geo.Box(head,new Vector3(side*.047f,.157f,-.131f),new Vector3(.034f,.023f,.006f),new Color(.015f,.018f,.02f),0,false,0);
		Geo.Box(head,new Vector3(0,.065f,-.132f),new Vector3(.052f,.032f,.008f),new Color(.023f,.021f,.02f),0,false,0);
		head.GetNode<Node3D>("CapCrown").Hide(); head.GetNode<Node3D>("CapBrim").Hide();
		figure.Scale=new Vector3(.86f,1.14f,.85f);
		AssetAuthoring.Save(figure,"res://scenes/effects/HollowPassenger.tscn"); figure.Free();
	}
	public static HorrorDirector Create(int carriage)
	{
		var root=new HorrorDirector { Name="Horror",Carriage=carriage };
		foreach(string name in new[] { "WindowWatcher","AisleFigure" }) { var fig=SceneAssets.Load<Node3D>("res://scenes/effects/HollowPassenger.tscn"); fig.Name=name; fig.Visible=false; root.AddChild(fig); }
		foreach(string name in new[] { "Knock","Breath","Sting","Scrape" })
			root.AddChild(new AudioStreamPlayer3D { Name=name,Stream=GD.Load<AudioStream>($"res://assets/audio/horror_{name.ToLowerInvariant()}.wav"),Bus="Effects",VolumeDb=name=="Sting"?-15:-12,MaxDistance=16,UnitSize=4 });
		return root;
	}
}
