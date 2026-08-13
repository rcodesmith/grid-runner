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
        // Destroying every spawn point ends the flow of enemies for good.
        if (_player == null || SpawnPoint.All.Count == 0)
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

    // Picks one of the surviving spawn points at random. Only called while at
    // least one remains; Update stops spawning once they are all destroyed.
    static Vector2 NextSpawnPosition()
    {
        var points = SpawnPoint.All;
        return points[Random.Range(0, points.Count)].transform.position;
    }
}
