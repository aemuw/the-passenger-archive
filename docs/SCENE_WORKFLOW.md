# Editing the scene based prototype

The four carriage slice now loads saved scenes instead of building its world at runtime. Open a carriage directly in Godot's 3D editor to move seats, lights, people and collision shapes. A missing scene is an error; there is no procedural fallback.

## Scene locations

- `scenes/carriages/Carriage01.tscn` through `Carriage04.tscn`: the current four carriages. Passenger nodes keep their stable `PersonId`; their position and appearance are editable independently of story data.
- `scenes/memories/route.tscn`, `ride.tscn`, `walk.tscn`, `depart.tscn`, `arrive.tscn`: five played memories. `StartLocal` and `StartYaw` control the saved spawn. The controller binds existing actors, fixtures and the boarding interaction.
- `scenes/memories/route_evening0.tscn` through `route_evening3.tscn`: saved variations for the four evenings. They are loaded, not generated during play.
- `scenes/props/Bench.tscn`: reusable bench with tapered corners, individual supports, upholstery seams and separate collision shapes. Editing this instance source updates every carriage.
- `scenes/characters`: standing and seated character model scenes. Character models contain editable meshes and AnimationPlayer clips for breathing, speech gestures and the sleeve tell. Carriage actors currently embed the model so their appearance is directly editable; dynamic attack actors load the matching standing model.
- `scenes/actors/Player.tscn`: saved capsule, camera and interaction ray.
- `scenes/ui/ArchiveMenu.tscn` and `Hud.tscn`: editable menu and HUD layouts.
- `scenes/audio/TrainAudio.tscn`: ready audio players with replaceable streams and bus assignments.
- `scenes/effects/BloodPool.tscn`: transparent irregular pool used for pools and drops. `scenes/props/Knife.tscn` and `BluntProp.tscn` are the saved attack props; the saved `attack` animation replaces the original brief flash.

## Dialogue and story editing

`data/dialogues/*.json` is the runtime source for all conversations. Edit lines, options, conditions, flags and effect links there. These changes do not require C# compilation. Graph validation rejects missing links and duplicate IDs when conversations load.

When exporting an executable, include `data/dialogues/*.json` in Godot's non-resource export filter. Runtime JSON is read through FileAccess and must be present in the exported package.

NPC placement is controlled by the scene. `scripts/Data/Carriages.cs` still supplies character identities, story metadata, the overheard script and rule definitions. Its conversation definitions are authoring templates rather than the runtime text source. Do not regenerate dialogue files after making manual JSON edits unless those edits have been incorporated into the templates.

The first dialogue pass makes Viktor and Maksym less cryptic, revises Halyna's opening, and removes unsupported real-world claims. Most existing conversations are retained; this is not a claim that every line has received a final editorial pass.

## Presentation

The subsequent cast, texture, lighting and horror pass is documented in [HORROR_REDESIGN.md](HORROR_REDESIGN.md), including editable encounter timings, scare settings and reproduction commands. Current surface assets are 128px; faces remain 64px.

Muted green, paper and brown are shared between the carriage and menu. The menu is typeset from real Ukrainian text; it uses no generated image or fake lettering. Textures are editable PNGs, and blood is an authored SVG with a transparent background. All current resources originate in this project; no third-party artwork or music was downloaded.

The model pass adds faceted tapered anatomy, shaped heads, drawn pixel faces and saved gestures. Face PNGs are in `assets/textures/faces`; the same identity keeps its face when clothing changes. Glasses and facial features are drawn pixels rather than protruding black blocks. It remains a simple low-poly art baseline, with further bespoke character work still needed.

Carriages have angled roof shoulders and ribs, divided glazed windows, folded curtains, open metal luggage racks and a few deliberately placed cases. Warm lamps and cool window light share the same palette. Dust and grain are restrained, and the internal render is half the window size to preserve facial readability.

Audio files are WAV exports of the original sound designs, now loaded through saved audio players. They are synthesized source assets, not train field recordings or actor recordings. They can be replaced through the Inspector without changing the controller. Master, Ambience, Music and Effects buses are separate.

## Playing and persistence

Start with New Journey or Continue. Escape pauses and resumes; it preserves the current dialogue or memory. Settings include master volume, mouse sensitivity, image distortion, camera motion, blood intensity and dialogue text size.

Checkpoint saves are at carriage boundaries, including the start of a new journey. Loading resumes at the start of that carriage; mid-memory progress is not saved. A death retry restores run-wide flags and remembered names to the carriage entry state. Settings and checkpoint JSON live in Godot's `user://` directory.

## Verification

Build with `dotnet build`, then run Godot with `--headless --path . -- --verify`. The verification command checks dialogue links, checkpoint overwrite/round trip in an isolated QA file, all four carriage bindings, all five memory entries/exits, evening variants, rule death, retry, learning a name and a dialogue choice that enters a played memory and unlocks the next carriage.

The test exercises actual controllers and loaded scenes. It does not replace a complete manual playthrough of every choice, an audio listening pass, or collision inspection from every angle.

## Authoring tools

The development commands `--bake-media` and `--bake-assets` create the initial saved resources. They are never invoked during a normal run. **They overwrite their generated scene and dialogue destinations.** Use them only deliberately, before manual scene edits, or after backing up edits. Normal editing should happen in the saved scenes and resource files.

The project still contains development-only mesh and audio authoring helpers so source assets can be reproduced. Small runtime effects such as dust drift, scenery movement, head tracking and blood growth animate saved resources.

For the face/geometry pass, run `--bake-faces`, import the PNGs in the editor, then run `--bake-visuals`. The latter recreates character, bench, carriage and memory scenes but leaves dialogue JSON, menus, player and audio scenes alone. **It still overwrites manually edited world and character scenes.** Back up those edits before using it. The complete `--bake-assets` command also requires face PNGs to have been created and imported first.

## Remaining production work

The project is a four-carriage slice, not the full 10–15 carriage game. Documented cases, additional story formats, bespoke final character art, train recordings, actor voices and a full editorial pass are not yet supplied. Fictional story cards no longer pretend that unsourced placeholder summaries describe documented events.
