using UnityEngine;

/// <summary>
/// Builds the hand-authored three-room dungeon from the values in GameConfig.
///
/// This is the seam where a world file will eventually plug in: today the room
/// descriptions are written here in C#, later they will be read from a text
/// world specification. Either way the result is a Dungeon, and everything
/// downstream (Bootstrap, Enemy) only knows about that.
/// </summary>
public static class GauntletDungeon
{
    public static Dungeon Build()
    {
        float t = GameConfig.WallThickness;

        var room1 = new Room("Room 1",
            Vector2.zero,
            new Vector2(GameConfig.ArenaWidth, GameConfig.ArenaHeight));

        var room2 = new Room("Room 2",
            new Vector2(room1.MaxX + t + GameConfig.Room2Width / 2f, 0f),
            new Vector2(GameConfig.Room2Width, GameConfig.Room2Height));

        var room3 = new Room("Room 3",
            new Vector2(room2.MaxX + t + GameConfig.Room3Width / 2f, 0f),
            new Vector2(GameConfig.Room3Width, GameConfig.Room3Height));

        // Doorways sit on the centreline of the wall between each pair, at
        // y = 0 — the layout the game has always had.
        var door1 = new Doorway(room1, room2,
            new Vector2(room1.MaxX + t / 2f, 0f), GameConfig.DoorHeight);

        var door2 = new Doorway(room2, room3,
            new Vector2(room2.MaxX + t / 2f, 0f), GameConfig.DoorHeight);

        return new Dungeon(
            new[] { room1, room2, room3 },
            new[] { door1, door2 },
            GameConfig.DoorwayOvershoot);
    }

    /// <summary>
    /// Where the player starts. Centre of the first room — the origin today.
    /// </summary>
    public static Vector2 PlayerStart(Dungeon dungeon)
    {
        return dungeon.Rooms.Count > 0 ? dungeon.Rooms[0].Center : Vector2.zero;
    }

    /// <summary>
    /// Where the treasure sits: inset from the far edge of the last room — the
    /// length of the dungeon away from the player's start.
    /// </summary>
    public static Vector2 TreasureAt(Dungeon dungeon)
    {
        var lastRoom = dungeon.Rooms[dungeon.Rooms.Count - 1];
        return new Vector2(lastRoom.MaxX - GameConfig.TreasureEdgeInset, lastRoom.Center.y);
    }
}
