using Godot;

namespace PassengerArchive;

public static class MemoryArt
{
	public static void Dress(MemoryScene scene, string id)
	{
		var art=new Node3D { Name="ArchitectureDetails" }; scene.AddChild(art);
		Color stone=new(.19f,.21f,.23f), frame=new(.14f,.16f,.17f);
		if(id is "route" or "walk")
		{
			foreach(int side in new[] {-1,1})
			{
				Geo.Box(art,new Vector3(side*2.8f,.09f,0),new Vector3(.22f,.18f,id=="walk"?58:36),stone,0,false,.08f,Surface.Plaster);
				for(int block=0;block<5;block++)
				{
					float z=id=="walk"?24-block*8:-16+block*8;
					Geo.Box(art,new Vector3(side*2.89f,1.05f,z-2.6f),new Vector3(.14f,2.1f,.82f),frame,0,false,.08f,Surface.Wood);
					Geo.Box(art,new Vector3(side*2.78f,1.1f,z-2.3f),new Vector3(.04f,.13f,.035f),new Color(.49f,.42f,.28f),0,false,.08f);
					Geo.Box(art,new Vector3(side*2.9f,2.2f,z+3.1f),new Vector3(.09f,4.4f,.09f),frame,0,false,.08f,Surface.Metal);
					for(int floor=0;floor<2;floor++)for(int win=0;win<2;win++)
					{
						float y=2.65f+floor*1.75f, wz=z-1.25f+win*2.5f;
						Geo.Box(art,new Vector3(side*2.94f,y,wz),new Vector3(.10f,1.15f,.90f),frame,0,false,.04f,Surface.Wood);
						Geo.Box(art,new Vector3(side*2.87f,y,wz),new Vector3(.015f,.94f,.69f),new Color(.045f,.075f,.12f),.05f,false,.04f);
						Geo.Box(art,new Vector3(side*2.84f,y,wz),new Vector3(.04f,.95f,.035f),stone,0,false,.04f);
						Geo.Box(art,new Vector3(side*2.84f,y-.60f,wz),new Vector3(.22f,.07f,1.05f),stone,0,false,.08f);
					}
				}
			}
		}
		if(id is "depart" or "arrive")
		{
			for(int beam=0;beam<6;beam++) {
				float z=-16+beam*6;
				Geo.Box(art,new Vector3(0,4.35f,z),new Vector3(8,.16f,.16f),frame,0,false,.08f,Surface.Metal);
				var bench=SceneAssets.Load<Node3D>("res://scenes/props/Bench.tscn"); bench.Position=new Vector3(2.8f,0,z); bench.RotationDegrees=new Vector3(0,-90,0); art.AddChild(bench);
			}
			art.AddChild(new Label3D { Name="PlatformSign",Text=id=="arrive"?"ВИХІД":"ПОСАДКА", Position=new Vector3(0,3.7f,-9),FontSize=48,PixelSize=.004f,Modulate=new Color(.74f,.73f,.58f),OutlineSize=3 });
		}
		if(id=="ride")
		{
			foreach(var n in scene.GetChildren()) if(n is MeshInstance3D mesh&&mesh.Mesh is BoxMesh box&&box.Size==new Vector3(1.6f,1.4f,1.6f))
				mesh.Mesh=new CylinderMesh { TopRadius=0,BottomRadius=1.05f,Height=3.0f,RadialSegments=6,Rings=1 };
		}
		if(id is "ride" or "walk")
		{
			var rain=new GpuParticles3D { Name="Rain",Position=new Vector3(0,5,0),Amount=500,Lifetime=1.4,VisibilityAabb=new Aabb(new Vector3(-10,-8,-40),new Vector3(20,16,80)),
				ProcessMaterial=new ParticleProcessMaterial { EmissionShape=ParticleProcessMaterial.EmissionShapeEnum.Box,EmissionBoxExtents=new Vector3(7,.2f,32),Direction=Vector3.Down,Spread=4,InitialVelocityMin=8,InitialVelocityMax=10,Gravity=new Vector3(0,-2,0) },
				DrawPass1=new QuadMesh { Size=new Vector2(.009f,.16f),Material=new StandardMaterial3D { ShadingMode=BaseMaterial3D.ShadingModeEnum.Unshaded,AlbedoColor=new Color(.43f,.55f,.65f,.17f),Transparency=BaseMaterial3D.TransparencyEnum.Alpha,BillboardMode=BaseMaterial3D.BillboardModeEnum.Enabled } } };
			art.AddChild(rain);
		}
	}
}
