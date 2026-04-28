using System.Collections.Generic;
using System.Linq;

namespace KnightsAndGM.Shared
{
    public sealed class CharacterCreationConfig
    {
        public int BaseVirtueStart { get; set; } = 6;
        public int VirtueMax { get; set; } = 18;
        public int StartingPoints { get; set; } = 50;
        public int DeedPointsPerDeed { get; set; } = 3;
        public int FlawGrant { get; set; } = 5;
        public int CoreAbilityCost { get; set; } = 15;
        public int NewSkillCost { get; set; } = 3;
        public int MaxSkills { get; set; } = 10;
    }

    public static class CharacterCreationRules
    {
        public static int CalculatePointsLeft(CharacterSheetModel character, CharacterCreationConfig config)
        {
            var virtuesSpent =
                ClampVirtueSpend(character.Vigor, config) +
                ClampVirtueSpend(character.Clarity, config) +
                ClampVirtueSpend(character.Spirit, config);

            var skillsSpent = character.Skills.Sum(skill => skill.Value < config.NewSkillCost ? config.NewSkillCost : skill.Value);

            var inventorySpent = 0;
            foreach (var slot in character.Inventory)
            {
                if (slot.Equipment != null && slot.Equipment.CostsCreationPoints)
                {
                    inventorySpent += slot.Equipment.PointCost;
                }
            }

            var coreSpent = character.HasCoreAbility ? config.CoreAbilityCost : 0;
            var granted = (character.FlawCount * config.FlawGrant) + (character.DeedCount * config.DeedPointsPerDeed);

            return config.StartingPoints + granted - virtuesSpent - skillsSpent - inventorySpent - coreSpent;
        }

        public static bool CanAdjustVirtue(int currentValue, int delta, CharacterCreationConfig config)
        {
            var next = currentValue + delta;
            return next >= config.BaseVirtueStart && next <= config.VirtueMax;
        }

        public static bool CanAddSkill(CharacterSheetModel character, CharacterCreationConfig config)
        {
            return character.Skills.Count < config.MaxSkills && CalculatePointsLeft(character, config) >= config.NewSkillCost;
        }

        private static int ClampVirtueSpend(int value, CharacterCreationConfig config)
        {
            return value > config.BaseVirtueStart ? value - config.BaseVirtueStart : 0;
        }
    }

    public static class InventoryStatCalculator
    {
        public static Dictionary<PortableChunkColor, int> CountChunks(CharacterSheetModel character)
        {
            var counts = new Dictionary<PortableChunkColor, int>
            {
                [PortableChunkColor.Red] = 0,
                [PortableChunkColor.Green] = 0,
                [PortableChunkColor.Blue] = 0,
                [PortableChunkColor.Rainbow] = 0
            };

            for (var index = 0; index < character.Inventory.Count; index++)
            {
                var equipment = character.Inventory[index].Equipment;
                if (equipment == null)
                {
                    continue;
                }

                if (equipment.CenterChunk != PortableChunkColor.None)
                {
                    counts[equipment.CenterChunk]++;
                }

                var row = index / 3;
                var column = index % 3;

                if (equipment.RightHalf != PortableChunkColor.None && column < 2)
                {
                    var rightNeighbor = character.Inventory[index + 1].Equipment;
                    if (rightNeighbor != null && rightNeighbor.LeftHalf == equipment.RightHalf)
                    {
                        counts[equipment.RightHalf]++;
                    }
                }

                if (equipment.BottomHalf != PortableChunkColor.None && row < 2)
                {
                    var bottomNeighbor = character.Inventory[index + 3].Equipment;
                    if (bottomNeighbor != null && bottomNeighbor.TopHalf == equipment.BottomHalf)
                    {
                        counts[equipment.BottomHalf]++;
                    }
                }
            }

            return counts;
        }

        public static int GetVigorDamageBonus(CharacterSheetModel character)
        {
            return character.Vigor / 6;
        }

        public static int GetSpiritGuardBonus(CharacterSheetModel character)
        {
            return character.Spirit / 3;
        }

        public static int GetClaritySaveModifier(CharacterSheetModel character)
        {
            return character.Clarity / 8;
        }

        public static int GetEffectiveArmorTotal(CharacterSheetModel character)
        {
            var bestByLocation = new Dictionary<PortableEquipmentLocation, int>();
            foreach (var slot in character.Inventory)
            {
                var equipment = slot.Equipment;
                if (equipment == null || !equipment.IsArmor)
                {
                    continue;
                }

                if (!bestByLocation.TryGetValue(equipment.ArmorLocation, out var currentBest))
                {
                    bestByLocation[equipment.ArmorLocation] = equipment.ArmorValue;
                    continue;
                }

                if (equipment.ArmorValue > currentBest)
                {
                    bestByLocation[equipment.ArmorLocation] = equipment.ArmorValue;
                }
            }

            return bestByLocation.Values.Sum();
        }

        public static IEnumerable<PortableEquipmentModel> GetActiveWeapons(CharacterSheetModel character)
        {
            return character.Inventory
                .Where(slot => slot.Equipment != null && slot.Equipment.IsWeapon)
                .Select(slot => slot.Equipment);
        }
    }
}
