using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using KnightsAndGM.Shared;

public enum ChunkColor
{
    None,
    Red,
    Green,
    Blue,
    Rainbow
}

public enum EquipLocation
{
    None,
    Head,
    Torso,
    Legs,
    Hands,
    Waist,
    Shield
}

public class InventoryGrid : MonoBehaviour
{
    [Header("References")]
    public CharacterData character;
    public List<Button> slotButtons; // 9 buttons (arranged left→right, top→bottom)
    public GameObject slotPrefab;    // optional prefab (if you instantiate dynamically)
    public Canvas rootCanvas;        // assign your root canvas here (required for drag icon)
    public GraphicRaycaster graphicRaycaster; // assign root canvas GraphicRaycaster
    public EventSystem eventSystem; // assign EventSystem in scene

    private void Start()
    {
        if (slotButtons.Count != 9)
            Debug.LogWarning("InventoryGrid expects 9 slot buttons.");

        // ensure character inventory has 9 slots
        if (character != null)
            character.EnsureInventorySlots();

        // Initialize each slot's InventorySlotUI with index + parent refs
        for (int i = 0; i < slotButtons.Count; i++)
        {
            int idx = i;
            var btn = slotButtons[i];
            // wire click
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSlotClicked(idx));

            // find or add InventorySlotUI
            var slotUI = btn.GetComponentInChildren<InventorySlotUI>();
            if (slotUI == null)
            {
                slotUI = btn.gameObject.AddComponent<InventorySlotUI>();
            }
            slotUI.Initialize(this, idx, rootCanvas);
            RefreshSlot(idx);
        }
        // initial chunk detect
        CheckAllChunks();
    }

    public void RefreshAllSlots()
    {
        for (int i = 0; i < slotButtons.Count; i++) RefreshSlot(i);
        CheckAllChunks();
    }

    public void RefreshSlot(int index)
    {
        if (character == null) return;
        var inst = character.inventory[index];
        var slotButton = slotButtons[index];

        InventorySlotUI slotUI = slotButton.GetComponentInChildren<InventorySlotUI>();
        if (slotUI != null)
        {
            slotUI.UpdateFromEquipment(inst != null ? inst.equipment : null);
        }
        else
        {
            var text = slotButton.GetComponentInChildren<Text>();
            if (text != null)
                text.text = inst == null || inst.equipment == null ? "Empty" : inst.equipment.itemName;
        }
    }

    private void OnSlotClicked(int index)
    {
        var inst = character.inventory[index];
        if (inst == null || inst.equipment == null)
        {
            Debug.Log($"Inventory slot {index} is empty.");
            return;
        }
        Debug.Log($"Inventory slot {index} clicked: {inst.equipment.itemName}");
    }

    public int GetSlotIndexAtPointer(PointerEventData eventData)
    {
        if (graphicRaycaster == null || eventSystem == null)
        {
            Debug.LogWarning("GraphicRaycaster or EventSystem not assigned on InventoryGrid.");
            return -1;
        }

        var results = new List<RaycastResult>();
        graphicRaycaster.Raycast(eventData, results);
        foreach (var r in results)
        {
            // check if RaycastResult corresponds to one of our slot buttons (or a child)
            for (int i = 0; i < slotButtons.Count; i++)
            {
                var btnGO = slotButtons[i].gameObject;
                if (r.gameObject == btnGO || r.gameObject.transform.IsChildOf(btnGO.transform))
                    return i;
            }
        }
        return -1;
    }

    // Swap two inventory slots
    public void SwapSlots(int a, int b)
    {
        if (a < 0 || b < 0 || a >= character.inventory.Count || b >= character.inventory.Count) return;
        if (a == b) return;

        var tmp = character.inventory[a];
        character.inventory[a] = character.inventory[b];
        character.inventory[b] = tmp;

        Debug.Log($"Swapped inventory slots {a} <-> {b}");
        RefreshSlot(a);
        RefreshSlot(b);
        CheckAllChunks();
    }

    // Grid helper: index -> (row,col)
    private void IndexToRC(int index, out int row, out int col) { row = index / 3; col = index % 3; }
    private int RCtoIndex(int row, int col) => row * 3 + col;

    public void CheckAllChunks()
    {
        if (character == null) return;
        Debug.Log("InventoryGrid: Checking chunks in grid.");
        for (int i = 0; i < 9; i++)
        {
            var left = character.inventory[i]?.equipment?.leftHalf ?? ChunkColor.None;
            var right = character.inventory[i]?.equipment?.rightHalf ?? ChunkColor.None;
            var top = character.inventory[i]?.equipment?.topHalf ?? ChunkColor.None;
            var bottom = character.inventory[i]?.equipment?.bottomHalf ?? ChunkColor.None;
            var center = character.inventory[i]?.equipment?.centerChunk ?? ChunkColor.None;

            int r, c; IndexToRC(i, out r, out c);

            if (right != ChunkColor.None && c < 2)
            {
                int neighborIndex = RCtoIndex(r, c + 1);
                var neighborLeft = character.inventory[neighborIndex]?.equipment?.leftHalf ?? ChunkColor.None;
                if (neighborLeft == right && neighborLeft != ChunkColor.None)
                    Debug.Log($"Chunk formed between slots {i} (right {right}) and {neighborIndex} (left {neighborLeft}) — color {right}");
            }
            if (bottom != ChunkColor.None && r < 2)
            {
                int neighborIndex = RCtoIndex(r + 1, c);
                var neighborTop = character.inventory[neighborIndex]?.equipment?.topHalf ?? ChunkColor.None;
                if (neighborTop == bottom && neighborTop != ChunkColor.None)
                    Debug.Log($"Chunk formed between slots {i} (bottom {bottom}) and {neighborIndex} (top {neighborTop}) — color {bottom}");
            }
            if (center != ChunkColor.None)
            {
                Debug.Log($"Slot {i} contains a center/full chunk of color {center}");
            }

            // also refresh UI text & visuals (safety in case something changed externally)
            RefreshSlot(i);
        }
    }

    public List<EquipmentData> GetActiveWeapons()
    {
        var list = new List<EquipmentData>();
        foreach (var slot in character.inventory)
        {
            if (slot != null && slot.equipment != null && slot.equipment.IsWeapon)
                list.Add(slot.equipment);
        }
        return list;
    }

    public int RollAllWeaponsDamage()
    {
        var weapons = GetActiveWeapons();
        if (weapons.Count == 0) return 0;

        int total = 0;
        var roller = new DiceRollerRandom();

        foreach (var w in weapons)
        {
            total += RollWeaponDamage(w, roller);
        }

        total += CountChunks().GetValueOrDefault(ChunkColor.Red, 0);
        total += GetVigorDamageBonus();

        return total;
    }


    public Dictionary<ChunkColor, int> CountChunks()
    {
        var portableCounts = InventoryStatCalculator.CountChunks(CharacterRulesAdapter.ToModel(character));
        var counts = new Dictionary<ChunkColor, int>();
        foreach (var portableCount in portableCounts)
        {
            counts[MapPortableChunk(portableCount.Key)] = portableCount.Value;
            Debug.Log($"Chunk count: {portableCount.Key} => {portableCount.Value}");
        }
        return counts;
    }

    // virtue-based bonuses
    public int GetVigorDamageBonus()
    {
        if (character == null) return 0;
        return InventoryStatCalculator.GetVigorDamageBonus(CharacterRulesAdapter.ToModel(character));
    }
    public int GetSpiritGuardBonus()
    {
        if (character == null) return 0;
        return InventoryStatCalculator.GetSpiritGuardBonus(CharacterRulesAdapter.ToModel(character));
    }
    public int GetClaritySaveModifier()
    {
        if (character == null) return 0;
        return InventoryStatCalculator.GetClaritySaveModifier(CharacterRulesAdapter.ToModel(character));
    }

    public int GetEffectiveArmorTotal()
    {
        int total = InventoryStatCalculator.GetEffectiveArmorTotal(CharacterRulesAdapter.ToModel(character));
        Debug.Log($"Effective armor total = {total}");
        return total;
    }


    public int SelectRandomAttritionSlot()
    {
        var validIndices = new List<int>();
        for (int i = 0; i < character.inventory.Count; i++)
            if (character.inventory[i] != null && character.inventory[i].equipment != null)
                validIndices.Add(i);

        if (validIndices.Count == 0) return -1;
        int pick = validIndices[UnityEngine.Random.Range(0, validIndices.Count)];
        Debug.Log($"Attrition: selected slot {pick} ({character.inventory[pick].equipment.itemName})");
        return pick;
    }

    public void ApplyAttritionDamage(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= character.inventory.Count) return;
        var inst = character.inventory[slotIndex];
        if (inst == null || inst.equipment == null) return;

        // Example: destroy item outright with some probability, or degrade it (customize)
        float destroyChance = 0.25f; // 25% destroy, adjust
        if (UnityEngine.Random.value <= destroyChance)
        {
            Debug.Log($"Attrition: destroying item {inst.equipment.itemName} in slot {slotIndex}");
            character.inventory[slotIndex] = null;
            RefreshSlot(slotIndex);
        }
        else
        {
            Debug.Log($"Attrition: item {inst.equipment.itemName} in slot {slotIndex} survived attrition.");
        }
    }

    public static int RollWeaponDamage(EquipmentData weapon, IDiceRoller roller)
    {
        return RollWeaponDamage(weapon, (count, sides) =>
        {
            roller.Roll(count, sides, out var rolledTotal);
            return rolledTotal;
        });
    }

    public static int RollWeaponDamage(EquipmentData weapon, Func<int, int, int> rollDice)
    {
        if (weapon == null || string.IsNullOrWhiteSpace(weapon.damageDiceNotation))
        {
            return 0;
        }

        if (!DiceNotationParser.TryParse(weapon.damageDiceNotation, out var parsed))
        {
            Debug.LogWarning($"InventoryGrid: invalid damage notation '{weapon.damageDiceNotation}' on {weapon.itemName}.");
            return 0;
        }

        var diceCount = parsed.Count;
        var flatBonus = parsed.Modifier;

        foreach (var trait in weapon.traits)
        {
            trait.ModifyDamageRoll(ref diceCount, ref flatBonus);
        }

        var rolledTotal = rollDice(diceCount, parsed.Sides);
        var total = rolledTotal + flatBonus;
        Debug.Log($"{weapon.itemName} rolled {diceCount}d{parsed.Sides} + {flatBonus} = {total}");
        return total;
    }

    public int ParseDiceCount(string notation)
    {
        if (!DiceNotationParser.TryParse(notation, out var parsed))
        {
            return 0;
        }

        return parsed.Count;
    }

    private ChunkColor MapPortableChunk(PortableChunkColor color)
    {
        switch (color)
        {
            case PortableChunkColor.Red:
                return ChunkColor.Red;
            case PortableChunkColor.Green:
                return ChunkColor.Green;
            case PortableChunkColor.Blue:
                return ChunkColor.Blue;
            case PortableChunkColor.Rainbow:
                return ChunkColor.Rainbow;
            default:
                return ChunkColor.None;
        }
    }
}
