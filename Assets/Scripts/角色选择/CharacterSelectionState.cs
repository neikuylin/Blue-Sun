using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CharacterSelectionState : MonoBehaviour
{
    [Serializable]
    public struct PortraitLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
    }

    [Serializable]
    public struct SlotSelection
    {
        public string slotName;
        public string characterId;
        public Sprite portraitSprite;
        public PortraitLayout portraitLayout;
        public bool isMainSlot;
        public bool isActiveSlot;
    }

    [Serializable]
    private struct GrantedSkillSnapshot
    {
        public string characterId;
        public List<string> skillIds;
    }

    [Serializable]
    private struct WeaponAttackPowerSnapshot
    {
        public string characterId;
        public float attackPower;
    }

    private static CharacterSelectionState instance;

    [SerializeField] private string activeCharacterId = string.Empty;
    [SerializeField] private List<SlotSelection> slotSelections = new List<SlotSelection>();
    [SerializeField] private List<GrantedSkillSnapshot> grantedSkillSnapshots = new List<GrantedSkillSnapshot>();
    [SerializeField] private List<WeaponAttackPowerSnapshot> weaponAttackPowerSnapshots = new List<WeaponAttackPowerSnapshot>();

    public static string ActiveCharacterId => instance != null ? instance.activeCharacterId : string.Empty;
    public static IReadOnlyList<SlotSelection> SlotSelections => instance != null ? instance.slotSelections : Array.Empty<SlotSelection>();

    public static IReadOnlyList<string> GetCapturedGrantedSkills(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return Array.Empty<string>();
        }

        for (int i = 0; i < instance.grantedSkillSnapshots.Count; i++)
        {
            GrantedSkillSnapshot snapshot = instance.grantedSkillSnapshots[i];
            if (string.Equals(snapshot.characterId, characterId, StringComparison.Ordinal))
            {
                return snapshot.skillIds ?? (IReadOnlyList<string>)Array.Empty<string>();
            }
        }

        return Array.Empty<string>();
    }

    public static float GetCapturedWeaponAttackPower(string characterId)
    {
        if (instance == null || string.IsNullOrWhiteSpace(characterId))
        {
            return 0f;
        }

        for (int i = 0; i < instance.weaponAttackPowerSnapshots.Count; i++)
        {
            WeaponAttackPowerSnapshot snapshot = instance.weaponAttackPowerSnapshots[i];
            if (string.Equals(snapshot.characterId, characterId, StringComparison.Ordinal))
            {
                return Mathf.Max(0f, snapshot.attackPower);
            }
        }

        return 0f;
    }

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
        if (!string.IsNullOrWhiteSpace(instance.activeCharacterId))
        {
            界面ID列表.设置当前ID(instance.activeCharacterId);
        }

        List<CharacterSlotView> orderedSlots = OrderSlots(slots);
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            CharacterSlotView slot = orderedSlots[i];
            if (slot == null)
            {
                continue;
            }

            instance.slotSelections.Add(new SlotSelection
            {
                slotName = slot.name,
                characterId = ResolveCharacterId(slot),
                portraitSprite = ResolvePortraitSprite(slot),
                portraitLayout = ResolvePortraitLayout(slot),
                isMainSlot = slot.isMainSlot,
                isActiveSlot = slot == activeSlot
            });
        }

        instance.CaptureGrantedSkills(orderedSlots);
        instance.CaptureWeaponAttackPower(orderedSlots);
    }

    public static void CaptureFromCurrentScene()
    {
        CharacterSlotView[] slots = FindObjectsOfType<CharacterSlotView>(true);
        CharacterSlotView activeSlot = 角色选择槽位服务.查找当前激活槽位(slots);
        UpdateSelections(slots, activeSlot);
    }

    public static string ResolveCharacterId(CharacterSlotView slot)
    {
        return 角色选择槽位服务.解析角色ID(slot);
    }

    public static Sprite ResolvePortraitSprite(CharacterSlotView slot)
    {
        return 角色选择槽位服务.解析立绘图片(slot);
    }

    public static PortraitLayout ResolvePortraitLayout(CharacterSlotView slot)
    {
        return 角色选择槽位服务.解析立绘布局(slot);
    }

    private static List<CharacterSlotView> OrderSlots(IEnumerable<CharacterSlotView> slots)
    {
        return 角色选择槽位服务.排序槽位(slots);
    }

    private void CaptureGrantedSkills(List<CharacterSlotView> orderedSlots)
    {
        角色选择快照服务.捕获授予技能快照(
            orderedSlots,
            grantedSkillSnapshots,
            ResolveCharacterId,
            GetCapturedGrantedSkillsFromInventory,
            (characterId, skillIds) => new GrantedSkillSnapshot
            {
                characterId = characterId,
                skillIds = skillIds
            });
    }

    private void CaptureWeaponAttackPower(List<CharacterSlotView> orderedSlots)
    {
        角色选择快照服务.捕获武器攻击力快照(
            orderedSlots,
            weaponAttackPowerSnapshots,
            ResolveCharacterId,
            InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower,
            (characterId, attackPower) => new WeaponAttackPowerSnapshot
            {
                characterId = characterId,
                attackPower = attackPower
            });
    }

    private static IReadOnlyList<string> GetCapturedGrantedSkillsFromInventory(string characterId)
    {
        List<string> grantedSkills = InventoryShortcutRuntimeBinder.GetGrantedSkillIdsForCharacter(characterId);
        return grantedSkills ?? (IReadOnlyList<string>)Array.Empty<string>();
    }
}
