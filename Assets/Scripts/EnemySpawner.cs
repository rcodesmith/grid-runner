using UnityEngine;

/// <summary>
/// Drives every SpawnPoint's own spawn timer. Each point spawns on its own
/// independent schedule, ramping from SpawnIntervalStart down to
/// SpawnIntervalEnd over SpawnRampDuration seconds, so more surviving points
/// means proportionally more enemies. Pauses automatically on game over
/// because Time.timeScale is 0.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    Transform _player;
    Transform _container;

    public void Init(Transform player, Transform container)
    {
        _player = player;
        _container = container;
    }

    void Update()
    {
        // Destroying every spawn point ends the flow of enemies for good.
        if (_player == null || SpawnPoint.All.Count == 0)
        {
            return;
        }

        // Backwards: a point can destroy itself and leave All mid-iteration.
        for (int i = SpawnPoint.All.Count - 1; i >= 0; i--)
        {
            var point = SpawnPoint.All[i];
            if (point.Tick(Time.deltaTime))
            {
                Enemy.Spawn(point.transform.position, _player, _container);
            }
        }
    }
}
