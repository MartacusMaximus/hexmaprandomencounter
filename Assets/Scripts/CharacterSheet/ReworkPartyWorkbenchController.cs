using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ReworkPartyWorkbenchController : MonoBehaviour
{
    [Header("Data")]
    public CampaignData campaignData;
    public EquipmentLibrarySO equipmentLibrary;

    [Header("Scene References")]
    public CharacterCreationManager creationManager;
    public InventoryGrid inventoryGrid;
    public CharacterSheetUI characterSheetUI;
    public SkillsPanelManager skillsPanelManager;
    public Canvas rootCanvas;
    public Button partyButton;
    public RectTransform partyPanelRoot;

    private CharacterData activeCharacter;
    private TMP_InputField characterNameInput;
    private bool suppressNameSync;
    private bool partyVisible;
    private string selectedCatalogFilter = "All";

    private RectTransform dockRoot;
    private RectTransform guildRosterContent;
    private RectTransform partyMembersContent;
    private RectTransform storageContent;
    private RectTransform catalogContent;
    private TextMeshProUGUI selectedCharacterSummary;

    private readonly string[] catalogFilters = { "All", "Weapons", "Armor", "Utility", "Crown" };

    private IEnumerator Start()
    {
        yield return null;

        EnsureState();
        ResolveSceneReferences();
        ResolveNameInput();
        BuildPartyDock();
        HookEvents();
        var initialCharacter =
            campaignData != null && campaignData.activeParty != null
                ? campaignData.activeParty.members.FirstOrDefault()
                : null;
        initialCharacter ??= campaignData != null ? campaignData.allCharacters.FirstOrDefault() : null;
        initialCharacter ??= creationManager != null ? creationManager.workingCharacter : null;

        if (initialCharacter != null)
        {
            SelectCharacter(initialCharacter);
        }

        SetPartyPanelVisible(false);
    }

    private void OnDestroy()
    {
        UIRefreshBus.OnRefresh -= HandleExternalRefresh;

        if (characterNameInput != null)
        {
            characterNameInput.onValueChanged.RemoveListener(OnCharacterNameChanged);
        }

        if (partyButton != null)
        {
            partyButton.onClick.RemoveListener(TogglePartyPanel);
        }

        if (characterSheetUI != null && characterSheetUI.openButton != null)
        {
            characterSheetUI.openButton.onClick.RemoveListener(ShowSheetOnly);
        }

        if (characterSheetUI != null && characterSheetUI.closeButton != null)
        {
            characterSheetUI.closeButton.onClick.RemoveListener(HidePartyAndSheet);
        }
    }

    public void SelectCharacter(CharacterData character)
    {
        if (character == null || creationManager == null || inventoryGrid == null || characterSheetUI == null || skillsPanelManager == null)
        {
            return;
        }

        activeCharacter = character;
        activeCharacter.EnsureInventorySlots();

        creationManager.workingCharacter = activeCharacter;
        inventoryGrid.SetCharacter(activeCharacter);
        characterSheetUI.character = activeCharacter;
        skillsPanelManager.SetCharacterData(activeCharacter);
        PersistState(activeCharacter, campaignData);
        ShowSheetOnly();
    }

    public bool TryDropItemOnCharacterSlot(PartyWorkbenchDragController.DragPayload payload, InventoryGrid targetGrid, int slotIndex)
    {
        if (payload == null || targetGrid == null || activeCharacter == null || targetGrid.character != activeCharacter)
        {
            return false;
        }

        if (payload.IsCharacterSlotItem)
        {
            if (payload.sourceGrid != targetGrid)
            {
                return false;
            }

            targetGrid.SwapSlots(payload.sourceSlotIndex, slotIndex);
            PersistState(activeCharacter, campaignData);
            RefreshAllViews();
            return true;
        }

        if (activeCharacter.inventory[slotIndex] != null)
        {
            return false;
        }

        if (payload.IsCatalogItem)
        {
            return TryApplyCharacterInventoryChange(() =>
            {
                activeCharacter.inventory[slotIndex] = new EquipmentInstance { equipment = payload.catalogEquipment };
            });
        }

        if (payload.IsStoredItem)
        {
            return TryApplyCharacterInventoryChange(() =>
            {
                campaignData.campaignInventory.TryRemove(payload.itemInstance);
                activeCharacter.inventory[slotIndex] = payload.itemInstance;
            },
            () => campaignData.campaignInventory.TryAdd(payload.itemInstance));
        }

        return false;
    }

    public bool TryDropItemOnCampaignStorage(PartyWorkbenchDragController.DragPayload payload)
    {
        if (payload == null || campaignData == null || campaignData.campaignInventory == null)
        {
            return false;
        }

        if (payload.IsCatalogItem)
        {
            campaignData.campaignInventory.TryAdd(new EquipmentInstance { equipment = payload.catalogEquipment });
            PersistState(campaignData);
            RefreshWorkbench();
            return true;
        }

        if (payload.IsStoredItem)
        {
            return true;
        }

        if (payload.IsCharacterSlotItem)
        {
            var sourceCharacter = payload.sourceGrid.character;
            sourceCharacter.inventory[payload.sourceSlotIndex] = null;
            campaignData.campaignInventory.TryAdd(payload.itemInstance);
            if (sourceCharacter == activeCharacter)
            {
                PersistState(sourceCharacter, campaignData);
                RefreshAllViews();
            }
            else
            {
                PersistState(sourceCharacter, campaignData);
            }

            RefreshWorkbench();
            return true;
        }

        return false;
    }

    public bool TryMoveCharacter(PartyWorkbenchDragController.DragPayload payload, bool toParty)
    {
        if (payload == null || !payload.IsCharacterEntry || payload.character == null || campaignData == null || campaignData.activeParty == null)
        {
            return false;
        }

        if (toParty)
        {
            if (campaignData.activeParty.members.Contains(payload.character))
            {
                return true;
            }

            campaignData.activeParty.members.Add(payload.character);
            payload.character.currentStatus = "On Expedition";
        }
        else
        {
            if (!campaignData.activeParty.members.Remove(payload.character))
            {
                return false;
            }

            payload.character.currentStatus = payload.character.isAlive ? "Available" : "Dead";
        }

        RefreshWorkbench();
        PersistState(payload.character, campaignData);
        return true;
    }

    private bool TryApplyCharacterInventoryChange(System.Action applyChange, System.Action rollbackChange = null)
    {
        if (activeCharacter == null || creationManager == null || inventoryGrid == null || skillsPanelManager == null)
        {
            return false;
        }

        var snapshot = new List<EquipmentInstance>(activeCharacter.inventory);
        applyChange();
        creationManager.RecalculatePointsFromCharacterData();

        if (creationManager.PointsLeft < 0)
        {
            activeCharacter.inventory = snapshot;
            rollbackChange?.Invoke();
            PersistState(activeCharacter, campaignData);
            RefreshAllViews();
            return false;
        }

        PersistState(activeCharacter, campaignData);
        RefreshAllViews();
        return true;
    }

    private void EnsureState()
    {
        if (campaignData == null)
        {
            return;
        }

        if (campaignData.allCharacters == null)
        {
            campaignData.allCharacters = new List<CharacterData>();
        }

        if (campaignData.campaignInventory == null)
        {
            campaignData.campaignInventory = new CampaignInventory();
        }

        if (campaignData.activeParty == null)
        {
            campaignData.activeParty = new PartyData();
        }

        foreach (var character in campaignData.allCharacters)
        {
            if (character != null)
            {
                character.EnsureInventorySlots();
            }
        }
    }

    private void ResolveSceneReferences()
    {
        if (creationManager == null)
        {
            creationManager = FindFirstObjectByType<CharacterCreationManager>(FindObjectsInactive.Include);
        }

        if (inventoryGrid == null)
        {
            inventoryGrid = FindFirstObjectByType<InventoryGrid>(FindObjectsInactive.Include);
        }

        if (characterSheetUI == null)
        {
            characterSheetUI = FindFirstObjectByType<CharacterSheetUI>(FindObjectsInactive.Include);
        }

        if (skillsPanelManager == null)
        {
            skillsPanelManager = FindFirstObjectByType<SkillsPanelManager>(FindObjectsInactive.Include);
        }

        if (rootCanvas == null)
        {
            rootCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
        }
    }

    private void ResolveNameInput()
    {
        if (creationManager == null)
        {
            return;
        }

        characterNameInput = creationManager
            .GetComponentsInChildren<TMP_InputField>(true)
            .FirstOrDefault(field => field != creationManager.newSkillNameInput);

        if (characterNameInput != null)
        {
            characterNameInput.onValueChanged.AddListener(OnCharacterNameChanged);
        }
    }

    private void HookEvents()
    {
        UIRefreshBus.OnRefresh += HandleExternalRefresh;

        if (partyButton != null)
        {
            partyButton.onClick.RemoveListener(TogglePartyPanel);
            partyButton.onClick.AddListener(TogglePartyPanel);
        }

        if (characterSheetUI != null && characterSheetUI.openButton != null)
        {
            characterSheetUI.openButton.onClick.AddListener(ShowSheetOnly);
        }

        if (characterSheetUI != null && characterSheetUI.closeButton != null)
        {
            characterSheetUI.closeButton.onClick.AddListener(HidePartyAndSheet);
        }
    }

    private void HandleExternalRefresh()
    {
        if (activeCharacter == null || creationManager == null || inventoryGrid == null || skillsPanelManager == null)
        {
            return;
        }

        creationManager.RecalculatePointsFromCharacterData();
        RefreshAllViews();
    }

    private void OnCharacterNameChanged(string value)
    {
        if (suppressNameSync || activeCharacter == null)
        {
            return;
        }

        activeCharacter.characterName = string.IsNullOrWhiteSpace(value) ? "New Character" : value.Trim();
        PersistState(activeCharacter, campaignData);
        RefreshAllViews();
    }

    private void SyncCharacterNameField()
    {
        if (characterNameInput == null || activeCharacter == null)
        {
            return;
        }

        suppressNameSync = true;
        characterNameInput.text = activeCharacter.characterName;
        suppressNameSync = false;
    }

    private void TogglePartyPanel()
    {
        if (partyVisible)
        {
            SetPartyPanelVisible(false);
            return;
        }

        ShowPartyOnly();
    }

    private void SetPartyPanelVisible(bool visible)
    {
        partyVisible = visible;
        if (dockRoot != null)
        {
            dockRoot.gameObject.SetActive(visible);
        }

        if (partyPanelRoot != null)
        {
            partyPanelRoot.gameObject.SetActive(visible);
        }

        Canvas.ForceUpdateCanvases();
    }

    private void BuildPartyDock()
    {
        if (partyPanelRoot == null || rootCanvas == null || campaignData == null)
        {
            return;
        }

        partyPanelRoot.gameObject.SetActive(true);
        foreach (Transform child in partyPanelRoot)
        {
            Destroy(child.gameObject);
        }

        var dragController = rootCanvas.GetComponent<PartyWorkbenchDragController>();
        if (dragController == null)
        {
            dragController = rootCanvas.gameObject.AddComponent<PartyWorkbenchDragController>();
        }
        dragController.Configure(this, rootCanvas);

        dockRoot = CreatePanel(partyPanelRoot, "WorkbenchDock", new Color(0.1f, 0.11f, 0.13f, 0.94f));
        dockRoot.anchorMin = new Vector2(1f, 0f);
        dockRoot.anchorMax = new Vector2(1f, 1f);
        dockRoot.pivot = new Vector2(1f, 0.5f);
        dockRoot.sizeDelta = new Vector2(560f, 0f);
        dockRoot.anchoredPosition = Vector2.zero;

        var vertical = dockRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(16, 16, 16, 16);
        vertical.spacing = 12f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;

        CreateHeader(dockRoot);
        CreateMemberSections(dockRoot);
        CreateStorageSection(dockRoot);
        CreateCatalogSection(dockRoot);
    }

    private void CreateHeader(Transform parent)
    {
        var header = CreatePanel(parent, "Header", new Color(0.15f, 0.17f, 0.21f, 1f));
        var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 10f;
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = true;
        layout.childForceExpandWidth = false;
        header.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;

        var title = CreateText(header, "Guild Hall", 26f, FontStyles.Bold);
        title.color = Color.white;
        title.gameObject.AddComponent<LayoutElement>().preferredWidth = 190f;

        selectedCharacterSummary = CreateText(header, string.Empty, 16f, FontStyles.Normal);
        selectedCharacterSummary.alignment = TextAlignmentOptions.MidlineLeft;
        selectedCharacterSummary.color = new Color(0.84f, 0.86f, 0.9f, 1f);
        selectedCharacterSummary.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private void CreateMemberSections(Transform parent)
    {
        var row = new GameObject("MemberRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.transform.SetParent(parent, false);
        var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 12f;
        rowLayout.childControlHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childForceExpandWidth = true;
        row.GetComponent<LayoutElement>().preferredHeight = 220f;

        guildRosterContent = CreateCharacterSection(row.transform, "Guild Roster", false);
        partyMembersContent = CreateCharacterSection(row.transform, campaignData.activeParty != null ? campaignData.activeParty.partyName : "Active Expedition", true);
    }

    private RectTransform CreateCharacterSection(Transform parent, string title, bool acceptsPartyMembers)
    {
        var section = CreatePanel(parent, title.Replace(" ", string.Empty), new Color(0.16f, 0.18f, 0.22f, 1f));
        section.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        var vertical = section.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(10, 10, 10, 10);
        vertical.spacing = 8f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;

        var heading = CreateText(section, title, 20f, FontStyles.Bold);
        heading.color = Color.white;
        heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        var viewport = CreateScrollViewport(section, out var content);
        viewport.gameObject.AddComponent<PartyWorkbenchCharacterListDropZone>().acceptsPartyMembers = acceptsPartyMembers;
        return content;
    }

    private void CreateStorageSection(Transform parent)
    {
        var section = CreatePanel(parent, "StorageSection", new Color(0.16f, 0.18f, 0.22f, 1f));
        var vertical = section.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(10, 10, 10, 10);
        vertical.spacing = 8f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;
        section.gameObject.AddComponent<LayoutElement>().preferredHeight = 170f;

        var heading = CreateText(section, "Shared Storage", 20f, FontStyles.Bold);
        heading.color = Color.white;

        var viewport = CreateScrollViewport(section, out var content);
        viewport.gameObject.AddComponent<PartyWorkbenchStorageDropZone>();
        storageContent = content;
    }

    private void CreateCatalogSection(Transform parent)
    {
        var section = CreatePanel(parent, "CatalogSection", new Color(0.16f, 0.18f, 0.22f, 1f));
        var vertical = section.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(10, 10, 10, 10);
        vertical.spacing = 8f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;
        section.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

        var heading = CreateText(section, "Item Catalog", 20f, FontStyles.Bold);
        heading.color = Color.white;

        var filterRow = new GameObject("CatalogFilters", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        filterRow.transform.SetParent(section, false);
        var filterLayout = filterRow.GetComponent<HorizontalLayoutGroup>();
        filterLayout.spacing = 6f;
        filterLayout.childControlWidth = false;
        filterLayout.childControlHeight = true;
        filterLayout.childForceExpandHeight = false;
        filterLayout.childForceExpandWidth = false;
        filterRow.GetComponent<LayoutElement>().preferredHeight = 34f;

        foreach (var filter in catalogFilters)
        {
            CreateFilterButton(filterRow.transform, filter);
        }

        CreateScrollViewport(section, out catalogContent);
    }

    private void CreateFilterButton(Transform parent, string filter)
    {
        var buttonObject = CreatePanel(parent, filter, filter == selectedCatalogFilter ? new Color(0.3f, 0.41f, 0.54f, 1f) : new Color(0.22f, 0.24f, 0.29f, 1f));
        var layout = buttonObject.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 86f;
        layout.preferredHeight = 30f;

        var button = buttonObject.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() =>
        {
            selectedCatalogFilter = filter;
            RefreshWorkbench();
        });

        var label = CreateText(buttonObject, filter, 14f, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
    }

    private RectTransform CreateScrollViewport(Transform parent, out RectTransform content)
    {
        var viewport = CreatePanel(parent, "Viewport", new Color(0.08f, 0.09f, 0.11f, 0.65f));
        viewport.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter)).GetComponent<RectTransform>();
        content.transform.SetParent(viewport, false);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = new Vector2(0f, 0f);
        content.offsetMax = new Vector2(0f, 0f);

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.viewport = viewport;
        scrollRect.content = content;

        return viewport;
    }

    private void RefreshWorkbench()
    {
        if (campaignData == null || campaignData.activeParty == null || campaignData.campaignInventory == null)
        {
            return;
        }

        if (selectedCharacterSummary != null && activeCharacter != null)
        {
            var life = activeCharacter.isAlive ? "Alive" : "Dead";
            selectedCharacterSummary.text = $"{activeCharacter.characterName}  {activeCharacter.knightRole}  {life}  {activeCharacter.currentStatus}";
        }

        RebuildCharacterList(guildRosterContent, campaignData.allCharacters.Where(character => !campaignData.activeParty.members.Contains(character)));
        RebuildCharacterList(partyMembersContent, campaignData.activeParty.members);
        RebuildStorageList();
        RebuildCatalogList();
        Canvas.ForceUpdateCanvases();
    }

    private void RebuildCharacterList(RectTransform content, IEnumerable<CharacterData> characters)
    {
        if (content == null)
        {
            return;
        }

        ClearChildren(content);
        foreach (var character in characters.Where(character => character != null))
        {
            var card = CreateCard(content, activeCharacter == character ? new Color(0.29f, 0.34f, 0.44f, 1f) : new Color(0.22f, 0.24f, 0.29f, 1f), 64f);
            var title = CreateText(card, character.characterName, 18f, FontStyles.Bold);
            title.color = Color.white;
            var detail = CreateText(card, string.Empty, 13f, FontStyles.Normal);
            detail.color = new Color(0.82f, 0.85f, 0.9f, 1f);
            card.gameObject.AddComponent<PartyWorkbenchCharacterEntryUI>()
                .Configure(this, character, campaignData.activeParty.members.Contains(character), title, detail);
        }
    }

    private void RebuildStorageList()
    {
        if (storageContent == null)
        {
            return;
        }

        ClearChildren(storageContent);
        foreach (var item in campaignData.campaignInventory.Items.Where(item => item != null && item.equipment != null))
        {
            CreateItemCard(storageContent, item.equipment, item, false);
        }
    }

    private void RebuildCatalogList()
    {
        if (catalogContent == null || equipmentLibrary == null)
        {
            return;
        }

        ClearChildren(catalogContent);
        foreach (var item in FilteredCatalogItems())
        {
            CreateItemCard(catalogContent, item, null, true);
        }
    }

    private IEnumerable<EquipmentData> FilteredCatalogItems()
    {
        var items = equipmentLibrary.items.Where(item => item != null);

        switch (selectedCatalogFilter)
        {
            case "Weapons":
                items = items.Where(item => item.displayCategory == "weapon" || item.displayCategory == "shield");
                break;
            case "Armor":
                items = items.Where(item =>
                    item.displayCategory == "armor" ||
                    item.displayCategory == "shield" ||
                    item.displayCategory == "headwear" ||
                    item.displayCategory == "waist");
                break;
            case "Utility":
                items = items.Where(item =>
                    item.displayCategory == "tool" ||
                    item.displayCategory == "remedy" ||
                    item.displayCategory == "poison" ||
                    item.displayCategory == "relic" ||
                    item.displayCategory == "trinket");
                break;
            case "Crown":
                items = items.Where(item => item.sourceTags != null && item.sourceTags.Contains("Crown"));
                break;
        }

        return items
            .OrderBy(item => item.displayCategory)
            .ThenBy(item => item.pointCost)
            .ThenBy(item => item.itemName);
    }

    private void CreateItemCard(RectTransform parent, EquipmentData equipment, EquipmentInstance instance, bool fromCatalog)
    {
        var card = CreateCard(parent, ColorForCategory(equipment.displayCategory), 62f);
        var title = CreateText(card, equipment.itemName, 17f, FontStyles.Bold);
        title.color = Color.white;

        var detail = CreateText(card, string.Empty, 12f, FontStyles.Normal);
        detail.color = new Color(0.85f, 0.88f, 0.92f, 1f);

        var entry = card.gameObject.AddComponent<PartyWorkbenchItemEntryUI>();
        entry.Configure(this, equipment, instance, fromCatalog, title, detail);
    }

    private RectTransform CreateCard(Transform parent, Color color, float preferredHeight)
    {
        var card = CreatePanel(parent, "Card", color);
        card.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;
        var vertical = card.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(10, 10, 8, 8);
        vertical.spacing = 2f;
        vertical.childControlHeight = true;
        vertical.childControlWidth = true;
        vertical.childForceExpandHeight = false;
        vertical.childForceExpandWidth = true;
        return card;
    }

    private RectTransform CreatePanel(Transform parent, string name, Color color)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = color;
        return panel;
    }

    private TextMeshProUGUI CreateText(Transform parent, string content, float fontSize, FontStyles style)
    {
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }

    private void ClearChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }

    private void RefreshAllViews()
    {
        if (creationManager != null)
        {
            creationManager.RecalculatePointsFromCharacterData();
        }

        if (inventoryGrid != null)
        {
            inventoryGrid.RefreshAllSlots();
        }

        if (skillsPanelManager != null)
        {
            skillsPanelManager.RefreshAllSlots();
        }

        SyncCharacterNameField();
        RefreshWorkbench();
        Canvas.ForceUpdateCanvases();
    }

    private void ShowPartyOnly()
    {
        if (characterSheetUI != null && characterSheetUI.IsOpen)
        {
            characterSheetUI.CloseSheet();
        }

        SetPartyPanelVisible(true);
        RefreshWorkbench();
    }

    private void ShowSheetOnly()
    {
        SetPartyPanelVisible(false);
        if (characterSheetUI != null && !characterSheetUI.IsOpen)
        {
            characterSheetUI.OpenSheet();
        }

        RefreshAllViews();
    }

    private void HidePartyAndSheet()
    {
        SetPartyPanelVisible(false);
    }

    private void PersistState(params Object[] objectsToPersist)
    {
#if UNITY_EDITOR
        foreach (var target in objectsToPersist)
        {
            if (target != null)
            {
                EditorUtility.SetDirty(target);
            }
        }

        AssetDatabase.SaveAssets();
#endif
    }

    private Color ColorForCategory(string category)
    {
        switch (category)
        {
            case "weapon":
                return new Color(0.35f, 0.21f, 0.21f, 1f);
            case "armor":
            case "shield":
            case "headwear":
            case "waist":
                return new Color(0.21f, 0.25f, 0.31f, 1f);
            case "tool":
            case "remedy":
            case "poison":
                return new Color(0.22f, 0.3f, 0.24f, 1f);
            default:
                return new Color(0.28f, 0.23f, 0.31f, 1f);
        }
    }
}
