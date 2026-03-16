using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillLoadoutRuntimeBinder : MonoBehaviour
{
    private const float SkillTooltipDelaySeconds = 0.5f;
    private static readonly string[] JourneySkillContainerChain =
    {
        "\u89d2\u8272\u9875\u9762",
        "\u6280\u80fd\u680f\u4f4d",
        "\u6280\u80fd\u683c\u5b50\u533a\u57df"
    };

    private const string OverlayIconName = "\u6280\u80fd\u56fe\u6848";
    private const string DefaultCharacterId = "\u73a9\u5bb6";

    private sealed class SkillSlotWidget
    {
        public RectTransform root;
        public Image skillIcon;
        public string skillId;
        public SkillHoverRelay hoverRelay;
    }

    private sealed class SkillHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private SkillLoadoutRuntimeBinder owner;
        private int index;

        public void Configure(SkillLoadoutRuntimeBinder binder, int slotIndex)
        {
            owner = binder;
            index = slotIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandleSkillPointerEnter(index);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandleSkillPointerExit(index, eventData);
        }
    }

    private static SkillLoadoutRuntimeBinder instance;

    private readonly List<SkillSlotWidget> journeySkillSlots = new List<SkillSlotWidget>();
    private BattleSkillDatabase skillDatabase;
    private string currentCharacterId = string.Empty;
    private int lastEquipmentSkillRevision = -1;
    private RectTransform journeySkillContainer;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(SkillLoadoutRuntimeBinder));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillLoadoutRuntimeBinder>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        journeySkillSlots.Clear();
    }

    private void Update()
    {
        string targetCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        int equipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        if (string.Equals(currentCharacterId, targetCharacterId, StringComparison.Ordinal) &&
            lastEquipmentSkillRevision == equipmentSkillRevision)
        {
            return;
        }

        currentCharacterId = targetCharacterId;
        lastEquipmentSkillRevision = equipmentSkillRevision;
        RefreshJourneySkillSlots();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        skillDatabase = BattleSkillDatabase.LoadDefault();
        journeySkillContainer = ResolveJourneySkillContainer();
        CollectJourneySkillSlots();
        currentCharacterId = ResolveCharacterId(CharacterSelectionState.ActiveCharacterId);
        lastEquipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        RefreshJourneySkillSlots();
    }

    private void CollectJourneySkillSlots()
    {
        journeySkillSlots.Clear();

        RectTransform container = journeySkillContainer != null ? journeySkillContainer : ResolveJourneySkillContainer();
        if (container == null)
        {
            return;
        }

        EnsureJourneyGridLayout(container);

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child = container.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            Image overlay = EnsureOverlayIcon(child);
            if (overlay == null)
            {
                continue;
            }

            journeySkillSlots.Add(new SkillSlotWidget
            {
                root = child,
                skillIcon = overlay
            });
        }
    }

    private static RectTransform ResolveJourneySkillContainer()
    {
        JourneySceneBindings bindings = JourneySceneBindings.FindInActiveScene();
        if (bindings != null && bindings.skillSlotContainer != null)
        {
            return bindings.skillSlotContainer;
        }

        return FindJourneySkillContainer();
    }

    private static RectTransform FindJourneySkillContainer()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid())
        {
            return null;
        }

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            RectTransform found = FindContainerRecursive(roots[i] != null ? roots[i].transform : null, 0);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static RectTransform FindContainerRecursive(Transform current, int matchedDepth)
    {
        if (current == null)
        {
            return null;
        }

        if (string.Equals(current.name, JourneySkillContainerChain[matchedDepth], StringComparison.Ordinal))
        {
            if (matchedDepth == JourneySkillContainerChain.Length - 1)
            {
                return current as RectTransform;
            }

            for (int i = 0; i < current.childCount; i++)
            {
                RectTransform nested = FindContainerRecursive(current.GetChild(i), matchedDepth + 1);
                if (nested != null)
                {
                    return nested;
                }
            }
        }

        for (int i = 0; i < current.childCount; i++)
        {
            RectTransform nested = FindContainerRecursive(current.GetChild(i), matchedDepth);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void EnsureJourneyGridLayout(RectTransform container)
    {
        if (container == null)
        {
            return;
        }

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null)
        {
            grid = container.gameObject.AddComponent<GridLayoutGroup>();
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;

            int childCount = Mathf.Max(1, container.childCount);
            int columnCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(childCount)));
            grid.constraintCount = columnCount;
        }
    }

    private void RefreshJourneySkillSlots()
    {
        if (journeySkillSlots.Count == 0)
        {
            return;
        }

        List<string> skillIds = BuildJourneySkillList(currentCharacterId);
        for (int i = 0; i < journeySkillSlots.Count; i++)
        {
            string skillId = i < skillIds.Count ? skillIds[i] : string.Empty;
            Sprite icon = ResolveSkillIcon(skillId);
            SkillSlotWidget widget = journeySkillSlots[i];
            widget.skillId = skillId;
            EnsureHoverRelay(widget, i);
            Image target = widget.skillIcon;
            if (target == null)
            {
                continue;
            }

            target.sprite = icon;
            target.enabled = icon != null;
            target.gameObject.SetActive(icon != null);
            target.raycastTarget = false;
        }
    }

    private List<string> BuildJourneySkillList(string characterId)
    {
        return CharacterSkillListUtility.BuildSkillIds(ResolveCharacterId(characterId));
    }

    private Sprite ResolveSkillIcon(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        return entry != null ? entry.icon : null;
    }

    private void HandleSkillPointerEnter(int index)
    {
        if (index < 0 || index >= journeySkillSlots.Count)
        {
            return;
        }

        SkillSlotWidget widget = journeySkillSlots[index];
        if (widget == null || string.IsNullOrWhiteSpace(widget.skillId))
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(widget.skillId) : null;
        if (entry == null || entry.group != BattleSkillDatabase.SkillGroup.CombatArt)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        float attackPower = InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(currentCharacterId);
        float multiplier = entry != null ? Mathf.Max(0f, entry.damageMultiplier) : 0f;
        SkillTooltipRuntime.Snapshot snapshot = new SkillTooltipRuntime.Snapshot
        {
            skillId = widget.skillId,
            displayName = widget.skillId,
            description = entry != null ? entry.description ?? string.Empty : string.Empty,
            ownerCharacterId = currentCharacterId ?? string.Empty,
            damage = Mathf.Max(0, Mathf.RoundToInt(attackPower * multiplier)),
            icon = entry != null ? entry.icon : null,
            isEmpty = false
        };
        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Skill,
            widget.root,
            SkillTooltipDelaySeconds,
            () => SkillTooltipRuntime.Show(snapshot),
            SkillTooltipRuntime.Hide);
    }

    private void HandleSkillPointerExit(int index, PointerEventData eventData)
    {
        SkillSlotWidget widget = index >= 0 && index < journeySkillSlots.Count ? journeySkillSlots[index] : null;
        if (widget == null || widget.root == null)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Skill, widget.root, eventData);
    }

    private static void EnsureHoverRelay(SkillSlotWidget widget, int index)
    {
        if (widget == null || widget.root == null || instance == null)
        {
            return;
        }

        if (widget.hoverRelay == null)
        {
            widget.hoverRelay = widget.root.GetComponent<SkillHoverRelay>();
            if (widget.hoverRelay == null)
            {
                widget.hoverRelay = widget.root.gameObject.AddComponent<SkillHoverRelay>();
            }
        }

        widget.hoverRelay.Configure(instance, index);
    }

    private static Image EnsureOverlayIcon(RectTransform slotRoot)
    {
        Transform existing = FindChildByName(slotRoot, OverlayIconName);
        if (existing == null)
        {
            GameObject iconObject = new GameObject(OverlayIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing = iconObject.transform;
            existing.SetParent(slotRoot, false);
        }

        RectTransform rect = existing as RectTransform;
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (rect == null || image == null)
        {
            return null;
        }

        if (existing.parent != slotRoot)
        {
            existing.SetParent(slotRoot, false);
        }

        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }

    private string ResolveCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
