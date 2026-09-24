# World format — map

The long-term direction for this project, and where the current work sits
against it. Written 2026-08-14, at the end of the Dungeon refactor.

## Where this is heading

Turn this from one Gauntlet-style game into a **general engine that can run
many kinds of worlds** — fantasy dungeons, space stations, whatever — where a
world is *described in text* rather than built in C#.

The end state:

1. A user describes a world in as much or as little detail as they like.
2. An LLM writes a full world specification in a text format.
3. The engine loads that specification at startup or on demand.
4. The user plays it.

The format must be readable and authorable by both humans and LLMs.

## Decisions already made

Settled during a grilling session on 2026-08-14. These are decided — don't
re-litigate them without a reason.

| Decision | Answer |
|---|---|
| First milestone | **Reproduce the current 3-room dungeon exactly, from data.** No new features, no LLM, no new themes. A hard pass/fail: it plays identically or it doesn't. |
| Format vocabulary | **Generic and spatial.** Rooms/regions with typed contents. "goblin" vs "alien" is a name and a colour, not a schema change. Themed vocabularies (bulkhead, airlock) were rejected — they multiply surface area and give an LLM more ways to be wrong. |
| Engine/game split | **Conceptual for now.** One assembly, written so nothing dungeon-specific leaks into the core. A structural `Engine`/`Game` asmdef split is cheap later and the compiler will find every leak. |
| Behaviour changes | **Kept separate from structural work.** Refactors preserve behaviour exactly; known oddities get their own tickets. |
| Enemy HP | **Deferred.** See [#4](https://github.com/rcodesmith/grid-runner/issues/4) — it's a prerequisite for data-driven enemy variety, but it changes how the game plays. |

Motivation for the cleanup generally: **preventive**, getting ahead of technical
debt. Adding room 3 was not painful. There is no fire — don't over-scope.

Appetite: **one or two sittings at a time, then reassess.**

## Decisions so far

- **2026-08-14 — Dungeon module extracted** (commit `3277ba6`, branch
  `dungeon-module`). Room geometry is now runtime data rather than compile-time
  constants. `Dungeon` is plain C# holding rooms + doorways and answering
  `RoomAt` / `RouteTo` / `CornerAnchors`. `GauntletDungeon.Build()` seeds it
  with today's three rooms.

  **This is the seam the world file plugs into.** Whatever loads a world file
  produces a `Dungeon`; everything downstream already only knows about that.

  Topology independence is real and tested: four-room and vertical-doorway
  layouts both route correctly, neither expressible before.

## Next step

**Load the current dungeon from a text file.**

`GauntletDungeon.Build()` is the only thing standing between the engine and
data-driven worlds — it's a 60-line file with an obvious shape. Replacing its
hardcoded rooms with a parsed file achieves the first milestone above.

Notes for whoever picks this up:

- `UnityEngine.JsonUtility` is **already available** (pulled in transitively via
  ugui — no manifest change needed). Its limits matter though: no dictionaries,
  no polymorphism, no top-level arrays. Worth weighing against adding
  Newtonsoft, especially since an LLM-authored format will want nesting.
- There is currently **no `Assets/Resources/` or `StreamingAssets/`** folder —
  no location exists yet from which a runtime text file would load.
- `GeometryParityTests.cs` and `LegacyGeometryParityTests.cs` are scaffolding.
  They pin the new code to the old `GameConfig` room constants, so they will
  **block** removing those constants. Delete both as part of this work.
- Spawn-point placement currently uses a `KeepsCorner` rule invented during the
  refactor (first room's left corners, middle rooms' right, last room all four).
  It reproduces today's layout exactly but is not a real design rule — in a
  world file, spawn positions should be declared explicitly and this rule
  disappears.

## Fog

Open questions, not yet decided:

- **What the format actually looks like.** JSON? YAML? Something more
  prose-like that an LLM writes more naturally? Undecided.
- **How much a world file declares vs. the engine infers.** Does the file list
  every wall, or just rooms and doorways with walls derived (as `Bootstrap`
  does today)? Leaning toward the latter — less for an LLM to get wrong.
- **Where entity types are defined.** A world file says "an armoured enemy" —
  does the same file define what that means, or is there a separate bestiary?
- **Tilemap.** The original README roadmap says "migrate to Tilemap" for
  tile-based rooms. This is now understood as a *rendering/collision* decision
  downstream of the format, not a blocker for it. May never be needed.
- **Non-linear topologies.** `Dungeon` supports them; nothing else has been
  designed for them (camera framing, spawn placement, the format itself).

## Remaining architecture candidates

From the 2026-08-14 architecture review. Candidate 1 is done (the Dungeon
module above). The rest, in the review's recommended order:

- **Candidate 2 — `SpawnSchedule`** (Strong). The spawn ramp is pure arithmetic
  trapped in a MonoBehaviour, coordinated through the static mutable
  `SpawnPoint.All` list with three write sites. Same shape as the Dungeon work.
- **Candidate 3 — `Round`** (Worth exploring). Three modules reach into
  `GameManager.Instance` and already disagree about what "round over" means (see
  [#2](https://github.com/rcodesmith/grid-runner/issues/2)). Would also give
  `Dungeon.Current` a better home than a static.
- **Candidate 4 — actor factory** (was Speculative, now stronger). Five spawn
  factories repeat the same six-step recipe. "Spawn an entity from a spec" is
  exactly what loading `{"type": "enemy", "at": [3,4]}` needs, so the
  world-format direction promotes this.

Known deliberate compromise: `Dungeon.Current` is a static, the same pattern the
review criticised in `GameManager.Instance`. Chosen because enemies are created
at runtime by spawn points and threading the dungeon through every factory was
more churn than that refactor warranted. Revisit with candidate 3.
