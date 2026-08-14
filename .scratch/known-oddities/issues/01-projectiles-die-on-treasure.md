# Projectiles are destroyed by the treasure

Status: needs-triage

## Problem

`Projectile.OnTriggerEnter2D` identifies arena walls structurally, as "a
collider with no attached rigidbody":

```csharp
// Projectile.cs:70-71
// Colliders with no attached rigidbody are the static arena walls.
if (other.attachedRigidbody == null)
```

`Treasure` is a trigger collider with no rigidbody (`Treasure.cs:26-27`), so it
matches that predicate. Shooting toward the treasure destroys the projectile as
if it had hit a wall.

## Why it matters

It is undiscoverable from either file whether this is intended. More
importantly, the "no rigidbody = wall" rule grows fragile as more world objects
arrive: every data-defined static object becomes a projectile blocker with no
way to opt out. A world file that places decorative scenery would silently make
it bulletproof.

## Notes

Found during the architecture review, not during play. Nobody has reported it
as a gameplay problem — it may not be worth changing on its own, but it should
be settled before world files can define arbitrary static objects.
