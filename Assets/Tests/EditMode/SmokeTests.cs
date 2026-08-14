using NUnit.Framework;

/// <summary>
/// Proves the EditMode test loop is wired up: the test assembly compiles,
/// NUnit resolves, and the Gauntlet runtime assembly is referenceable.
/// Asserts against GameConfig because it is pure data with no Unity lifecycle
/// — no GameObject, no Play mode.
/// </summary>
public class SmokeTests
{
    [Test]
    public void RuntimeAssemblyIsReferenceable()
    {
        Assert.That(GameConfig.ArenaWidth, Is.GreaterThan(0f));
    }

    [Test]
    public void RoomsAreLaidOutLeftToRightWithoutOverlapping()
    {
        // Room 1 is centred on the origin; rooms 2 and 3 chain off to the right.
        float room1RightEdge = GameConfig.ArenaWidth / 2f;
        float room2LeftEdge = GameConfig.Room2CenterX - GameConfig.Room2Width / 2f;
        float room3LeftEdge = GameConfig.Room3CenterX - GameConfig.Room3Width / 2f;
        float room2RightEdge = GameConfig.Room2CenterX + GameConfig.Room2Width / 2f;

        Assert.That(room2LeftEdge, Is.GreaterThan(room1RightEdge), "room 2 overlaps room 1");
        Assert.That(room3LeftEdge, Is.GreaterThan(room2RightEdge), "room 3 overlaps room 2");
    }

    [Test]
    public void EachDividerSitsBetweenTheRoomsItSeparates()
    {
        float room1RightEdge = GameConfig.ArenaWidth / 2f;
        float room2LeftEdge = GameConfig.Room2CenterX - GameConfig.Room2Width / 2f;

        Assert.That(GameConfig.DividerX, Is.GreaterThanOrEqualTo(room1RightEdge));
        Assert.That(GameConfig.DividerX, Is.LessThanOrEqualTo(room2LeftEdge));
    }

    [Test]
    public void PlayerOutrunsEnemies()
    {
        // Load-bearing for the core loop: the player must always be able to escape.
        Assert.That(GameConfig.EnemySpeed, Is.LessThan(GameConfig.PlayerSpeed));
    }
}
