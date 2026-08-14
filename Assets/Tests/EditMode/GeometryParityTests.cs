using NUnit.Framework;

/// <summary>
/// Temporary scaffold: proves the Dungeon-built geometry matches the original
/// GameConfig constants exactly, so the refactor moves no walls.
///
/// Delete once the room constants themselves are gone — at that point these
/// assertions compare the new values against nothing meaningful.
/// </summary>
public class GeometryParityTests
{
    [Test]
    public void RoomCentersMatchTheOriginalConstants()
    {
        var rooms = GauntletDungeon.Build().Rooms;

        Assert.That(rooms[0].Center.x, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(rooms[1].Center.x, Is.EqualTo(GameConfig.Room2CenterX).Within(0.0001f));
        Assert.That(rooms[2].Center.x, Is.EqualTo(GameConfig.Room3CenterX).Within(0.0001f));
    }

    [Test]
    public void RoomSizesMatchTheOriginalConstants()
    {
        var rooms = GauntletDungeon.Build().Rooms;

        Assert.That(rooms[0].Size.x, Is.EqualTo(GameConfig.ArenaWidth).Within(0.0001f));
        Assert.That(rooms[0].Size.y, Is.EqualTo(GameConfig.ArenaHeight).Within(0.0001f));
        Assert.That(rooms[1].Size.x, Is.EqualTo(GameConfig.Room2Width).Within(0.0001f));
        Assert.That(rooms[1].Size.y, Is.EqualTo(GameConfig.Room2Height).Within(0.0001f));
        Assert.That(rooms[2].Size.x, Is.EqualTo(GameConfig.Room3Width).Within(0.0001f));
        Assert.That(rooms[2].Size.y, Is.EqualTo(GameConfig.Room3Height).Within(0.0001f));
    }

    [Test]
    public void DoorwayPositionsMatchTheOriginalDividerConstants()
    {
        var doorways = GauntletDungeon.Build().Doorways;

        Assert.That(doorways[0].Position.x, Is.EqualTo(GameConfig.DividerX).Within(0.0001f));
        Assert.That(doorways[1].Position.x, Is.EqualTo(GameConfig.Divider2X).Within(0.0001f));
    }

    [Test]
    public void TreasurePositionMatchesTheOriginalFormula()
    {
        var dungeon = GauntletDungeon.Build();
        var treasure = GauntletDungeon.TreasureAt(dungeon);

        float originalX = GameConfig.Room3CenterX + GameConfig.Room3Width / 2f
            - GameConfig.TreasureEdgeInset;

        Assert.That(treasure.x, Is.EqualTo(originalX).Within(0.0001f));
        Assert.That(treasure.y, Is.EqualTo(0f).Within(0.0001f));
    }
}
