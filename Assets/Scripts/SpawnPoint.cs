using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A fixed location enemies spawn from, drawn as a red circle on the floor.
/// Visual only — no collider, so the player and projectiles pass straight over
/// it. Every instance registers itself in All so EnemySpawner can pick one.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    public static readonly List<SpawnPoint> All = new List<SpawnPoint>();

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

        return go.AddComponent<SpawnPoint>();
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
}
