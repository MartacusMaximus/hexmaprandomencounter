using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;

namespace KnightsAndGM.Domain.Tests;

public sealed class SharedRulesTests
{
    [Fact]
    public void AdjacentHexesAreRecognizedByCubeDistance()
    {
        var origin = new HexCoordinate(0, 0);
        var adjacent = new HexCoordinate(1, 0);
        var distant = new HexCoordinate(2, -1);

        Assert.True(HexAdjacency.AreNeighbors(origin, adjacent));
        Assert.False(HexAdjacency.AreNeighbors(origin, distant));
    }

    [Fact]
    public void OddRowOffsetRoundTripsThroughCubeCoordinates()
    {
        var coordinate = HexCoordinate.FromOddRowOffset(3, 5);
        var roundTrip = coordinate.ToOddRowOffset();

        Assert.Equal(3, roundTrip.Column);
        Assert.Equal(5, roundTrip.Row);
    }

    [Fact]
    public void DeterministicMapGenerationHonorsSeedAndOverrides()
    {
        var generator = new DeterministicMapGenerator();
        var overrideCoordinate = new HexCoordinate(1, -1);
        var first = generator.Generate(Guid.NewGuid(), "seed-a", 2, new[]
        {
            new HexTileOverride
            {
                Coordinate = overrideCoordinate,
                Terrain = TerrainType.City,
                PublicLandmarkName = "Override Landmark"
            }
        });
        var second = generator.Generate(Guid.NewGuid(), "seed-a", 2, new[]
        {
            new HexTileOverride
            {
                Coordinate = overrideCoordinate,
                Terrain = TerrainType.City,
                PublicLandmarkName = "Override Landmark"
            }
        });

        Assert.Equal(first.Count, second.Count);
        Assert.Equal("Override Landmark", first.Single(tile => tile.Coordinate == overrideCoordinate).PublicLandmarkName);
        Assert.Equal(first.Select(tile => tile.Terrain), second.Select(tile => tile.Terrain));
    }

    [Fact]
    public void TravelRulesRejectNonNeighborMovesAndSpendEffortOnValidMoves()
    {
        var ruleSet = new TravelRuleSet(1);

        var success = ruleSet.ResolveMove(new HexCoordinate(0, 0), new HexCoordinate(1, 0), 3);
        var failure = ruleSet.ResolveMove(new HexCoordinate(0, 0), new HexCoordinate(2, 0), 3);

        Assert.True(success.Success);
        Assert.Equal(2, success.RemainingEffort);
        Assert.False(failure.Success);
        Assert.Equal("not_adjacent", failure.ErrorCode);
    }

    [Fact]
    public void ArmorTotalUsesBestPiecePerLocationIncludingShield()
    {
        var character = new CharacterSheetModel
        {
            Inventory = new List<EquipmentSlotModel>
            {
                new() { SlotIndex = 0, Equipment = new PortableEquipmentModel { IsArmor = true, ArmorValue = 1, ArmorLocation = PortableEquipmentLocation.Head } },
                new() { SlotIndex = 1, Equipment = new PortableEquipmentModel { IsArmor = true, ArmorValue = 4, ArmorLocation = PortableEquipmentLocation.Torso } },
                new() { SlotIndex = 2, Equipment = new PortableEquipmentModel { IsArmor = true, ArmorValue = 2, ArmorLocation = PortableEquipmentLocation.Torso } },
                new() { SlotIndex = 3, Equipment = new PortableEquipmentModel { IsArmor = true, ArmorValue = 3, ArmorLocation = PortableEquipmentLocation.Shield } }
            }
        };

        var total = InventoryStatCalculator.GetEffectiveArmorTotal(character);

        Assert.Equal(8, total);
    }

    [Fact]
    public void ChunkCounterCountsCentersAndCompletedLinks()
    {
        var character = new CharacterSheetModel
        {
            Inventory = Enumerable.Range(0, 9)
                .Select(index => new EquipmentSlotModel { SlotIndex = index })
                .ToList()
        };

        character.Inventory[0].Equipment = new PortableEquipmentModel
        {
            RightHalf = PortableChunkColor.Red,
            BottomHalf = PortableChunkColor.Blue
        };
        character.Inventory[1].Equipment = new PortableEquipmentModel
        {
            LeftHalf = PortableChunkColor.Red
        };
        character.Inventory[3].Equipment = new PortableEquipmentModel
        {
            TopHalf = PortableChunkColor.Blue
        };
        character.Inventory[4].Equipment = new PortableEquipmentModel
        {
            CenterChunk = PortableChunkColor.Green
        };

        var counts = InventoryStatCalculator.CountChunks(character);

        Assert.Equal(1, counts[PortableChunkColor.Red]);
        Assert.Equal(1, counts[PortableChunkColor.Blue]);
        Assert.Equal(1, counts[PortableChunkColor.Green]);
        Assert.Equal(0, counts[PortableChunkColor.Rainbow]);
    }
}
