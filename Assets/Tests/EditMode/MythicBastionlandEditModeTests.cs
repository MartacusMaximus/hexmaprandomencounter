using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class MythicBastionlandEditModeTests
{
    [Test]
    public void ImportedMythicBastionlandContentHasExpectedKnightAndMythCounts()
    {
        InvokeStatic("MythicBastionlandImporter", "ImportPdfContent");

        var library = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/MythicBastionlandContentLibrary.asset");
        Assert.That(library, Is.Not.Null);

        var knights = ((IEnumerable)GetField(library, "knights")).Cast<object>().ToList();
        var myths = ((IEnumerable)GetField(library, "myths")).Cast<object>().ToList();
        Assert.That(knights.Count, Is.EqualTo(72));
        Assert.That(myths.Count, Is.EqualTo(72));
        Assert.That(knights.All(knight => GetField(knight, "grantedAbility") != null && GetField(knight, "steed") != null), Is.True);
        Assert.That(myths.All(myth => ((IList)GetField(myth, "omens")).Count == 6), Is.True);
    }

    [Test]
    public void KnightingUnlockAndAssignmentPersistGrantedAbilityAndSteed()
    {
        var character = CreateScriptableObject("CharacterData");
        SetField(character, "deedCount", 5);
        Invoke(character, "EnsureInventorySlots");

        var knight = CreateScriptableObject("KnightDefinitionSO");
        SetField(knight, "knightName", "Test Knight");

        var ability = CreateScriptableObject("AbilitySO");
        SetField(ability, "abilityName", "Free Technique");
        SetField(knight, "grantedAbility", ability);

        var steed = CreateScriptableObject("SteedDefinitionSO");
        SetField(steed, "steedName", "Reliable Horse");
        SetField(steed, "vigor", 10);
        SetField(steed, "clarity", 8);
        SetField(steed, "spirit", 5);
        SetField(steed, "guard", 3);
        SetField(knight, "steed", steed);

        var isReady = (bool)InvokeStatic("KnightingService", "IsReadyForKnighting", character);
        var assigned = (bool)InvokeStatic("KnightingService", "AssignKnight", character, knight, null);

        Assert.That(isReady, Is.True);
        Assert.That(assigned, Is.True);
        Assert.That(GetField(character, "assignedKnight"), Is.EqualTo(knight));
        Assert.That(GetField(character, "grantedKnightAbility"), Is.EqualTo(ability));
        Assert.That(((IEnumerable)GetField(character, "knownAbilities")).Cast<object>(), Contains.Item(ability));
        var steedInstance = GetField(character, "steed");
        Assert.That(GetField(steedInstance, "definition"), Is.EqualTo(steed));
        Assert.That(GetField(character, "knightingStatus").ToString(), Is.EqualTo("Knighted"));
    }

    [Test]
    public void BondedItemsRejectNonOwnerAndRemediesRejectBaseInventory()
    {
        var owner = CreateScriptableObject("CharacterData");
        Invoke(owner, "EnsureInventorySlots");
        var stranger = CreateScriptableObject("CharacterData");
        Invoke(stranger, "EnsureInventorySlots");

        var bondedEquipment = CreateScriptableObject("EquipmentData");
        SetField(bondedEquipment, "itemName", "Bonded Relic");
        SetField(bondedEquipment, "isBondedProperty", true);
        SetField(bondedEquipment, "usableByNonOwner", false);

        var remedy = CreateScriptableObject("EquipmentData");
        SetField(remedy, "itemName", "Great Remedy");
        SetField(remedy, "displayCategory", "remedy");
        SetField(remedy, "storageRule", RuntimeEnum("EquipmentStorageRule", "ContainerOnly"));
        SetField(remedy, "occupiesFullContainer", true);

        var bondedItem = CreateEquipmentInstance(bondedEquipment);
        SetField(bondedItem, "ownerCharacterId", (string)GetField(owner, "characterId"));
        SetField(bondedItem, "bondedToOwner", true);
        Invoke(bondedItem, "EnsureInstance");

        var remedyItem = CreateEquipmentInstance(remedy);
        Invoke(remedyItem, "EnsureInstance");

        Assert.That((bool)InvokeStatic("InventoryOwnershipRules", "CanCharacterEquip", owner, bondedItem), Is.True);
        Assert.That((bool)InvokeStatic("InventoryOwnershipRules", "CanCharacterEquip", stranger, bondedItem), Is.False);
        Assert.That((bool)InvokeStatic("InventoryOwnershipRules", "CanCharacterEquip", owner, remedyItem), Is.False);
    }

    [Test]
    public void BackpackContentsDoNotContributeToEquippedBonuses()
    {
        var character = new CharacterSheetModel
        {
            Vigor = 12,
            Spirit = 9,
            Inventory = Enumerable.Range(0, 9)
                .Select(index => new EquipmentSlotModel { SlotIndex = index })
                .ToList()
        };

        character.Inventory[0].Equipment = new PortableEquipmentModel
        {
            Name = "Visible Sword",
            IsWeapon = true,
            ContributesToEquippedBonuses = true,
            CenterChunk = PortableChunkColor.Red
        };
        character.Inventory[1].Equipment = new PortableEquipmentModel
        {
            Name = "Backpacked Shield",
            IsArmor = true,
            ArmorValue = 4,
            ArmorLocation = PortableEquipmentLocation.Shield,
            ContributesToEquippedBonuses = false,
            CenterChunk = PortableChunkColor.Blue
        };

        var chunks = InventoryStatCalculator.CountChunks(character);
        var armor = InventoryStatCalculator.GetEffectiveArmorTotal(character);
        var weapons = InventoryStatCalculator.GetActiveWeapons(character).ToList();

        Assert.That(chunks[PortableChunkColor.Red], Is.EqualTo(1));
        Assert.That(chunks[PortableChunkColor.Blue], Is.EqualTo(0));
        Assert.That(armor, Is.EqualTo(0));
        Assert.That(weapons.Count, Is.EqualTo(1));
    }

    [Test]
    public void PhaseTravelRulesResolveGallopAndWinterNightPenalties()
    {
        var result = PhaseTravelRules.Resolve(new TravelPhaseContext
        {
            From = new HexCoordinate(0, 0),
            To = new HexCoordinate(2, -1),
            AvailableEffort = 3,
            PathLength = 2,
            Method = TravelMethod.Gallop,
            HasSteed = true,
            IsNight = true,
            CampingOutdoors = true,
            IsWinter = true,
            GallopLossRoll = 4,
            NightSpiritLossRoll = 2,
            WinterVigorLossRoll = 3,
            SleepClarityLossRoll = 5
        });

        Assert.That(result.Success, Is.True);
        Assert.That(result.RemainingEffort, Is.EqualTo(2));
        Assert.That(result.SteedVigorLoss, Is.EqualTo(4));
        Assert.That(result.NightSpiritLoss, Is.EqualTo(2));
        Assert.That(result.WinterVigorLoss, Is.EqualTo(3));
        Assert.That(result.SleepClarityLoss, Is.EqualTo(5));
    }

    [Test]
    public void PhaseTravelRulesRejectBarrierAndTriggerMythHexOmen()
    {
        var blocked = PhaseTravelRules.Resolve(new TravelPhaseContext
        {
            From = new HexCoordinate(0, 0),
            To = new HexCoordinate(1, 0),
            AvailableEffort = 2,
            PathLength = 1,
            Method = TravelMethod.Trek,
            BarrierBlocksRoute = true
        });

        Assert.That(blocked.Success, Is.False);

        var mythHex = PhaseTravelRules.Resolve(new TravelPhaseContext
        {
            From = new HexCoordinate(0, 0),
            To = new HexCoordinate(1, 0),
            AvailableEffort = 2,
            PathLength = 1,
            Method = TravelMethod.Trek,
            EndsInMythHex = true
        });

        Assert.That(mythHex.Success, Is.True);
        Assert.That(mythHex.TriggerNearestMythOmen, Is.True);
    }

    private static ScriptableObject CreateScriptableObject(string typeName)
    {
        return ScriptableObject.CreateInstance(RuntimeType(typeName));
    }

    private static Type RuntimeType(string typeName)
    {
        return Type.GetType($"{typeName}, Assembly-CSharp", false)
            ?? Type.GetType($"{typeName}, Assembly-CSharp-Editor", true);
    }

    private static object RuntimeEnum(string enumTypeName, string valueName)
    {
        return Enum.Parse(RuntimeType(enumTypeName), valueName);
    }

    private static object GetField(object target, string fieldName)
    {
        return target.GetType().GetField(fieldName).GetValue(target);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName).SetValue(target, value);
    }

    private static object Invoke(object target, string methodName, params object[] args)
    {
        return target.GetType().GetMethod(methodName).Invoke(target, args);
    }

    private static object InvokeStatic(string typeName, string methodName, params object[] args)
    {
        return RuntimeType(typeName).GetMethod(methodName).Invoke(null, args);
    }

    private static object CreateEquipmentInstance(object equipment)
    {
        var instance = Activator.CreateInstance(RuntimeType("EquipmentInstance"));
        SetField(instance, "equipment", equipment);
        return instance;
    }
}
