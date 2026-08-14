# Enemies have no HP — one-hit death is structural

Status: ready-for-human

## Problem

Enemy health does not exist as a concept. `Projectile` calls `enemy.Die()`
directly (`Projectile.cs:57`), and `Die()` destroys unconditionally
(`Enemy.cs:117-125`). There is no HP field, and projectiles carry no damage
value.

The same is true of the player's damage output generally: `EnemyContactDamage`
exists as a number, but nothing on the enemy side does.

Note `SpawnPoint` already implements exactly the pattern needed — a
`_hitsRemaining` counter, a `TakeHit()` that decrements and destroys at zero,
and a visual fade as it wears down (`SpawnPoint.cs:94-122`). The logic exists
but is not shared.

## Why it matters

A world file describing enemy variety (`{"type": "armoured", "hp": 3}`) has
nothing to bind to. This is a prerequisite for data-driven enemies, not merely
a tuning improvement.

## Why deferred

Adding HP is a **behaviour change**, not a refactor: it stops one-shot kills
being universal, which alters how the game plays. It was deliberately kept out
of the Dungeon refactor so that structural and behavioural changes stayed
separable.

## Sketch

- An HP field on `Enemy`, defaulting to 1 so current behaviour is preserved
- `Enemy.TakeDamage(int)` replacing the direct `Die()` call
- A damage value on `Projectile`, defaulting to 1
- Consider hoisting the shared "hits remaining, fade, destroy" behaviour out of
  `SpawnPoint` so both use it
