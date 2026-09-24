using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for how a character's facing follows the way it is trying to move:
/// it turns to any real direction and keeps its last facing when stopped.
/// </summary>
public class HeadingTests
{
    [Test]
    public void TurnsToFaceTheDesiredDirection()
    {
        var facing = Heading.Toward(Vector2.up, new Vector2(3f, 0f));

        Assert.That(facing, Is.EqualTo(Vector2.right));
    }

    [Test]
    public void KeepsItsFacingWhenStopped()
    {
        var facing = Heading.Toward(Vector2.left, Vector2.zero);

        Assert.That(facing, Is.EqualTo(Vector2.left));
    }

    [Test]
    public void IgnoresNegligibleNudges()
    {
        var facing = Heading.Toward(Vector2.left, new Vector2(0f, 0.001f));

        Assert.That(facing, Is.EqualTo(Vector2.left));
    }
}
