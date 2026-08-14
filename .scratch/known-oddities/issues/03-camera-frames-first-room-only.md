# Camera framing is sized to the first room only

Status: needs-triage

## Problem

`Bootstrap.CreateCamera` sets the orthographic size from the starting room's
dimensions, so the view is framed for a 24x14 room. Room 2 is 36x22 — larger
than the camera shows.

After the Dungeon refactor this reads `dungeon.Rooms[0].Size` rather than the
`ArenaWidth`/`ArenaHeight` constants, so the behaviour is unchanged but the
assumption is now explicit and easy to find.

## Why it matters

In a world loaded from a file, room sizes are arbitrary. Framing on whichever
room happens to be first is unlikely to be right. Options worth weighing:

- Frame on the largest room in the world
- Frame per-room, transitioning as the player crosses a doorway
- Declare the camera framing in the world file itself

## Notes

Not a bug today — the current dungeon plays fine, and `CameraFollow` keeps the
player centred. Filed because the world-file direction makes "the first room"
an arbitrary choice rather than a sensible default.
