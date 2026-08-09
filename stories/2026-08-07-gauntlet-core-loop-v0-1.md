# Story: Gauntlet-Starter — Core Loop v0.1

## Description
Create a really simple 2D top-down Unity game in this empty folder — the playable kernel of a Gauntlet (1985)-style dungeon crawler. The first slice is the core loop only: move in 8 directions, shoot projectiles, enemies chase you, contact drains HP, game over, restart.

The user is new to Unity and does not have it installed yet, so the project must be **code-first**: pressing Play in a freshly opened project builds the entire scene from a bootstrap script — no editor wiring, no hand-authored scene content, no art assets. Every future Gauntlet system (dungeons, keys/doors, monster generators, health-as-timer, classes) layers onto this foundation without rearchitecting.

## Acceptance Criteria
- [ ] Opening this folder as a project in Unity Hub (Unity 6 LTS) and pressing Play starts the game with no manual editor setup
- [ ] Player moves in 8 directions with WASD/arrow keys and is blocked by the arena walls
- [ ] Player shoots projectiles in the facing direction with spacebar; projectiles destroy enemies on hit and disappear against walls
- [ ] Enemies spawn around the arena edges over time and chase the player
- [ ] Player has a visible HP bar; contact with enemies drains HP
- [ ] HP reaching 0 shows a Game Over screen; pressing R restarts the game with full state reset
- [x] All visuals are code-generated placeholder sprites (colored squares/circles) — zero imported art assets
- [ ] No errors or warnings from the game's own scripts in the Unity console during a full play/die/restart cycle
- [x] README documents install prerequisites, controls, and the Gauntlet roadmap

## Tasks
- [x] Scaffold minimal Unity project files: `Assets/Scripts/`, `Packages/manifest.json` (ugui + 2D sprite + built-in physics2d/audio modules; legacy Input, no Input System package), `ProjectSettings/ProjectVersion.txt` targeting Unity 6000.0.81f1, Unity-standard `.gitignore`
- [x] `Bootstrap.cs` — `[RuntimeInitializeOnLoadMethod]` entry point that creates the camera, arena walls, player, HUD, and spawner objects at Play time, all under a single `GameRoot`; exposes static `BuildWorld()` for restarts
- [x] `PlaceholderSprites.cs` — helper that generates square/circle `Sprite`s from `Texture2D` at runtime (player, enemy, projectile, wall colors)
- [x] `PlayerController.cs` — 8-direction movement via `Rigidbody2D`, tracks facing direction (last nonzero move direction, up at spawn)
- [x] `PlayerShooting.cs` + `Projectile.cs` — spacebar fires a projectile in the facing direction; projectile kills enemies, dies on walls, despawns after a lifetime; self-collision prevented via `Physics2D.IgnoreCollision`
- [x] `Enemy.cs` — chases the player via `Rigidbody2D` velocity; damages player on contact; dies when hit by a projectile
- [x] `EnemySpawner.cs` — spawns enemies at intervals at random points just inside the arena edges, ramping 2.0s → 0.6s over 60s
- [x] `PlayerHealth.cs` + `HudController.cs` — HP value, on-screen HP bar, contact damage with 0.75s invulnerability window + sprite flicker
- [x] `GameManager.cs` — game-over detection, Game Over UI, R-to-restart via destroy-`GameRoot`-and-`Bootstrap.BuildWorld()` (no scene reload — the bootstrap hook fires once per Play session)
- [x] `README.md` — Unity Hub + Unity 6 LTS install steps, how to open/play, controls, gameplay-constants table, and the roadmap of future stories toward Gauntlet
- [x] Validate: automated batch-mode compile check with Unity 6000.0.81f1 passed (0 errors, 0 warnings from project scripts); full manual play session per Validation Steps remains for the user (editor Play mode required)

