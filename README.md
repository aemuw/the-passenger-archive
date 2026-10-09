# The Passenger Archive

First-person PS1-style horror anthology on a train that never stops. Every carriage holds someone's story —
told as dialogue, an overheard conversation, a memory you walk through. Stories are fictional, inspired by
real cases, or (later) documented. Some carriages hide a rule that comes straight out of the victim's story.
Nobody tells you the rule. Break it and the killer does to you what he did to the victim.

You are not a detective. You are a passenger. You listen, you watch, you remember — and one man has been
riding with you from carriage to carriage since the very start.

**Engine:** Godot 4.7 (.NET / C#) · **Renderer:** Mobile · **Status:** vertical-slice prototype (4 carriages)

## Run
1. Install Godot 4.7.x **.NET** build and the .NET 8 SDK.
2. Open `project.godot`, let it build the C# solution (hammer icon → *Build*), press **F5**.
3. If NuGet complains about the SDK version, edit the first line of `the-passanger-archive.csproj`
   (`Godot.NET.Sdk/4.7.2`) to match your editor version exactly.

## Controls
| Key | Action |
|---|---|
| WASD / arrows, mouse | move, look |
| E | talk / use / answer |
| F | alternative action (only shown when it exists) |
| Tab | your ticket (the destination is erased — it slowly comes back) |
| R | retry after death / restart after the end |
| Esc | release / capture the mouse |

## The prototype carriages
| # | Type | Structure | What it shows |
|---|---|---|---|
| 1 | Fiction | one person you can talk to + a **played memory** | Viktor's way home, four evenings, one more sign each time. Hidden rule: a phone rings in the carriage. |
| 2 | Inspired | two people talking to each other; you **overhear** and may interrupt | a widow and her friend with flowers; a stuck door that a friendly stranger helps you open. |
| 3 | Inspired | five voices that contradict each other | victim, newspaper reader, witness who "could do nothing", a liar, a friendly man who offers to guide you when the lights go out. |
| 4 | Hero | the conductor opposite you | your memories: boarding in a hurry, and the empty terminus. Ends with *TRAIN / FINAL MEMORY*. |

Always true: doors lock behind you (the train never goes back) · the conductor never explains and never lies ·
the victim cannot be saved · after carriages 2–3 a card shows how the story relates to reality
(FICTIONAL / INSPIRED / DOCUMENTED + *real-world context*).

## The man who rides with you
In carriages 1, 2, 3 and in your own memories there is one man in different clothes:
long coat → shirt with a newspaper → brown coat by the door. Same tells every time:
he adjusts his cuff, and he says *«Далі — то далі.»* Nothing marks him as dangerous. In carriage 2 he is kind to you.

## How it works
- Everything is built in code (no imported assets): `scenes/Main.tscn` only holds the `Game` node.
- **PS1 look:** vertex-snapping shader, world-space grime texture, internal render at window/3, Bayer dithering,
  colour quantisation, grain, scanlines, vignette. Reality = warm yellow-green, memory = cold blue.
- **Rule system:** a `RuleDef` per carriage (`RuleKind.PhoneRing`, `RuleKind.OfferGuide`). Violation →
  lamps die, everyone freezes, the clacking stops, one flash of the killer standing in front of you, death text with the victim's method.
- **Authoring:** edit `scripts/Data/Carriages.cs` — people, lines (`LineSet` blocks that unlock by flags), overheard scripts, rules, cards.
  Lines can carry effects: `done` (story complete → door unlocks), `memory:<id>`, `unstick`, `flag:<x>`.
  Memories are in `scripts/World/MemoryScene.cs`.

## Layout
```
scenes/Main.tscn            single scene
scripts/Game.cs             flow: carriages, memories, doors, rule/death, ending
scripts/Data/               CarriageData (types), Carriages (content)
scripts/World/              CarriageBuilder, MemoryScene, FigureBuilder, Scenery, FlickerLight, TrainAudio, Geo, Ps1Materials
scripts/Actors/             Player, Passenger
scripts/Interact/           Interactable, DoorInteractable, ActionProp
scripts/UI/Hud.cs
shaders/                    ps1.gdshader, post_ps1.gdshader
docs/DESIGN.md              concept, structure, world rules, art/sound, roadmap
```

## Git workflow
`main` is stable; work in `feature/*` branches. Commit messages in English, imperative mood.
