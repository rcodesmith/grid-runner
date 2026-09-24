using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Tests for the facing → pose mapping that picks a character's sprite frame.
/// Sheets draw 5 poses (0 up, 1 up-right, 2 right, 3 down-right, 4 down); the
/// left-hand directions reuse the right-hand poses mirrored with flipX.
/// </summary>
public class FacingPoseTests
{
    [TestCase(0f, 1f, 0, false, TestName = "Up")]
    [TestCase(1f, 1f, 1, false, TestName = "UpRight")]
    [TestCase(1f, 0f, 2, false, TestName = "Right")]
    [TestCase(1f, -1f, 3, false, TestName = "DownRight")]
    [TestCase(0f, -1f, 4, false, TestName = "Down")]
    [TestCase(-1f, -1f, 3, true, TestName = "DownLeft")]
    [TestCase(-1f, 0f, 2, true, TestName = "Left")]
    [TestCase(-1f, 1f, 1, true, TestName = "UpLeft")]
    public void CompassDirectionsMapToTheirPose(float x, float y, int index, bool flipX)
    {
        var pose = FacingPose.For(new Vector2(x, y));

        Assert.That(pose.Index, Is.EqualTo(index));
        Assert.That(pose.FlipX, Is.EqualTo(flipX));
    }

    [Test]
    public void NoDirectionFacesUp()
    {
        var pose = FacingPose.For(Vector2.zero);

        Assert.That(pose.Index, Is.EqualTo(0));
        Assert.That(pose.FlipX, Is.False);
    }

    // Each compass direction owns the 45° wedge centred on it, so the switch
    // happens 22.5° either side. Angles are degrees clockwise from up.
    [TestCase(22.4f, 0, false)]
    [TestCase(22.6f, 1, false)]
    [TestCase(67.4f, 1, false)]
    [TestCase(67.6f, 2, false)]
    [TestCase(112.4f, 2, false)]
    [TestCase(112.6f, 3, false)]
    [TestCase(157.4f, 3, false)]
    [TestCase(157.6f, 4, false)]
    [TestCase(-157.6f, 4, false)]
    [TestCase(-157.4f, 3, true)]
    [TestCase(-112.6f, 3, true)]
    [TestCase(-112.4f, 2, true)]
    [TestCase(-67.6f, 2, true)]
    [TestCase(-67.4f, 1, true)]
    [TestCase(-22.6f, 1, true)]
    [TestCase(-22.4f, 0, false)]
    public void DirectionsSnapToTheNearestCompassPoint(float degreesClockwiseFromUp, int index, bool flipX)
    {
        float radians = degreesClockwiseFromUp * Mathf.Deg2Rad;
        var pose = FacingPose.For(new Vector2(Mathf.Sin(radians), Mathf.Cos(radians)));

        Assert.That(pose.Index, Is.EqualTo(index));
        Assert.That(pose.FlipX, Is.EqualTo(flipX));
    }

    [Test]
    public void StraightDownIsNeverMirrored()
    {
        // -0 on x reads as -180°, the left-hand side of down.
        var pose = FacingPose.For(new Vector2(-0f, -1f));

        Assert.That(pose.Index, Is.EqualTo(4));
        Assert.That(pose.FlipX, Is.False);
    }
}
