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

    // Second room, attached to the right of the arena through a doorway in the
    // shared wall. The doorway is centered vertically on y = 0.
    public const float Room2Width = 36f;
    public const float Room2Height = 22f;
    public const float Room2CenterX = ArenaWidth / 2f + WallThickness + Room2Width / 2f;
    public const float DoorHeight = 4f;
    // Centerline of the dividing wall; anything with x beyond this is in room 2.
    public const float DividerX = ArenaWidth / 2f + WallThickness / 2f;

    // Third room (medium — between room 1 and room 2 in size), attached to the
    // right of room 2 through a second doorway, also centered on y = 0.
    public const float Room3Width = 30f;
    public const float Room3Height = 18f;
    public const float Room3CenterX = Room2CenterX + Room2Width / 2f + WallThickness + Room3Width / 2f;
    // Centerline of the room 2 / room 3 dividing wall.
    public const float Divider2X = Room2CenterX + Room2Width / 2f + WallThickness / 2f;

    // Treasure: sitting at the far end of room 3. Touching it wins the game.
    public const float TreasureSize = 1.8f;
    public const float TreasureEdgeInset = 4f;

    // Player
    public const float PlayerSpeed = 6f;
    public const int PlayerMaxHp = 100;
    public const float InvulnerabilityWindow = 0.75f;

    // Enemies (EnemySpeed must stay below PlayerSpeed so the player can escape)
    public const float EnemySpeed = 3.5f;
    public const int EnemyContactDamage = 10;
    public const float SpawnIntervalStart = 3.5f;
    public const float SpawnIntervalEnd = 1.5f;
    public const float SpawnRampDuration = 60f;

    // Spawn points: fixed red circles enemies emerge from, two per room. Insets
    // are measured from each room's corner, so the points sit on the diagonal
    // just inside opposite corners.
    // Shooting a spawn point destroys it after SpawnPointHits hits; it fades
    // toward SpawnPointMinAlphaFactor of its starting alpha as it takes damage.
    public const float SpawnPointSize = 1.6f;
    public const float SpawnPointCornerInset = 2.5f;
    public const int SpawnPointHits = 10;
    public const float SpawnPointMinAlphaFactor = 0.3f;

    // Projectiles (one hit kills an enemy)
    public const float ProjectileSpeed = 12f;
    public const float ProjectileLifetime = 2f;

    // Placeholder palette
    public static readonly Color PlayerColor = new Color(0.30f, 0.85f, 0.35f);
    public static readonly Color EnemyColor = new Color(0.90f, 0.25f, 0.20f);
    public static readonly Color ProjectileColor = new Color(1.00f, 0.85f, 0.25f);
    public static readonly Color WallColor = new Color(0.45f, 0.45f, 0.50f);
    public static readonly Color SpawnPointColor = new Color(0.85f, 0.15f, 0.15f, 0.75f);
    public static readonly Color TreasureColor = new Color(1.00f, 0.80f, 0.15f);
    public static readonly Color FloorColor = new Color(0.13f, 0.12f, 0.15f);
    public static readonly Color CameraBackgroundColor = new Color(0.05f, 0.05f, 0.07f);
    public static readonly Color HpBarFillColor = new Color(0.30f, 0.85f, 0.35f);
    public static readonly Color HpBarBackColor = new Color(0f, 0f, 0f, 0.6f);
}
