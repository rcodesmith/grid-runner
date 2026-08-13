using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fixed location enemies spawn from, drawn as a red circle on the floor.
/// Carries a trigger collider so projectiles can hit it while the player and
/// enemies still walk straight over it. Takes GameConfig.SpawnPointHits shots
/// to destroy, fading as it goes.
///
/// Each point runs its own spawn timer on the shared ramp curve (interval
/// falling from SpawnIntervalStart to SpawnIntervalEnd over
/// SpawnRampDuration), so the arena's total spawn rate scales with how many
/// points are still standing. Every live instance registers itself in All;
/// EnemySpawner ticks them and owns the actual Enemy.Spawn call.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    public static readonly List<SpawnPoint> All = new List<SpawnPoint>();

    SpriteRenderer _renderer;
    Color _fullColor;
    int _hitsRemaining = GameConfig.SpawnPointHits;
    float _elapsed;
    float _nextSpawnIn = GameConfig.SpawnIntervalStart;

    public static SpawnPoint Create(Transform parent, string name, Vector2 position)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(GameConfig.SpawnPointSize, GameConfig.SpawnPointSize, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Circle(GameConfig.SpawnPointColor);
        // Above the floor, below enemies and the player.
        renderer.sortingOrder = -5;

        // Trigger so nothing collides with it physically; projectiles find it
        // via OnTriggerEnter2D. No Rigidbody2D needed — the projectile is the
        // moving trigger, and its own rigidbody drives the overlap callbacks.
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        return go.AddComponent<SpawnPoint>();
    }

    void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _fullColor = _renderer.color;
    }

    void OnEnable()
    {
        All.Add(this);
    }

    void OnDisable()
    {
        // Restart destroys GameRoot and rebuilds; without this the static list
        // would keep growing and hand out destroyed points.
        All.Remove(this);
    }

    /// <summary>
    /// Advances this point's own spawn timer by deltaTime and reports whether
    /// it is due to spawn, rearming the timer at the current ramped interval
    /// when it is. Driven by EnemySpawner rather than a local Update so all
    /// points stop cleanly with the spawner (e.g. once the player is gone).
    /// </summary>
    public bool Tick(float deltaTime)
    {
        _elapsed += deltaTime;
        _nextSpawnIn -= deltaTime;

        if (_nextSpawnIn > 0f)
        {
            return false;
        }

        _nextSpawnIn = CurrentInterval();
        return true;
    }

    // Same ramp for every point, each on its own clock: intervals shorten from
    // SpawnIntervalStart to SpawnIntervalEnd over SpawnRampDuration seconds.
    float CurrentInterval()
    {
        float ramp = Mathf.Clamp01(_elapsed / GameConfig.SpawnRampDuration);
        return Mathf.Lerp(GameConfig.SpawnIntervalStart, GameConfig.SpawnIntervalEnd, ramp);
    }

    /// <summary>Applies one shot. Destroys the spawn point once its hits run out.</summary>
    public void TakeHit()
    {
        if (_hitsRemaining <= 0)
        {
            return;
        }

        _hitsRemaining--;
        if (_hitsRemaining <= 0)
        {
            // Leave All immediately: Destroy is deferred to end of frame, and
            // the spawner must not pick this point in the meantime.
            All.Remove(this);
            Destroy(gameObject);
            return;
        }

        FadeToDamage();
    }

    // Dims toward SpawnPointMinAlphaFactor of full alpha as hits accumulate, so
    // a worn-down point reads as damaged without vanishing before it dies.
    void FadeToDamage()
    {
        float health = (float)_hitsRemaining / GameConfig.SpawnPointHits;
        var color = _fullColor;
        color.a = _fullColor.a * Mathf.Lerp(GameConfig.SpawnPointMinAlphaFactor, 1f, health);
        _renderer.color = color;
    }
}
