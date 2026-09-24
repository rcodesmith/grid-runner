using UnityEngine;

/// <summary>
/// Which frame of a character sheet to show for a facing direction. Sheets
/// draw five poses — 0 up, 1 up-right, 2 right, 3 down-right, 4 down — and the
/// left-hand directions reuse the right-hand poses mirrored with flipX, giving
/// eight directions in all. Pure C#: FacingSprite applies it to a renderer.
/// </summary>
public readonly struct FacingPose
{
    /// <summary>Frames a character sheet must have, one per drawn pose.</summary>
    public const int Count = 5;

    public readonly int Index;
    public readonly bool FlipX;

    FacingPose(int index, bool flipX)
    {
        Index = index;
        FlipX = flipX;
    }

    /// <summary>Snaps a direction to the nearest of the eight compass directions.</summary>
    public static FacingPose For(Vector2 direction)
    {
        // Degrees clockwise from up: 0 up, 90 right, ±180 down, -90 left.
        float angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        int sector = Mathf.FloorToInt(angle / 45f + 0.5f);
        int index = Mathf.Abs(sector);
        // Down sits at both +4 and -4; only the left-hand poses 1-3 mirror.
        return new FacingPose(index, sector < 0 && index < Count - 1);
    }
}
