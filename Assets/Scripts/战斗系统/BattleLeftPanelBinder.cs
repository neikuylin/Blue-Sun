using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleLeftPanelBinder : MonoBehaviour
{
    private const string OverlayIconName = "\u6280\u80fd\u56fe\u6848";
    private const string LeftPanelPortraitPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d/\u89d2\u8272\u80cc\u666f\u6846\u5de6/\u89d2\u8272\u80cc\u666f\u6846\u7acb\u7ed8";
    private const string LeftPanelSkillPath = "Canvas/\u5f39\u7a97/\u5de6\u8fb9\u680f\u4f4d/\u6280\u80fd\u680f\u4f4d/\u6280\u80fd\u683c\u5b50\u533a\u57df";

    private sealed class SkillSlotWidget
    {
        public RectTransform root;
        public Image skillIcon;
        public string skillId;
        public SkillHoverRelay hoverRelay;
    }

    private sealed class SkillHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BattleLeftPanelBinder owner;
        private int index;

        public void Configure(BattleLeftPanelBinder binder, int slotIndex)
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

    private static BattleLeftPanelBinder instance;

    private readonly List<SkillSlotWidget> skillSlots = new List<SkillSlotWidget>();
    private BattleSkillDatabase skillDatabase;
    private BattleCharacterBindingDatabase characterBindingDatabase;
    private BattleSceneBindings battleBindings;
    private Image leftPanelPortraitImage;
    private RectTransform leftPanelPortraitAnchor;
    private RectTransform leftPanelSkillContainer;
    private string currentCharacterId = string.Empty;
    private int lastEquipmentSkillRevision = -1;
    private GameObject activePortraitPrefabInstance;
    private string activePortraitPrefabCharacterId = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(BattleLeftPanelBinder));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<BattleLeftPanelBinder>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        skillSlots.Clear();
    }

    private void Update()
    {
        string targetCharacterId = ResolveCharacterId();
        int equipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        if (string.Equals(currentCharacterId, targetCharacterId, StringComparison.Ordinal) &&
            lastEquipmentSkillRevision == equipmentSkillRevision)
        {
            return;
        }

        currentCharacterId = targetCharacterId;
        lastEquipmentSkillRevision = equipmentSkillRevision;
        RefreshLeftPanel();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        skillDatabase = BattleSkillDatabase.LoadDefault();
        characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        battleBindings = BattleSceneBindings.FindInActiveScene();
        leftPanelPortraitImage = ResolveLeftPanelPortrait();
        leftPanelPortraitAnchor = ResolveLeftPanelPortraitAnchor();
        leftPanelSkillContainer = ResolveLeftPanelSkillContainer();
        CollectSkillSlots();
        currentCharacterId = ResolveCharacterId();
        lastEquipmentSkillRevision = InventoryShortcutRuntimeBinder.EquipmentSkillRevision;
        RefreshLeftPanel();
    }

    private void CollectSkillSlots()
    {
        skillSlots.Clear();
        RectTransform container = leftPanelSkillContainer != null ? leftPanelSkillContainer : ResolveLeftPanelSkillContainer();
        if (container == null)
        {
            return;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform child = container.GetChild(i) as RectTransform;
            if (child == null)
            {
                continue;
            }

            Image icon = EnsureOverlayIcon(child);
            if (icon == null)
            {
                continue;
            }

            skillSlots.Add(new SkillSlotWidget
            {
                root = child,
                skillIcon = icon
            });
        }
    }

    private void RefreshLeftPanel()
    {
        RefreshPortrait();
        RefreshSkillSlots();
    }

    private void RefreshPortrait()
    {
        if (leftPanelPortraitImage == null)
        {
            leftPanelPortraitImage = ResolveLeftPanelPortrait();
        }

        if (leftPanelPortraitAnchor == null)
        {
            leftPanelPortraitAnchor = ResolveLeftPanelPortraitAnchor();
        }

        if (leftPanelPortraitImage == null && leftPanelPortraitAnchor == null)
        {
            return;
        }

        if (characterBindingDatabase == null)
        {
            characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        }

        if (TryShowBackgroundPortraitPrefab(currentCharacterId))
        {
            if (leftPanelPortraitImage != null)
            {
                leftPanelPortraitImage.enabled = false;
                leftPanelPortraitImage.color = new Color(1f, 1f, 1f, 0f);
            }

            return;
        }

        DestroyActivePortraitPrefabInstance();

        if (leftPanelPortraitImage == null)
        {
            return;
        }

        leftPanelPortraitImage.sprite = null;
        leftPanelPortraitImage.enabled = false;
        leftPanelPortraitImage.preserveAspect = true;
        leftPanelPortraitImage.color = new Color(1f, 1f, 1f, 0f);
        leftPanelPortraitImage.gameObject.SetActive(true);
    }

    private void RefreshSkillSlots()
    {
        if (skillSlots.Count == 0)
        {
            return;
        }

        List<string> skillIds = CharacterSkillListUtility.BuildSkillIds(currentCharacterId);
        for (int i = 0; i < skillSlots.Count; i++)
        {
            string skillId = i < skillIds.Count ? skillIds[i] : string.Empty;
            Sprite icon = ResolveSkillIcon(skillId);
            SkillSlotWidget widget = skillSlots[i];
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
        }
    }

    private void HandleSkillPointerEnter(int index)
    {
        if (index < 0 || index >= skillSlots.Count)
        {
            return;
        }

        SkillSlotWidget widget = skillSlots[index];
        if (widget == null || widget.root == null || string.IsNullOrWhiteSpace(widget.skillId))
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
        float multiplier = Mathf.Max(0f, entry.damageMultiplier);
        SkillTooltipRuntime.Snapshot snapshot = new SkillTooltipRuntime.Snapshot
        {
            skillId = widget.skillId,
            displayName = widget.skillId,
            description = entry.description ?? string.Empty,
            ownerCharacterId = currentCharacterId ?? string.Empty,
            damage = Mathf.Max(0, Mathf.RoundToInt(attackPower * multiplier)),
            icon = entry.icon,
            isEmpty = false
        };

        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Skill,
            widget.root,
            0.5f,
            () => SkillTooltipRuntime.Show(snapshot),
            SkillTooltipRuntime.Hide);
    }

    private void HandleSkillPointerExit(int index, PointerEventData eventData)
    {
        SkillSlotWidget widget = index >= 0 && index < skillSlots.Count ? skillSlots[index] : null;
        if (widget == null || widget.root == null)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Skill, widget.root, eventData);
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

    private Sprite ResolveBackgroundPortraitSprite(string characterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId) && characterBindingDatabase != null)
        {
            BattleCharacterBindingDatabase.BindingEntry binding = characterBindingDatabase.FindBinding(characterId);
            if (binding != null && binding.backgroundPortraitSprite != null)
            {
                return binding.backgroundPortraitSprite;
            }
        }

        return CharacterSelectionState.GetCapturedBackgroundPortraitSprite(characterId);
    }

    private bool TryShowBackgroundPortraitPrefab(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || characterBindingDatabase == null || leftPanelPortraitAnchor == null)
        {
            return false;
        }

        BattleCharacterBindingDatabase.BindingEntry binding = characterBindingDatabase.FindBinding(characterId);
        if (binding == null || binding.backgroundPortraitPrefab == null)
        {
            return false;
        }

        if (activePortraitPrefabInstance != null &&
            string.Equals(activePortraitPrefabCharacterId, characterId, StringComparison.Ordinal))
        {
            return true;
        }

        DestroyActivePortraitPrefabInstance();
        activePortraitPrefabInstance = Instantiate(binding.backgroundPortraitPrefab, leftPanelPortraitAnchor, false);
        activePortraitPrefabInstance.name = binding.backgroundPortraitPrefab.name;
        activePortraitPrefabInstance.SetActive(true);
        activePortraitPrefabCharacterId = characterId;
        return true;
    }

    private void DestroyActivePortraitPrefabInstance()
    {
        if (activePortraitPrefabInstance != null)
        {
            Destroy(activePortraitPrefabInstance);
            activePortraitPrefabInstance = null;
        }

        activePortraitPrefabCharacterId = string.Empty;
    }

    private string ResolveCharacterId()
    {
        string characterId = InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId;
        string source = "equipment";

        BattleTurnSystem battleTurnSystem = FindObjectOfType<BattleTurnSystem>(true);
        if (battleTurnSystem != null)
        {
            return string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId;
        }

        if (string.IsNullOrWhiteSpace(characterId))
        {
            characterId = CharacterSelectionState.ActiveCharacterId;
            source = "active-selection";
        }
        return characterId;
    }

    private Image ResolveLeftPanelPortrait()
    {
        if (battleBindings != null && battleBindings.leftPanelPortraitImage != null)
        {
            return battleBindings.leftPanelPortraitImage;
        }

        Transform target = SceneHierarchyPathUtility.FindInActiveScene(LeftPanelPortraitPath);
        if (target == null)
        {
            return null;
        }

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            return image;
        }

        return target.GetComponentInChildren<Image>(true);
    }

    private RectTransform ResolveLeftPanelPortraitAnchor()
    {
        Transform target = SceneHierarchyPathUtility.FindInActiveScene(LeftPanelPortraitPath);
        if (target is RectTransform targetRect)
        {
            return targetRect;
        }

        if (leftPanelPortraitImage != null)
        {
            return leftPanelPortraitImage.rectTransform;
        }

        return null;
    }

    private RectTransform ResolveLeftPanelSkillContainer()
    {
        if (battleBindings != null && battleBindings.leftPanelSkillSlotContainer != null)
        {
            return battleBindings.leftPanelSkillSlotContainer;
        }

        return SceneHierarchyPathUtility.FindInActiveScene(LeftPanelSkillPath) as RectTransform;
    }

    private static Image EnsureOverlayIcon(RectTransform slotRoot)
    {
        Transform existing = SceneHierarchyPathUtility.FindDirectChildByName(slotRoot, OverlayIconName);
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

    private static void EnsureHoverRelay(SkillSlotWidget widget, int index)
    {
        if (widget == null || widget.root == null)
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

    private void OnDestroy()
    {
        DestroyActivePortraitPrefabInstance();
    }
}
