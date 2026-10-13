# Asset origins

All resources added in this scene migration are project-authored. No third-party packs, paid assets or AI image generations were used.

| Resources | Origin | Notes |
| --- | --- | --- |
| Carriages and memories | Existing project geometry reorganised into saved scenes | Editable layout and colliders; still a prototype art baseline |
| Bench | New project-authored mesh and scene | Hand-defined faceted corners, frame and upholstery seam |
| Character models | Existing project designs with tapered anatomy and saved clips | Low-poly; no scanned people or likeness assets |
| Face PNGs and heads | Hand-defined pixel drawing and faceted mesh in tools/FaceAuthoring.cs | Same face resource across clothing variations; editable 64 × 64 PNGs, no image generator |
| Roof, window frames, luggage racks and cases | Project-authored mesh layout | Saved into carriage scenes; constrained railway palette |
| Surface PNGs | Existing surface designs exported to files | Replaceable, nearest-filtered |
| Blood SVG | New hand-defined vector paths | Transparent irregular silhouette |
| WAV clips | Existing original synthesis exported to files | Not field recordings or voice acting |
| Menu and HUD | Godot controls and original layouts | Real text; engine default font |
| Tailoring and updated surface PNGs | Drawing rules in tools/ArtAuthoring.cs | Saved 128px images; no AI image generation |
| Hollow passenger | Project-authored character variant and mask | Saved scene; no borrowed game characters |
| Horror WAV cues | Original sound designs in tools/HorrorAuthoring.cs | Saved assets; no field recordings or voice actor |
| Menu window SVG | Explicit vector paths in menu_window.svg | Railway frame and reflection; no embedded lettering |
| Memory architecture and rain | Scene details in tools/MemoryArt.cs | Saved meshes and particle systems |

These resources follow the repository's licensing terms. The repository does not currently declare a licence; this table does not assign a new one. Godot's bundled default font remains governed by the engine distribution's font notices.
