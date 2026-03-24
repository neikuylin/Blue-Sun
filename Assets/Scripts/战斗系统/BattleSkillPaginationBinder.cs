using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleSkillPaginationBinder : MonoBehaviour
{
    private const int SkillsPerPage = 6;
    private const float SkillTooltipDelaySeconds = 0.5f;
    private const string DefaultCharacterId = "\u73a9\u5bb6";
    private const string SkillPatternName = "\u6280\u80fd\u56fe\u6848";
    private static readonly Color DisabledSkillColor = new Color32(100, 100, 100, 255);
    private static readonly Color EnabledSkillColor = Color.white;
    private const string SkillSlotContainerPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u680f\u4f4d/\u6280\u80fd\u683c\u5b50\u533a\u57df";
    private const string PreviousPageButtonPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u7ffb\u9875\u7cfb\u7edf/\u5f80\u524d\u7ffb\u9875";
    private const string NextPageButtonPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u7ffb\u9875\u7cfb\u7edf/\u5f80\u540e\u7ffb\u9875";
    private const string SpellCurrentPageTextPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u6570\u5b57\u663e\u793a/\u6cd5\u672f\u5f53\u524d\u9875";
    private const string SpellTotalPageTextPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u6280\u80fd\u9875\u7cfb\u7edf/\u6570\u5b57\u663e\u793a/\u603b\u9875";

    [Serializable]
    public struct SkillInstanceSnapshot
    {
        public int index;
        public string skillId;
        public string displayName;
        public string description;
        public string ownerCharacterId;
        public string source;
        public float damageMultiplier;
        public int damage;
        public bool isEmpty;
    }

    private sealed class SkillHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private BattleSkillPaginationBinder owner;
        private int index;

        public void Configure(BattleSkillPaginationBinder binder, int widgetIndex)
        {
            owner = binder;
            index = widgetIndex;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            owner?.HandleSkillPointerEnter(index, eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            owner?.HandleSkillPointerExit(index, eventData);
        }
    }

    private sealed class SkillButtonWidget
    {
        public Button button;
        public Image icon;
        public GameObject iconObject;
        public string skillId = string.Empty;
        public SkillHoverRelay hoverRelay;
    }

    private static BattleSkillPaginationBinder instance;
    private readonly List<SkillButtonWidget> widgets = new List<SkillButtonWidget>();
    private readonly List<UnityAction> widgetActions = new List<UnityAction>();
    private readonly List<SkillInstanceSnapshot> currentSkillSnapshots = new List<SkillInstanceSnapshot>();

    private BattleTurnSystem turnSystem;
    private BattleSkillDatabase skillDatabase;
    private BattleSceneBindings sceneBindings;
    private Button previousPageButton;
    private Button nextPageButton;
    private TMP_Text currentPageText;
    private TMP_Text totalPageText;
    private string currentCharacterId = string.Empty;
    private int currentPageIndex;
    private int lastTotalPages = -1;

    public void Initialize(BattleTurnSystem system)
    {
        instance = this;
        turnSystem = system;
        skillDatabase = BattleSkillDatabase.LoadDefault();
        sceneBindings = BattleSceneBindings.FindInActiveScene();
        CacheBindings();
        HookPaginationButtons();
        Refresh(force: true);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }

        UnhookPaginationButtons();
        UnhookSkillButtons();
        SkillTooltipRuntime.Hide();
    }

    public static List<SkillInstanceSnapshot> GetCurrentSkillSnapshots()
    {
        return instance != null
            ? new List<SkillInstanceSnapshot>(instance.currentSkillSnapshots)
            : new List<SkillInstanceSnapshot>();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || turnSystem == null)
        {
            return;
        }

        Refresh(force: false);
    }

    private void CacheBindings()
    {
        CacheSkillButtons();
        previousPageButton = sceneBindings != null && sceneBindings.previousSkillPageButton != null
            ? sceneBindings.previousSkillPageButton
            : FindButtonByPath(PreviousPageButtonPath);
        nextPageButton = sceneBindings != null && sceneBindings.nextSkillPageButton != null
            ? sceneBindings.nextSkillPageButton
            : FindButtonByPath(NextPageButtonPath);
        currentPageText = sceneBindings != null && sceneBindings.spellCurrentPageText != null
            ? sceneBindings.spellCurrentPageText
            : FindTextByPath(SpellCurrentPageTextPath);
        totalPageText = sceneBindings != null && sceneBindings.spellTotalPageText != null
            ? sceneBindings.spellTotalPageText
            : FindTextByPath(SpellTotalPageTextPath);
    }

    private void CacheSkillButtons()
    {
        if (widgets.Count > 0)
        {
            return;
        }

        List<Button> buttons = sceneBindings != null ? sceneBindings.skillPageButtons : null;
        List<Image> icons = sceneBindings != null ? sceneBindings.skillPageIcons : null;

        if (buttons != null && buttons.Count > 0)
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                Button button = buttons[i];
                if (button == null)
                {
                    continue;
                }

                Image icon = icons != null && i < icons.Count ? icons[i] : FindBestIcon(button);
                widgets.Add(new SkillButtonWidget
                {
                    button = button,
                    icon = icon,
                    iconObject = icon != null ? icon.gameObject : null
                });
            }
        }
        else
        {
            RectTransform container = FindRectTransformByPath(SkillSlotContainerPath);
            if (container != null)
            {
                for (int i = 0; i < container.childCount; i++)
                {
                    RectTransform child = container.GetChild(i) as RectTransform;
                    if (child == null)
                    {
                        continue;
                    }

                    Button button = child.GetComponent<Button>();
                    Image icon = FindBestIconInRoot(child);
                    if (button == null)
                    {
                        button = child.gameObject.AddComponent<Button>();
                    }

                    widgets.Add(new SkillButtonWidget
                    {
                        button = button,
                        icon = icon,
                        iconObject = icon != null ? icon.gameObject : null
                    });
                }
            }
        }

        HookSkillButtons();
    }

    private void HookPaginationButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(GoToPreviousPage);
            previousPageButton.onClick.AddListener(GoToPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(GoToNextPage);
            nextPageButton.onClick.AddListener(GoToNextPage);
        }
    }

    private void UnhookPaginationButtons()
    {
        if (previousPageButton != null)
        {
            previousPageButton.onClick.RemoveListener(GoToPreviousPage);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.RemoveListener(GoToNextPage);
        }
    }

    private void HookSkillButtons()
    {
        while (widgetActions.Count < widgets.Count)
        {
            widgetActions.Add(null);
        }

        for (int i = 0; i < widgets.Count; i++)
        {
            SkillButtonWidget widget = widgets[i];
            if (widget.button == null)
            {
                continue;
            }

            EnsureHoverRelay(widget, i);
            int capturedIndex = i;
            UnityAction action = widgetActions[i];
            if (action != null)
            {
                widget.button.onClick.RemoveListener(action);
            }

            action = () => OnSkillButtonClicked(capturedIndex);
            widgetActions[i] = action;
            widget.button.onClick.AddListener(action);
        }
    }

    private void UnhookSkillButtons()
    {
        for (int i = 0; i < widgets.Count; i++)
        {
            SkillButtonWidget widget = widgets[i];
            UnityAction action = i < widgetActions.Count ? widgetActions[i] : null;
            if (widget.button != null && action != null)
            {
                widget.button.onClick.RemoveListener(action);
            }
        }
    }

    private void Refresh(bool force)
    {
        string nextCharacterId = ResolveActiveCharacterId();
        List<string> allSkills = GetSkillsForCharacter(nextCharacterId);
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(allSkills.Count / (float)SkillsPerPage));
        bool characterChanged = !string.Equals(currentCharacterId, nextCharacterId, StringComparison.Ordinal);

        if (force || characterChanged)
        {
            currentCharacterId = nextCharacterId;
            currentPageIndex = 0;
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
        }

        currentPageIndex = Mathf.Clamp(currentPageIndex, 0, totalPages - 1);
        if (!force &&
            string.Equals(currentCharacterId, nextCharacterId, StringComparison.Ordinal) &&
            lastTotalPages == totalPages)
        {
            ApplySkillsToWidgets(allSkills, totalPages);
            return;
        }

        lastTotalPages = totalPages;
        ApplySkillsToWidgets(allSkills, totalPages);
    }

    private void ApplySkillsToWidgets(List<string> allSkills, int totalPages)
    {
        int startIndex = currentPageIndex * SkillsPerPage;
        currentSkillSnapshots.Clear();
        for (int i = 0; i < widgets.Count; i++)
        {
            SkillButtonWidget widget = widgets[i];
            string skillId = startIndex + i < allSkills.Count ? allSkills[startIndex + i] : string.Empty;
            widget.skillId = skillId;
            Sprite icon = ResolveSkillIcon(skillId);
            bool isUsable = IsSkillUsable(currentCharacterId, skillId);
            currentSkillSnapshots.Add(BuildSkillSnapshot(i, currentCharacterId, skillId));

            if (widget.icon != null)
            {
                widget.icon.sprite = icon;
                widget.icon.enabled = icon != null;
                widget.icon.raycastTarget = false;
                widget.icon.color = isUsable ? EnabledSkillColor : DisabledSkillColor;
            }

            if (widget.iconObject != null)
            {
                widget.iconObject.SetActive(icon != null);
            }

            if (widget.button != null)
            {
                widget.button.interactable = !string.IsNullOrWhiteSpace(skillId) && isUsable;
                Image buttonImage = widget.button.targetGraphic as Image;
                if (buttonImage != null)
                {
                    buttonImage.color = isUsable ? EnabledSkillColor : DisabledSkillColor;
                }
            }
        }

        if (currentPageText != null)
        {
            currentPageText.text = (currentPageIndex + 1).ToString();
        }

        if (totalPageText != null)
        {
            totalPageText.text = totalPages.ToString();
        }

        if (previousPageButton != null)
        {
            previousPageButton.interactable = currentPageIndex > 0;
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = currentPageIndex < totalPages - 1;
        }
    }

    private void GoToPreviousPage()
    {
        currentPageIndex = Mathf.Max(0, currentPageIndex - 1);
        HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
        Refresh(force: true);
    }

    private void GoToNextPage()
    {
        int totalPages = Mathf.Max(1, Mathf.CeilToInt(GetSkillsForCharacter(currentCharacterId).Count / (float)SkillsPerPage));
        currentPageIndex = Mathf.Min(totalPages - 1, currentPageIndex + 1);
        HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
        Refresh(force: true);
    }

    private void OnSkillButtonClicked(int index)
    {
        if (index < 0 || index >= widgets.Count || turnSystem == null)
        {
            return;
        }

        string skillId = widgets[index].skillId;
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return;
        }

        if (!IsSkillUsable(currentCharacterId, skillId))
        {
            return;
        }

        turnSystem.ToggleSkillMode(skillId);
    }

    private void HandleSkillPointerEnter(int index, PointerEventData eventData)
    {
        if (index < 0 || index >= currentSkillSnapshots.Count)
        {
            return;
        }

        SkillInstanceSnapshot snapshot = currentSkillSnapshots[index];
        if (snapshot.isEmpty)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        SkillButtonWidget widget = index >= 0 && index < widgets.Count ? widgets[index] : null;
        if (widget == null || widget.button == null)
        {
            return;
        }

        HoverTooltipController.BeginHover(
            HoverTooltipController.HoverCategory.Skill,
            widget.button.transform,
            SkillTooltipDelaySeconds,
            () => ShowSkillContent(snapshot),
            SkillTooltipRuntime.Hide);
    }

    private void HandleSkillPointerExit(int index, PointerEventData eventData)
    {
        SkillButtonWidget widget = index >= 0 && index < widgets.Count ? widgets[index] : null;
        if (widget == null || widget.button == null)
        {
            HoverTooltipController.Cancel(HoverTooltipController.HoverCategory.Skill, SkillTooltipRuntime.Hide);
            return;
        }

        Transform pointerTransform = eventData != null
            ? (eventData.pointerEnter != null ? eventData.pointerEnter.transform :
                eventData.pointerCurrentRaycast.gameObject != null ? eventData.pointerCurrentRaycast.gameObject.transform : null)
            : null;

        if (widget != null && widget.button != null && pointerTransform != null)
        {
            Transform buttonTransform = widget.button.transform;
            if (pointerTransform == buttonTransform || pointerTransform.IsChildOf(buttonTransform))
            {
                return;
            }
        }

        HoverTooltipController.EndHover(HoverTooltipController.HoverCategory.Skill, widget.button.transform, eventData);
    }

    private string ResolveActiveCharacterId()
    {
        BattleUnit activeUnit = turnSystem != null ? turnSystem.ActiveUnit : null;
        if (activeUnit != null && activeUnit.isPlayerControlled && !string.IsNullOrWhiteSpace(activeUnit.characterId))
        {
            return activeUnit.characterId;
        }

        return string.IsNullOrWhiteSpace(CharacterSelectionState.ActiveCharacterId)
            ? DefaultCharacterId
            : CharacterSelectionState.ActiveCharacterId;
    }

    private List<string> GetSkillsForCharacter(string characterId)
    {
        if (turnSystem != null && turnSystem.IsExplorationMode)
        {
            return new List<string>
            {
                BattleTurnSystem.ExplorationIdleSkillId,
                BattleTurnSystem.ExplorationMoveSkillId
            };
        }

        return CharacterSkillListUtility.BuildSkillIds(
            string.IsNullOrWhiteSpace(characterId) ? DefaultCharacterId : characterId);
    }

    private Sprite ResolveSkillIcon(string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return null;
        }

        if (string.Equals(skillId, BattleTurnSystem.ExplorationMoveSkillId, StringComparison.Ordinal))
        {
            BattleSkillDatabase.SkillEntry moveEntry = skillDatabase != null ? skillDatabase.FindEntry(BattleSkillDatabase.MoveSkillId) : null;
            return moveEntry != null ? moveEntry.icon : null;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        return entry != null ? entry.icon : null;
    }

    private SkillInstanceSnapshot BuildSkillSnapshot(int index, string ownerCharacterId, string skillId)
    {
        if (string.Equals(skillId, BattleTurnSystem.ExplorationIdleSkillId, StringComparison.Ordinal))
        {
            return new SkillInstanceSnapshot
            {
                index = index,
                skillId = skillId,
                displayName = skillId,
                description = "切换到探索待机状态。",
                ownerCharacterId = ownerCharacterId ?? string.Empty,
                source = "探索全局动作",
                damageMultiplier = 0f,
                damage = 0,
                isEmpty = false
            };
        }

        if (string.Equals(skillId, BattleTurnSystem.ExplorationMoveSkillId, StringComparison.Ordinal))
        {
            return new SkillInstanceSnapshot
            {
                index = index,
                skillId = skillId,
                displayName = skillId,
                description = "切换到探索移动状态。",
                ownerCharacterId = ownerCharacterId ?? string.Empty,
                source = "探索全局动作",
                damageMultiplier = 0f,
                damage = 0,
                isEmpty = false
            };
        }

        BattleSkillDatabase.SkillEntry entry = !string.IsNullOrWhiteSpace(skillId) && skillDatabase != null
            ? skillDatabase.FindEntry(skillId)
            : null;
        bool isCombatArt = entry != null && entry.group == BattleSkillDatabase.SkillGroup.CombatArt;
        float multiplier = entry != null ? Mathf.Max(0f, entry.damageMultiplier) : 0f;
        float attackPower = string.IsNullOrWhiteSpace(ownerCharacterId)
            ? 0f
            : InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(ownerCharacterId);

        return new SkillInstanceSnapshot
        {
            index = index,
            skillId = skillId ?? string.Empty,
            displayName = skillId ?? string.Empty,
            description = isCombatArt ? entry.description ?? string.Empty : string.Empty,
            ownerCharacterId = ownerCharacterId ?? string.Empty,
            source = ResolveSkillSourceDisplay(ownerCharacterId, skillId),
            damageMultiplier = isCombatArt ? multiplier : 0f,
            damage = isCombatArt ? Mathf.Max(0, Mathf.RoundToInt(attackPower * multiplier)) : 0,
            isEmpty = string.IsNullOrWhiteSpace(skillId) || !isCombatArt
        };
    }

    private bool IsSkillUsable(string ownerCharacterId, string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return false;
        }

        if (string.Equals(skillId, BattleTurnSystem.ExplorationIdleSkillId, StringComparison.Ordinal) ||
            string.Equals(skillId, BattleTurnSystem.ExplorationMoveSkillId, StringComparison.Ordinal))
        {
            return true;
        }

        if (skillDatabase == null)
        {
            skillDatabase = BattleSkillDatabase.LoadDefault();
        }

        BattleSkillDatabase.SkillEntry entry = skillDatabase != null ? skillDatabase.FindEntry(skillId) : null;
        if (entry == null)
        {
            return false;
        }

        if (entry.requiredWeaponCategories == null || entry.requiredWeaponCategories.Count == 0)
        {
            return true;
        }

        ItemDatabase.WeaponCategory equippedCategory = InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(ownerCharacterId);
        return entry.RequiresWeaponCategory(equippedCategory);
    }

    private static string ResolveSkillSourceDisplay(string ownerCharacterId, string skillId)
    {
        if (string.IsNullOrWhiteSpace(skillId))
        {
            return string.Empty;
        }

        string itemId = InventoryShortcutRuntimeBinder.GetGrantedSkillSourceItemIdForCharacter(ownerCharacterId, skillId);
        if (!string.IsNullOrWhiteSpace(itemId))
        {
            return InventoryShortcutRuntimeBinder.GetItemDisplayName(itemId);
        }

        CharacterSkillLoadoutDatabase loadoutDatabase = CharacterSkillLoadoutDatabase.LoadDefault();
        CharacterSkillLoadoutDatabase.CharacterSkillEntry entry =
            loadoutDatabase != null ? loadoutDatabase.FindEntry(ownerCharacterId) : null;
        if (entry != null && entry.skillIds != null)
        {
            for (int i = 0; i < entry.skillIds.Count; i++)
            {
                if (string.Equals(entry.skillIds[i], skillId, StringComparison.Ordinal))
                {
                    return "角色技能栏";
                }
            }
        }

        return string.Empty;
    }

    private void ShowSkillContent(SkillInstanceSnapshot snapshot)
    {
        SkillTooltipRuntime.Show(new SkillTooltipRuntime.Snapshot
        {
            skillId = snapshot.skillId,
            displayName = snapshot.displayName,
            description = snapshot.description,
            ownerCharacterId = snapshot.ownerCharacterId,
            damage = snapshot.damage,
            icon = ResolveSkillIcon(snapshot.skillId),
            isEmpty = snapshot.isEmpty
        });
    }

    private static void EnsureHoverRelay(SkillButtonWidget widget, int index)
    {
        if (widget == null || widget.button == null || instance == null)
        {
            return;
        }

        if (widget.hoverRelay == null)
        {
            widget.hoverRelay = widget.button.GetComponent<SkillHoverRelay>();
            if (widget.hoverRelay == null)
            {
                widget.hoverRelay = widget.button.gameObject.AddComponent<SkillHoverRelay>();
            }
        }

        widget.hoverRelay.Configure(instance, index);
    }

    private static Image FindBestIcon(Button button)
    {
        if (button == null)
        {
            return null;
        }

        Image[] images = button.GetComponentsInChildren<Image>(true);
        Image rootImage = button.GetComponent<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == rootImage)
            {
                continue;
            }

            return image;
        }

        return rootImage;
    }

    private static Image FindBestIconInRoot(RectTransform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform explicitPattern = SceneHierarchyPathUtility.FindDirectChildByName(root, SkillPatternName) ??
            FindDescendantByName(root, SkillPatternName);
        if (explicitPattern != null)
        {
            Image explicitImage = explicitPattern.GetComponent<Image>();
            if (explicitImage != null)
            {
                return explicitImage;
            }
        }

        Image[] images = root.GetComponentsInChildren<Image>(true);
        Image rootImage = root.GetComponent<Image>();
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image == rootImage)
            {
                continue;
            }

            return image;
        }

        return rootImage;
    }

    private static RectTransform FindRectTransformByPath(string path)
    {
        return FindTransformByPath(path) as RectTransform;
    }

    private static Button FindButtonByPath(string path)
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<Button>() : null;
    }

    private static TMP_Text FindTextByPath(string path)
    {
        Transform target = FindTransformByPath(path);
        return target != null ? target.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindTransformByPath(string path)
    {
        return SceneHierarchyPathUtility.FindInActiveScene(path);
    }

    private static Transform FindChildByName(Transform parent, string targetName)
    {
        return SceneHierarchyPathUtility.FindDirectChildByName(parent, targetName);
    }

    private static Transform FindDescendantByName(Transform parent, string targetName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(targetName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, targetName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            Transform nested = FindDescendantByName(child, targetName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
