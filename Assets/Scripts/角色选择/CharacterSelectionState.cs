using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CharacterSelectionState : MonoBehaviour
{
    [Serializable]
    public struct SlotSelection
    {
        public string slotName;
        public string characterId;
        public bool isMainSlot;
        public bool isActiveSlot;
    }

    private static CharacterSelectionState instance;

    [SerializeField] private string activeCharacterId = string.Empty;
    [SerializeField] private List<SlotSelection> slotSelections = new List<SlotSelection>();

    public static string ActiveCharacterId => instance != null ? instance.activeCharacterId : string.Empty;
    public static IReadOnlyList<SlotSelection> SlotSelections => instance != null ? instance.slotSelections : Array.Empty<SlotSelection>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("CharacterSelectionState");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<CharacterSelectionState>();
    }

    public static void UpdateSelections(IEnumerable<CharacterSlotView> slots, CharacterSlotView activeSlot)
    {
        if (instance == null)
        {
            Bootstrap();
        }

        if (instance == null)
        {
            return;
        }

        instance.slotSelections.Clear();
        instance.activeCharacterId = ResolveCharacterId(activeSlot);

        if (slots == null)
        {
            return;
        }

        foreach (CharacterSlotView slot in slots)
        {
            if (slot == null)
            {
                continue;
            }

            instance.slotSelections.Add(new SlotSelection
            {
                slotName = slot.name,
                characterId = ResolveCharacterId(slot),
                isMainSlot = slot.isMainSlot,
                isActiveSlot = slot == activeSlot
            });
        }
    }

    public static void CaptureFromCurrentScene()
    {
        CharacterSlotView[] slots = FindObjectsOfType<CharacterSlotView>(true);
        CharacterSlotView activeSlot = null;

        for (int i = 0; i < slots.Length; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            for (int j = 0; j < slot.selectToggles.Count; j++)
            {
                Toggle toggle = slot.selectToggles[j];
                if (toggle != null && toggle.isOn)
                {
                    activeSlot = slot;
                    break;
                }
            }

            if (activeSlot != null)
            {
                break;
            }
        }

        UpdateSelections(slots, activeSlot);
    }

    public static string ResolveCharacterId(CharacterSlotView slot)
    {
        if (slot == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(slot.selectedCharacterId))
        {
            return slot.selectedCharacterId;
        }

        if (!string.IsNullOrEmpty(slot.slotCharacterId))
        {
            return slot.slotCharacterId;
        }

        return string.Empty;
    }
}
