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

    // Straight-line chase pins enemies against the dividing wall when the
    // player is in the other room, so steer through the doorway first. The
    // waypoint sits slightly past the divider so the enemy actually crosses
    // it before switching back to chasing the player directly.
    Vector2 ChaseTarget()
    {
        bool enemyInSecondRoom = _body.position.x > GameConfig.DividerX;
        bool playerInSecondRoom = _target.position.x > GameConfig.DividerX;
        if (enemyInSecondRoom == playerInSecondRoom)
        {
            return _target.position;
        }

        float doorwayOvershoot = playerInSecondRoom ? 1.5f : -1.5f;
        return new Vector2(GameConfig.DividerX + doorwayOvershoot, 0f);
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
