using UnityEngine;

/// <summary>
/// The win condition: a gold square sitting at the far end of the third room.
/// The player touching it wins the game. A trigger collider so the player
/// walks onto it rather than bumping into it; enemies pass straight over.
/// Created entirely from code via the Spawn factory.
/// </summary>
public class Treasure : MonoBehaviour
{
    bool _claimed;

    public static Treasure Spawn(Transform parent, Vector2 position)
    {
        var go = new GameObject("Treasure");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(GameConfig.TreasureSize, GameConfig.TreasureSize, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Square(GameConfig.TreasureColor);
        // Above the floor and spawn points, below enemies and the player.
        renderer.sortingOrder = -3;

        // Trigger only; the player's rigidbody drives the overlap callback.
        var collider = go.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;

        return go.AddComponent<Treasure>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_claimed || other.GetComponent<PlayerController>() == null)
        {
            return;
        }

        _claimed = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerWin();
        }
    }
}
