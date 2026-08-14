# Shooting is still allowed after winning

Status: needs-triage

## Problem

`GameManager` models the end of a round two ways: `IsGameOver` (loss), `HasWon`
(win), and `IsRoundOver` which is either. `PlayerShooting` gates on the wrong
one:

```csharp
// PlayerShooting.cs:20
if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
```

So after taking the treasure, shooting is still permitted, where after dying it
is not.

## Why it matters

Currently harmless — `Time.timeScale` is 0 on both paths, so nothing moves. It
is a latent inconsistency rather than a visible bug: any future code that stops
relying on `timeScale` (an end-of-round animation, a score tally screen) would
expose it.

The deeper issue is that three modules reach into `GameManager.Instance` and
each re-derives what "the round is over" means. Candidate 3 in the architecture
review proposes a `Round` module that states the rule once.

## Fix

One-line change to gate on `IsRoundOver`. The larger `Round` refactor is
separate.
