using System.Collections.Generic;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Temporary scaffold: reproduces the exact geometry the pre-refactor
/// Bootstrap emitted (transcribed from commit 656c684) and asserts the
/// Dungeon-driven build places every wall, floor, spawn point and the treasure
/// at the same coordinates.
///
/// This guards the "preserve behaviour exactly" requirement of the Dungeon
/// refactor. Delete it once the room constants are gone and the world is
/// loaded from a file — at that point there is no legacy formula to compare
/// against.
/// </summary>
public class LegacyGeometryParityTests
{
    // A placed rectangle: name is ignored for comparison, only geometry counts.
    struct Piece
    {
        public Vector2 Center;
        public Vector2 Size;

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "({0:F3},{1:F3}) {2:F3}x{3:F3}", Center.x, Center.y, Size.x, Size.y);
        }
    }

    static void Add(List<Piece> into, Vector2 center, Vector2 size)
    {
        into.Add(new Piece { Center = center, Size = size });
    }

    // ---- the old code's arithmetic, transcribed verbatim -------------------

    static void LegacyWallsAndFloors(List<Piece> walls, List<Piece> floors)
    {
        float halfW = GameConfig.ArenaWidth / 2f;
        float halfH = GameConfig.ArenaHeight / 2f;
        float t = GameConfig.WallThickness;

        Add(floors, Vector2.zero, new Vector2(GameConfig.ArenaWidth, GameConfig.ArenaHeight));
        Add(walls, new Vector2(0f, halfH + t / 2f), new Vector2(GameConfig.ArenaWidth + 2f * t, t));
        Add(walls, new Vector2(0f, -(halfH + t / 2f)), new Vector2(GameConfig.ArenaWidth + 2f * t, t));
        Add(walls, new Vector2(-(halfW + t / 2f), 0f), new Vector2(t, GameConfig.ArenaHeight));

        float r2HalfH = GameConfig.Room2Height / 2f;
        float cx = GameConfig.Room2CenterX;

        Add(floors, new Vector2(cx, 0f), new Vector2(GameConfig.Room2Width, GameConfig.Room2Height));
        Add(walls, new Vector2(cx, r2HalfH + t / 2f), new Vector2(GameConfig.Room2Width + 2f * t, t));
        Add(walls, new Vector2(cx, -(r2HalfH + t / 2f)), new Vector2(GameConfig.Room2Width + 2f * t, t));

        float r3HalfW = GameConfig.Room3Width / 2f;
        float r3HalfH = GameConfig.Room3Height / 2f;
        float cx3 = GameConfig.Room3CenterX;

        Add(floors, new Vector2(cx3, 0f), new Vector2(GameConfig.Room3Width, GameConfig.Room3Height));
        Add(walls, new Vector2(cx3, r3HalfH + t / 2f), new Vector2(GameConfig.Room3Width + 2f * t, t));
        Add(walls, new Vector2(cx3, -(r3HalfH + t / 2f)), new Vector2(GameConfig.Room3Width + 2f * t, t));
        Add(walls, new Vector2(cx3 + r3HalfW + t / 2f, 0f), new Vector2(t, GameConfig.Room3Height));

        LegacyDivider(walls, floors, GameConfig.DividerX,
            Mathf.Max(GameConfig.ArenaHeight, GameConfig.Room2Height));
        LegacyDivider(walls, floors, GameConfig.Divider2X,
            Mathf.Max(GameConfig.Room2Height, GameConfig.Room3Height));
    }

    static void LegacyDivider(List<Piece> walls, List<Piece> floors, float x, float height)
    {
        float t = GameConfig.WallThickness;
        float doorHalf = GameConfig.DoorHeight / 2f;
        float segmentH = height / 2f - doorHalf;

        Add(walls, new Vector2(x, doorHalf + segmentH / 2f), new Vector2(t, segmentH));
        Add(walls, new Vector2(x, -(doorHalf + segmentH / 2f)), new Vector2(t, segmentH));
        Add(floors, new Vector2(x, 0f), new Vector2(t, GameConfig.DoorHeight));
    }

    static List<Vector2> LegacySpawnPoints()
    {
        float inset = GameConfig.SpawnPointCornerInset;
        var points = new List<Vector2>();

        float halfW = GameConfig.ArenaWidth / 2f - inset;
        float halfH = GameConfig.ArenaHeight / 2f - inset;
        points.Add(new Vector2(-halfW, halfH));
        points.Add(new Vector2(-halfW, -halfH));

        float cx = GameConfig.Room2CenterX;
        float r2HalfW = GameConfig.Room2Width / 2f - inset;
        float r2HalfH = GameConfig.Room2Height / 2f - inset;
        points.Add(new Vector2(cx + r2HalfW, r2HalfH));
        points.Add(new Vector2(cx + r2HalfW, -r2HalfH));

        float cx3 = GameConfig.Room3CenterX;
        float r3HalfW = GameConfig.Room3Width / 2f - inset;
        float r3HalfH = GameConfig.Room3Height / 2f - inset;
        points.Add(new Vector2(cx3 - r3HalfW, r3HalfH));
        points.Add(new Vector2(cx3 - r3HalfW, -r3HalfH));
        points.Add(new Vector2(cx3 + r3HalfW, r3HalfH));
        points.Add(new Vector2(cx3 + r3HalfW, -r3HalfH));

        return points;
    }

    // ---- the new build, expressed the way Bootstrap now does it ------------

    static void CurrentWallsAndFloors(List<Piece> walls, List<Piece> floors)
    {
        var dungeon = GauntletDungeon.Build();
        float t = GameConfig.WallThickness;

        foreach (var room in dungeon.Rooms)
        {
            Add(floors, room.Center, room.Size);
            Add(walls, new Vector2(room.Center.x, room.MaxY + t / 2f),
                new Vector2(room.Size.x + 2f * t, t));
            Add(walls, new Vector2(room.Center.x, room.MinY - t / 2f),
                new Vector2(room.Size.x + 2f * t, t));

            if (!SideHasDoorway(dungeon, room, left: true))
            {
                Add(walls, new Vector2(room.MinX - t / 2f, room.Center.y),
                    new Vector2(t, room.Size.y));
            }
            if (!SideHasDoorway(dungeon, room, left: false))
            {
                Add(walls, new Vector2(room.MaxX + t / 2f, room.Center.y),
                    new Vector2(t, room.Size.y));
            }
        }

        foreach (var doorway in dungeon.Doorways)
        {
            float height = Mathf.Max(doorway.A.Size.y, doorway.B.Size.y);
            float doorHalf = doorway.Width / 2f;
            float segmentH = height / 2f - doorHalf;
            float x = doorway.Position.x;
            float y = doorway.Position.y;

            Add(walls, new Vector2(x, y + doorHalf + segmentH / 2f), new Vector2(t, segmentH));
            Add(walls, new Vector2(x, y - doorHalf - segmentH / 2f), new Vector2(t, segmentH));
            Add(floors, new Vector2(x, y), new Vector2(t, doorway.Width));
        }
    }

    static bool SideHasDoorway(Dungeon dungeon, Room room, bool left)
    {
        float edge = left ? room.MinX : room.MaxX;
        foreach (var doorway in dungeon.Doorways)
        {
            if (doorway.Other(room) == null)
            {
                continue;
            }
            if (Mathf.Abs(doorway.Position.x - edge) <= GameConfig.WallThickness)
            {
                return true;
            }
        }
        return false;
    }

    static List<Vector2> CurrentSpawnPoints()
    {
        var dungeon = GauntletDungeon.Build();
        float inset = GameConfig.SpawnPointCornerInset;
        var points = new List<Vector2>();

        for (int i = 0; i < dungeon.Rooms.Count; i++)
        {
            var room = dungeon.Rooms[i];
            bool isLastRoom = i == dungeon.Rooms.Count - 1;
            int index = 0;
            foreach (var anchor in Dungeon.CornerAnchors(room, inset))
            {
                bool isLeftCorner = index < 2;
                bool keep = isLastRoom || (i == 0 ? isLeftCorner : !isLeftCorner);
                if (keep)
                {
                    points.Add(anchor);
                }
                index++;
            }
        }
        return points;
    }

    // ---- comparisons -------------------------------------------------------

    static void AssertSameSet(List<Piece> expected, List<Piece> actual, string what)
    {
        Assert.That(actual, Has.Count.EqualTo(expected.Count), what + ": piece count changed");

        foreach (var want in expected)
        {
            bool found = actual.Exists(got =>
                Vector2.Distance(got.Center, want.Center) < 0.0005f &&
                Vector2.Distance(got.Size, want.Size) < 0.0005f);

            Assert.That(found, Is.True, what + ": no match for legacy piece " + want);
        }
    }

    [Test]
    public void WallsAreUnchanged()
    {
        var legacyWalls = new List<Piece>();
        var legacyFloors = new List<Piece>();
        LegacyWallsAndFloors(legacyWalls, legacyFloors);

        var currentWalls = new List<Piece>();
        var currentFloors = new List<Piece>();
        CurrentWallsAndFloors(currentWalls, currentFloors);

        AssertSameSet(legacyWalls, currentWalls, "walls");
    }

    [Test]
    public void FloorsAreUnchanged()
    {
        var legacyWalls = new List<Piece>();
        var legacyFloors = new List<Piece>();
        LegacyWallsAndFloors(legacyWalls, legacyFloors);

        var currentWalls = new List<Piece>();
        var currentFloors = new List<Piece>();
        CurrentWallsAndFloors(currentWalls, currentFloors);

        AssertSameSet(legacyFloors, currentFloors, "floors");
    }

    [Test]
    public void SpawnPointsAreUnchanged()
    {
        var legacy = LegacySpawnPoints();
        var current = CurrentSpawnPoints();

        Assert.That(current, Has.Count.EqualTo(legacy.Count), "spawn point count changed");

        foreach (var want in legacy)
        {
            bool found = current.Exists(got => Vector2.Distance(got, want) < 0.0005f);
            Assert.That(found, Is.True, "no spawn point at legacy position " + want);
        }
    }

    [Test]
    public void TreasureAndPlayerStartAreUnchanged()
    {
        var dungeon = GauntletDungeon.Build();

        float legacyTreasureX = GameConfig.Room3CenterX + GameConfig.Room3Width / 2f
            - GameConfig.TreasureEdgeInset;

        Assert.That(GauntletDungeon.TreasureAt(dungeon).x,
            Is.EqualTo(legacyTreasureX).Within(0.0005f));
        Assert.That(GauntletDungeon.TreasureAt(dungeon).y, Is.EqualTo(0f).Within(0.0005f));
        Assert.That(GauntletDungeon.PlayerStart(dungeon), Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void EnemyRoutingMatchesTheLegacyWaypoints()
    {
        var dungeon = GauntletDungeon.Build();

        // Legacy: heading right from room 0, aim at DividerX + 1.5 on y = 0.
        var fromRoom1 = dungeon.RouteTo(new Vector2(-5f, 3f), new Vector2(GameConfig.Room3CenterX, 0f));
        Assert.That(fromRoom1.x, Is.EqualTo(GameConfig.DividerX + 1.5f).Within(0.0005f));
        Assert.That(fromRoom1.y, Is.EqualTo(0f).Within(0.0005f));

        // Legacy: heading left from room 2, aim at Divider2X - 1.5 on y = 0.
        var fromRoom3 = dungeon.RouteTo(new Vector2(GameConfig.Room3CenterX, 2f), new Vector2(-5f, 0f));
        Assert.That(fromRoom3.x, Is.EqualTo(GameConfig.Divider2X - 1.5f).Within(0.0005f));
        Assert.That(fromRoom3.y, Is.EqualTo(0f).Within(0.0005f));
    }
}
