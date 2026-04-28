using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ReworkPartyWorkbenchSetup
{
    private const string GeneratedRoot = "Assets/Generated/ReworkParty";
    private const string CharactersRoot = GeneratedRoot + "/Characters";
    private const string EquipmentLibraryPath = GeneratedRoot + "/ReworkEquipmentLibrary.asset";
    private const string CampaignPath = GeneratedRoot + "/ReworkCampaign.asset";
    private const string ReworkScenePath = "Assets/Scenes/ReworkScene.unity";
    private const string ItemsFolder = "Assets/Scripts/ScriptableObjects/ITEMS";

    [MenuItem("Tools/Rework/Setup Party Workbench")]
    public static void BuildReworkPartyWorkbench()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "ReworkParty");
        EnsureFolder(GeneratedRoot, "Characters");

        var equipmentByName = LoadEquipmentByName();
        var equipmentLibrary = CreateOrUpdateEquipmentLibrary(equipmentByName);
        var campaign = CreateOrUpdateCampaign(equipmentByName);

        WireReworkScene(campaign, equipmentLibrary);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Rework party workbench setup complete.");
    }

    private static EquipmentLibrarySO CreateOrUpdateEquipmentLibrary(Dictionary<string, EquipmentData> equipmentByName)
    {
        var library = AssetDatabase.LoadAssetAtPath<EquipmentLibrarySO>(EquipmentLibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<EquipmentLibrarySO>();
            AssetDatabase.CreateAsset(library, EquipmentLibraryPath);
        }

        library.items = equipmentByName.Values
            .Where(item => item != null)
            .OrderBy(item => item.displayCategory)
            .ThenBy(item => item.pointCost)
            .ThenBy(item => item.itemName)
            .ToList();

        EditorUtility.SetDirty(library);
        return library;
    }

    private static CampaignData CreateOrUpdateCampaign(Dictionary<string, EquipmentData> equipmentByName)
    {
        var campaign = AssetDatabase.LoadAssetAtPath<CampaignData>(CampaignPath);
        if (campaign == null)
        {
            campaign = ScriptableObject.CreateInstance<CampaignData>();
            AssetDatabase.CreateAsset(campaign, CampaignPath);
        }

        var dungBeetleKnight = CreateOrUpdateCharacter(
            CharactersRoot + "/DungBeetleKnight.asset",
            "Dung Beetle Knight",
            "Bulwark",
            9,
            6,
            8,
            new[]
            {
                Skill("Bladeguard", 4),
                Skill("Burial Rites", 4),
                Skill("Fort Command", 3)
            },
            equipmentByName,
            "Studded Mace",
            "Steel Shield",
            "Chain Mail",
            "Helm",
            "Great Bear Pelt",
            "Greaves",
            "Sustenance");

        var pigeonKnight = CreateOrUpdateCharacter(
            CharactersRoot + "/PigeonKnight.asset",
            "Pigeon Knight",
            "Courier",
            6,
            9,
            8,
            new[]
            {
                Skill("Squadron", 4),
                Skill("Sun Keeper", 4),
                Skill("Artillery", 3)
            },
            equipmentByName,
            "Dagger",
            "Short Bow",
            "Cloak",
            "Roc Feather",
            "Stimulant",
            "Sun Amulet");

        var mountainKnight = CreateOrUpdateCharacter(
            CharactersRoot + "/MountainKnight.asset",
            "Mountain Knight",
            "Highlander",
            10,
            6,
            7,
            new[]
            {
                Skill("Skewering Strike", 5),
                Skill("Bladeguard", 4),
                Skill("Ancestor", 3)
            },
            equipmentByName,
            "Great Sword",
            "Chest Plate",
            "Helm",
            "Greaves",
            "Sacrament",
            "War Hammer");

        var ratKnight = CreateOrUpdateCharacter(
            CharactersRoot + "/RatKnight.asset",
            "Rat Knight",
            "Scavenger",
            7,
            9,
            7,
            new[]
            {
                Skill("Court", 4),
                Skill("Bloodlines", 4),
                Skill("Codex Language", 3)
            },
            equipmentByName,
            "Dagger",
            "Common Poison",
            "Leather Vest",
            "Prophet's Hood",
            "Animal Trap",
            "Sewing Set");

        dungBeetleKnight.currentStatus = "On Expedition";
        pigeonKnight.currentStatus = "On Expedition";
        mountainKnight.currentStatus = "Available";
        ratKnight.currentStatus = "Available";

        campaign.allCharacters = new List<CharacterData>
        {
            dungBeetleKnight,
            pigeonKnight,
            mountainKnight,
            ratKnight
        };

        campaign.campaignInventory ??= new CampaignInventory();
        campaign.campaignInventory.Items.Clear();
        AddInventoryItem(campaign.campaignInventory.Items, equipmentByName, "Tower Shield");
        AddInventoryItem(campaign.campaignInventory.Items, equipmentByName, "Long Sword");
        AddInventoryItem(campaign.campaignInventory.Items, equipmentByName, "Silver Shield");
        AddInventoryItem(campaign.campaignInventory.Items, equipmentByName, "Smithing Tools");
        AddInventoryItem(campaign.campaignInventory.Items, equipmentByName, "Stimulant");
        AddInventoryItem(campaign.campaignInventory.Items, equipmentByName, "Sacrament");

        campaign.activeParty ??= new PartyData();
        campaign.activeParty.partyName = "Active Expedition";
        campaign.activeParty.members.Clear();
        campaign.activeParty.members.Add(dungBeetleKnight);
        campaign.activeParty.members.Add(pigeonKnight);
        campaign.activeParty.partyInventory.Clear();

        EditorUtility.SetDirty(campaign);
        return campaign;
    }

    private static CharacterData CreateOrUpdateCharacter(
        string assetPath,
        string characterName,
        string knightRole,
        int vigor,
        int clarity,
        int spirit,
        IEnumerable<SkillEntry> skills,
        IReadOnlyDictionary<string, EquipmentData> equipmentByName,
        params string[] equipmentNames)
    {
        var character = AssetDatabase.LoadAssetAtPath<CharacterData>(assetPath);
        if (character == null)
        {
            character = ScriptableObject.CreateInstance<CharacterData>();
            AssetDatabase.CreateAsset(character, assetPath);
        }

        character.characterName = characterName;
        character.knightRole = knightRole;
        character.isAlive = true;
        character.currentStatus = "Available";
        character.vigor = vigor;
        character.clarity = clarity;
        character.spirit = spirit;
        character.movedThisTurn = false;
        character.flawCount = 0;
        character.hasCoreAbility = false;
        character.deedCount = 0;
        character.skills = skills
            .Select(skill => new SkillEntry { skillName = skill.skillName, value = skill.value })
            .ToList();

        character.inventory = new List<EquipmentInstance>();
        character.EnsureInventorySlots();
        for (var index = 0; index < equipmentNames.Length && index < character.inventory.Count; index++)
        {
            character.inventory[index] = new EquipmentInstance
            {
                equipment = RequireEquipment(equipmentByName, equipmentNames[index])
            };
        }

        character.cachedPointsLeft = CharacterCreationRules.CalculatePointsLeft(
            CharacterRulesAdapter.ToModel(character),
            new CharacterCreationConfig());

        EditorUtility.SetDirty(character);
        return character;
    }

    private static void WireReworkScene(CampaignData campaign, EquipmentLibrarySO equipmentLibrary)
    {
        var scene = EditorSceneManager.OpenScene(ReworkScenePath, OpenSceneMode.Single);

        var creationManager = FindSceneObject<CharacterCreationManager>();
        var inventoryGrid = FindSceneObject<InventoryGrid>();
        var characterSheetUI = FindSceneObject<CharacterSheetUI>();
        var skillsPanelManager = FindSceneObject<SkillsPanelManager>();
        var partyButton = FindSceneObject<Button>("PARTY");
        var partyPanelRoot = FindSceneObject<RectTransform>("PARTYPanel");

        if (creationManager == null || inventoryGrid == null || characterSheetUI == null || skillsPanelManager == null || partyButton == null || partyPanelRoot == null)
        {
            throw new System.InvalidOperationException("Rework scene is missing one or more required workbench anchors.");
        }

        var defaultCharacter = campaign.activeParty.members.FirstOrDefault() ?? campaign.allCharacters.FirstOrDefault();
        creationManager.workingCharacter = defaultCharacter;
        inventoryGrid.character = defaultCharacter;
        characterSheetUI.character = defaultCharacter;
        skillsPanelManager.characterData = defaultCharacter;
        skillsPanelManager.creationManager = creationManager;

        var controller = creationManager.GetComponent<ReworkPartyWorkbenchController>();
        if (controller == null)
        {
            controller = creationManager.gameObject.AddComponent<ReworkPartyWorkbenchController>();
        }

        controller.campaignData = campaign;
        controller.equipmentLibrary = equipmentLibrary;
        controller.creationManager = creationManager;
        controller.inventoryGrid = inventoryGrid;
        controller.characterSheetUI = characterSheetUI;
        controller.skillsPanelManager = skillsPanelManager;
        controller.rootCanvas = inventoryGrid.rootCanvas;
        controller.partyButton = partyButton;
        controller.partyPanelRoot = partyPanelRoot;

        partyPanelRoot.gameObject.SetActive(false);

        EditorUtility.SetDirty(creationManager);
        EditorUtility.SetDirty(inventoryGrid);
        EditorUtility.SetDirty(characterSheetUI);
        EditorUtility.SetDirty(skillsPanelManager);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static Dictionary<string, EquipmentData> LoadEquipmentByName()
    {
        return AssetDatabase.FindAssets("t:EquipmentData", new[] { ItemsFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<EquipmentData>)
            .Where(item => item != null)
            .ToDictionary(item => item.itemName, item => item);
    }

    private static void AddInventoryItem(ICollection<EquipmentInstance> destination, IReadOnlyDictionary<string, EquipmentData> equipmentByName, string equipmentName)
    {
        destination.Add(new EquipmentInstance { equipment = RequireEquipment(equipmentByName, equipmentName) });
    }

    private static EquipmentData RequireEquipment(IReadOnlyDictionary<string, EquipmentData> equipmentByName, string equipmentName)
    {
        if (!equipmentByName.TryGetValue(equipmentName, out var equipment) || equipment == null)
        {
            throw new KeyNotFoundException($"Missing equipment asset '{equipmentName}'.");
        }

        return equipment;
    }

    private static SkillEntry Skill(string name, int value)
    {
        return new SkillEntry { skillName = name, value = value };
    }

    private static void EnsureFolder(string parent, string name)
    {
        var folderPath = $"{parent}/{name}";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    private static T FindSceneObject<T>() where T : Object
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(obj => IsSceneObject(obj));
    }

    private static T FindSceneObject<T>(string objectName) where T : Component
    {
        return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .FirstOrDefault(component => IsSceneObject(component) && component.name == objectName);
    }

    private static bool IsSceneObject(Object obj)
    {
        switch (obj)
        {
            case Component component:
                return component.gameObject.scene.IsValid();
            case GameObject gameObject:
                return gameObject.scene.IsValid();
            default:
                return false;
        }
    }
}
