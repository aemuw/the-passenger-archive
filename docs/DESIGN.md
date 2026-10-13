# Design

## Scene based implementation

The October 2026 revision loads checked-in carriage, memory, character, player, UI and audio scenes. Layout, meshes, colliders, materials, AnimationPlayer clips, PNG textures and WAV streams are editable before play. Development authoring commands remain available but do not run during a normal session. See `SCENE_WORKFLOW.md` for locations, editing rules and verification limits.

The current scope remains the four-carriage slice. It now has a start menu, pause, accessibility controls and carriage checkpoints. Unsupported real-world placeholder summaries have been removed; additional documented stories require actual research before implementation.

## One line
You cannot change what happened. You can remember why.

## Premise
A train with no final station. Each carriage keeps one story — usually of someone who died. The hero is an
ordinary passenger who does not investigate; he listens, watches and walks through other people's memories.
Underneath the anthology runs the hero's own story: why he is on this train, and who has been riding with him.

## Structure of the game
- 10–15 carriages, 1.5–3 h, replayable.
- **No "one carriage = one victim + one killer" template.** A carriage may have:
  one person (talk + memory) · two people talking to each other (overheard, you may interrupt) ·
  5+ people with contradicting versions · only objects · a wrong version · a truth nobody believes ·
  the killer telling his own story · a newspaper read aloud · a memory of the hero.
- Not every story has a moral. Some: "this is why it is dangerous". Some: "wrong place, wrong time".
  Some: no explanation. Some: pure nightmare.
- Doors lock behind you. You can only go forward.

## Story kinds (shown on a card when a carriage ends)
FICTIONAL · INSPIRED (real types of situations, invented people) · DOCUMENTED (close to a specific case, sources
at the end) · NIGHTMARE (paranormal) · MEMORY (the hero). Inspired/Documented carriages end with a
*REAL-WORLD CONTEXT* note: what really happened, what is fiction, what to take from it.
Real stories are told truthfully — no sanitising — but never as a reconstruction that exploits a named victim.

## The hidden rule (a tool, not the formula)
- It is born from the victim's story, never from game design: not "you cannot go back", but
  "she always went back to check if anyone was behind her" — and the player does exactly that in the memory.
- The game never states it. First pass: the player likely breaks it and sees the killer. Second pass: knows.
- Consequence: lights die, passengers freeze, the clacking stops, the killer stands in front of you and does to
  the hero what he did to the victim. Then the carriage resets.
- The victim cannot be saved: *"Don't try to save me. You're not here for that."*
- The killer of a carriage stays on the train, among the passengers. The camera just lingers; the train goes on.

## The man who rides with the hero
The hero's own killer boards with him and is in the background of every carriage in different clothes/roles,
with fixed tells: a gesture (adjusts his cuff), a phrase («До кінця — то до кінця.»), a habit (near doors).
The player can talk to him normally in one carriage (he is kind, helps with a stuck door). Other characters
quote his phrase. He is never the "monster" — the fear is the moment you realise you already knew him.
Because doors lock behind, he can only be in one carriage at a time.

## Ending
Last carriages are the hero's memories — mirroring the first carriage ("talk → see their memory" becomes
"now see yours"): boarding in a hurry (missed calls, wrong train time, the man at the door), then the terminus.
The final memory replays the moment from another angle: the killer sat in the train from the start.
Post-credits idea: an empty carriage, a new passenger boards, and in the background sits the hero.

## World rules (never broken)
1. The train always moves. 2. You cannot go back. 3. Everyone has a reason to be here.
4. The conductor never lies — and never explains («Далі.» / «Пасажири.» / «А чому ти?»).
5. History cannot be changed. 6. Every carriage holds something from the past.
7. The hero cannot just get off.

## Look
First person, PS1 + late-90s survival horror + VHS unease. Palette: dirty yellow, grey-green, brown, cold blue.
Reality warm yellow-green · memory cold blue · danger amber/red · paranormal near-monochrome.
Light is the main tool (dead lamps, faces half in shadow). Silhouettes read from afar (long coats, big collars, hats, bags).
No HUD: only the ticket (destination erased, slowly returns).

