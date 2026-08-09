using UnityEngine;

/// <summary>
/// Central tuning constants for the core loop. Every gameplay number lives here
/// so future stories (dungeons, generators, classes) tweak one file.
/// </summary>
public static class GameConfig
{
    // Arena (inner playable area, world units)
    public const float ArenaWidth = 24f;
    public const float ArenaHeight = 14f;
    public const float WallThickness = 1f;

    // Player
    public const float PlayerSpeed = 6f;
    public const int PlayerMaxHp = 100;
    public const float InvulnerabilityWindow = 0.75f;

    // Enemies (EnemySpeed must stay below PlayerSpeed so the player can escape)
    public const float EnemySpeed = 3.5f;
    public const int EnemyContactDamage = 10;
    public const float SpawnIntervalStart = 2.0f;
    public const float SpawnIntervalEnd = 0.6f;
    public const float SpawnRampDuration = 60f;
    public const float SpawnEdgeInset = 1.0f;

    // Projectiles (one hit kills an enemy)
    public const float ProjectileSpeed = 12f;
    public const float ProjectileLifetime = 2f;

    // Placeholder palette
    public static readonly Color PlayerColor = new Color(0.30f, 0.85f, 0.35f);
    public static readonly Color EnemyColor = new Color(0.90f, 0.25f, 0.20f);
    public static readonly Color ProjectileColor = new Color(1.00f, 0.85f, 0.25f);
    public static readonly Color WallColor = new Color(0.45f, 0.45f, 0.50f);
    public static readonly Color FloorColor = new Color(0.13f, 0.12f, 0.15f);
    public static readonly Color CameraBackgroundColor = new Color(0.05f, 0.05f, 0.07f);
    public static readonly Color HpBarFillColor = new Color(0.30f, 0.85f, 0.35f);
    public static readonly Color HpBarBackColor = new Color(0f, 0f, 0f, 0.6f);
}
