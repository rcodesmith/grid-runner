# Headless 2D simulation in Unity 6: feasibility and cost

Research for [issue #8](https://github.com/rcodesmith/grid-runner/issues/8).
Unity 6000.0.81f1, Test Framework 1.6.0, macOS (Apple Silicon), measured 2026-09-23.

**Bottom line:** yes. PlayMode tests run under `-batchmode -nographics`, and
Physics2D and its callbacks behave normally there. `Time.captureDeltaTime = 0.02`
runs the real game loop (Update, FixedUpdate and physics, one fixed step per frame)
at about **280× real time**, with run-to-run results that repeat. A warm Unity
invocation costs **about 2 s**. `GameManager.Restart` gives clean,
leak-free rebuilds in **under 1 ms**. The one blocker: `Bootstrap` destroys the
test runner, so PlayMode tests hang unless the runner is protected (see §1).

Confidence: **High** = measured in the spike or stated by Unity docs.
**Medium** = inferred from code or from a small sample. **Low** = educated guess.

## Spike

The spike code lives on this branch only:

- `Assets/Tests/PlayMode/HeadlessSimulationSpike.cs`: PlayMode tests A–H. Each
  one logs `[SPIKE] …` lines, and the numbers below are quoted from them.
- `Assets/Tests/PlayMode/ProtectTestRunner.cs`: the workaround for §1.
- `Assets/Tests/PlayMode/SpikeProbe.cs`: counts callbacks.
- `Assets/Tests/SpikeEditor/HeadlessSpikeEditor.cs`: two `-executeMethod` entry
  points, `Noop` and `EditModePhysics`.

Reproduce:

```
Unity -batchmode -nographics -runTests -testPlatform PlayMode -projectPath . \
      -testResults pm.xml -logFile pm.log        # then: grep SPIKE pm.log
Unity -batchmode -nographics -projectPath . -executeMethod HeadlessSpikeEditor.Noop -logFile noop.log
```

## 1. PlayMode tests under `-batchmode -nographics`

| Fact | Confidence |
|---|---|
| PlayMode tests run headless. Environment reported: `isBatchMode=True graphicsDevice=Null fixedDeltaTime=0.02 maximumDeltaTime=0.333`. | High (measured) |
| Rigidbody2D, OnCollisionEnter2D and OnTriggerEnter2D all fire under the Null graphics device (test B: `collisionEnter=1 triggerEnter=1`). SpriteRenderer, Canvas and runtime `Texture2D` sprite generation also build without errors. | High (measured) |
| **Blocker: out of the box, PlayMode tests hang forever on this project.** `Bootstrap.Init` is `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` and destroys every root GameObject in the active scene. UTF puts its driver there: a root object named `"Code-based tests runner"` (`CreateBootstrapSceneTask.cs`), whose `Start()` coroutine runs the whole test run (`PlaymodeTestsController.cs:55`). Bootstrap destroys it before `Start`, so the first run spun at 120% CPU with no log output until I killed it after 10 minutes. | High (measured, UTF source read) |
| Workaround used in the spike: a test-assembly `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` hooks `SceneManager.sceneLoaded` and moves the runner to `DontDestroyOnLoad`. That handler runs before Bootstrap's AfterSceneLoad sweep. Unity documents AfterSceneLoad as "after Awake has been called" and `sceneLoaded` as "after OnEnable but before Start". The cleaner long-term fix is for Bootstrap to skip the sweep, or give the harness a way to disable it. | High that it works (measured). Medium on the ordering guarantee, which I observed but which isn't explicitly documented. |
| `Bootstrap` builds the full game world automatically when a PlayMode test enters Play mode (`GameManager.Instance`, 8 spawn points, `Dungeon.Current` all present in test A). A harness gets a world for free, but it can't opt out without changing Bootstrap. | High (measured) |
| `-executeMethod` without Play mode is **not** a way to simulate gameplay. `Physics2D.Simulate` in edit mode resolves contacts (the body stopped at the wall, `contacts=2`), but `Awake` doesn't run and no `OnCollision*` callbacks arrive (`awake=False collisionEnter=0`). This matches the docs: outside play mode "contacts are not reported via the standard script callbacks" ([Physics2D.Simulate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics2D.Simulate.html)). | High (measured and documented) |

## 2. Faster than real time: three ways to drive time

These rows come from tests D, E and F. A "run" means an idle player left until
the enemies kill it, about 13 simulated seconds.

| Approach | Throughput | Fidelity | Confidence |
|---|---|---|---|
| **`Time.captureDeltaTime = 0.02`** | **≈280×** real time: 13.06 sim s in 0.05 s real per run, 5 runs | Full. Every frame advances exactly 0.02 s, which is one FixedUpdate followed by one Update. The PlayerController Update→FixedUpdate input hand-off, the Update-driven EnemySpawner, and the Time.time-based invulnerability and projectile lifetime all behave as in play. Unity: "Time.time increases at an interval of captureDeltaTime (scaled by Time.timeScale) regardless of real time" ([docs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-captureDeltaTime.html)). | High |
| `Time.timeScale = 100` | **Exactly 100×**. The cap is timeScale itself: headless play runs about 26,000 fps, so real time is the limit. | Nearly full. Frames are about 0.004 s of game time, about 5 Updates per FixedUpdate. **Not repeatable**: frame deltas follow wall-clock time, and time-to-death varied between 12.94 and 12.99 s. | High |
| `timeScale` > 100 | Not possible in the editor. Setting 1000 reads back 1 and logs "Time.timeScale is out of range. When running in the editor this value needs to be less than or equal to 100.0". The message implies a standalone player allows more. | n/a | High (editor). Low (player). |
| **`Physics2D.simulationMode = Script`** plus a tight `Physics2D.Simulate(0.02f)` loop within one frame | Physics alone: **≈6,700–7,000×** (3000 steps covering 60 sim s took 0.009 s, with 40 enemies). With `Enemy.FixedUpdate` called by hand through a delegate: **≈121×**, and the per-enemy `Dungeon.RouteTo` BFS dominates. | **Poor for this codebase as written.** (a) `Simulate` doesn't call FixedUpdate: "Calling Physics2D.Simulate does not cause Unity to call FixedUpdate" ([docs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics2D.Simulate.html)). The probe logged `FixedUpdate calls=0`. (b) No frame passes, so Update never runs: no spawning and no player input. (c) `Time.time` stays frozen (0.45 for all 60 sim s), so `PlayerHealth`'s invulnerability window never expires. The player took 7–13 contact-enters and thousands of stays but never died (`IsGameOver=False`). The approach only works if game logic moves off MonoBehaviour messages and `Time.time` onto an explicit `Step(dt)` driven by the harness. | High |
| Callbacks inside a `Simulate` loop | Script mode does dispatch OnCollisionEnter2D during the `Simulate` call itself (test C: `collisionEnter=1 … frames elapsed=0`). | Callback order in the normal loop is FixedUpdate → physics step → callbacks. This was observed, and matches the manual's execution-order diagram. | High (measured). Medium (ordering claim). |

**Tunnelling** (test C, fixed step `dt` fed to `Simulate`, 1-unit wall):

| dt | Projectile (trigger + `Continuous`, 12 u/s) hit the wall? | Discrete circle, r=0.4, 14 u/s (4× enemy speed) |
|---|---|---|
| 0.02, 0.05, 0.1 | yes | stopped |
| 0.2 | yes | **passed through** |
| 0.333 | **no, flew through** | stopped (chance alignment) |

- **`Continuous` doesn't protect triggers.** `Projectile` is a trigger, so once a step
  moves it more than wall width + diameter (1.3 u), it *can* skip a wall.
  Whether it does depends on where the steps land. At 12 u/s, any dt above
  about 0.1 s is at risk.
  (High, measured.)
- Discrete bodies tunnel depending on where each step happens to land
  (0.2 s tunnelled but 0.333 s didn't). At the game's real speeds (enemy 3.5,
  player 6 and `Continuous`) and dt=0.02, every body moves ≤0.12 u per step,
  well under the 1.8 u needed. (High.)
- The takeaway: keep dt = `fixedDeltaTime` (0.02). `captureDeltaTime` does
  this automatically. Raising `timeScale` doesn't raise dt either, because Unity
  runs more FixedUpdates per frame instead. The risk only appears if someone
  passes a large dt to `Simulate` or raises `Time.fixedDeltaTime` to go faster.
  (High.)

## 3. Cost per Unity invocation (measured locally, wall-clock, same machine)

| Invocation | Time |
|---|---|
| First run in a fresh worktree (no `Library/`, full import + compile), `-runTests EditMode` | **11 s** (log: "Rebuilding Library because the asset database could not be found!", refresh 6.5 s, compile 4.0 s) |
| First run after adding scripts (recompile), `-executeMethod Noop` | 4.35 s |
| Warm `-executeMethod HeadlessSpikeEditor.Noop` (×3) | **1.65–1.69 s** |
| Warm `-executeMethod EditModePhysics` | 1.74 s |
| Warm `-runTests -testPlatform EditMode` (66 tests) (×2) | 1.84–1.87 s |
| Warm `-runTests -testPlatform PlayMode`, one trivial test | **2.29 s**. Entering Play mode adds about 0.45 s (domain reload). |
| Warm `-runTests -testPlatform PlayMode`, whole spike (10 tests, ~11 full game runs, 50 restarts) | 7.7–8.3 s |

- Licensing didn't block anything. It connected to the running Hub licensing
  client, logged a harmless "Access token is unavailable", and continued.
  (High.)
- Nothing refused with "project already open", because the worktree is a
  separate project path. Two Unity processes can't open the *same* project
  path at the same time. (Medium: standard Unity behaviour, not tested here.)
- Against the map's **2-minute budget**: startup is about 2 s. An idle
  13-second game costs about 0.05 s. Even a 60-second game with 40 enemies
  would cost about 0.5 s at the measured enemy-logic rate. Dozens of runs fit in
  one invocation. (Medium: the heavy-scene figure extrapolates from the
  hand-driven loop.)

## 4. Many runs in one process

| Fact | Confidence |
|---|---|
| `GameManager.Restart()` between runs works headless. 5 + 5 full runs, each ending in game over, all rebuilt correctly. | High |
| No leaks after 50 restarts: `SpawnPoint.All` 8→8, live `GameManager`s = 1, `Texture2D` 54→54, `Sprite` 14→14, `GameObject` 66→66, managed heap about 9.6 MB → 9.3 MB. | High |
| Rebuild cost: **0.5–0.7 ms** per `Restart()`. | High |
| Runs repeat under `captureDeltaTime`: time-to-death was 13.06 s in 4 of 5 runs, with the first run after a fresh Restart at 13.07–13.08. Under `timeScale` it varied from 12.94 to 12.99. Sample: 5 runs, idle player, no randomness in the game code (`UnityEngine.Random` is unused in `Assets/Scripts`). | Medium |

### Static and global state across runs (from reading the code)

| State | Behaviour across Restart | Risk |
|---|---|---|
| `SpawnPoint.All` (`SpawnPoint.cs:18`) | Cleaned up. `OnDisable` removes each point, and `Restart` deactivates the old root (`SetActive(false)`) *before* rebuilding, so the list empties immediately. | Low. It would leak for one frame only if a world were destroyed without being deactivated first. |
| `GameManager.Instance` (`GameManager.cs:11`) | The new manager's `Awake` overwrites it. The old one's `OnDestroy` only clears it `if (Instance == this)`. | Low for sequential runs. Wrong if two worlds exist at once. |
| `Dungeon.Current` (`Dungeon.cs:23`) | Overwritten by `BuildWorld`, **never cleared**, so it outlives its world after teardown. | Low today. Medium for a world-file loader that tears down without rebuilding, or that runs worlds side by side. |
| `Time.timeScale` | Global. Round-over sets it to 0 (`GameManager.cs:59,74`), and `BuildWorld` resets it to 1. **A harness that uses timeScale must reapply it after every Restart** (the spike does). | Medium |
| `Time.time` / `frameCount` | Never reset between runs. The code only uses them relative to the current time (`Projectile`, `PlayerHealth`), so this is fine. A per-run clock has to be measured from the run's start. | Low |
| `PlaceholderSprites` colour caches | Persist across runs by design. Counts stayed stable. | Low |
| `Physics2D.simulationMode` | A **project setting**. Changing it in edit mode (`-executeMethod`) got saved into `ProjectSettings/Physics2DSettings.asset` on exit. I had to revert it. Restore it in `finally`. | Medium |
| Legacy `Input` | Returns nothing headless. Simulating a *moving* player needs an input seam in front of `PlayerController`/`PlayerShooting`. | Medium (inferred) |
| One physics world | Every run shares the default physics scene, so runs can happen **one after another**, not **side by side**. Running them side by side would need `LocalPhysicsMode.Physics2D` scenes and removing the statics above. | Medium |

## What this means for the approach decision (for the human to weigh)

- The cheapest high-fidelity path is one PlayMode-test (or play-mode) invocation
  that sets `Time.captureDeltaTime = Time.fixedDeltaTime` and loops
  Restart → run → collect. That gives about 2 s of fixed cost plus
  milliseconds per game, and it needs no changes to gameplay code apart from
  the Bootstrap test-runner issue and an input seam.
- Script-mode `Simulate` is 25× faster again on raw physics, but only pays off
  after refactoring game logic onto an explicit `Step(dt)`. At today's scale
  that isn't needed to fit the budget.

## Sources

- Unity scripting API 6000.0: [Physics2D.Simulate](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Physics2D.Simulate.html), [SimulationMode2D.Script](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/SimulationMode2D.Script.html), [Time.captureDeltaTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-captureDeltaTime.html), [Time.timeScale](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-timeScale.html), [Time.maximumDeltaTime](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Time-maximumDeltaTime.html) ("bounds the maximum number of times Unity executes MonoBehaviour.FixedUpdate in a frame to maximumDeltaTime / fixedDeltaTime"), [RuntimeInitializeLoadType.AfterSceneLoad](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/RuntimeInitializeLoadType.AfterSceneLoad.html), [Order of execution](https://docs.unity3d.com/6000.0/Documentation/Manual/execution-order.html) (`sceneLoaded` fires "after OnEnable but before Start").
- Unity Test Framework 1.6.0 source (`Library/PackageCache/com.unity.test-framework@082c24152fd4`): `UnityEditor.TestRunner/TestRun/Tasks/CreateBootstrapSceneTask.cs:48` creates the runner object, and `UnityEngine.TestRunner/TestRunner/PlaymodeTestsController.cs:46,55` defines its name and `Start()`.
- The Unity editor log for the `timeScale` > 100 message and for licensing behaviour.
- This repo: `Bootstrap.cs`, `GameManager.cs`, `SpawnPoint.cs`, `Dungeon.cs`, `PlayerController.cs`, `Enemy.cs`, `EnemySpawner.cs`, `PlayerHealth.cs`, `Projectile.cs`, `ProjectSettings/TimeManager.asset` (fixed step 0.02, max 0.333).
