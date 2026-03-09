using System;
using System.Collections.Generic;
using UnityEngine;

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

    [SerializeField] private string primaryCharacterId = string.Empty;
    [SerializeField] private string activeCharacterId = string.Empty;
    [SerializeField] private List<SlotSelection> slotSelections = new List<SlotSelection>();

    public static string PrimaryCharacterId => instance != null ? instance.primaryCharacterId : string.Empty;
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

    public static void UpdateSelections(IEnumerable<CharacterSlotView> slots, CharacterSlotView activeSlot, string playerCharacterId)
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
        instance.activeCharacterId = ResolveCharacterId(activeSlot, playerCharacterId);
        instance.primaryCharacterId = string.Empty;

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

            string characterId = ResolveCharacterId(slot, playerCharacterId);
            bool isActive = slot == activeSlot;

            instance.slotSelections.Add(new SlotSelection
            {
                slotName = slot.name,
                characterId = characterId,
                isMainSlot = slot.isMainSlot,
                isActiveSlot = isActive
            });

            if (string.IsNullOrEmpty(instance.primaryCharacterId) && !slot.isMainSlot && !string.IsNullOrEmpty(characterId))
            {
                instance.primaryCharacterId = characterId;
            }
        }

        if (string.IsNullOrEmpty(instance.primaryCharacterId))
        {
            instance.primaryCharacterId = instance.activeCharacterId;
        }
    }

    private static string ResolveCharacterId(CharacterSlotView slot, string playerCharacterId)
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

        if (slot.isMainSlot)
        {
            return playerCharacterId;
        }

        return string.Empty;
    }
}
