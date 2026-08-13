using UnityEngine;

/// <summary>
/// Chases the player via Rigidbody2D velocity, damages the player on contact
/// (PlayerHealth enforces the invulnerability window), and dies in one hit
/// from a projectile. Created entirely from code via the Spawn factory.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    Rigidbody2D _body;
    Transform _target;
    bool _dead;

    public static Enemy Spawn(Vector2 position, Transform target, Transform parent)
    {
        var go = new GameObject("Enemy");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Square(GameConfig.EnemyColor);
        renderer.sortingOrder = 8;

        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Round collider slides along walls and other enemies without snagging.
        go.AddComponent<CircleCollider2D>();

        var enemy = go.AddComponent<Enemy>();
        enemy._target = target;
        return enemy;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        if (_target == null)
        {
            _body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = ChaseTarget() - _body.position;
        _body.linearVelocity = toTarget.sqrMagnitude > 0.0001f
            ? toTarget.normalized * GameConfig.EnemySpeed
            : Vector2.zero;
    }

    // Straight-line chase pins enemies against a dividing wall when the player
    // is in another room, so steer through the doorways first. The rooms form
    // a left-to-right chain, so the enemy heads for the one divider between it
    // and the player, then re-evaluates once through. The waypoint sits
    // slightly past the divider so the enemy actually crosses it before
    // switching back to chasing the player directly.
    Vector2 ChaseTarget()
    {
        int enemyRoom = RoomIndex(_body.position.x);
        int playerRoom = RoomIndex(_target.position.x);
        if (enemyRoom == playerRoom)
        {
            return _target.position;
        }

        // Move one room toward the player: the divider to cross is the one on
        // that side of the enemy's current room.
        bool playerIsRight = playerRoom > enemyRoom;
        float dividerX = DividerBetween(playerIsRight ? enemyRoom : enemyRoom - 1);
        float doorwayOvershoot = playerIsRight ? 1.5f : -1.5f;
        return new Vector2(dividerX + doorwayOvershoot, 0f);
    }

    // 0 = starting arena, 1 = second room, 2 = third room.
    static int RoomIndex(float x)
    {
        if (x > GameConfig.Divider2X)
        {
            return 2;
        }
        return x > GameConfig.DividerX ? 1 : 0;
    }

    // Centerline of the wall on the right side of the given room.
    static float DividerBetween(int leftRoomIndex)
    {
        return leftRoomIndex == 0 ? GameConfig.DividerX : GameConfig.Divider2X;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamagePlayer(collision);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // Re-applies damage after the invulnerability window while touching.
        TryDamagePlayer(collision);
    }

    void TryDamagePlayer(Collision2D collision)
    {
        var health = collision.collider.GetComponent<PlayerHealth>();
        if (health != null)
        {
            health.TakeDamage(GameConfig.EnemyContactDamage);
        }
    }

    public void Die()
    {
        if (_dead)
        {
            return;
        }
        _dead = true;
        Destroy(gameObject);
    }
}
