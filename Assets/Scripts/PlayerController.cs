using UnityEngine;

/// <summary>
/// 8-direction movement via Rigidbody2D using the legacy Input axes
/// (WASD + arrow keys). Tracks the facing direction: the last nonzero
/// movement direction, defaulting to up at spawn.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, IFacing
{
    Rigidbody2D _body;
    Vector2 _input;

    public Vector2 Facing { get; private set; } = Vector2.up;

    void Awake()
    {
        _body = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        _input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (_input.sqrMagnitude > 1f)
        {
            _input.Normalize(); // keep diagonals the same speed
        }

        Facing = Heading.Toward(Facing, _input);
    }

    void FixedUpdate()
    {
        _body.linearVelocity = _input * GameConfig.PlayerSpeed;
    }
}
