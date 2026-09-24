using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// Research spike for issue #8 — throwaway measurement code, not a regression
/// suite. Each test prints "[SPIKE]" lines to the Unity log; the findings doc
/// (docs/research/headless-simulation.md) quotes them.
///
/// Run headless with:
///   Unity -batchmode -nographics -runTests -testPlatform PlayMode -projectPath . \
///         -testResults pm.xml -logFile pm.log
/// </summary>
public class HeadlessSimulationSpike
{
    const float FixedStep = 0.02f;

    static void Report(string message) => Debug.Log("[SPIKE] " + message);

    [TearDown]
    public void RestoreGlobals()
    {
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        Time.timeScale = 1f;
        Time.captureDeltaTime = 0f;
    }

    // ------------------------------------------------------------------
    // 1. Environment + does Bootstrap's world exist inside a PlayMode test?
    // ------------------------------------------------------------------
    [UnityTest, Order(1)]
    public IEnumerator A_Environment_And_BootstrapWorld()
    {
        yield return null;

        Report($"isBatchMode={Application.isBatchMode} graphicsDevice={SystemInfo.graphicsDeviceType} " +
               $"targetFrameRate={Application.targetFrameRate} vSync={QualitySettings.vSyncCount} " +
               $"fixedDeltaTime={Time.fixedDeltaTime} maximumDeltaTime={Time.maximumDeltaTime} " +
               $"simulationMode={Physics2D.simulationMode}");

        var managers = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Report($"Bootstrap world present: GameManager.Instance={(GameManager.Instance != null)} managers={managers.Length} " +
               $"SpawnPoint.All={SpawnPoint.All.Count} Dungeon.Current={(Dungeon.Current != null)} " +
               $"GameRoot={(GameObject.Find("GameRoot") != null)}");

        Assert.That(GameManager.Instance, Is.Not.Null, "Bootstrap's RuntimeInitializeOnLoadMethod should build the world on entering Play mode");
    }

    // ------------------------------------------------------------------
    // 2. Physics2D, Rigidbody2D, collision + trigger callbacks, normal loop.
    // ------------------------------------------------------------------
    [UnityTest, Order(2)]
    public IEnumerator B_Physics2D_Callbacks_Fire_Headless()
    {
        var origin = new Vector2(-1000f, 0f);

        var mover = MakeBody("Mover", origin, isTrigger: false);
        mover.linearVelocity = new Vector2(5f, 0f);
        var moverProbe = mover.gameObject.AddComponent<SpikeProbe>();
        MakeStaticWall("Wall", origin + new Vector2(3f, 0f), new Vector2(1f, 4f));

        var shot = MakeBody("Shot", origin + new Vector2(0f, 10f), isTrigger: true);
        shot.linearVelocity = new Vector2(5f, 0f);
        var shotProbe = shot.gameObject.AddComponent<SpikeProbe>();
        MakeStaticWall("TriggerTarget", origin + new Vector2(3f, 10f), new Vector2(1f, 4f));

        for (int i = 0; i < 100 && (moverProbe.CollisionEnter == 0 || shotProbe.TriggerEnter == 0); i++)
        {
            yield return new WaitForFixedUpdate();
        }

        Report($"normal loop: collisionEnter={moverProbe.CollisionEnter} collisionStay={moverProbe.CollisionStay} " +
               $"triggerEnter={shotProbe.TriggerEnter} fixedUpdates={moverProbe.FixedUpdates} " +
               $"order=[{string.Join(", ", moverProbe.Order)}]");

        Object.Destroy(mover.gameObject);
        Object.Destroy(shot.gameObject);

        Assert.That(moverProbe.CollisionEnter, Is.GreaterThan(0));
        Assert.That(shotProbe.TriggerEnter, Is.GreaterThan(0));
    }

