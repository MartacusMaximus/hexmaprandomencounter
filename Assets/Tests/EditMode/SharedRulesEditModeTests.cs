using KnightsAndGM.Shared;
using NUnit.Framework;

public class SharedRulesEditModeTests
{
    [Test]
    public void OffsetCoordinatesRoundTripInUnityTestRunner()
    {
        var coordinate = HexCoordinate.FromOddRowOffset(2, 3);
        var roundTrip = coordinate.ToOddRowOffset();

        Assert.AreEqual(2, roundTrip.Column);
        Assert.AreEqual(3, roundTrip.Row);
    }

    [Test]
    public void TravelRulesOnlyAllowAdjacentMoves()
    {
        var rules = new TravelRuleSet(1);
        var success = rules.ResolveMove(new HexCoordinate(0, 0), new HexCoordinate(1, 0), 2);
        var failure = rules.ResolveMove(new HexCoordinate(0, 0), new HexCoordinate(2, 0), 2);

        Assert.IsTrue(success.Success);
        Assert.IsFalse(failure.Success);
    }
}
