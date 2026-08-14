# Gauntlet-Starter

A code-first Unity 6 (6000.0.81f1) 2D dungeon crawler in the style of Gauntlet
(1985). There is no authored scene and no editor wiring:
`Assets/Scripts/Bootstrap.cs` builds the entire world at Play time under a
single `GameRoot`.

## Working in this repo

- **Never suggest editor wiring.** No prefab dragging, no inspector fields, no
  scene authoring — everything is constructed in code. A change that requires
  someone to click something in the editor is the wrong change.
- **Legacy input.** Uses `UnityEngine.Input`, not the Input System package.
- **Gameplay numbers live in `Assets/Scripts/GameConfig.cs`.** Known exception:
  the `1.5f` doorway overshoot in `Enemy.cs:77`.
- **Zero art assets.** Sprites are generated at runtime by
  `PlaceholderSprites.cs`. Don't add image files.

## Verifying a change

EditMode tests run headless and are the only automated signal:

```
/Applications/Unity/Hub/Editor/6000.0.81f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -runTests -projectPath . -testPlatform EditMode \
  -testResults results.xml -nographics -silent-crashes
```

Exit 0 = all passed. Exit 2 = failures; read `results.xml` for the assertion.

**You cannot verify gameplay.** Movement, collision, spawning, rendering and
restart all need Play mode, which a human has to run. When a change touches
those, say what remains unverified rather than claiming it works.

## Assemblies

- `Assets/Scripts/Gauntlet.asmdef` — runtime code
- `Assets/Tests/EditMode/Gauntlet.Tests.EditMode.asmdef` — Editor-only tests

Tests reach runtime code through the `Gauntlet` reference. Logic that needs a
GameObject to exercise is logic that can't be tested here — prefer pure C#
behind a small interface.

## Agent skills

### Issue tracker

Issues live as local markdown under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

The five canonical roles, used as-is. See `docs/agents/triage-labels.md`.

### Domain docs

Single-context: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
