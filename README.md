# Gauntlet-Starter — Core Loop v0.1

The playable kernel of a Gauntlet (1985)-style dungeon crawler, built in Unity 6 LTS. This first slice is the core loop only: move in 8 directions, shoot projectiles, enemies chase you, contact drains HP, game over, press R to restart.

The project is **code-first**: there is no hand-authored scene, no editor wiring, and zero imported art assets. Pressing Play runs `Assets/Scripts/Bootstrap.cs`, which builds the entire game (camera, arena, player, HUD, spawner) from code, using colored placeholder sprites generated at runtime.

## Prerequisites (one-time setup)

1. **Install Unity Hub** — download it from [unity.com/download](https://unity.com/download) and install it. Unity Hub is the launcher that manages editor versions and projects.
2. **Sign in and activate a license** — create/sign in to a Unity account inside the Hub, then activate a free **Personal** license (Hub prompts you; it will not open projects until a license is active).
3. **Install the editor** — in Hub go to **Installs → Install Editor** and install **Unity 6 LTS** (this project targets **6000.0.81f1**; any 6000.0.x works — Unity will offer to open with a close version). Default modules are fine; no extra modules are needed.

## Open and play

1. In Unity Hub: **Projects → Add → Add project from disk** and select this folder.
2. Open the project. **The first open imports packages and compiles scripts — expect a few minutes on a blank cache.**
3. Press the **Play** button at the top of the editor. The arena, player (green square), and HP bar appear immediately — no scene setup needed.

## Controls

| Input | Action |
|---|---|
| WASD / arrow keys | Move in 8 directions |
| Spacebar | Shoot in the facing direction (last direction moved; up at spawn) |
| R | Restart after Game Over |

## Gameplay constants

All tuning values live in `Assets/Scripts/GameConfig.cs`:

| Constant | Value |
|---|---|
| Arena size | 24 x 14 world units |
| Player speed | 6 |
| Enemy speed | 3.5 (kept below player speed) |
| Player max HP | 100 |
| Enemy contact damage | 10 |
| Invulnerability window | 0.75 s |
| Enemy spawn interval | 2.0 s ramping down to 0.6 s over 60 s |
| Projectile speed / lifetime | 12 / 2 s |
| Enemy kill | 1 projectile hit |

## How it works

- `Bootstrap.cs` runs via `[RuntimeInitializeOnLoadMethod]` when Play starts and builds everything under a single `GameRoot` object.
- `PlaceholderSprites.cs` generates square/circle sprites from `Texture2D` at runtime — no art files.
- Restart destroys `GameRoot` and calls `Bootstrap.BuildWorld()` again for a full state reset.
- Input uses the legacy `UnityEngine.Input` class; physics is `Rigidbody2D` + 2D colliders; the HUD is a code-built screen-space Canvas.

## Roadmap (future stories toward Gauntlet)

1. Tile-based dungeon rooms with keys, doors, and an exit (migrate to Tilemap)
2. Monster generators you can destroy
3. Health-drain-as-timer + food pickups
4. Score and treasure
5. Character classes (Warrior / Valkyrie / Wizard / Elf)
6. Sound and polish
