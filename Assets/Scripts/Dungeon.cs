using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The spatial layout of a world: a set of rooms joined by doorways, plus the
/// anchor points things get placed at. Plain C# — no MonoBehaviour, no Unity
/// lifecycle — so it can be built and asked questions in EditMode tests.
///
/// A Dungeon is built from descriptions rather than read from global
/// constants, so a world loaded from a file is the same kind of thing as the
/// hand-written one in GameConfig. Nothing here assumes how many rooms there
/// are, or that they run left to right.
/// </summary>
public class Dungeon
{
    /// <summary>
    /// The dungeon the running game is playing in. Set by Bootstrap when the
    /// world is built, so entities created later (enemies, from spawn points)
    /// can navigate without every factory threading it through.
    ///
    /// Tests build their own Dungeon directly and never touch this.
    /// </summary>
    public static Dungeon Current { get; set; }

    readonly List<Room> _rooms;
    readonly List<Doorway> _doorways;

    /// <summary>How far past a doorway a traveller aims, so they clear the wall before re-steering.</summary>
    public float DoorwayOvershoot { get; }

    public Dungeon(IEnumerable<Room> rooms, IEnumerable<Doorway> doorways, float doorwayOvershoot)
    {
        _rooms = new List<Room>(rooms);
        _doorways = new List<Doorway>(doorways);
        DoorwayOvershoot = doorwayOvershoot;
    }

    public IReadOnlyList<Room> Rooms => _rooms;
    public IReadOnlyList<Doorway> Doorways => _doorways;

    /// <summary>
    /// The room containing this point, or null if it is inside no room (in a
    /// wall, or outside the world entirely).
    /// </summary>
    public Room RoomAt(Vector2 position)
    {
        foreach (var room in _rooms)
        {
            if (room.Contains(position))
            {
                return room;
            }
        }
        return null;
    }

    /// <summary>
    /// Where something at <paramref name="from"/> should head next to reach
    /// <paramref name="to"/>. Returns the destination itself when both are in
    /// the same room; otherwise the far side of the next doorway on the route.
    ///
    /// Callers steer toward the returned point and ask again as they move —
    /// this is one step of a route, not the whole path.
    /// </summary>
    public Vector2 RouteTo(Vector2 from, Vector2 to)
    {
        var fromRoom = RoomAt(from);
        var toRoom = RoomAt(to);

        // Outside any room (mid-doorway, or in a wall) — no route to give, so
        // head straight for the destination and re-ask once inside a room.
        if (fromRoom == null || toRoom == null || fromRoom == toRoom)
        {
            return to;
        }

        var doorway = FirstDoorwayOnRoute(fromRoom, toRoom);
        if (doorway == null)
        {
            // Disconnected rooms: nothing sensible to aim at.
            return to;
        }

        return doorway.ExitPointFrom(fromRoom, DoorwayOvershoot);
    }

    /// <summary>
    /// The first doorway to pass through travelling from one room to another,
    /// found by breadth-first search so the shortest hop count wins. Null when
    /// the rooms are not connected.
    /// </summary>
    Doorway FirstDoorwayOnRoute(Room from, Room to)
    {
        // Each frontier entry remembers the doorway the route started with, so
        // when the search arrives the answer is already in hand.
        var queue = new Queue<(Room room, Doorway firstStep)>();
        var visited = new HashSet<Room> { from };

        queue.Enqueue((from, null));

        while (queue.Count > 0)
        {
            var (room, firstStep) = queue.Dequeue();

            foreach (var doorway in _doorways)
            {
                var next = doorway.Other(room);
                if (next == null || !visited.Add(next))
                {
                    continue;
                }

                var step = firstStep ?? doorway;
                if (next == to)
                {
                    return step;
                }

                queue.Enqueue((next, step));
            }
        }

        return null;
    }

    /// <summary>
    /// Positions just inside a room's corners, on the diagonal. Used to place
    /// things (spawn points today) away from the middle of a room.
    /// </summary>
    public static IEnumerable<Vector2> CornerAnchors(Room room, float inset)
    {
        float x = room.Size.x / 2f - inset;
        float y = room.Size.y / 2f - inset;

        yield return room.Center + new Vector2(-x, y);
        yield return room.Center + new Vector2(-x, -y);
        yield return room.Center + new Vector2(x, y);
        yield return room.Center + new Vector2(x, -y);
    }
}

/// <summary>A rectangular room, identified by name and located by its center and size.</summary>
public class Room
{
    public string Name { get; }
    public Vector2 Center { get; }
    public Vector2 Size { get; }

    public Room(string name, Vector2 center, Vector2 size)
    {
        Name = name;
        Center = center;
        Size = size;
    }

    public float MinX => Center.x - Size.x / 2f;
    public float MaxX => Center.x + Size.x / 2f;
    public float MinY => Center.y - Size.y / 2f;
    public float MaxY => Center.y + Size.y / 2f;

    /// <summary>True when the point is within this room's bounds, edges included.</summary>
    public bool Contains(Vector2 point)
    {
        return point.x >= MinX && point.x <= MaxX
            && point.y >= MinY && point.y <= MaxY;
    }

    public override string ToString() => Name;
}

/// <summary>
/// A gap in the wall between two rooms. The position is the centre of the gap;
/// travellers aim slightly past it (see Dungeon.DoorwayOvershoot) so they clear
/// the wall rather than grinding along it.
/// </summary>
public class Doorway
{
    public Room A { get; }
    public Room B { get; }

    /// <summary>Centre of the gap, in world space.</summary>
    public Vector2 Position { get; }

    /// <summary>Width of the gap across the wall it pierces.</summary>
    public float Width { get; }

    public Doorway(Room a, Room b, Vector2 position, float width)
    {
        A = a;
        B = b;
        Position = position;
        Width = width;
    }

    /// <summary>The room on the far side of this doorway, or null if the given room isn't on either side.</summary>
    public Room Other(Room room)
    {
        if (room == A)
        {
            return B;
        }
        return room == B ? A : null;
    }

    /// <summary>
    /// A point just beyond the doorway as seen from <paramref name="from"/>,
    /// far enough through that the traveller has cleared the wall. Offset runs
    /// along the axis separating the two rooms.
    /// </summary>
    public Vector2 ExitPointFrom(Room from, float overshoot)
    {
        var destination = Other(from);
        if (destination == null)
        {
            return Position;
        }

        Vector2 towardDestination = destination.Center - from.Center;

        // The doorway pierces one wall, so step through it along whichever
        // axis actually separates the rooms.
        if (Mathf.Abs(towardDestination.x) >= Mathf.Abs(towardDestination.y))
        {
            return Position + new Vector2(Mathf.Sign(towardDestination.x) * overshoot, 0f);
        }
        return Position + new Vector2(0f, Mathf.Sign(towardDestination.y) * overshoot);
    }
}