## Sound
Train ambience ~90%; may thin out to silence before something happens but never fully vanishes.
Music only in key moments; one 3–5 note motif (menu, piano in a memory, distorted in nightmare, faint at the end).

## Roadmap
- [ ] More carriage types: objects only, wrong version, unheard truth, the killer's own story, newspaper
- [ ] A DOCUMENTED carriage with sources and a real-world context screen
- [ ] Fake window reflections (late, wrong clothes, someone who isn't there)
- [ ] Between-carriage vestibule + sound pass with real recordings, the main motif
- [ ] Autosave per carriage; settings/menu
- [ ] Real low-poly models and 64×64 textures; face shadows pass
- [ ] Carriages 5–15; the hero's story woven into earlier carriages
- [ ] Alternative endings for the last carriage (get off / bathroom / just sit)

## Prototype status vs. concept
Done in the slice (4 carriages): one-person carriage + played memory · two people you overhear · five voices that
contradict each other (victim, newspaper reader, "I could do nothing" witness, liar, friendly killer) · hidden rule
learned from the story · death by the victim's method · hero's killer in changing clothes with fixed tells and one
friendly conversation · doors lock behind you · conductor who only answers «Далі.» / «Пасажири.» / «А чому ти?» ·
FICTIONAL / INSPIRED card + real-world context · the hero's own memories and ending.
Not yet: DOCUMENTED carriage with sources · NIGHTMARE carriage · objects-only / wrong-version / unheard-truth carriages ·
fake window reflections · real models, textures, music.
**The `RealWorld` texts in `Carriages.cs` are placeholders** — replace them with sourced text before release.

## v3 changes
- **Strangers first.** Everyone is «Незнайомець / Незнайомка …» with a one-line description of what the hero sees; the real name appears only
  after a conversation reveals it (the ticket lists whom you got to know). The man who rides with you is never named.
- **Conversations with choices.** The hero picks what to say; answers change what people tell you (per-carriage flags) and some choices are
  kept for the whole run (`k:` flags). The killer at the terminus quotes them back («Ви казали, що не пам'ятаєте, звідки їдете»).
- **Memories in every story carriage:** route (Viktor) · ride (Halyna's imagined night; the hitch-hiker has no face because she never saw him) ·
  walk (Solomiya: say "no" twice, the third time the word won't come; the lamps die behind you) · depart/arrive (the hero).
- **The hero's reason:** he didn't wait for the 05:10 train with Marta (the person he loves) and didn't listen to her voicemail
  (optional, remembered). At the terminus her call rings with the same phone sound that ended Viktor.
- **The ending:** the man says «До кінця — то до кінця.» and kills the hero. Violence is shown (blood on the lens, pool on the floor, camera falls).
- **Rule deaths** also show it. The F key is gone: Taras's offer is now a conversation (go with him = rule broken).
- Door lamp: red = locked, green = story finished. No more "click" caption.

## v4 changes
- **New reason for the hero's trip.** He did not wait for Marta (05:10, together) because he answered an ad for her late father's camera («Зеніт»):
  night, cash, alone, "only tonight". The seller "Максим" is the man who rides with him; in carriages 1–3 he gives his name when asked —
  it is the same name as in the hero's phone in the depart memory. At the terminus he asks "ви — по «Зеніт»?" and quotes the hero's own answers back.
- **New phrase:** «До кінця — то до кінця.» (was «Далі — то далі.»). The conductor keeps saying «Далі.».
- **Every talkable person has a voice and a name you can ask for** (Максим always gives his; the conductor says "Провідник — імені тут ніхто не питає").
- **Characters live:** breathing, talking gestures, window glances, fidgeting, sleeping, barks when you linger. Better figures (hair, hands, shoes, glasses, beards, bags, build).
- **Look & feel:** procedural surface textures, lamp haze, dust, straps/curtains/posters/rails, VHS bleed + tracking glitch + "stress" pulse near a killer.
- **Sound & music:** footsteps, creaks, PA chime, wind/rain in memories, heartbeat near a killer, a five-note motif (D F A G F) in three forms, drone pad.
