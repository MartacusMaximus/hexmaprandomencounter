using System;
using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;

public static class CharacterRulesAdapter
{
    public static CharacterSheetModel ToModel(CharacterData character)
    {
        var model = new CharacterSheetModel
        {
            Id = Guid.Empty,
            Name = character.characterName,
            Vigor = character.vigor,
            Clarity = character.clarity,
            Spirit = character.spirit,
            FlawCount = character.flawCount,
            HasCoreAbility = character.hasCoreAbility,
            DeedCount = character.deedCount,
            CachedPointsLeft = character.cachedPointsLeft,
            Skills = character.skills.Select(ToModel).ToList()
        };

        character.EnsureInventorySlots();
        for (var index = 0; index < character.inventory.Count; index++)
        {
            model.Inventory.Add(new EquipmentSlotModel
            {
                SlotIndex = index,
                Equipment = ToModel(character.inventory[index]?.equipment)
            });
        }

        return model;
    }

    private static CharacterSkillModel ToModel(SkillEntry skill)
    {
        return new CharacterSkillModel
        {
            Name = skill.skillName,
            Value = skill.value
        };
    }

    private static PortableEquipmentModel ToModel(EquipmentData equipment)
    {
        if (equipment == null)
        {
            return null;
        }

        var armorLocation = PortableEquipmentLocation.None;
        var armorTrait = equipment.traits.OfType<ArmorTrait>().FirstOrDefault();
        if (armorTrait != null)
        {
            armorLocation = MapLocation(armorTrait.location);
        }

        return new PortableEquipmentModel
        {
            Id = Guid.Empty,
            Name = equipment.itemName,
            PointCost = equipment.pointCost,
            Rarity = equipment.rarity,
            DisplayCategory = equipment.displayCategory,
            RulesText = equipment.rulesText,
            DamageDiceNotation = equipment.damageDiceNotation,
            ArmorValue = equipment.armorValue,
            CostsCreationPoints = equipment.costsCreationPoints,
            IsWeapon = equipment.IsWeapon,
            IsArmor = equipment.IsArmor,
            RequiredHands = equipment.RequiredHands(),
            ArmorLocation = armorLocation,
            TraitNames = new List<string>(equipment.traits.Select(trait => trait.traitName)),
            SourceTags = new List<string>(equipment.sourceTags ?? new List<string>()),
            Ability = ToModel(equipment.ability),
            LeftHalf = MapChunk(equipment.leftHalf),
            RightHalf = MapChunk(equipment.rightHalf),
            TopHalf = MapChunk(equipment.topHalf),
            BottomHalf = MapChunk(equipment.bottomHalf),
            CenterChunk = MapChunk(equipment.centerChunk)
        };
    }

    private static PortableAbilityModel ToModel(AbilitySO ability)
    {
        if (ability == null)
        {
            return null;
        }

        return new PortableAbilityModel
        {
            Name = ability.abilityName,
            Description = ability.description,
            RequiredChunkCount = ability.requiredChunkCount,
            RequiredColor = MapChunk(ability.requiredColor),
            RequiresLinkedChunk = ability.requiresLinkedChunk,
            AddDamageFlat = ability.addDamageFlat,
            AddGuardFlat = ability.addGuardFlat,
            ModifyReaction = ability.modifyReaction
        };
    }

    private static PortableChunkColor MapChunk(ChunkColor color)
    {
        switch (color)
        {
            case ChunkColor.Red:
                return PortableChunkColor.Red;
            case ChunkColor.Green:
                return PortableChunkColor.Green;
            case ChunkColor.Blue:
                return PortableChunkColor.Blue;
            case ChunkColor.Rainbow:
                return PortableChunkColor.Rainbow;
            default:
                return PortableChunkColor.None;
        }
    }

    private static PortableEquipmentLocation MapLocation(EquipLocation location)
    {
        switch (location)
        {
            case EquipLocation.Head:
                return PortableEquipmentLocation.Head;
            case EquipLocation.Torso:
                return PortableEquipmentLocation.Torso;
            case EquipLocation.Legs:
                return PortableEquipmentLocation.Legs;
            case EquipLocation.Hands:
                return PortableEquipmentLocation.Hands;
            case EquipLocation.Waist:
                return PortableEquipmentLocation.Waist;
            case EquipLocation.Shield:
                return PortableEquipmentLocation.Shield;
            default:
                return PortableEquipmentLocation.None;
        }
    }
}
