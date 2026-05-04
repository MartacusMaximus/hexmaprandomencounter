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
            return BuildActivationSnapshot(character).ChunkCounts;
        }

        public static PortableInventoryActivationModel BuildActivationSnapshot(CharacterSheetModel character)
        {
            var snapshot = new PortableInventoryActivationModel
            {
                ChunkCounts = new Dictionary<PortableChunkColor, int>
                {
                    [PortableChunkColor.Red] = 0,
                    [PortableChunkColor.Green] = 0,
                    [PortableChunkColor.Blue] = 0,
                    [PortableChunkColor.Rainbow] = 0
                }
            };

            if (character == null)
            {
                return snapshot;
            }

            for (var index = 0; index < character.Inventory.Count; index++)
            {
                snapshot.Slots.Add(new PortableSlotActivationModel
                {
                    SlotIndex = index
                });
            }

            for (var index = 0; index < character.Inventory.Count; index++)
            {
                var equipment = character.Inventory[index].Equipment;
                if (equipment == null || !equipment.ContributesToEquippedBonuses)
                {
                    continue;
                }

                if (equipment.CenterChunk != PortableChunkColor.None)
                {
                    snapshot.ChunkCounts[equipment.CenterChunk]++;
                    snapshot.Slots[index].CenterActive = true;
                }

                var row = index / 3;
                var column = index % 3;

                if (equipment.RightHalf != PortableChunkColor.None && column < 2)
                {
                    var neighbor = character.Inventory[index + 1].Equipment;
                    if (neighbor != null &&
                        neighbor.ContributesToEquippedBonuses &&
                        TryResolveLinkedColor(equipment.RightHalf, neighbor.LeftHalf, out var linkedColor))
                    {
                        snapshot.ChunkCounts[linkedColor]++;
                        snapshot.Slots[index].RightLinked = true;
                        snapshot.Slots[index + 1].LeftLinked = true;
                    }
                }

                if (equipment.BottomHalf != PortableChunkColor.None && row < 2)
                {
                    var neighbor = character.Inventory[index + 3].Equipment;
                    if (neighbor != null &&
                        neighbor.ContributesToEquippedBonuses &&
                        TryResolveLinkedColor(equipment.BottomHalf, neighbor.TopHalf, out var linkedColor))
                    {
                        snapshot.ChunkCounts[linkedColor]++;
                        snapshot.Slots[index].BottomLinked = true;
                        snapshot.Slots[index + 3].TopLinked = true;
                    }
                }
            }

            for (var index = 0; index < character.Inventory.Count; index++)
            {
                var equipment = character.Inventory[index].Equipment;
                var slot = snapshot.Slots[index];
                if (equipment?.Ability == null || !equipment.ContributesToEquippedBonuses)
                {
                    continue;
                }

                slot.LinkedRequirementMet = IsLinkedRequirementMet(equipment, slot);
                slot.ChunkRequirementMet = IsChunkRequirementMet(equipment.Ability, snapshot.ChunkCounts);
                slot.AbilityActive = slot.LinkedRequirementMet && slot.ChunkRequirementMet;

                if (!slot.AbilityActive)
                {
                    continue;
                }

                snapshot.ActiveAbilityDamageFlat += equipment.Ability.AddDamageFlat;
                snapshot.ActiveAbilityGuardFlat += equipment.Ability.AddGuardFlat;
                snapshot.ActiveAbilityReactionModifier += equipment.Ability.ModifyReaction;
            }

            return snapshot;
        }

        public static int GetVigorDamageBonus(CharacterSheetModel character)
        {
            return character.Vigor / 6;
        }

        public static int GetSpiritGuardBonus(CharacterSheetModel character)
        {
            return (character.Spirit / 3) + BuildActivationSnapshot(character).ActiveAbilityGuardFlat;
        }

        public static int GetClaritySaveModifier(CharacterSheetModel character)
        {
            return character.Clarity / 8;
        }

        public static int GetReactionModifier(CharacterSheetModel character)
        {
            return BuildActivationSnapshot(character).ActiveAbilityReactionModifier;
        }

        public static int GetActiveAbilityDamageBonus(CharacterSheetModel character)
        {
            return BuildActivationSnapshot(character).ActiveAbilityDamageFlat;
        }

        public static int GetEffectiveArmorTotal(CharacterSheetModel character)
        {
            var bestByLocation = new Dictionary<PortableEquipmentLocation, int>();
            foreach (var slot in character.Inventory)
            {
                var equipment = slot.Equipment;
                if (equipment == null || !equipment.IsArmor || !equipment.ContributesToEquippedBonuses)
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
                .Where(slot => slot.Equipment != null && slot.Equipment.IsWeapon && slot.Equipment.ContributesToEquippedBonuses)
                .Select(slot => slot.Equipment);
        }

        private static bool TryResolveLinkedColor(
            PortableChunkColor first,
            PortableChunkColor second,
            out PortableChunkColor linkedColor)
        {
            linkedColor = PortableChunkColor.None;
            if (first == PortableChunkColor.None || second == PortableChunkColor.None)
            {
                return false;
            }

            if (first == second)
            {
                linkedColor = first;
                return true;
            }

            if (first == PortableChunkColor.Rainbow)
            {
                linkedColor = second;
                return true;
            }

            if (second == PortableChunkColor.Rainbow)
            {
                linkedColor = first;
                return true;
            }

            return false;
        }

        private static bool IsLinkedRequirementMet(PortableEquipmentModel equipment, PortableSlotActivationModel slot)
        {
            if (equipment?.Ability == null)
            {
                return false;
            }

            if (!equipment.Ability.RequiresLinkedChunk)
            {
                return true;
            }

            if (equipment.TopHalf != PortableChunkColor.None && !slot.TopLinked)
            {
                return false;
            }

            if (equipment.BottomHalf != PortableChunkColor.None && !slot.BottomLinked)
            {
                return false;
            }

            if (equipment.LeftHalf != PortableChunkColor.None && !slot.LeftLinked)
            {
                return false;
            }

            if (equipment.RightHalf != PortableChunkColor.None && !slot.RightLinked)
            {
                return false;
            }

            return true;
        }

        private static bool IsChunkRequirementMet(
            PortableAbilityModel ability,
            IReadOnlyDictionary<PortableChunkColor, int> chunkCounts)
        {
            if (ability == null)
            {
                return false;
            }

            if (ability.RequiredChunkCount <= 0)
            {
                return true;
            }

            if (ability.RequiredColor == PortableChunkColor.None)
            {
                return chunkCounts.Values.Sum() >= ability.RequiredChunkCount;
            }

            return chunkCounts.TryGetValue(ability.RequiredColor, out var count) && count >= ability.RequiredChunkCount;
        }
    }
}
