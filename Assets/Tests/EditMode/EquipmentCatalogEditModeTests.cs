using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class EquipmentCatalogEditModeTests
{
    [Test]
    public void CharacterRulesAdapterCarriesEquipmentMetadataIntoPortableModel()
    {
        var character = CreateScriptableObject("CharacterData");
        Invoke(character, "EnsureInventorySlots");

        var equipment = CreateScriptableObject("EquipmentData");
        SetField(equipment, "itemName", "Test Shield");
        SetField(equipment, "pointCost", 3);
        SetField(equipment, "rarity", "rare");
        SetField(equipment, "displayCategory", "armor");
        SetField(equipment, "rulesText", "Repels terrible things.");
        SetField(equipment, "damageDiceNotation", "2d8");
        SetField(equipment, "armorValue", 2);
        SetField(equipment, "leftHalf", RuntimeEnum("ChunkColor", "Blue"));
        SetField(equipment, "sourceTags", new List<string> { "CrownAndSkull", "Crown" });

        var armorTrait = CreateScriptableObject("ArmorTrait");
        SetField(armorTrait, "traitName", "Shield Armor");
        SetField(armorTrait, "location", RuntimeEnum("EquipLocation", "Shield"));
        AddToListField(equipment, "traits", armorTrait);

        var weaponTrait = CreateScriptableObject("WeaponTrait");
        SetField(weaponTrait, "traitName", "Weapon");
        AddToListField(equipment, "traits", weaponTrait);

        var ability = CreateScriptableObject("AbilitySO");
        SetField(ability, "abilityName", "Sun Wall");
        SetField(ability, "description", "Hold the line with light.");
        SetField(ability, "requiredChunkCount", 2);
        SetField(ability, "requiredColor", RuntimeEnum("ChunkColor", "Blue"));
        SetField(ability, "requiresLinkedChunk", true);
        SetField(ability, "addGuardFlat", 1);
        SetField(equipment, "ability", ability);

        var inventory = (IList)GetField(character, "inventory");
        inventory[0] = CreateEquipmentInstance(equipment);

        var model = ToCharacterSheetModel(character);
        var mapped = model.Inventory[0].Equipment;

        Assert.That(mapped.Name, Is.EqualTo("Test Shield"));
        Assert.That(mapped.Rarity, Is.EqualTo("rare"));
        Assert.That(mapped.DisplayCategory, Is.EqualTo("armor"));
        Assert.That(mapped.RulesText, Is.EqualTo("Repels terrible things."));
        Assert.That(mapped.IsArmor, Is.True);
        Assert.That(mapped.IsWeapon, Is.True);
        Assert.That(mapped.ArmorLocation, Is.EqualTo(PortableEquipmentLocation.Shield));
        Assert.That(mapped.SourceTags, Is.EquivalentTo(new[] { "CrownAndSkull", "Crown" }));
        Assert.That(mapped.Ability, Is.Not.Null);
        Assert.That(mapped.Ability.Name, Is.EqualTo("Sun Wall"));
        Assert.That(mapped.Ability.RequiredColor, Is.EqualTo(PortableChunkColor.Blue));
        Assert.That(mapped.LeftHalf, Is.EqualTo(PortableChunkColor.Blue));
    }

    [TestCase("d4", 1, 4, 4)]
    [TestCase("d6", 1, 6, 6)]
    [TestCase("d8", 1, 8, 8)]
    [TestCase("d10", 1, 10, 10)]
    [TestCase("d12", 1, 12, 12)]
    [TestCase("2d8", 2, 8, 16)]
    [TestCase("2d10", 2, 10, 20)]
    public void RollWeaponDamageUsesParsedDiceSides(string notation, int expectedCount, int expectedSides, int expectedTotal)
    {
        var equipment = CreateScriptableObject("EquipmentData");
        SetField(equipment, "itemName", notation);
        SetField(equipment, "damageDiceNotation", notation.StartsWith("d", StringComparison.Ordinal) ? $"1{notation}" : notation);

        var lastCount = 0;
        var lastSides = 0;
        Func<int, int, int> rollDice = (count, sides) =>
        {
            lastCount = count;
            lastSides = sides;
            return count * sides;
        };

        var total = (int)RuntimeType("InventoryGrid")
            .GetMethods()
            .Single(method =>
                method.Name == "RollWeaponDamage" &&
                method.GetParameters().Length == 2 &&
                method.GetParameters()[1].ParameterType == typeof(Func<int, int, int>))
            .Invoke(null, new object[] { equipment, rollDice });

        Assert.That(lastCount, Is.EqualTo(expectedCount));
        Assert.That(lastSides, Is.EqualTo(expectedSides));
        Assert.That(total, Is.EqualTo(expectedTotal));
    }

    [Test]
    public void ImportedEquipmentCatalogIsCompleteAndStructured()
    {
        var assets = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Scripts/ScriptableObjects/ITEMS" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadMainAssetAtPath)
            .OfType<ScriptableObject>()
            .Where(asset => asset.GetType().Name == "EquipmentData")
            .ToList();

        Assert.That(assets.Count, Is.EqualTo(62));
        Assert.That(assets.All(asset => !string.IsNullOrWhiteSpace((string)GetField(asset, "itemName"))), Is.True);
        Assert.That(assets.All(asset => !string.IsNullOrWhiteSpace((string)GetField(asset, "rarity"))), Is.True);
        Assert.That(assets.All(asset => !string.IsNullOrWhiteSpace((string)GetField(asset, "displayCategory"))), Is.True);
        Assert.That(assets.All(asset => ((IList)GetField(asset, "sourceTags")).Count > 0), Is.True);
        Assert.That(assets.All(asset =>
            !string.IsNullOrWhiteSpace((string)GetField(asset, "damageDiceNotation")) ||
            (int)GetField(asset, "armorValue") > 0 ||
            GetField(asset, "ability") != null ||
            !string.IsNullOrWhiteSpace((string)GetField(asset, "rulesText"))), Is.True);
    }

    [Test]
    public void ImportedCrownSkillCatalogIsComplete()
    {
        var assets = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Scripts/ScriptableObjects/SKILLS" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadMainAssetAtPath)
            .OfType<ScriptableObject>()
            .Where(asset => asset.GetType().Name == "SkillDefinitionSO")
            .ToList();

        Assert.That(assets.Count, Is.EqualTo(12));
        Assert.That(assets.All(asset => !string.IsNullOrWhiteSpace((string)GetField(asset, "skillName"))), Is.True);
        Assert.That(assets.All(asset => !string.IsNullOrWhiteSpace((string)GetField(asset, "description"))), Is.True);
        Assert.That(assets.All(asset => ((IEnumerable<string>)GetField(asset, "sourceTags")).Contains("Crown")), Is.True);
    }

    [Test]
    public void ReworkPartyWorkbenchAssetsExistWithSampleCampaignData()
    {
        const string libraryPath = "Assets/Generated/ReworkParty/ReworkEquipmentLibrary.asset";
        const string campaignPath = "Assets/Generated/ReworkParty/ReworkCampaign.asset";

        var library = AssetDatabase.LoadMainAssetAtPath(libraryPath) as ScriptableObject;
        var campaign = AssetDatabase.LoadMainAssetAtPath(campaignPath) as ScriptableObject;

        Assert.That(library, Is.Not.Null, $"Expected generated equipment library at {libraryPath}");
        Assert.That(campaign, Is.Not.Null, $"Expected generated campaign at {campaignPath}");

        var libraryItems = (IEnumerable)GetField(library, "items");
        Assert.That(libraryItems.Cast<object>().Count(), Is.GreaterThanOrEqualTo(62));

        var allCharacters = ((IEnumerable)GetField(campaign, "allCharacters")).Cast<object>().ToList();
        Assert.That(allCharacters.Count, Is.EqualTo(4));
        Assert.That(allCharacters.Select(character => (string)GetField(character, "characterName")), Is.EquivalentTo(new[]
        {
            "Dung Beetle Knight",
            "Pigeon Knight",
            "Mountain Knight",
            "Rat Knight"
        }));

        var activeParty = GetField(campaign, "activeParty");
        var partyMembers = ((IEnumerable)GetField(activeParty, "members")).Cast<object>().ToList();
        Assert.That(partyMembers.Count, Is.EqualTo(2));
    }

    private static ScriptableObject CreateScriptableObject(string typeName)
    {
        return ScriptableObject.CreateInstance(RuntimeType(typeName));
    }

    private static Type RuntimeType(string typeName)
    {
        return Type.GetType($"{typeName}, Assembly-CSharp", true);
    }

    private static object RuntimeEnum(string enumTypeName, string valueName)
    {
        return Enum.Parse(RuntimeType(enumTypeName), valueName);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName).SetValue(target, value);
    }

    private static object GetField(object target, string fieldName)
    {
        return target.GetType().GetField(fieldName).GetValue(target);
    }

    private static void AddToListField(object target, string fieldName, object value)
    {
        ((IList)GetField(target, fieldName)).Add(value);
    }

    private static void Invoke(object target, string methodName)
    {
        target.GetType().GetMethod(methodName).Invoke(target, null);
    }

    private static object CreateEquipmentInstance(object equipment)
    {
        var instance = Activator.CreateInstance(RuntimeType("EquipmentInstance"));
        SetField(instance, "equipment", equipment);
        return instance;
    }

    private static CharacterSheetModel ToCharacterSheetModel(object character)
    {
        var adapter = RuntimeType("CharacterRulesAdapter");
        return (CharacterSheetModel)adapter.GetMethod("ToModel").Invoke(null, new[] { character });
    }
}
