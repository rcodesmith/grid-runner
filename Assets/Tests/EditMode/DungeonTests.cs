using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the Dungeon module — the spatial layout of a world. These run
/// against Dungeon's interface with no GameObjects and no Play mode.
///
/// Several tests build layouts the shipped game does not have (four rooms, a
/// vertical branch) to pin down that Dungeon is topology-agnostic.
/// </summary>
public class DungeonTests
{
    // A plain two-room layout: 10x10 rooms side by side with a 1-unit wall.
    static Dungeon TwoRooms(out Room left, out Room right)
    {
        left = new Room("Left", new Vector2(-5.5f, 0f), new Vector2(10f, 10f));
        right = new Room("Right", new Vector2(5.5f, 0f), new Vector2(10f, 10f));
        var door = new Doorway(left, right, new Vector2(0f, 0f), 4f);
        return new Dungeon(new[] { left, right }, new[] { door }, 1.5f);
    }

    [Test]
    public void RoomAtFindsTheRoomContainingAPoint()
    {
        var dungeon = TwoRooms(out var left, out var right);

        Assert.That(dungeon.RoomAt(new Vector2(-5.5f, 0f)), Is.SameAs(left));
        Assert.That(dungeon.RoomAt(new Vector2(5.5f, 0f)), Is.SameAs(right));
    }

    [Test]
    public void RoomAtReturnsNullOutsideEveryRoom()
    {
        var dungeon = TwoRooms(out _, out _);

        // Inside the dividing wall, and far outside the world.
        Assert.That(dungeon.RoomAt(new Vector2(0f, 0f)), Is.Null);
        Assert.That(dungeon.RoomAt(new Vector2(0f, 500f)), Is.Null);
    }

    [Test]
    public void RouteWithinOneRoomHeadsStraightForTheDestination()
    {
        var dungeon = TwoRooms(out _, out _);
        var from = new Vector2(-8f, 2f);
        var to = new Vector2(-3f, -2f);

        Assert.That(dungeon.RouteTo(from, to), Is.EqualTo(to));
    }

    [Test]
    public void RouteToAnotherRoomAimsPastTheDoorway()
    {
        var dungeon = TwoRooms(out _, out _);
        var from = new Vector2(-8f, 3f);
        var to = new Vector2(8f, -3f);

        var waypoint = dungeon.RouteTo(from, to);

        // Door is at x = 0; travelling right, so the waypoint sits past it.
        Assert.That(waypoint.x, Is.EqualTo(1.5f).Within(0.001f));
        Assert.That(waypoint.y, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void RouteBackwardsAimsPastTheDoorwayTheOtherWay()
    {
        var dungeon = TwoRooms(out _, out _);

        var waypoint = dungeon.RouteTo(new Vector2(8f, 0f), new Vector2(-8f, 0f));

        Assert.That(waypoint.x, Is.EqualTo(-1.5f).Within(0.001f));
    }

    [Test]
    public void DoorwayOvershootClearsTheWall()
    {
        // The constraint that was previously an unwritten rule: aim far enough
        // past the doorway to be out of the wall, or travellers grind on it.
        Assert.That(GameConfig.DoorwayOvershoot,
            Is.GreaterThan(GameConfig.WallThickness / 2f),
            "overshoot must clear the wall or enemies stick in the doorway");
    }

    // ---- topology independence -------------------------------------------

    [Test]
    public void RoutesAcrossFourRoomsPickTheNearestDoorwayFirst()
    {
        // Four rooms in a row — one more than the shipped game has.
        var rooms = new List<Room>();
        var doorways = new List<Doorway>();
        for (int i = 0; i < 4; i++)
        {
            rooms.Add(new Room("R" + i, new Vector2(i * 11f, 0f), new Vector2(10f, 10f)));
        }
        for (int i = 0; i < 3; i++)
        {
            doorways.Add(new Doorway(rooms[i], rooms[i + 1],
                new Vector2(i * 11f + 5.5f, 0f), 4f));
        }
        var dungeon = new Dungeon(rooms, doorways, 1.5f);

        // From room 0 to room 3, the first hop is through the 0|1 doorway.
        var waypoint = dungeon.RouteTo(rooms[0].Center, rooms[3].Center);

        Assert.That(waypoint.x, Is.EqualTo(5.5f + 1.5f).Within(0.001f));
    }

    [Test]
    public void RoutesThroughAVerticalDoorway()
    {
        // A branch going up rather than along — impossible in the old
        // left-to-right pathing.
        var ground = new Room("Ground", Vector2.zero, new Vector2(10f, 10f));
        var upstairs = new Room("Upstairs", new Vector2(0f, 11f), new Vector2(10f, 10f));
        var door = new Doorway(ground, upstairs, new Vector2(0f, 5.5f), 4f);
        var dungeon = new Dungeon(new[] { ground, upstairs }, new[] { door }, 1.5f);

        var waypoint = dungeon.RouteTo(ground.Center, upstairs.Center);

        Assert.That(waypoint.x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(waypoint.y, Is.EqualTo(5.5f + 1.5f).Within(0.001f));
    }

    [Test]
    public void RouteIntoADisconnectedRoomFallsBackToTheDestination()
    {
        var a = new Room("A", Vector2.zero, new Vector2(10f, 10f));
        var island = new Room("Island", new Vector2(100f, 0f), new Vector2(10f, 10f));
        var dungeon = new Dungeon(new[] { a, island }, new Doorway[0], 1.5f);

        var to = island.Center;
        Assert.That(dungeon.RouteTo(a.Center, to), Is.EqualTo(to));
    }

    [Test]
    public void CornerAnchorsSitInsetOnTheDiagonal()
    {
        var room = new Room("R", new Vector2(10f, 20f), new Vector2(10f, 10f));

        var anchors = Dungeon.CornerAnchors(room, 2f).ToList();

        Assert.That(anchors, Has.Count.EqualTo(4));
        Assert.That(anchors, Has.Member(new Vector2(7f, 23f)));
        Assert.That(anchors, Has.Member(new Vector2(7f, 17f)));
        Assert.That(anchors, Has.Member(new Vector2(13f, 23f)));
        Assert.That(anchors, Has.Member(new Vector2(13f, 17f)));
    }
}
