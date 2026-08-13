using UnityEngine;

/// <summary>
/// Spawns enemies at a randomly chosen SpawnPoint marker. The spawn
/// interval ramps from SpawnIntervalStart down to SpawnIntervalEnd over
/// SpawnRampDuration seconds. Pauses automatically on game over because
/// Time.timeScale is 0.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    Transform _player;
    Transform _container;
    float _elapsed;
    float _nextSpawnIn;

    public void Init(Transform player, Transform container)
    {
        _player = player;
        _container = container;
        _nextSpawnIn = GameConfig.SpawnIntervalStart;
    }

    void Update()
    {
        if (_player == null)
        {
            return;
        }

        _elapsed += Time.deltaTime;
        _nextSpawnIn -= Time.deltaTime;

        if (_nextSpawnIn <= 0f)
        {
            Enemy.Spawn(NextSpawnPosition(), _player, _container);
            _nextSpawnIn = CurrentInterval();
        }
    }

    float CurrentInterval()
    {
        float ramp = Mathf.Clamp01(_elapsed / GameConfig.SpawnRampDuration);
        return Mathf.Lerp(GameConfig.SpawnIntervalStart, GameConfig.SpawnIntervalEnd, ramp);
    }

    // Picks one of the placed spawn points at random. Falls back to a random
    // room edge if none exist, so the spawner still works without them.
    static Vector2 NextSpawnPosition()
    {
        var points = SpawnPoint.All;
        return points.Count > 0
            ? (Vector2)points[Random.Range(0, points.Count)].transform.position
            : RandomEdgePosition();
    }

    static Vector2 RandomEdgePosition()
    {
        // Either room; enemies from the other room chase through the doorway.
        bool secondRoom = Random.value < 0.5f;
        Vector2 center = secondRoom ? new Vector2(GameConfig.Room2CenterX, 0f) : Vector2.zero;
        float halfW = (secondRoom ? GameConfig.Room2Width : GameConfig.ArenaWidth) / 2f - GameConfig.SpawnEdgeInset;
        float halfH = (secondRoom ? GameConfig.Room2Height : GameConfig.ArenaHeight) / 2f - GameConfig.SpawnEdgeInset;

        switch (Random.Range(0, 4))
        {
            case 0: return center + new Vector2(Random.Range(-halfW, halfW), halfH);   // top
            case 1: return center + new Vector2(Random.Range(-halfW, halfW), -halfH);  // bottom
            case 2: return center + new Vector2(-halfW, Random.Range(-halfH, halfH));  // left
            default: return center + new Vector2(halfW, Random.Range(-halfH, halfH));  // right
        }
    }
}