## Implementation Notes
- **Unity 6 LTS (6000.x), 2D, built-in render pipeline or URP-lite defaults** — whatever the minimal manifest gives; avoid pipeline configuration complexity in v0.1.
- **Runtime bootstrap pattern**: `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]` builds everything in the default empty scene. This avoids hand-authoring `.unity` YAML and `.meta` GUID wiring entirely — the biggest risk area for a project generated outside the editor.
- **Prerequisite (user step)**: install Unity Hub, then Unity 6 LTS (Personal license) with default modules. The implementation can be written and committed before Unity is installed; validation requires the editor.
- **Project files are generated as text**; Unity regenerates `Library/`, remaining `ProjectSettings/` assets, and `.meta` files on first open. Keep `manifest.json` minimal (2D sprite, Input System *not* required — use legacy `Input` class for v0.1 simplicity).
- **Physics**: `Rigidbody2D` (gravity scale 0) + `BoxCollider2D`/`CircleCollider2D`; walls are static colliders. Layers/tags via strings set up in code where possible; avoid custom tags that require editor registration — use component checks (`GetComponent<Enemy>()`) instead of tag comparisons.
- **HUD** via a code-created world-space or screen-space `Canvas` with a simple filled-bar `Image`; Game Over text the same way.
- **Restart** via `SceneManager.LoadScene` of the active scene — bootstrap re-runs naturally if hooked to scene load, otherwise re-invoke setup explicitly from `GameManager`.
- **Roadmap (future stories, one Gauntlet system each)**: tile-based dungeon rooms with keys/doors/exit (migrate to Tilemap) → monster generators you destroy → health-drain-as-timer + food pickups → score/treasure → character classes → sound/polish.
- Expected layout: everything under `Assets/Scripts/` (11 small files — the ~9 above plus `GameConfig.cs` for tuning constants), plus `README.md`, `worklog.md` later via `/update-worklog`.

## Validation Steps
1. Open Unity Hub → Add project from disk → select this folder → open with Unity 6 LTS (first open will import; expect a few minutes).
2. Press Play. Confirm: arena, player, and HP bar appear; console shows no errors from project scripts.
3. Move with WASD and arrows — all 8 directions work; player cannot pass through walls.
4. Press spacebar — projectiles fire in the last-moved direction; they vanish on walls.
5. Wait for enemies to spawn; confirm they chase the player and die when shot.
6. Let enemies touch the player — HP bar drains; brief invulnerability flicker between hits.
7. Let HP reach 0 — Game Over screen appears; gameplay stops.
8. Press R — game restarts cleanly at full HP with no leftover enemies/projectiles.
9. Repeat a second play/die/restart cycle to confirm no state leaks.

## Completion Summary
- Shipped the full code-first core loop: 11 scripts under `Assets/Scripts/` build the entire game at Play time under a single `GameRoot` (camera, arena walls, floor, player, HUD canvas, spawner, game manager) — no scene authoring, no art assets.
- Restart deviates from the original note by design: `GameManager.Restart()` destroys `GameRoot` and calls `Bootstrap.BuildWorld()` again (no `SceneManager.LoadScene`, since the bootstrap hook fires once per Play session); game over pauses via `Time.timeScale = 0`.
- All tuning values (arena 24x14, player speed 6, enemy speed 3.5, 100 HP, 10 contact damage, 0.75s i-frames, spawn ramp 2.0s→0.6s/60s, projectile 12 speed/2s life) live in `GameConfig.cs` and are tabled in the README.
- Scope drift: `Packages/manifest.json` needed explicit `com.unity.modules.physics2d` and `com.unity.modules.audio` entries — a hand-authored minimal manifest does not enable built-in modules, which the first compile run proved (CS1069 on all 2D physics types).
- Validation: batch-mode compile with Unity 6000.0.81f1 succeeded — 0 compiler errors, 0 warnings from project scripts; `ProjectSettings` were generated by the editor on first import with legacy input active (`activeInputHandler: 0`).
- Manual Play-mode validation (Validation Steps 2–9: move/shoot/spawn/damage/game-over/restart cycles) remains for the user in the editor.