    // ------------------------------------------------------------------
    // 3. Script mode: are callbacks delivered from inside Physics2D.Simulate,
    //    and does FixedUpdate run? Then tunnelling at coarse steps.
    // ------------------------------------------------------------------
    [UnityTest, Order(3)]
    public IEnumerator C_ScriptMode_Callbacks_And_Tunnelling()
    {
        yield return null;
        Physics2D.simulationMode = SimulationMode2D.Script;

        var origin = new Vector2(-2000f, 0f);
        var mover = MakeBody("Mover", origin, isTrigger: false);
        mover.linearVelocity = new Vector2(5f, 0f);
        var probe = mover.gameObject.AddComponent<SpikeProbe>();
        MakeStaticWall("Wall", origin + new Vector2(3f, 0f), new Vector2(1f, 4f));

        int frameBefore = Time.frameCount;
        int steps = 0;
        while (probe.CollisionEnter == 0 && steps < 200)
        {
            Physics2D.Simulate(FixedStep);
            steps++;
        }
        Report($"script mode, tight loop in one frame: collisionEnter={probe.CollisionEnter} after {steps} Simulate calls, " +
               $"frames elapsed={Time.frameCount - frameBefore}, FixedUpdate calls on probe={probe.FixedUpdates}");
        Object.Destroy(mover.gameObject);

        // Tunnelling: a projectile exactly as Projectile.Spawn builds it (trigger,
        // continuous) and an enemy-like discrete circle, fired at a 1-unit wall.
        float[] steps2 = { 0.02f, 0.05f, 0.1f, 0.2f, 1f / 3f };
        for (int i = 0; i < steps2.Length; i++)
        {
            float dt = steps2[i];
            var baseAt = new Vector2(-3000f, i * 20f);

            MakeStaticWall("Wall", baseAt + new Vector2(5f, 0f), new Vector2(GameConfig.WallThickness, 6f));
            var projectile = MakeProjectileLike(baseAt + new Vector2(0f, 0f), GameConfig.ProjectileSpeed);
            var projectileProbe = projectile.gameObject.AddComponent<SpikeProbe>();

            MakeStaticWall("Wall", baseAt + new Vector2(5f, 10f), new Vector2(GameConfig.WallThickness, 6f));
            var enemy = MakeEnemyLike(baseAt + new Vector2(0f, 10f), GameConfig.EnemySpeed * 4f);

            for (int s = 0; s < Mathf.CeilToInt(3f / dt); s++)
            {
                Physics2D.Simulate(dt);
            }

            Report($"tunnelling dt={dt:0.###}: projectile(trigger,continuous,{GameConfig.ProjectileSpeed}u/s) hitWall={projectileProbe.TriggerEnter > 0} " +
                   $"endX={projectile.position.x - baseAt.x:0.00}; discrete circle({GameConfig.EnemySpeed * 4f}u/s) endX={enemy.position.x - baseAt.x:0.00} " +
                   $"(wall face at 4.5, passed={enemy.position.x - baseAt.x > 5.5f})");
        }
    }

    // ------------------------------------------------------------------
    // 4. Script mode throughput on the real game world, logic driven by hand.
    // ------------------------------------------------------------------
    [UnityTest, Order(4)]
    public IEnumerator D_ScriptMode_Throughput_On_Game_World()
    {
        GameManager.Instance.Restart();
        yield return null;

        var player = GameObject.Find("Player");
        var playerProbe = player.AddComponent<SpikeProbe>();

        // Stand in for 60 s of spawning: 40 enemies from the spawn points.
        var root = player.transform.parent;
        var enemies = new List<Enemy>();
        for (int i = 0; i < 40; i++)
        {
            var point = SpawnPoint.All[i % SpawnPoint.All.Count];
            enemies.Add(Enemy.Spawn((Vector2)point.transform.position + UnityEngine.Random.insideUnitCircle, player.transform, root));
        }
        yield return null; // let Awake/Start settle

        var fixedUpdate = typeof(Enemy).GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
        var enemySteps = enemies.ConvertAll(e => (Action)Delegate.CreateDelegate(typeof(Action), e, fixedUpdate));

        Physics2D.simulationMode = SimulationMode2D.Script;
        const int Steps = 3000; // 60 simulated seconds at 50 Hz

        var sw = Stopwatch.StartNew();
        for (int s = 0; s < Steps; s++)
        {
            Physics2D.Simulate(FixedStep);
        }
        sw.Stop();
        double physicsOnly = sw.Elapsed.TotalSeconds;

        sw.Restart();
        for (int s = 0; s < Steps; s++)
        {
            foreach (var step in enemySteps) step();
            Physics2D.Simulate(FixedStep);
        }
        sw.Stop();
        double withLogic = sw.Elapsed.TotalSeconds;

        Report($"script-mode throughput, 40 enemies + world, {Steps} steps (60 sim s): physics only {physicsOnly:0.000}s real " +
               $"({60.0 / physicsOnly:0}x real time); with Enemy.FixedUpdate driven by hand {withLogic:0.000}s ({60.0 / withLogic:0}x). " +
               $"player collisionEnter={playerProbe.CollisionEnter} stay={playerProbe.CollisionStay}; " +
               $"frames elapsed during loop=0 so Time.time is frozen at {Time.time:0.00}; " +
               $"IsGameOver={GameManager.Instance.IsGameOver} (PlayerHealth invulnerability is Time.time-based)");
    }

    // ------------------------------------------------------------------
    // 5/6. Many full runs in one process: idle player until round over,
    //      then GameManager.Restart. Deterministic stepping vs raised timeScale.
    // ------------------------------------------------------------------
    [UnityTest, Order(5), Timeout(300000)]
    public IEnumerator E_Runs_With_CaptureDeltaTime()
    {
        Time.captureDeltaTime = FixedStep; // one frame = exactly one fixed step, as fast as the CPU goes
        yield return RunMany("captureDeltaTime=0.02", 5);
    }

    [UnityTest, Order(6), Timeout(300000)]
    public IEnumerator F_Runs_With_TimeScale100()
    {
        yield return MeasureFrameRate();
        yield return RunMany("timeScale=100", 5, timeScale: 100f);
    }

