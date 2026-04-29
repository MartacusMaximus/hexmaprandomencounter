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
    private const string EquipmentFolder = RootFolder + "/Equipment";
    private const string AbilityFolder = RootFolder + "/Abilities";
    private const string SeerFolder = RootFolder + "/Seers";
    private const string SteedFolder = RootFolder + "/Steeds";
    private const string KnightFolder = RootFolder + "/Knights";
    private const string MythFolder = RootFolder + "/Myths";
    private const string PersonFolder = RootFolder + "/People";
    private const string NameFolder = RootFolder + "/Names";
    private const string CharacteristicFolder = RootFolder + "/Characteristics";
    private const string ObjectFolder = RootFolder + "/Objects";
    private const string BeastFolder = RootFolder + "/Beasts";
    private const string StateFolder = RootFolder + "/States";
    private const string ThemeFolder = RootFolder + "/Themes";
    private const string DwellingFolder = RootFolder + "/Dwellings";
    private const string SanctumFolder = RootFolder + "/Sanctums";
    private const string MonumentFolder = RootFolder + "/Monuments";
    private const string HazardFolder = RootFolder + "/Hazards";
    private const string CurseFolder = RootFolder + "/Curses";
    private const string RuinFolder = RootFolder + "/Ruins";
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
        EnsureFolder(RootFolder, "People");
        EnsureFolder(RootFolder, "Names");
        EnsureFolder(RootFolder, "Characteristics");
        EnsureFolder(RootFolder, "Objects");
        EnsureFolder(RootFolder, "Beasts");
        EnsureFolder(RootFolder, "States");
        EnsureFolder(RootFolder, "Themes");
        EnsureFolder(RootFolder, "Dwellings");
        EnsureFolder(RootFolder, "Sanctums");
        EnsureFolder(RootFolder, "Monuments");
        EnsureFolder(RootFolder, "Hazards");
        EnsureFolder(RootFolder, "Curses");
        EnsureFolder(RootFolder, "Ruins");

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

        foreach (var knight in payload.knights ?? Array.Empty<KnightRecord>())
        {
            var seer = GetOrCreateSeer(knight.linkedSeer, seersByName);
            var ability = GetOrCreateAbility(knight.abilityName, knight.abilityDescription, abilitiesByName);
            var steed = GetOrCreateSteed(knight.steed);
            var propertyItems = knight.propertyItems?
                .Select(item => GetOrCreateEquipment(item, equipmentByName))
                .Where(item => item != null)
                .ToList() ?? new List<EquipmentData>();

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
            asset.randomFlavorTable = ToRollTable(knight.randomFlavorTable);
            EditorUtility.SetDirty(asset);
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

        contentLibrary.seers = seersByName.Values.OrderBy(asset => asset.seerName).ToList();
        contentLibrary.abilities = abilitiesByName.Values.OrderBy(asset => asset.abilityName).ToList();
        contentLibrary.equipment = equipmentByName.Values.OrderBy(asset => asset.itemName).ToList();
        contentLibrary.knights = (payload.knights ?? Array.Empty<KnightRecord>())
            .Select(knight => AssetDatabase.LoadAssetAtPath<KnightDefinitionSO>($"{KnightFolder}/{Sanitize(knight.knightName)}.asset"))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.pageNumber)
            .ToList();
        contentLibrary.myths = (payload.myths ?? Array.Empty<MythRecord>())
            .Select(myth => AssetDatabase.LoadAssetAtPath<MythSO>($"{MythFolder}/{Sanitize(myth.mythName)}.asset"))
            .Where(asset => asset != null)
            .OrderBy(asset => asset.pageNumber)
            .ToList();
        contentLibrary.people = GetOrCreateTextList<PersonSO>(PersonFolder, PeopleListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.personHook));
        contentLibrary.names = GetOrCreateTextList<NameSO>(NameFolder, NamesListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.nameHook));
        contentLibrary.characteristics = GetOrCreateTextList<CharacteristicSO>(CharacteristicFolder, CharacteristicsListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.characteristicHook));
        contentLibrary.objects = GetOrCreateTextList<ObjectSO>(ObjectFolder, ObjectsListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.objectHook));
        contentLibrary.beasts = GetOrCreateTextList<BeastSO>(BeastFolder, BeastsListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.beastHook));
        contentLibrary.states = GetOrCreateTextList<StateSO>(StateFolder, StatesListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.stateHook));
        contentLibrary.themes = GetOrCreateTextList<ThemeSO>(ThemeFolder, ThemesListAssetName, (payload.knights ?? Array.Empty<KnightRecord>()).Select(knight => knight.themeHook));
        contentLibrary.dwellings = GetOrCreateTextList<DwellingSO>(DwellingFolder, DwellingsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.dwelling));
        contentLibrary.sanctums = GetOrCreateTextList<SanctumSO>(SanctumFolder, SanctumsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.sanctum));
        contentLibrary.monuments = GetOrCreateTextList<MonumentSO>(MonumentFolder, MonumentsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.monument));
        contentLibrary.hazards = GetOrCreateTextList<HazardSO>(HazardFolder, HazardsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.hazard));
        contentLibrary.curses = GetOrCreateTextList<CurseSO>(CurseFolder, CursesListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.curse));
        contentLibrary.ruins = GetOrCreateTextList<RuinSO>(RuinFolder, RuinsListAssetName, (payload.myths ?? Array.Empty<MythRecord>()).Select(myth => myth.ruin));
        EditorUtility.SetDirty(contentLibrary);

        PruneDeprecatedEquipmentAssets();
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

    private static EquipmentData GetOrCreateEquipment(EquipmentRecord record, IDictionary<string, EquipmentData> cache)
    {
        if (record == null || string.IsNullOrWhiteSpace(record.name))
        {
            return null;
        }

        if (CountWords(record.name) > 3)
        {
            return null;
        }

        if (cache.TryGetValue(record.name, out var existing))
        {
            return existing;
        }

        var asset = LoadOrCreateAsset<EquipmentData>(EquipmentFolder, record.name);
        asset.itemName = record.name;
        asset.rulesText = record.rulesText;
        asset.rarity = string.IsNullOrWhiteSpace(record.rarity) ? "bonded" : record.rarity;
        asset.displayCategory = string.IsNullOrWhiteSpace(record.displayCategory) ? "tool" : record.displayCategory;
        asset.damageDiceNotation = record.damageDiceNotation;
        asset.armorValue = record.armorValue;
        asset.costsCreationPoints = false;
        asset.isBondedProperty = record.isBondedProperty;
        asset.usableByNonOwner = !record.isBondedProperty;
        asset.sourceTags = record.sourceTags?.ToList() ?? new List<string> { "MythicBastionland" };
        if (asset.displayCategory == "remedy")
        {
            asset.storageRule = EquipmentStorageRule.ContainerOnly;
            asset.occupiesFullContainer = true;
            asset.contributesToEquippedBonuses = false;
        }

        EditorUtility.SetDirty(asset);
        cache[record.name] = asset;
        return asset;
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

    private static void PruneDeprecatedEquipmentAssets()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:EquipmentData", new[] { EquipmentFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);
            if (asset != null && CountWords(asset.itemName) > 3)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
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
}
