using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
                if (snapshot.skillIds != null)
                {
                    return snapshot.skillIds;
                }

                return Array.Empty<string>();
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

    public static Sprite ResolvePortraitSprite(CharacterSlotView slot)
    {
        Image portraitImage = ResolvePortraitImage(slot, requireActive: true);
        if (portraitImage != null)
        {
            return portraitImage.sprite;
        }

        portraitImage = ResolvePortraitImage(slot, requireActive: false);
        return portraitImage != null ? portraitImage.sprite : null;
    }

    public static PortraitLayout ResolvePortraitLayout(CharacterSlotView slot)
    {
        PortraitLayout result = new PortraitLayout
        {
            anchorMin = new Vector2(0.5f, 0.5f),
            anchorMax = new Vector2(0.5f, 0.5f),
            pivot = new Vector2(0.5f, 0.5f),
            anchoredPosition = Vector2.zero,
            sizeDelta = Vector2.zero,
            localScale = Vector3.one
        };

        Image portraitImage = ResolvePortraitImage(slot, requireActive: true) ?? ResolvePortraitImage(slot, requireActive: false);
        if (portraitImage == null)
        {
            return result;
        }

        RectTransform rectTransform = portraitImage.rectTransform;
        if (rectTransform == null)
        {
            return result;
        }

        result.anchorMin = rectTransform.anchorMin;
        result.anchorMax = rectTransform.anchorMax;
        result.pivot = rectTransform.pivot;
        result.anchoredPosition = rectTransform.anchoredPosition;
        result.sizeDelta = rectTransform.sizeDelta;
        result.localScale = rectTransform.localScale;
        return result;
    }

    private static Image ResolvePortraitImage(CharacterSlotView slot, bool requireActive)
    {
        if (slot == null)
        {
            return null;
        }

        if (slot.portraitImage != null && slot.portraitImage.sprite != null)
        {
            if (!requireActive || slot.portraitImage.gameObject.activeInHierarchy)
            {
                return slot.portraitImage;
            }
        }

        Image[] childImages = slot.GetComponentsInChildren<Image>(true);
        return FindPreferredPortraitImage(childImages, requireActive);
    }

    private static Image FindPreferredPortraitImage(Image[] images, bool requireActive)
    {
        if (images == null)
        {
            return null;
        }

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
            {
                continue;
            }

            if (requireActive && !image.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (image.name.Contains("头像", StringComparison.Ordinal))
            {
                return image;
            }
        }

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
            {
                continue;
            }

            if (requireActive && !image.gameObject.activeInHierarchy)
            {
                continue;
            }

            return image;
        }

        return null;
    }

    private static List<CharacterSlotView> OrderSlots(IEnumerable<CharacterSlotView> slots)
    {
        List<CharacterSlotView> result = new List<CharacterSlotView>();
        if (slots == null)
        {
            return result;
        }

        foreach (CharacterSlotView slot in slots)
        {
            if (slot != null)
            {
                result.Add(slot);
            }
        }

        result.Sort(CompareSlotOrder);
        return result;
    }

    private void CaptureGrantedSkills(List<CharacterSlotView> orderedSlots)
    {
        grantedSkillSnapshots.Clear();
        if (orderedSlots == null)
        {
            return;
        }

        HashSet<string> seenCharacterIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            CharacterSlotView slot = orderedSlots[i];
            string characterId = ResolveCharacterId(slot);
            if (string.IsNullOrWhiteSpace(characterId) || !seenCharacterIds.Add(characterId))
            {
                continue;
            }

            List<string> grantedSkills = InventoryShortcutRuntimeBinder.GetGrantedSkillIdsForCharacter(characterId);
            grantedSkillSnapshots.Add(new GrantedSkillSnapshot
            {
                characterId = characterId,
                skillIds = grantedSkills != null ? new List<string>(grantedSkills) : new List<string>()
            });
        }
    }

    private void CaptureWeaponAttackPower(List<CharacterSlotView> orderedSlots)
    {
        weaponAttackPowerSnapshots.Clear();
        if (orderedSlots == null)
        {
            return;
        }

        HashSet<string> seenCharacterIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < orderedSlots.Count; i++)
        {
            CharacterSlotView slot = orderedSlots[i];
            string characterId = ResolveCharacterId(slot);
            if (string.IsNullOrWhiteSpace(characterId) || !seenCharacterIds.Add(characterId))
            {
                continue;
            }

            weaponAttackPowerSnapshots.Add(new WeaponAttackPowerSnapshot
            {
                characterId = characterId,
                attackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(characterId)
            });
        }
    }

    private static int CompareSlotOrder(CharacterSlotView left, CharacterSlotView right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        string leftPath = BuildHierarchyPath(left.transform);
        string rightPath = BuildHierarchyPath(right.transform);
        return string.Compare(leftPath, rightPath, StringComparison.Ordinal);
    }

    private static string BuildHierarchyPath(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        List<string> segments = new List<string>();
        Transform current = target;
        while (current != null)
        {
            segments.Add(current.GetSiblingIndex().ToString("D4") + "_" + current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }
}
