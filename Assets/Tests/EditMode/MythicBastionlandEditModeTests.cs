using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
    public void ImportSeparatesSilkKnightTableHooksAndLinkedSeer()
    {
        InvokeStatic("MythicBastionlandImporter", "ImportPdfContent");

        var knight = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/Knights/Silk_Knight.asset");
        Assert.That(knight, Is.Not.Null);
        var linkedSeer = GetField(knight, "linkedSeer");
        Assert.That(linkedSeer, Is.Not.Null);
        Assert.That(GetField(linkedSeer, "seerName"), Is.EqualTo("The Crimson Seer"));
        var bondedProperty = ((IEnumerable)GetField(knight, "bondedProperty")).Cast<object>().ToList();
        Assert.That(bondedProperty.Select(item => (string)GetField(item, "itemName")), Is.EqualTo(new[]
        {
            "Delicate halberd",
            "woven coat armour",
            "Intricate brass puzzle"
        }));
        Assert.That(bondedProperty.Any(item =>
        {
            var name = (string)GetField(item, "itemName");
            return name.Contains("Crimson Seer", StringComparison.OrdinalIgnoreCase)
                || name.Contains("Scabby merchant", StringComparison.OrdinalIgnoreCase);
        }), Is.False);
        var randomFlavorTable = GetField(knight, "randomFlavorTable");
        Assert.That(GetField(randomFlavorTable, "title"), Is.EqualTo("A YOUNG STEED"));
        var columns = ((IEnumerable)GetField(randomFlavorTable, "columns")).Cast<object>().ToList();
        Assert.That(columns.Select(column => (string)GetField(column, "header")), Is.EqualTo(new[]
        {
            "Distracted by...",
            "Scared by..."
        }));
        Assert.That(((IEnumerable)GetField(columns[0], "values")).Cast<string>(), Is.EqualTo(new[]
        {
            "Smaller animals",
            "Moss",
            "Water",
            "Salt",
            "Fruit",
            "Shiny things"
        }));
        Assert.That(((IEnumerable)GetField(columns[1], "values")).Cast<string>(), Is.EqualTo(new[]
        {
            "Other steeds",
            "Fire",
            "Darkness",
            "Children",
            "Music",
            "Being alone"
        }));
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("personHook"), Is.Null);
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("nameHook"), Is.Null);
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("characteristicHook"), Is.Null);
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("objectHook"), Is.Null);
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("beastHook"), Is.Null);
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("stateHook"), Is.Null);
        Assert.That(RuntimeType("KnightDefinitionSO").GetField("themeHook"), Is.Null);

        var library = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/MythicBastionlandContentLibrary.asset");
        Assert.That(GetEntries(GetField(library, "people")), Contains.Item("Scabby merchant"));
        Assert.That(GetEntries(GetField(library, "names")), Contains.Item("Floria"));
        Assert.That(GetEntries(GetField(library, "characteristics")), Contains.Item("Destructive klutz"));
        Assert.That(GetEntries(GetField(library, "objects")), Contains.Item("Petty candle"));
        Assert.That(GetEntries(GetField(library, "beasts")), Contains.Item("Ghost carp"));
        Assert.That(GetEntries(GetField(library, "states")), Contains.Item("Enfeebled"));
        Assert.That(GetEntries(GetField(library, "themes")), Contains.Item("Armour"));
        Assert.That(GetEntries(GetField(library, "characteristics")), Contains.Item("Head in the clouds"));
    }

    [Test]
    public void ImportSeparatesChangelingVictimTableAndRealmEntries()
    {
        InvokeStatic("MythicBastionlandImporter", "ImportPdfContent");

        var myth = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/Myths/Changeling.asset");
        Assert.That(myth, Is.Not.Null);
        Assert.That(GetField(myth, "verse"), Is.EqualTo("When eyes can lie and words beguile\nSo falsehoods gain a truthness vile"));
        var castEntries = ((IEnumerable)GetField(myth, "castEntries")).Cast<object>().ToList();
        Assert.That(castEntries.Select(entry => (string)GetField(entry, "name")), Is.EqualTo(new[]
        {
            "The Changeling, in its True Form",
            "Elderly Rider, Beltor",
            "Horned Wolves, Malicorn"
        }));
        var flavorTable = GetField(myth, "flavorTable");
        Assert.That(GetField(flavorTable, "title"), Is.EqualTo("CHOSEN VICTIM"));
        var columns = ((IEnumerable)GetField(flavorTable, "columns")).Cast<object>().ToList();
        Assert.That(columns.Select(column => (string)GetField(column, "header")), Is.EqualTo(new[]
        {
            "Victim",
            "Clue"
        }));
        Assert.That(((IEnumerable)GetField(columns[0], "values")).Cast<string>(), Is.EqualTo(new[]
        {
            "Ruler of the Realm",
            "Known Knight",
            "Known Seer",
            "Known Vassal",
            "The last person the Company spoke to",
            "The next person the Company meets"
        }));
        Assert.That(((IEnumerable)GetField(columns[1], "values")).Cast<string>(), Is.EqualTo(new[]
        {
            "They cannot eat",
            "They cannot drink",
            "Animals hate them",
            "Children are scared",
            "They do not have any of their memories",
            "Sunlight causes great discomfort"
        }));
        Assert.That(RuntimeType("MythSO").GetField("dwelling"), Is.Null);
        Assert.That(RuntimeType("MythSO").GetField("sanctum"), Is.Null);
        Assert.That(RuntimeType("MythSO").GetField("monument"), Is.Null);
        Assert.That(RuntimeType("MythSO").GetField("hazard"), Is.Null);
        Assert.That(RuntimeType("MythSO").GetField("curse"), Is.Null);
        Assert.That(RuntimeType("MythSO").GetField("ruin"), Is.Null);

        var library = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/MythicBastionlandContentLibrary.asset");
        Assert.That(GetEntries(GetField(library, "dwellings")), Contains.Item("Bright windmill"));
        Assert.That(GetEntries(GetField(library, "sanctums")), Contains.Item("Silent sands"));
        Assert.That(GetEntries(GetField(library, "monuments")), Contains.Item("Crowned oak"));
        Assert.That(GetEntries(GetField(library, "hazards")), Contains.Item("Scalding heat"));
        Assert.That(GetEntries(GetField(library, "curses")), Contains.Item("Mocking clouds"));
        Assert.That(GetEntries(GetField(library, "ruins")), Contains.Item("Ghostly village"));
        Assert.That(GetEntries(GetField(library, "monuments")), Contains.Item("Roots of the world"));
    }

    [Test]
    public void ImportPrunesDeprecatedLongFormEquipmentAssets()
    {
        InvokeStatic("MythicBastionlandImporter", "ImportPdfContent");

        foreach (var guid in AssetDatabase.FindAssets("t:EquipmentData", new[] { "Assets/Resources/MythicBastionland/Equipment" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            Assert.That(CountWords((string)GetField(asset, "itemName")), Is.LessThanOrEqualTo(3), path);
            Assert.That(((string)GetField(asset, "itemName")).TrimStart(), Does.Not.StartWith("and "), path);
        }
    }

    [Test]
    public void ImportLinksSeeBelowEquipmentToItsKnightPageTable()
    {
        InvokeStatic("MythicBastionlandImporter", "ImportPdfContent");

        var trident = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/Equipment/Elaborate_trident.asset");
        Assert.That(trident, Is.Not.Null);
        Assert.That(GetField(trident, "rulesText"), Is.EqualTo("(d10 long, see below)"));
        var table = GetField(trident, "seeBelowTable");
        Assert.That((bool)GetProperty(trident, "HasSeeBelowTable"), Is.True);
        Assert.That(GetField(table, "title"), Is.EqualTo("AN ELABORATE TRIDENT"));
        var columns = ((IEnumerable)GetField(table, "columns")).Cast<object>().ToList();
        Assert.That(columns.Select(column => (string)GetField(column, "header")), Is.EqualTo(new[]
        {
            "Appearance",
            "Ability"
        }));
        Assert.That(((IEnumerable)GetField(columns[0], "values")).Cast<string>(), Is.EqualTo(new[]
        {
            "Silver rings",
            "Blackened iron",
            "Two-headed",
            "Faint golden glow",
            "Five prongs",
            "Telescopic shaft"
        }));
        var abilities = ((IEnumerable)GetField(columns[1], "values")).Cast<string>().ToList();
        Assert.That(abilities.Count, Is.EqualTo(6));
        Assert.That(abilities[0], Is.EqualTo("Can be thrown (d6)"));
        Assert.That(abilities[3], Is.EqualTo("+d8 vs flying beings"));
        Assert.That(abilities[5], Is.EqualTo("Utterly unbreakable"));
    }

    [Test]
    public void ImportCanonicalizesCompoundKnightPropertyItemsIntoReusableSupportAssets()
    {
        InvokeStatic("MythicBastionlandImporter", "ImportPdfContent");

        var muleKnight = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/Knights/Mule_Knight.asset");
        var muleProperty = ((IEnumerable)GetField(muleKnight, "bondedProperty")).Cast<object>().ToList();
        Assert.That(muleProperty.Select(item => (string)GetField(item, "itemName")), Is.EqualTo(new[]
        {
            "Weighted longstaff",
            "chainmail",
            "3 explosives"
        }));

        var saddleKnight = AssetDatabase.LoadMainAssetAtPath("Assets/Resources/MythicBastionland/Knights/Saddle_Knight.asset");
        var saddleProperty = ((IEnumerable)GetField(saddleKnight, "bondedProperty")).Cast<object>().ToList();
        Assert.That(saddleProperty.Select(item => (string)GetField(item, "itemName")), Is.EqualTo(new[]
        {
            "saddle and tack",
            "3 Rider's Axes",
            "mail",
            "rider's plate"
        }));
        Assert.That(saddleProperty.Any(item => ((string)GetField(item, "itemName")).Contains("be thrown)", StringComparison.OrdinalIgnoreCase)), Is.False);
        Assert.That(saddleProperty.Any(item => ((string)GetField(item, "itemName")).Contains("just layers", StringComparison.OrdinalIgnoreCase)), Is.False);
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
    public void KnightingResolvesSeeBelowPropertyTextPerInstance()
    {
        var character = CreateScriptableObject("CharacterData");
        SetField(character, "deedCount", 5);
        Invoke(character, "EnsureInventorySlots");

        var knight = CreateScriptableObject("KnightDefinitionSO");
        SetField(knight, "knightName", "Table Knight");

        var property = CreateScriptableObject("EquipmentData");
        SetField(property, "itemName", "Test Trident");
        SetField(property, "rulesText", "(d10 long, see below)");
        SetField(property, "damageDiceNotation", "1d10");
        SetField(property, "seeBelowTable", CreateRollTable(
            ("Appearance", "Blackened iron"),
            ("Ability", "+d8 vs flying beings")));
        ((IList)GetField(knight, "bondedProperty")).Add(property);

        var assigned = (bool)InvokeStatic("KnightingService", "AssignKnight", character, knight, null);

        Assert.That(assigned, Is.True);
        var item = ((IEnumerable)GetField(character, "inventory")).Cast<object>().FirstOrDefault(entry => entry != null);
        Assert.That(item, Is.Not.Null);
        Assert.That(GetField(item, "equipment"), Is.EqualTo(property));
        Assert.That(GetField(item, "resolvedRulesText"), Is.EqualTo("(d10 long, Appearance: Blackened iron, Ability: +d8 vs flying beings)"));
        Assert.That(GetField(item, "resolvedSeeBelowRowIndex"), Is.EqualTo(0));
        Assert.That(GetProperty(item, "RulesText"), Is.EqualTo(GetField(item, "resolvedRulesText")));
        Assert.That(GetField(property, "rulesText"), Is.EqualTo("(d10 long, see below)"));

        var model = ToCharacterSheetModel(character);
        Assert.That(model.Inventory[0].Equipment.RulesText, Is.EqualTo(GetField(item, "resolvedRulesText")));
    }

    [Test]
    public void SeeBelowResolutionExtractsStructuredOverridesAndGeneratedTags()
    {
        var equipment = CreateScriptableObject("EquipmentData");
        SetField(equipment, "itemName", "Nameless Arsenal");
        SetField(equipment, "rulesText", "see below");
        SetField(equipment, "seeBelowTable", CreateRollTable(
            ("Form", "Black iron blade (d10 long)"),
            ("Ability", "Can be thrown (d6), +d8 vs flying beings")));

        var instance = CreateEquipmentInstance(equipment);
        SetField(instance, "resolvedRulesText", string.Empty);
        Invoke(instance, "ResolveSeeBelow", new System.Random(0));

        Assert.That(GetProperty(instance, "RulesText"), Is.EqualTo("Form: Black iron blade (d10 long), Ability: Can be thrown (d6), +d8 vs flying beings"));
        Assert.That(GetProperty(instance, "DamageDiceNotation"), Is.EqualTo("1d10"));
        Assert.That(GetProperty(instance, "RequiredHands"), Is.EqualTo(2));
        Assert.That(((IEnumerable)GetField(instance, "resolvedTraitNames")).Cast<string>(), Contains.Item("Long"));
        Assert.That(((IEnumerable)GetField(instance, "resolvedGeneratedTags")).Cast<string>(), Contains.Item("thrown_damage:1d6"));
        Assert.That(((IEnumerable)GetField(instance, "resolvedGeneratedTags")).Cast<string>(), Contains.Item("conditional_damage:+d8 vs flying beings"));
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

    private static object GetProperty(object target, string propertyName)
    {
        return target.GetType().GetProperty(propertyName).GetValue(target);
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

    private static CharacterSheetModel ToCharacterSheetModel(object character)
    {
        var adapter = RuntimeType("CharacterRulesAdapter");
        return (CharacterSheetModel)adapter.GetMethod("ToModel").Invoke(null, new[] { character });
    }

    private static object CreateRollTable(params (string header, string value)[] columns)
    {
        var table = Activator.CreateInstance(RuntimeType("MythicRollTable"));
        SetField(table, "title", "TEST TABLE");
        var tableColumns = (IList)GetField(table, "columns");

        foreach (var columnData in columns)
        {
            var column = Activator.CreateInstance(RuntimeType("MythicTableColumn"));
            SetField(column, "header", columnData.header);
            SetField(column, "values", new List<string> { columnData.value });
            tableColumns.Add(column);
        }

        return table;
    }

    private static int CountWords(string text)
    {
        return Regex.Matches(text ?? string.Empty, @"[A-Za-z0-9]+(?:'[A-Za-z0-9]+)?").Count;
    }

    private static List<string> GetEntries(object target)
    {
        return ((IEnumerable)GetField(target, "entries")).Cast<string>().ToList();
    }
}
