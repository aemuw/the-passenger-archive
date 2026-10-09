# The Passenger Archive

First-person PS1-style horror in an endless train. Walk through carriages, listen to passengers,
collect their stories and find the hidden killer — but break the carriage rule and the killer
kills *you* the same way they killed the victim.

**Engine:** Godot 4.7 (.NET / C#) · **Renderer:** Mobile · **Status:** vertical-slice prototype (2 carriages)

## Run
1. Install Godot 4.7.x **.NET** build and the .NET 8 SDK.
2. Open `project.godot`, let it build the C# solution (hammer icon → *Build*), press **F5**.
3. If NuGet complains about the SDK version, edit the first line of `the-passanger-archive.csproj`
   (`Godot.NET.Sdk/4.7.2`) to match your editor version exactly.

## Controls
| Key | Action |
|---|---|
| WASD / arrows | move |
| Mouse | look |
| E | talk / examine / use |
| F | accuse the passenger you are looking at (needs enough clues) |
| Tab | notebook (ticket, rule, clues) |
| R | retry after death / restart after the end |
| Esc | release / capture the mouse |

## What the prototype contains
- **Procedural carriage** (no imported assets): shell, seats, windows, luggage shelves, flickering lamps (some dead).
- **PS1 look:** vertex-snapping spatial shader, world-space grime texture, internal render at window/3,
  Bayer dithering + colour quantisation + grain + scanlines + vignette (`shaders/`).
- **Night outside the windows:** poles/trees streaming past; in carriage II a lit house that never moves.
- **Passengers:** box-built figures, faces half in shadow, line-by-line dialogue, clues go to the notebook.
  The killer turns their head to follow you only while you are *not* looking at them.
- **Carriage rule:** printed above the front door. Breaking it → lights die → death screen describing
  the victim's method used on you.
- **Accusation:** wrong accusation also kills (`GameConfig.WrongAccusationKills`). Right one unlocks the front door.
- **Procedural train audio** (rumble + wheel clacks) — replace with recordings later.
- Back door never opens: *the train never goes back*.

## Layout
```
scenes/Main.tscn          single scene; everything else is built in code
scripts/Game.cs           root: view, input, game rules, carriage flow
scripts/Data/             Carriages.cs  ← edit stories, rules, passengers here
scripts/World/            CarriageBuilder, Scenery, FlickerLight, TrainAudio, Ps1Materials
scripts/Actors/           Player, Passenger
scripts/Interact/         Interactable + door / rule prop / victim
scripts/UI/Hud.cs
shaders/                  ps1.gdshader, post_ps1.gdshader
docs/DESIGN.md            concept, world rules, art/sound direction, roadmap
```

## Git workflow
`main` is stable; work in `feature/*` branches. Commit messages in English, imperative mood.
