using Godot;

namespace PassengerArchive;

/// <summary>Deterministic development drawings, exported to replaceable PNG assets.</summary>
public static class ArtAuthoring
{
	public static void Draw()
	{
		DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath("res://assets/textures"));
		foreach (Surface s in System.Enum.GetValues<Surface>())
		{
			using var img = Image.CreateEmpty(128,128,false,Image.Format.Rgb8);
			for (int y=0;y<128;y++) for (int x=0;x<128;x++)
			{
				float grain = Mathf.Sin(x*.67f+y*.23f)*.018f;
				float v = s switch {
					Surface.Wood => .87f + .055f*Mathf.Sin(x*.24f+Mathf.Sin(y*.035f)*1.7f) - (x%43==0 ? .10f:0),
					Surface.Fabric => .94f - (x%4==0 && y%4==0 ? .045f:0),
					Surface.Plaster => .94f - .05f*Mathf.Pow(Mathf.Max(0,Mathf.Sin(x*.045f)*Mathf.Cos(y*.035f)),2),
					Surface.Metal => .91f + .04f*Mathf.Sin(y*.13f) - (x==22 && y>16&&y<68 ? .18f:0),
					Surface.Floor => x%64<2||y%64<2 ? .58f : .86f + .035f*Mathf.Sin(x*.15f+y*.07f),
					_ => .97f };
				img.SetPixel(x,y,new Color(v+grain,v+grain,v+grain));
			}
			img.SavePng($"res://assets/textures/{s.ToString().ToLowerInvariant()}.png");
		}
		using var coat = Image.CreateEmpty(128,128,false,Image.Format.Rgb8); coat.Fill(new Color(.86f,.86f,.86f));
		for(int y=0;y<128;y++) for(int x=0;x<128;x++)
		{
			float v=.88f+(((x+y)%4==0)? .025f:0);
			if (Mathf.Abs(x-64)<2) v=.52f;
			if (y<53 && Mathf.Abs(Mathf.Abs(x-64)-(24-y*.33f))<2) v=.64f;
			if(y>76&&y<79&&(x>15&&x<44||x>84&&x<113)) v=.60f;
			if(Mathf.Abs(x-66)<2&&(y==56||y==78||y==100)) v=.27f;
			coat.SetPixel(x,y,new Color(v,v,v));
		}
		coat.SavePng("res://assets/textures/tailoring.png");
		using var grime = Image.CreateEmpty(128,128,false,Image.Format.Rgb8);
		for(int y=0;y<128;y++)for(int x=0;x<128;x++) {
			float v=.91f+.07f*Mathf.Sin(x*.071f)*Mathf.Cos(y*.052f); grime.SetPixel(x,y,new Color(v,v,v)); }
		grime.SavePng("res://assets/textures/grime.png");
		GD.Print("Saved restrained 128px surface textures and tailored clothing atlas.");
	}
}
