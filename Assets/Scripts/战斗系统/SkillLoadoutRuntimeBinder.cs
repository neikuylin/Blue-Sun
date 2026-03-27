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
    private const string GrantedCornerMarkerName = "__GrantedSkillCornerMarker";
    private const string DefaultCharacterId = "\u73a9\u5bb6";
    private static readonly Color DisabledSkillColor = SkillUsabilityUtility.DisabledSkillColor;
    private static readonly Color EnabledSkillColor = SkillUsabilityUtility.EnabledSkillColor;

    private sealed class SkillSlotWidget
    {
        public RectTransform root;
        public Image skillIcon;
        public Image grantedCornerMarker;
        public string skillId;
        public bool isGranted;
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
    private JourneySkillGridBinding journeySkillGridBinding;

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
        journeySkillGridBinding = JourneySkillGridBinding.FindBindingInActiveScene();
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
                skillIcon = overlay,
                grantedCornerMarker = EnsureGrantedCornerMarker(child)
            });
        }
    }

    private static RectTransform ResolveJourneySkillContainer()
    {
        RectTransform boundContainer = JourneySkillGridBinding.FindInActiveScene();
        if (boundContainer != null)
        {
            return boundContainer;
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

        List<CharacterSkillListUtility.DisplaySkillEntry> displayEntries = BuildJourneySkillEntries(currentCharacterId);
        int memorySlotCount = ResolveVisibleSkillMemorySlotCount(currentCharacterId);
        int grantedSkillCount = CountGrantedSkills(displayEntries);
        int visibleSlotCount = grantedSkillCount + memorySlotCount;
        for (int i = 0; i < journeySkillSlots.Count; i++)
        {
            SkillSlotWidget widget = journeySkillSlots[i];
            bool shouldDisplay = i < visibleSlotCount;
            if (widget.root != null && widget.root.gameObject.activeSelf != shouldDisplay)
            {
                widget.root.gameObject.SetActive(shouldDisplay);
            }

            CharacterSkillListUtility.DisplaySkillEntry displayEntry =
                shouldDisplay && i < displayEntries.Count
                    ? displayEntries[i]
                    : default;
            string skillId = shouldDisplay && i < displayEntries.Count ? displayEntry.SkillId : string.Empty;
            Sprite icon = shouldDisplay ? ResolveSkillIcon(skillId) : null;
            widget.skillId = skillId;
            widget.isGranted = shouldDisplay && i < displayEntries.Count && displayEntry.IsGranted;
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
            bool isUsable = SkillUsabilityUtility.IsSkillUsable(skillDatabase, currentCharacterId, skillId);
            target.color = ResolveSkillDisplayColor(widget.isGranted, isUsable);

            RefreshGrantedCornerMarker(widget);
        }
    }

    private List<CharacterSkillListUtility.DisplaySkillEntry> BuildJourneySkillEntries(string characterId)
    {
        return CharacterSkillListUtility.BuildDisplaySkillEntries(ResolveCharacterId(characterId));
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

    private Image EnsureGrantedCornerMarker(RectTransform slotRoot)
    {
        if (slotRoot == null)
        {
            return null;
        }

        Transform existing = FindChildByName(slotRoot, GrantedCornerMarkerName);
        if (existing == null)
        {
            GameObject markerObject = new GameObject(GrantedCornerMarkerName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            existing = markerObject.transform;
            existing.SetParent(slotRoot, false);
        }

        RectTransform rect = existing as RectTransform;
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (rect == null || image == null)
        {
            return null;
        }

        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.localScale = Vector3.one;

        image.raycastTarget = false;
        existing.SetAsLastSibling();
        return image;
    }

    private string ResolveCharacterId(string characterId)
    {
        return string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId;
    }

    private static int ResolveVisibleSkillMemorySlotCount(string characterId)
    {
        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry =
            statDatabase != null ? statDatabase.FindEntry(string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId) : null;

        return statEntry != null
            ? statEntry.ResolveSkillMemorySlots()
            : CharacterStatDatabase.StatEntry.BaseSkillMemorySlots;
    }

    private static int CountGrantedSkills(List<CharacterSkillListUtility.DisplaySkillEntry> displayEntries)
    {
        if (displayEntries == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < displayEntries.Count; i++)
        {
            if (displayEntries[i].IsGranted)
            {
                count++;
            }
        }

        return count;
    }

    private static Color ResolveSkillDisplayColor(bool isGranted, bool isUsable)
    {
        if (!isUsable)
        {
            return DisabledSkillColor;
        }

        return EnabledSkillColor;
    }

    private void RefreshGrantedCornerMarker(SkillSlotWidget widget)
    {
        if (widget == null || widget.grantedCornerMarker == null)
        {
            return;
        }

        Sprite cornerSprite = journeySkillGridBinding != null ? journeySkillGridBinding.grantedSkillCornerSprite : null;
        bool shouldShow = widget.isGranted && cornerSprite != null && !string.IsNullOrWhiteSpace(widget.skillId);

        widget.grantedCornerMarker.sprite = cornerSprite;
        widget.grantedCornerMarker.enabled = shouldShow;
        widget.grantedCornerMarker.gameObject.SetActive(shouldShow);

        RectTransform markerRect = widget.grantedCornerMarker.rectTransform;
        if (markerRect == null)
        {
            return;
        }

        Vector2 markerSize = journeySkillGridBinding != null
            ? journeySkillGridBinding.grantedSkillCornerSize
            : new Vector2(18f, 18f);
        Vector2 anchoredPosition = journeySkillGridBinding != null
            ? journeySkillGridBinding.grantedSkillCornerAnchoredPosition
            : new Vector2(-6f, -6f);

        markerRect.sizeDelta = markerSize;
        markerRect.anchoredPosition = anchoredPosition;
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
