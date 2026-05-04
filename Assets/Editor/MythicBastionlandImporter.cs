using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class MythicBastionlandImporter
{
    private const string PdfPath = "/Users/mart/Documents/mythic-bastionland_0.pdf";
    private const string RootFolder = "Assets/Resources/MythicBastionland";
    private const string CuratedItemsFolder = "Assets/Scripts/ScriptableObjects/ITEMS";
    private const string TraitFolder = "Assets/Scripts/ScriptableObjects/TRAITS/Traits";
    private const string EquipmentFolder = RootFolder + "/Equipment";
    private const string AbilityFolder = RootFolder + "/Abilities";
    private const string SeerFolder = RootFolder + "/Seers";
    private const string SteedFolder = RootFolder + "/Steeds";
    private const string KnightFolder = RootFolder + "/Knights";
    private const string MythFolder = RootFolder + "/Myths";
    private const string ListsFolder = RootFolder + "/Lists";
    private const string ReportPath = RootFolder + "/ImportValidationReport.txt";
    private const string ContentLibraryPath = RootFolder + "/MythicBastionlandContentLibrary.asset";
    private const string BackpackPath = EquipmentFolder + "/TravellerBackpack.asset";
    private const string PeopleListAssetName = "People";
    private const string NamesListAssetName = "Names";
    private const string CharacteristicsListAssetName = "Characteristics";
    private const string ObjectsListAssetName = "Objects";
    private const string BeastsListAssetName = "Beasts";
    private const string StatesListAssetName = "States";
    private const string ThemesListAssetName = "Themes";
    private const string DwellingsListAssetName = "Dwellings";
    private const string SanctumsListAssetName = "Sanctums";
    private const string MonumentsListAssetName = "Monuments";
    private const string HazardsListAssetName = "Hazards";
    private const string CursesListAssetName = "Curses";
    private const string RuinsListAssetName = "Ruins";

    [MenuItem("Tools/Mythic Bastionland/Import PDF Content")]
    public static void ImportPdfContent()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "MythicBastionland");
        EnsureFolder(RootFolder, "Equipment");
        EnsureFolder(RootFolder, "Abilities");
        EnsureFolder(RootFolder, "Seers");
        EnsureFolder(RootFolder, "Steeds");
        EnsureFolder(RootFolder, "Knights");
        EnsureFolder(RootFolder, "Myths");
        EnsureFolder(RootFolder, "Lists");

        var jsonPath = Path.Combine(Path.GetTempPath(), "mythic-bastionland-import.json");
        RunParser(PdfPath, jsonPath);

        var payload = JsonUtility.FromJson<ImportPayload>(File.ReadAllText(jsonPath));
        if (payload == null)
        {
            throw new InvalidOperationException("Mythic Bastionland import payload was empty.");
        }

        var contentLibrary = AssetDatabase.LoadAssetAtPath<MythicBastionlandContentLibrarySO>(ContentLibraryPath);
        if (contentLibrary == null)
        {
            contentLibrary = ScriptableObject.CreateInstance<MythicBastionlandContentLibrarySO>();
            AssetDatabase.CreateAsset(contentLibrary, ContentLibraryPath);
        }

        var seersByName = new Dictionary<string, SeerDefinitionSO>(StringComparer.OrdinalIgnoreCase);
        var equipmentByName = new Dictionary<string, EquipmentData>(StringComparer.OrdinalIgnoreCase);
        var abilitiesByName = new Dictionary<string, AbilitySO>(StringComparer.OrdinalIgnoreCase);
        var curatedEquipmentByName = LoadCuratedEquipmentByName();
        var importedKnights = new List<KnightDefinitionSO>();

        foreach (var knight in payload.knights ?? Array.Empty<KnightRecord>())
        {
            var knightFlavorTable = ToRollTable(knight.randomFlavorTable);
            var seer = GetOrCreateSeer(knight.linkedSeer, seersByName);
            var ability = GetOrCreateAbility(knight.abilityName, knight.abilityDescription, abilitiesByName);
            var steed = GetOrCreateSteed(knight.steed);
            var propertyItems = knight.propertyItems?
                .Select(item => GetOrCreateEquipment(item, equipmentByName, curatedEquipmentByName))
                .Where(item => item != null)
                .ToList() ?? new List<EquipmentData>();
            AssignSeeBelowTable(propertyItems, knightFlavorTable);

            var asset = LoadOrCreateAsset<KnightDefinitionSO>(KnightFolder, knight.knightName);
            asset.pageNumber = knight.pageNumber;
            asset.knightName = knight.knightName;
            asset.titleVerse = knight.titleVerse;
            asset.passionText = string.IsNullOrWhiteSpace(knight.passionTitle)
                ? knight.passionText
                : $"{knight.passionTitle}: {knight.passionText}";
            asset.grantedAbility = ability;
            asset.linkedSeer = seer;
            asset.steed = steed;
            asset.bondedProperty = propertyItems;
            asset.randomFlavorTable = knightFlavorTable;
            EditorUtility.SetDirty(asset);
            importedKnights.Add(asset);
        }

        foreach (var myth in payload.myths ?? Array.Empty<MythRecord>())
        {
            var asset = LoadOrCreateAsset<MythSO>(MythFolder, myth.mythName);
            asset.pageNumber = myth.pageNumber;
            asset.mythName = myth.mythName;
            asset.verse = myth.verse;
            asset.omens = myth.omens?.ToList() ?? new List<string>();
            asset.castEntries = myth.castEntries?.Select(entry => new MythCastEntry
            {
                name = entry.name,
                statBlock = entry.statBlock,
                notes = entry.notes
            }).ToList() ?? new List<MythCastEntry>();
            asset.flavorTable = ToRollTable(myth.flavorTable);
            asset.visibleByDefault = false;
            EditorUtility.SetDirty(asset);
        }

        GetOrCreateBackpack(equipmentByName);
        AuthorEquipmentCatalog(curatedEquipmentByName.Values.Concat(equipmentByName.Values), importedKnights);

        contentLibrary.seers = seersByName.Values.OrderBy(asset => asset.seerName).ToList();
        contentLibrary.abilities = abilitiesByName.Values.OrderBy(asset => asset.abilityName).ToList();
        contentLibrary.equipment = equipmentByName.Values.OrderBy(asset => asset.itemName).ToList();
        contentLibrary.knights = importedKnights
            .Where(asset => asset != null)
            .OrderBy(asset => asset.pageNumber)
            .ToList();
        contentLibrary.myths = (payload.myths ?? Array.Empty<MythRecord>())
            .Select(myth => AssetDatabase.LoadAssetAtPath<MythSO>($"{MythFolder}/{Sanitize(myth.mythName)}.asset"))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.pageNumber)
            .ToList();
        contentLibrary.people = GetOrCreateTextList<PersonSO>(ListsFolder, PeopleListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.personHook));
        contentLibrary.names = GetOrCreateTextList<NameSO>(ListsFolder, NamesListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.nameHook));
        contentLibrary.characteristics = GetOrCreateTextList<CharacteristicSO>(ListsFolder, CharacteristicsListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.characteristicHook));
        contentLibrary.objects = GetOrCreateTextList<ObjectSO>(ListsFolder, ObjectsListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.objectHook));
        contentLibrary.beasts = GetOrCreateTextList<BeastSO>(ListsFolder, BeastsListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.beastHook));
        contentLibrary.states = GetOrCreateTextList<StateSO>(ListsFolder, StatesListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.stateHook));
        contentLibrary.themes = GetOrCreateTextList<ThemeSO>(ListsFolder, ThemesListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.themeHook));
        contentLibrary.dwellings = GetOrCreateTextList<DwellingSO>(ListsFolder, DwellingsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.dwelling));
        contentLibrary.sanctums = GetOrCreateTextList<SanctumSO>(ListsFolder, SanctumsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.sanctum));
        contentLibrary.monuments = GetOrCreateTextList<MonumentSO>(ListsFolder, MonumentsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.monument));
        contentLibrary.hazards = GetOrCreateTextList<HazardSO>(ListsFolder, HazardsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.hazard));
        contentLibrary.curses = GetOrCreateTextList<CurseSO>(ListsFolder, CursesListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.curse));
        contentLibrary.ruins = GetOrCreateTextList<RuinSO>(ListsFolder, RuinsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.ruin));
        EditorUtility.SetDirty(contentLibrary);

        PruneDeprecatedEquipmentAssets(BuildEquipmentKeepPaths(equipmentByName.Values));
        File.WriteAllText(ReportPath, string.Join(Environment.NewLine, payload.issues ?? Array.Empty<string>()));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void RunParser(string pdfPath, string jsonPath)
    {
        var scriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets/Editor/MythicBastionlandPdfParser.py");
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "python3",
            Arguments = $"\"{scriptPath}\" \"{pdfPath}\" \"{jsonPath}\"",
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process == null)
        {
            throw new InvalidOperationException("Failed to start Mythic Bastionland parser process.");
        }

        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
        }
    }

    private static SeerDefinitionSO GetOrCreateSeer(SeerRecord record, IDictionary<string, SeerDefinitionSO> cache)
    {
        if (record == null)
        {
            return null;
        }

        if (cache.TryGetValue(record.seerName, out var existing))
        {
            return existing;
        }

        var asset = LoadOrCreateAsset<SeerDefinitionSO>(SeerFolder, record.seerName);
        asset.seerName = record.seerName;
        asset.vigor = record.vigor;
        asset.clarity = record.clarity;
        asset.spirit = record.spirit;
        asset.guard = record.guard;
        asset.traits = record.traits?.ToList() ?? new List<string>();
        EditorUtility.SetDirty(asset);
        cache[record.seerName] = asset;
        return asset;
    }

    private static AbilitySO GetOrCreateAbility(string name, string description, IDictionary<string, AbilitySO> cache)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        if (cache.TryGetValue(name, out var existing))
        {
            return existing;
        }

        var asset = LoadOrCreateAsset<AbilitySO>(AbilityFolder, name);
        asset.abilityName = name;
        asset.description = description;
        EditorUtility.SetDirty(asset);
        cache[name] = asset;
        return asset;
    }

    private static EquipmentData GetOrCreateEquipment(
        EquipmentRecord record,
        IDictionary<string, EquipmentData> cache,
        IReadOnlyDictionary<string, EquipmentData> curatedEquipmentByName)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.name))
        {
            return null;
        }

        var resolvedIdentity = ResolveEquipmentIdentity(record);
        if (CountWords(resolvedIdentity.itemName) > 3)
        {
            return null;
        }

        if (curatedEquipmentByName.TryGetValue(resolvedIdentity.key, out var curated))
        {
            cache[resolvedIdentity.key] = curated;
            return curated;
        }

        if (cache.TryGetValue(resolvedIdentity.key, out var existing))
        {
            return existing;
        }

        var asset = LoadOrCreateAsset<EquipmentData>(EquipmentFolder, resolvedIdentity.itemName);
        asset.itemName = resolvedIdentity.itemName;
        asset.rulesText = resolvedIdentity.rulesText;
        asset.rarity = string.IsNullOrWhiteSpace(record.rarity) ? "bonded" : record.rarity;
        asset.displayCategory = string.IsNullOrWhiteSpace(record.displayCategory) ? "tool" : record.displayCategory;
        asset.damageDiceNotation = record.damageDiceNotation;
        asset.armorValue = record.armorValue;
        asset.costsCreationPoints = false;
        asset.isBondedProperty = record.isBondedProperty;
        asset.usableByNonOwner = !record.isBondedProperty;
        asset.seeBelowTable = new MythicRollTable();
        asset.sourceTags = record.sourceTags?.ToList() ?? new List<string> { "MythicBastionland" };
        if (asset.displayCategory == "remedy")
        {
            asset.storageRule = EquipmentStorageRule.ContainerOnly;
            asset.occupiesFullContainer = true;
            asset.contributesToEquippedBonuses = false;
        }

        EditorUtility.SetDirty(asset);
        cache[resolvedIdentity.key] = asset;
        return asset;
    }

    private static void AssignSeeBelowTable(IEnumerable<EquipmentData> propertyItems, MythicRollTable table)
    {
        if (!MythicEquipmentTableResolver.HasTable(table))
        {
            return;
        }

        foreach (var item in propertyItems ?? Enumerable.Empty<EquipmentData>())
        {
            if (item == null || string.IsNullOrWhiteSpace(item.rulesText))
            {
                continue;
            }

            if (!item.rulesText.Contains("see below", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            item.seeBelowTable = ToRollTableRecordCopy(table);
            EditorUtility.SetDirty(item);
        }
    }

    private static SteedDefinitionSO GetOrCreateSteed(SteedRecord record)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.steedName))
        {
            return null;
        }

        var asset = LoadOrCreateAsset<SteedDefinitionSO>(SteedFolder, record.steedName);
        asset.steedName = record.steedName;
        asset.vigor = record.vigor;
        asset.clarity = record.clarity;
        asset.spirit = record.spirit;
        asset.guard = record.guard;
        EditorUtility.SetDirty(asset);
        return asset;
    }

    private static EquipmentData GetOrCreateBackpack(IDictionary<string, EquipmentData> cache)
    {
        var asset = AssetDatabase.LoadAssetAtPath<EquipmentData>(BackpackPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<EquipmentData>();
            AssetDatabase.CreateAsset(asset, BackpackPath);
        }

        asset.itemName = "Traveller Backpack";
        asset.rarity = "common";
        asset.displayCategory = "tool";
        asset.rulesText = "Opens a separate 2x2 storage grid. Items inside do not grant equipped bonuses.";
        asset.costsCreationPoints = false;
        asset.containerKind = EquipmentContainerKind.Backpack;
        asset.contributesToEquippedBonuses = false;
        asset.sourceTags = new List<string> { "MythicBastionland", "Backpack" };
        EditorUtility.SetDirty(asset);
        cache[asset.itemName] = asset;
        return asset;
    }

    private static T GetOrCreateTextList<T>(string folder, string assetName, IEnumerable<string> rawEntries) where T : MythicBastionlandTextEntrySO
    {
        var asset = LoadOrCreateAsset<T>(folder, assetName);
        asset.entries = NormalizeEntries(rawEntries);
        EditorUtility.SetDirty(asset);
        DeleteExtraTextAssets<T>(folder, $"{folder}/{Sanitize(assetName)}.asset");
        return asset;
    }

    private static List<string> NormalizeEntries(IEnumerable<string> rawEntries)
    {
        var entries = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawEntry in rawEntries ?? Enumerable.Empty<string>())
        {
            var entry = rawEntry?.Trim();
            if (string.IsNullOrWhiteSpace(entry) || !seen.Add(entry))
            {
                continue;
            }

            entries.Add(entry);
        }

        entries.Sort(StringComparer.OrdinalIgnoreCase);
        return entries;
    }

    private static void DeleteExtraTextAssets<T>(string folder, string keepPath) where T : MythicBastionlandTextEntrySO
    {
        foreach (var guid in AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.Equals(path, keepPath, StringComparison.OrdinalIgnoreCase))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private static void AuthorEquipmentCatalog(IEnumerable<EquipmentData> assets, IReadOnlyList<KnightDefinitionSO> knights)
    {
        var authoring = EquipmentAuthoringContext.Load();
        var distinctAssets = assets
            .Where(asset => asset != null)
            .Distinct()
            .ToList();

        foreach (var asset in distinctAssets)
        {
            ApplyTraitAuthoring(asset, authoring);
            ApplyGenericChunkAuthoring(asset);
        }

        CoordinateKnightExclusiveChunks(knights, distinctAssets);
    }

    private static void ApplyTraitAuthoring(EquipmentData asset, EquipmentAuthoringContext authoring)
    {
        var changed = false;
        var probe = BuildTraitProbe(asset);

        if (ShouldBeWeapon(asset, probe) && authoring.weaponTrait != null && !asset.traits.Contains(authoring.weaponTrait))
        {
            asset.traits.Add(authoring.weaponTrait);
            changed = true;
        }

        var armorTrait = ResolveArmorTrait(asset, probe, authoring);
        if (armorTrait != null && !asset.traits.Contains(armorTrait))
        {
            asset.traits.Add(armorTrait);
            changed = true;
        }

        changed |= EnsureTrait(asset, authoring.longTrait, probe.Contains(" long"));
        changed |= EnsureTrait(asset, authoring.heftyTrait, probe.Contains(" hefty"));
        changed |= EnsureTrait(asset, authoring.slowTrait, probe.Contains(" slow"));
        changed |= EnsureTrait(asset, authoring.deadlyTrait, probe.Contains(" deadly"));

        if (changed)
        {
            EditorUtility.SetDirty(asset);
        }
    }

    private static bool EnsureTrait(EquipmentData asset, TraitSO trait, bool shouldApply)
    {
        if (!shouldApply || trait == null || asset.traits.Contains(trait))
        {
            return false;
        }

        asset.traits.Add(trait);
        return true;
    }

    private static void ApplyGenericChunkAuthoring(EquipmentData asset)
    {
        if (asset == null)
        {
            return;
        }

        var color = PickChunkColor(asset.itemName);
        var changed = false;

        if (IsChunklessItem(asset))
        {
            changed = ApplyChunkLayout(asset, ChunkColor.None, ChunkColor.None, ChunkColor.None, ChunkColor.None, ChunkColor.None);
        }
        else if (IsHeadArmor(asset))
        {
            changed = ApplyChunkLayout(asset, ChunkColor.None, ChunkColor.None, ChunkColor.None, color, ChunkColor.None);
        }
        else if (IsLegArmor(asset))
        {
            changed = ApplyChunkLayout(asset, ChunkColor.None, ChunkColor.None, color, ChunkColor.None, ChunkColor.None);
        }
        else if (!HasAnyChunks(asset))
        {
            if (IsShieldArmor(asset))
            {
                changed = ApplyChunkLayout(asset, ChunkColor.None, color, ChunkColor.None, ChunkColor.None, ChunkColor.None);
            }
            else if (IsTorsoArmor(asset))
            {
                changed = ApplyChunkLayout(asset, ChunkColor.None, ChunkColor.None, ChunkColor.None, ChunkColor.None, color);
            }
            else if (IsWeaponItem(asset))
            {
                changed = ApplyChunkLayout(asset, color, ChunkColor.None, ChunkColor.None, ChunkColor.None, ChunkColor.None);
            }
            else if (IsWaistArmor(asset))
            {
                changed = ApplyChunkLayout(asset, ChunkColor.None, ChunkColor.None, ChunkColor.None, ChunkColor.None, color);
            }
        }

        if (changed)
        {
            EditorUtility.SetDirty(asset);
        }
    }

    private static void CoordinateKnightExclusiveChunks(IReadOnlyList<KnightDefinitionSO> knights, IReadOnlyList<EquipmentData> allAssets)
    {
        var usageCounts = new Dictionary<EquipmentData, int>();
        foreach (var knight in knights ?? Array.Empty<KnightDefinitionSO>())
        {
            foreach (var asset in knight?.bondedProperty?.Where(item => item != null).Distinct() ?? Enumerable.Empty<EquipmentData>())
            {
                usageCounts[asset] = usageCounts.TryGetValue(asset, out var count) ? count + 1 : 1;
            }
        }

        foreach (var knight in knights ?? Array.Empty<KnightDefinitionSO>())
        {
            var exclusiveAssets = knight?.bondedProperty?
                .Where(item => item != null && usageCounts.TryGetValue(item, out var count) && count == 1)
                .Where(IsGeneratedMythicEquipment)
                .Distinct()
                .ToList();

            if (exclusiveAssets == null || exclusiveAssets.Count == 0)
            {
                continue;
            }

            ApplyKnightExclusiveChunkSet(knight.knightName, exclusiveAssets);
        }
    }

    private static void ApplyKnightExclusiveChunkSet(string knightName, IReadOnlyList<EquipmentData> exclusiveAssets)
    {
        var color = PickChunkColor(knightName);
        var torso = exclusiveAssets.FirstOrDefault(IsTorsoArmor);
        var head = exclusiveAssets.FirstOrDefault(IsHeadArmor);
        var legs = exclusiveAssets.FirstOrDefault(IsLegArmor);
        var shield = exclusiveAssets.FirstOrDefault(IsShieldArmor);
        var weapon = exclusiveAssets.FirstOrDefault(IsWeaponItem);

        if (torso == null)
        {
            return;
        }

        ClearChunks(torso);
        if (head != null)
        {
            ClearChunks(head);
            head.bottomHalf = color;
            torso.topHalf = color;
            EditorUtility.SetDirty(head);
        }

        if (legs != null)
        {
            ClearChunks(legs);
            legs.topHalf = color;
            torso.bottomHalf = color;
            EditorUtility.SetDirty(legs);
        }

        if (shield != null)
        {
            ClearChunks(shield);
            shield.rightHalf = color;
            torso.leftHalf = color;
            EditorUtility.SetDirty(shield);
        }

        if (weapon != null)
        {
            ClearChunks(weapon);
            weapon.leftHalf = color;
            torso.rightHalf = color;
            EditorUtility.SetDirty(weapon);
        }

        if (torso.topHalf == ChunkColor.None &&
            torso.bottomHalf == ChunkColor.None &&
            torso.leftHalf == ChunkColor.None &&
            torso.rightHalf == ChunkColor.None)
        {
            torso.centerChunk = color;
        }

        EditorUtility.SetDirty(torso);
    }

    private static bool HasAnyChunks(EquipmentData asset)
    {
        return asset.leftHalf != ChunkColor.None ||
               asset.rightHalf != ChunkColor.None ||
               asset.topHalf != ChunkColor.None ||
               asset.bottomHalf != ChunkColor.None ||
               asset.centerChunk != ChunkColor.None;
    }

    private static bool ApplyChunkLayout(
        EquipmentData asset,
        ChunkColor left,
        ChunkColor right,
        ChunkColor top,
        ChunkColor bottom,
        ChunkColor center)
    {
        if (asset.leftHalf == left &&
            asset.rightHalf == right &&
            asset.topHalf == top &&
            asset.bottomHalf == bottom &&
            asset.centerChunk == center)
        {
            return false;
        }

        asset.leftHalf = left;
        asset.rightHalf = right;
        asset.topHalf = top;
        asset.bottomHalf = bottom;
        asset.centerChunk = center;
        return true;
    }

    private static void ClearChunks(EquipmentData asset)
    {
        asset.leftHalf = ChunkColor.None;
        asset.rightHalf = ChunkColor.None;
        asset.topHalf = ChunkColor.None;
        asset.bottomHalf = ChunkColor.None;
        asset.centerChunk = ChunkColor.None;
    }

    private static bool IsChunklessItem(EquipmentData asset)
    {
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        var probe = BuildTraitProbe(asset);
        return category == "remedy" ||
               category == "consumable" ||
               category == "poison" ||
               probe.Contains(" strange ") ||
               probe.StartsWith("strange ", StringComparison.Ordinal);
    }

    private static bool IsWeaponItem(EquipmentData asset)
    {
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        return category == "weapon" || (!string.IsNullOrWhiteSpace(asset.damageDiceNotation) && category != "armor");
    }

    private static bool IsShieldArmor(EquipmentData asset)
    {
        var probe = BuildTraitProbe(asset);
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        return category == "shield" || probe.Contains(" shield");
    }

    private static bool IsHeadArmor(EquipmentData asset)
    {
        var probe = BuildTraitProbe(asset);
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        return category == "headwear" ||
               probe.Contains(" helm") ||
               probe.Contains(" helmet") ||
               probe.Contains(" hood") ||
               probe.Contains(" coif") ||
               probe.Contains(" head ");
    }

    private static bool IsLegArmor(EquipmentData asset)
    {
        var probe = BuildTraitProbe(asset);
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        return category == "footwear" ||
               category == "legwear" ||
               probe.Contains(" greaves") ||
               probe.Contains(" greave") ||
               probe.Contains(" boot") ||
               probe.Contains(" boots") ||
               probe.Contains(" leg ");
    }

    private static bool IsWaistArmor(EquipmentData asset)
    {
        var probe = BuildTraitProbe(asset);
        return probe.Contains(" pelt") ||
               probe.Contains(" belt") ||
               probe.Contains(" waist") ||
               probe.Contains(" cloak");
    }

    private static bool IsTorsoArmor(EquipmentData asset)
    {
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        if (category != "armor")
        {
            return false;
        }

        return !IsHeadArmor(asset) && !IsLegArmor(asset) && !IsWaistArmor(asset) && !IsShieldArmor(asset);
    }

    private static string BuildTraitProbe(EquipmentData asset)
    {
        return $" {NormalizeWhitespace(asset.itemName).ToLowerInvariant()} {NormalizeWhitespace(asset.rulesText).ToLowerInvariant()} ";
    }

    private static ChunkColor PickChunkColor(string key)
    {
        var hash = Math.Abs((key ?? string.Empty).GetHashCode());
        switch (hash % 3)
        {
            case 0:
                return ChunkColor.Red;
            case 1:
                return ChunkColor.Green;
            default:
                return ChunkColor.Blue;
        }
    }

    private static TraitSO ResolveArmorTrait(EquipmentData asset, string probe, EquipmentAuthoringContext authoring)
    {
        var category = NormalizeWhitespace(asset.displayCategory).ToLowerInvariant();
        if (category != "armor" && category != "shield")
        {
            return null;
        }

        if (IsShieldArmor(asset))
        {
            return authoring.shieldArmorTrait;
        }

        if (IsHeadArmor(asset))
        {
            return authoring.headArmorTrait;
        }

        if (IsLegArmor(asset))
        {
            return authoring.legArmorTrait;
        }

        if (IsWaistArmor(asset))
        {
            return authoring.waistArmorTrait;
        }

        return authoring.torsoArmorTrait;
    }

    private static bool ShouldBeWeapon(EquipmentData asset, string probe)
    {
        return IsWeaponItem(asset) || probe.Contains(" weapon ");
    }

    private static bool IsGeneratedMythicEquipment(EquipmentData asset)
    {
        var path = AssetDatabase.GetAssetPath(asset);
        return !string.IsNullOrWhiteSpace(path) && path.StartsWith(EquipmentFolder, StringComparison.OrdinalIgnoreCase);
    }

    private static void PruneDeprecatedEquipmentAssets(IReadOnlyCollection<string> keepPaths)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:EquipmentData", new[] { EquipmentFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (asset == null)
            {
                continue;
            }

            if (CountWords(asset.itemName) > 3 || !keepPaths.Contains(path))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private static IReadOnlyCollection<string> BuildEquipmentKeepPaths(IEnumerable<EquipmentData> assets)
    {
        var keepPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in assets ?? Enumerable.Empty<EquipmentData>())
        {
            if (asset == null)
            {
                continue;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            if (!string.IsNullOrWhiteSpace(path) && path.StartsWith(EquipmentFolder, StringComparison.OrdinalIgnoreCase))
            {
                keepPaths.Add(path);
            }
        }

        keepPaths.Add(BackpackPath);
        return keepPaths;
    }

    private static Dictionary<string, EquipmentData> LoadCuratedEquipmentByName()
    {
        var assetsByName = new Dictionary<string, EquipmentData>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in AssetDatabase.FindAssets("t:EquipmentData", new[] { CuratedItemsFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (asset == null || string.IsNullOrWhiteSpace(asset.itemName))
            {
                continue;
            }

            assetsByName[NormalizeItemKey(asset.itemName)] = asset;
        }

        return assetsByName;
    }

    private static (string itemName, string rulesText, string key) ResolveEquipmentIdentity(EquipmentRecord record)
    {
        var itemName = NormalizeWhitespace(record.name);
        if (record.isSupportFragment)
        {
            itemName = CanonicalizeSupportFragmentName(itemName, record.rulesText, record.damageDiceNotation, record.armorValue);
        }
        else
        {
            itemName = CanonicalizePrimaryFragmentName(itemName);
        }

        return (itemName, NormalizeWhitespace(record.rulesText), NormalizeItemKey(itemName));
    }

    private static string CanonicalizePrimaryFragmentName(string itemName)
    {
        var normalizedName = NormalizeWhitespace(itemName);
        if (CountWords(normalizedName) <= 3)
        {
            return normalizedName;
        }

        if (Regex.IsMatch(normalizedName, @"^fine\s+saddle\s+and\s+tack$", RegexOptions.IgnoreCase))
        {
            return "saddle and tack";
        }

        return normalizedName;
    }

    private static string CanonicalizeSupportFragmentName(string itemName, string rulesText, string damageDiceNotation, int armorValue)
    {
        var normalizedName = NormalizeWhitespace(itemName).ToLowerInvariant();
        var normalizedRules = NormalizeRuleSignature(rulesText);

        if ((normalizedRules == "(a1)" || string.IsNullOrWhiteSpace(normalizedRules)) &&
            (normalizedName.EndsWith(" chainmail", StringComparison.Ordinal) || normalizedName == "chainmail"))
        {
            return "chainmail";
        }

        if ((normalizedRules == "(a1)" || string.IsNullOrWhiteSpace(normalizedRules)) &&
            (normalizedName.EndsWith(" chain mail", StringComparison.Ordinal) || normalizedName == "chain mail"))
        {
            return "chainmail";
        }

        if ((normalizedRules == "(a1)" || string.IsNullOrWhiteSpace(normalizedRules)) &&
            (normalizedName.EndsWith(" ringmail", StringComparison.Ordinal) || normalizedName == "ringmail" ||
             normalizedName.EndsWith(" ringed mail", StringComparison.Ordinal) || normalizedName == "ringed mail"))
        {
            return "ringmail";
        }

        if ((normalizedRules == "(a1)" || string.IsNullOrWhiteSpace(normalizedRules)) &&
            normalizedName.EndsWith(" mail", StringComparison.Ordinal))
        {
            return "mail";
        }

        if ((normalizedRules == "(a1)" || string.IsNullOrWhiteSpace(normalizedRules)) &&
            normalizedName.EndsWith(" gambeson", StringComparison.Ordinal))
        {
            return "gambeson";
        }

        if ((normalizedRules == "(d4,a1)" || normalizedRules == "(a1,d4)") &&
            normalizedName.EndsWith(" buckler", StringComparison.Ordinal))
        {
            return "buckler";
        }

        if ((NormalizeDiceNotation(damageDiceNotation) == "1d6" || normalizedRules == "(d6)") &&
            (normalizedName.EndsWith(" javelins", StringComparison.Ordinal) || normalizedName.EndsWith(" javelin", StringComparison.Ordinal)))
        {
            return "javelin";
        }

        return itemName;
    }

    private static string NormalizeRuleSignature(string rulesText)
    {
        return Regex.Replace((rulesText ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", string.Empty);
    }

    private static string NormalizeDiceNotation(string notation)
    {
        return Regex.Replace((notation ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", string.Empty);
    }

    private static string NormalizeItemKey(string itemName)
    {
        return NormalizeWhitespace(itemName).ToLowerInvariant();
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace((value ?? string.Empty).Trim(), @"\s+", " ");
    }

    private static MythicRollTable ToRollTable(RollTableRecord record)
    {
        var table = new MythicRollTable
        {
            title = record?.title ?? string.Empty,
            columns = new List<MythicTableColumn>()
        };

        foreach (var column in record?.columns ?? Array.Empty<TableColumnRecord>())
        {
            table.columns.Add(new MythicTableColumn
            {
                header = column.header,
                values = column.values?.ToList() ?? new List<string>()
            });
        }

        return table;
    }

    private static MythicRollTable ToRollTableRecordCopy(MythicRollTable source)
    {
        var table = new MythicRollTable
        {
            title = source?.title ?? string.Empty,
            columns = new List<MythicTableColumn>()
        };

        foreach (var column in source?.columns ?? new List<MythicTableColumn>())
        {
            table.columns.Add(new MythicTableColumn
            {
                header = column?.header,
                values = column?.values?.ToList() ?? new List<string>()
            });
        }

        return table;
    }

    private static T LoadOrCreateAsset<T>(string folder, string rawName) where T : ScriptableObject
    {
        var safeName = Sanitize(rawName);
        var path = $"{folder}/{safeName}.asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static string Sanitize(string rawName)
    {
        var safe = Regex.Replace(rawName ?? string.Empty, @"[^A-Za-z0-9_]+", "_");
        safe = Regex.Replace(safe, @"_+", "_").Trim('_');
        if (string.IsNullOrWhiteSpace(safe))
        {
            safe = "GeneratedAsset";
        }

        return safe.Length <= 64 ? safe : safe.Substring(0, 64);
    }

    private static int CountWords(string text)
    {
        return Regex.Matches(text ?? string.Empty, @"[A-Za-z0-9]+(?:'[A-Za-z0-9]+)?").Count;
    }

    [Serializable]
    private sealed class ImportPayload
    {
        public KnightRecord[] knights;
        public MythRecord[] myths;
        public string[] issues;
    }

    [Serializable]
    private sealed class KnightRecord
    {
        public int pageNumber;
        public string knightName;
        public string titleVerse;
        public string passionTitle;
        public string passionText;
        public string abilityName;
        public string abilityDescription;
        public SeerRecord linkedSeer;
        public SteedRecord steed;
        public EquipmentRecord[] propertyItems;
        public RollTableRecord randomFlavorTable;
        public string personHook;
        public string nameHook;
        public string characteristicHook;
        public string objectHook;
        public string beastHook;
        public string stateHook;
        public string themeHook;
    }

    [Serializable]
    private sealed class MythRecord
    {
        public int pageNumber;
        public string mythName;
        public string verse;
        public string[] omens;
        public CastRecord[] castEntries;
        public RollTableRecord flavorTable;
        public string dwelling;
        public string sanctum;
        public string monument;
        public string hazard;
        public string curse;
        public string ruin;
    }

    [Serializable]
    private sealed class EquipmentRecord
    {
        public string name;
        public string rulesText;
        public string rarity;
        public string displayCategory;
        public string damageDiceNotation;
        public int armorValue;
        public string[] sourceTags;
        public bool isBondedProperty;
        public bool isSupportFragment;
    }

    [Serializable]
    private sealed class SeerRecord
    {
        public string seerName;
        public int vigor;
        public int clarity;
        public int spirit;
        public int guard;
        public string[] traits;
    }

    [Serializable]
    private sealed class SteedRecord
    {
        public string steedName;
        public int vigor;
        public int clarity;
        public int spirit;
        public int guard;
    }

    [Serializable]
    private sealed class CastRecord
    {
        public string name;
        public string statBlock;
        public string notes;
    }

    [Serializable]
    private sealed class RollTableRecord
    {
        public string title;
        public TableColumnRecord[] columns;
    }

    [Serializable]
    private sealed class TableColumnRecord
    {
        public string header;
        public string[] values;
    }

    private sealed class EquipmentAuthoringContext
    {
        public TraitSO weaponTrait;
        public TraitSO longTrait;
        public TraitSO heftyTrait;
        public TraitSO slowTrait;
        public TraitSO deadlyTrait;
        public TraitSO headArmorTrait;
        public TraitSO legArmorTrait;
        public TraitSO shieldArmorTrait;
        public TraitSO torsoArmorTrait;
        public TraitSO waistArmorTrait;

        public static EquipmentAuthoringContext Load()
        {
            return new EquipmentAuthoringContext
            {
                weaponTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Weapon.asset"),
                longTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Long.asset"),
                heftyTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Hefty.asset"),
                slowTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Slow.asset"),
                deadlyTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Deadly.asset"),
                headArmorTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Armor/Head Armor.asset"),
                legArmorTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Armor/Leg Armor.asset"),
                shieldArmorTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Armor/Shield Armor.asset"),
                torsoArmorTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Armor/Torso Armor.asset"),
                waistArmorTrait = AssetDatabase.LoadAssetAtPath<TraitSO>($"{TraitFolder}/Armor/Waist Armor.asset")
            };
        }
    }
}
