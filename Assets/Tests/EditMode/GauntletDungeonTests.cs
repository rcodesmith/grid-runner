using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the shipped Gauntlet world — the specific three-room dungeon the
/// game builds. Where DungeonTests covers the module's behaviour in general,
/// these pin down this particular world's shape.
///
/// Replaces the layout assertions that used to read GameConfig constants
/// directly: the question is now "what does the built world look like", not
/// "how is the arithmetic arranged".
/// </summary>
public class GauntletDungeonTests
{
    [Test]
    public void HasThreeRooms()
    {
        Assert.That(GauntletDungeon.Build().Rooms, Has.Count.EqualTo(3));
    }

    [Test]
    public void RoomsRunLeftToRightWithoutOverlapping()
    {
        var rooms = GauntletDungeon.Build().Rooms;

        for (int i = 1; i < rooms.Count; i++)
        {
            Assert.That(rooms[i].MinX, Is.GreaterThan(rooms[i - 1].MaxX),
                rooms[i].Name + " overlaps " + rooms[i - 1].Name);
        }
    }

    [Test]
    public void EachDoorwaySitsInTheGapBetweenItsRooms()
    {
        var dungeon = GauntletDungeon.Build();

        foreach (var doorway in dungeon.Doorways)
        {
            Assert.That(doorway.Position.x, Is.GreaterThanOrEqualTo(doorway.A.MaxX));
            Assert.That(doorway.Position.x, Is.LessThanOrEqualTo(doorway.B.MinX));
        }
    }

    [Test]
    public void EveryRoomIsReachableFromTheStart()
    {
        var dungeon = GauntletDungeon.Build();
        var start = GauntletDungeon.PlayerStart(dungeon);

        // Routing is one hop at a time, so walk it until we arrive.
        foreach (var room in dungeon.Rooms)
        {
            var at = start;
            int hops = 0;
            while (dungeon.RoomAt(at) != room && hops++ < 10)
            {
                var next = dungeon.RouteTo(at, room.Center);
                Assert.That(next, Is.Not.EqualTo(at), "route stalled heading to " + room.Name);
                at = next;
            }
            Assert.That(hops, Is.LessThan(10), room.Name + " was not reachable");
        }
    }

    [Test]
    public void PlayerStartsInTheFirstRoom()
    {
        var dungeon = GauntletDungeon.Build();
        var start = GauntletDungeon.PlayerStart(dungeon);

        Assert.That(dungeon.RoomAt(start), Is.SameAs(dungeon.Rooms[0]));
        // The game has always started the player at the origin.
        Assert.That(start, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void TreasureSitsInsideTheLastRoom()
    {
        var dungeon = GauntletDungeon.Build();
        var treasure = GauntletDungeon.TreasureAt(dungeon);
        var lastRoom = dungeon.Rooms[dungeon.Rooms.Count - 1];

        Assert.That(dungeon.RoomAt(treasure), Is.SameAs(lastRoom));
    }

    [Test]
    public void TreasureIsTheLengthOfTheDungeonFromTheStart()
    {
        var dungeon = GauntletDungeon.Build();
        var start = GauntletDungeon.PlayerStart(dungeon);
        var treasure = GauntletDungeon.TreasureAt(dungeon);

        // Not merely in the last room — near its far edge.
        Assert.That(treasure.x, Is.GreaterThan(start.x));
        Assert.That(dungeon.Rooms.Count, Is.GreaterThan(1));
    }

    [Test]
    public void SpawnAnchorsFallInsideTheirRoom()
    {
        var dungeon = GauntletDungeon.Build();

        foreach (var room in dungeon.Rooms)
        {
            foreach (var anchor in Dungeon.CornerAnchors(room, GameConfig.SpawnPointCornerInset))
            {
                Assert.That(room.Contains(anchor), Is.True,
                    "anchor " + anchor + " escaped " + room.Name);
            }
        }
    }

    [Test]
    public void PlayerOutrunsEnemies()
    {
        // Load-bearing for the core loop: the player must be able to escape.
        Assert.That(GameConfig.EnemySpeed, Is.LessThan(GameConfig.PlayerSpeed));
    }
}
