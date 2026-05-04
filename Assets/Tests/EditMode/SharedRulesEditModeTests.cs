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

    [Test]
    public void InventoryActivationSnapshotRequiresLinksAndCountsCenterChunks()
    {
        var character = new CharacterSheetModel
        {
            Spirit = 9
        };

        for (var index = 0; index < 9; index++)
        {
            character.Inventory.Add(new EquipmentSlotModel { SlotIndex = index });
        }

        character.Inventory[0].Equipment = new PortableEquipmentModel
        {
            Name = "Banner Token",
            ContributesToEquippedBonuses = true,
            CenterChunk = PortableChunkColor.Red
        };

        character.Inventory[3].Equipment = new PortableEquipmentModel
        {
            Name = "Left Link",
            ContributesToEquippedBonuses = true,
            RightHalf = PortableChunkColor.Red
        };

        character.Inventory[4].Equipment = new PortableEquipmentModel
        {
            Name = "Cuirass",
            ContributesToEquippedBonuses = true,
            LeftHalf = PortableChunkColor.Red,
            RightHalf = PortableChunkColor.Green,
            BottomHalf = PortableChunkColor.Blue,
            Ability = new PortableAbilityModel
            {
                RequiresLinkedChunk = true,
                RequiredChunkCount = 4,
                RequiredColor = PortableChunkColor.None,
                AddDamageFlat = 3,
                AddGuardFlat = 2,
                ModifyReaction = 1
            }
        };

        character.Inventory[5].Equipment = new PortableEquipmentModel
        {
            Name = "Right Link",
            ContributesToEquippedBonuses = true,
            LeftHalf = PortableChunkColor.Green
        };

        character.Inventory[7].Equipment = new PortableEquipmentModel
        {
            Name = "Lower Link",
            ContributesToEquippedBonuses = true,
            TopHalf = PortableChunkColor.Blue
        };

        var snapshot = InventoryStatCalculator.BuildActivationSnapshot(character);

        Assert.That(snapshot.ChunkCounts[PortableChunkColor.Red], Is.EqualTo(2));
        Assert.That(snapshot.ChunkCounts[PortableChunkColor.Green], Is.EqualTo(1));
        Assert.That(snapshot.ChunkCounts[PortableChunkColor.Blue], Is.EqualTo(1));
        Assert.That(snapshot.Slots[4].LinkedRequirementMet, Is.True);
        Assert.That(snapshot.Slots[4].ChunkRequirementMet, Is.True);
        Assert.That(snapshot.Slots[4].AbilityActive, Is.True);
        Assert.That(snapshot.ActiveAbilityDamageFlat, Is.EqualTo(3));
        Assert.That(snapshot.ActiveAbilityGuardFlat, Is.EqualTo(2));
        Assert.That(snapshot.ActiveAbilityReactionModifier, Is.EqualTo(1));
        Assert.That(InventoryStatCalculator.GetSpiritGuardBonus(character), Is.EqualTo(5));
        Assert.That(InventoryStatCalculator.GetReactionModifier(character), Is.EqualTo(1));
        Assert.That(InventoryStatCalculator.GetActiveAbilityDamageBonus(character), Is.EqualTo(3));
    }
}
