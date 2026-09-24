using UnityEngine;

/// <summary>
/// Chases the player via Rigidbody2D velocity, damages the player on contact
/// (PlayerHealth enforces the invulnerability window), and dies in one hit
/// from a projectile. Created entirely from code via the Spawn factory.
/// Drawn as a grunt that faces the way it is trying to go.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour, IFacing
{
    Rigidbody2D _body;
    Transform _target;
    bool _dead;

    /// <summary>
    /// The direction the enemy is trying to move — not its velocity, which
    /// collisions deflect. Kept while stopped; faces the player at spawn.
    /// </summary>
    public Vector2 Facing { get; private set; } = Vector2.down;

    public static Enemy Spawn(Vector2 position, Transform target, Transform parent)
    {
        var go = new GameObject("Enemy");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.8f, 0.8f, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 8;

        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Round collider slides along walls and other enemies without snagging.
        // Sized explicitly so the sprite's bounds never resize the hitbox.
        var collider = go.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        var enemy = go.AddComponent<Enemy>();
        enemy._target = target;
        enemy.Facing = Heading.Toward(enemy.Facing, (Vector2)target.position - position);
        go.AddComponent<FacingSprite>()
            .Init(CharacterSprites.Load("grunt", FacingPose.Count), enemy);
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
        Facing = Heading.Toward(Facing, toTarget);
    }

    // Straight-line chase pins enemies against a dividing wall when the player
    // is in another room, so ask the dungeon where to head next. It returns
    // the player directly when both are in the same room, and otherwise a
    // point just past the next doorway on the route.
    Vector2 ChaseTarget()
    {
        var dungeon = Dungeon.Current;
        if (dungeon == null)
        {
            return _target.position;
        }

        return dungeon.RouteTo(_body.position, _target.position);
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