    [UnityTest, Order(8)]
    public IEnumerator H_TimeScale_Ceiling()
    {
        LogAssert.ignoreFailingMessages = true; // an out-of-range set may log an error
        Time.timeScale = 1000f;
        float accepted = Time.timeScale;
        Time.timeScale = 1f;
        LogAssert.ignoreFailingMessages = false;
        Report($"Time.timeScale = 1000 -> reads back {accepted}");
        yield return null;
    }

    IEnumerator MeasureFrameRate()
    {
        Time.timeScale = 1f;
        int f0 = Time.frameCount;
        float t0 = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - t0 < 2f)
        {
            yield return null;
        }
        Report($"unthrottled headless frame rate at timeScale=1: {(Time.frameCount - f0) / (Time.realtimeSinceStartup - t0):0} fps");
    }

    IEnumerator RunMany(string label, int runs, float timeScale = 1f)
    {
        var results = new List<string>();
        for (int run = 0; run < runs; run++)
        {
            GameManager.Instance.Restart(); // BuildWorld resets timeScale to 1
            Time.timeScale = timeScale;
            yield return null;

            float simStart = Time.time;
            float realStart = Time.realtimeSinceStartup;
            int frameStart = Time.frameCount;
            int fixedFrames = 0;

            while (!GameManager.Instance.IsRoundOver && Time.time - simStart < 60f && Time.realtimeSinceStartup - realStart < 40f)
            {
                yield return new WaitForFixedUpdate();
                fixedFrames++;
            }

            float sim = Time.time - simStart;
            float real = Time.realtimeSinceStartup - realStart;
            results.Add($"run{run}: roundOver={GameManager.Instance.IsRoundOver} sim={sim:0.00}s real={real:0.00}s " +
                        $"({sim / real:0.0}x) frames={Time.frameCount - frameStart} fixedSteps~{fixedFrames}");
        }
        Report($"{label}: " + string.Join(" | ", results));
    }

    // ------------------------------------------------------------------
    // 7. Teardown/rebuild: do statics or objects leak across Restart?
    // ------------------------------------------------------------------
    [UnityTest, Order(7)]
    public IEnumerator G_Restart_Leak_Check()
    {
        GameManager.Instance.Restart();
        yield return null;
        yield return null;

        int spawnPoints = SpawnPoint.All.Count;
        int textures0 = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
        int sprites0 = Resources.FindObjectsOfTypeAll<Sprite>().Length;
        int objects0 = Resources.FindObjectsOfTypeAll<GameObject>().Length;
        long heap0 = GC.GetTotalMemory(true);
        var dungeonBefore = Dungeon.Current;

        const int Restarts = 50;
        var sw = Stopwatch.StartNew();
        double buildOnly = 0;
        for (int i = 0; i < Restarts; i++)
        {
            var b = Stopwatch.StartNew();
            GameManager.Instance.Restart();
            buildOnly += b.Elapsed.TotalMilliseconds;
            yield return null;
        }
        sw.Stop();
        yield return null;

        var managers = Object.FindObjectsByType<GameManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int textures1 = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
        int sprites1 = Resources.FindObjectsOfTypeAll<Sprite>().Length;
        int objects1 = Resources.FindObjectsOfTypeAll<GameObject>().Length;
        long heap1 = GC.GetTotalMemory(true);

        Report($"{Restarts} restarts: {sw.Elapsed.TotalMilliseconds / Restarts:0.0} ms each incl. one frame, " +
               $"{buildOnly / Restarts:0.0} ms in Restart() itself. SpawnPoint.All {spawnPoints}->{SpawnPoint.All.Count}, " +
               $"GameManagers alive={managers.Length}, Dungeon.Current replaced={!ReferenceEquals(dungeonBefore, Dungeon.Current)}, " +
               $"Texture2D {textures0}->{textures1}, Sprite {sprites0}->{sprites1}, GameObject {objects0}->{objects1}, " +
               $"managed heap {heap0 / 1024}KB->{heap1 / 1024}KB");

        Assert.That(SpawnPoint.All.Count, Is.EqualTo(spawnPoints));
        Assert.That(managers.Length, Is.EqualTo(1));
    }

    // ------------------------------------------------------------------
    static Rigidbody2D MakeBody(string name, Vector2 at, bool isTrigger)
    {
        var go = new GameObject("Spike " + name);
        go.transform.position = at;
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = isTrigger;
        return body;
    }

    static void MakeStaticWall(string name, Vector2 at, Vector2 size)
    {
        var go = new GameObject("Spike " + name);
        go.transform.position = at;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        go.AddComponent<BoxCollider2D>();
    }

    static Rigidbody2D MakeProjectileLike(Vector2 at, float speed)
    {
        var go = new GameObject("Spike Projectile");
        go.transform.position = at;
        go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = 0.5f;
        c.isTrigger = true;
        body.linearVelocity = new Vector2(speed, 0f);
        return body;
    }

    static Rigidbody2D MakeEnemyLike(Vector2 at, float speed)
    {
        var go = new GameObject("Spike EnemyLike");
        go.transform.position = at;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        var c = go.AddComponent<CircleCollider2D>();
        c.radius = 0.5f;
        body.linearVelocity = new Vector2(speed, 0f);
        return body;
    }
}
