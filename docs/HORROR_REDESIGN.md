# Horror and visual redesign

The current four-carriage slice has a finite suspense sequence authored into each carriage's `Horror` node. These are nonlethal disturbances of the archive, not new hidden rules or clues that identify Maksym. His face and sleeve tell remain consistent across clothing changes.

## Encounters

1. Knocks from the empty rear vestibule are answered from the opposite end. The nearest awake passenger turns towards the sound.
2. Breathing precedes a hollow passenger outside a window. The external landscape briefly stops moving while the train continues sounding. The watcher disappears after being looked at or after a bounded delay. Placement follows actual window openings.
3. A scrape and breath prepare a brief aisle apparition. Working lamps extinguish, a pale face appears with a restrained impact cue, then the figure vanishes and those lamps recover. Normal play never forces the camera towards it.

The hollow passenger is an anonymous, incomplete presence in the archive. It does not explain the train or alter a victim's past. Lethal consequences still belong to existing story rules.

`FirstDelay`, `WindowDelay` and `AisleDelay` are editable in each saved carriage. They count eligible exploration time. Conversations, overheard scripts, ringing telephone, ticket, pause, memories, transitions and death prevent encounters. Interruption restores scenery and only the lamps the encounter extinguished. Each beat runs at most once per loaded carriage; a retry loads a fresh carriage.

**Різкі появи та звуки** reduces the impact cue level; zero disables this sequence. Image distortion controls also reduce normal lamp flicker. Neither setting removes story information. No repeated white strobe or fullscreen monster image is used.

## Saved art and sound

- `scenes/effects/HollowPassenger.tscn`: editable apparition meshes and materials. WindowWatcher and AisleFigure are saved instances.
- `assets/audio/horror_*.wav`: four spatial knock, breath, scrape and impact sounds. These are original synthesized assets prepared during development, not runtime synthesis, actor voices or field recordings.
- Cast models: shaped coats with a UV atlas, segmented limbs, bent seated legs and forearms, shorter necks, revised heads, selected age details and bob/tied hairstyles. Recurring characters keep their facial identity across outfits.
- Surface PNGs: 128px restrained grain, wood, fabric, plaster, metal and floor drawings. `tailoring.png` gives coats seams, lapels and pockets.
- Key carriage lamps cast shadows. Street memories have framed windows, entrances, curbs and pipes; stations have beams, benches and real Ukrainian signs. Walk and ride include saved rain particles.
- Menu: a drawn railway window and shared palette. Interaction prompts sit at the bottom. All writing remains real Godot text.

## Editing and verification

After backing up manual edits, run `--bake-art-media`, import in the Godot editor, then `--bake-redesign`. These overwrite their PNG/WAV and world/model/menu/HUD destinations; dialogue JSON is preserved. Normal play never invokes authoring tools.

Build C#, then run `--headless --path . -- --verify` for controller checks and `--headless --path . -- --verify-horror` for encounters. Horror verification checks actual appearances, scenery and lamp restoration, interruption and disabled-scare setting. `--capture-horror` captures images under `.godot/qa` using accelerated QA timing in a graphical run; regular play retains the scene delays.

Automated success verifies behavior, not whether every player finds the game frightening. Complete manual playthrough, headphone listening and pacing feedback remain necessary. This redesign covers the existing slice, not the eventual 10–15 carriage expansion.
