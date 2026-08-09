using UnityEngine;

/// <summary>
/// Fires a projectile in the current facing direction when spacebar is pressed.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerShooting : MonoBehaviour
{
    PlayerController _controller;
    Collider2D _collider;

    void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _collider = GetComponent<Collider2D>();
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Projectile.Spawn(transform.position, _controller.Facing, _collider, transform.parent);
        }
    }
}
