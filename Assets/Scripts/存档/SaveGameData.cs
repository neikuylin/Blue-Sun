using System;
using System.Collections.Generic;

[Serializable]
public sealed class SaveGameData
{
    public int version = 1;
    public string savedAtUtc = string.Empty;
    public string currentSceneName = string.Empty;
    public CharacterSelectionSave characterSelection = new CharacterSelectionSave();
    public InventorySave inventory = new InventorySave();
    public SkillSave skills = new SkillSave();
    public EventSave events = new EventSave();
    public DungeonSave dungeon = new DungeonSave();

    [Serializable]
    public sealed class CharacterSelectionSave
    {
        public string activeCharacterId = string.Empty;
        public List<CharacterSlotSave> slots = new List<CharacterSlotSave>();
    }

    [Serializable]
    public sealed class CharacterSlotSave
    {
        public string slotName = string.Empty;
        public string characterId = string.Empty;
        public bool isMainSlot;
        public bool isActiveSlot;
    }

    [Serializable]
    public sealed class InventorySave
    {
        public int warehouseUsableSlotCount = -1;
        public int backpackUsableSlotCount = -1;
        public List<ItemSlotSave> warehouseSlots = new List<ItemSlotSave>();
        public List<ItemSlotSave> backpackSlots = new List<ItemSlotSave>();
        public List<CharacterEquipmentSave> equipmentByCharacter = new List<CharacterEquipmentSave>();
        public List<CharacterSlotCountSave> equipmentUsableSlotCounts = new List<CharacterSlotCountSave>();
    }

    [Serializable]
    public sealed class CharacterEquipmentSave
    {
        public string characterId = string.Empty;
        public List<ItemSlotSave> slots = new List<ItemSlotSave>();
    }

    [Serializable]
    public sealed class CharacterSlotCountSave
    {
        public string characterId = string.Empty;
        public int usableSlotCount = -1;
    }

    [Serializable]
    public sealed class ItemSlotSave
    {
        public string itemId = string.Empty;
        public int count;
        public int maxStack;
        public bool isRotated;
        public bool isFootprintExtension;
        public int primarySlotIndex = -1;
    }

    [Serializable]
    public sealed class SkillSave
    {
        public List<CharacterSkillSave> entries = new List<CharacterSkillSave>();
    }

    [Serializable]
    public sealed class CharacterSkillSave
    {
        public string characterId = string.Empty;
        public List<string> memorizedSkillIds = new List<string>();
        public List<int> memorizedSkillWeights = new List<int>();
        public List<string> warehouseSkillIds = new List<string>();
        public List<int> warehouseSkillWeights = new List<int>();
    }

    [Serializable]
    public sealed class EventSave
    {
        public List<EventStateSave> entries = new List<EventStateSave>();
    }

    [Serializable]
    public sealed class EventStateSave
    {
        public string eventId = string.Empty;
        public bool enabled;
    }

    [Serializable]
    public sealed class DungeonSave
    {
        public string currentDungeonTemplateId = string.Empty;
        public string currentDungeonNodeId = string.Empty;
        public List<string> clearedRoomKeys = new List<string>();
    }
}
