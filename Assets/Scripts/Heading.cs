using UnityEngine;

/// <summary>
/// How a character's facing follows the way it is trying to move: it turns
/// to any real direction and keeps its last facing when stopped. Pure C#,
/// shared by the player and enemies.
/// </summary>
public static class Heading
{
    public static Vector2 Toward(Vector2 current, Vector2 desired)
    {
        return desired.sqrMagnitude > 0.0001f ? desired.normalized : current;
    }
}
