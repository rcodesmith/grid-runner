using UnityEngine;

/// <summary>
/// A projectile that kills the first enemy it touches, disappears against
/// arena walls, and despawns after a fixed lifetime. Created entirely from
/// code via the Spawn factory.
/// </summary>
public class Projectile : MonoBehaviour
{
    float _despawnAt;

    public static Projectile Spawn(Vector2 position, Vector2 direction, Collider2D shooter, Transform parent)
    {
        var go = new GameObject("Projectile");
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = PlaceholderSprites.Circle(GameConfig.ProjectileColor);
        renderer.sortingOrder = 5;

        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;

        // Prevent self-collision with the shooter (no tags/layers in v0.1).
        if (shooter != null)
        {
            Physics2D.IgnoreCollision(collider, shooter);
        }

        body.linearVelocity = direction.normalized * GameConfig.ProjectileSpeed;

        var projectile = go.AddComponent<Projectile>();
        projectile._despawnAt = Time.time + GameConfig.ProjectileLifetime;
        return projectile;
    }

    void Update()
    {
        if (Time.time >= _despawnAt)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.Die();
            Destroy(gameObject);
            return;
        }

        var spawnPoint = other.GetComponent<SpawnPoint>();
        if (spawnPoint != null)
        {
            spawnPoint.TakeHit();
            Destroy(gameObject);
            return;
        }

        // Colliders with no attached rigidbody are the static arena walls.
        if (other.attachedRigidbody == null)
        {
            Destroy(gameObject);
        }
    }
}
