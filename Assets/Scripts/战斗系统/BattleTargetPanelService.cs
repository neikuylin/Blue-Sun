using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal sealed class BattleTargetPanelService
{
    private const string TargetPanelPath = "Canvas/上方栏位/目标";
    private const string TargetHealthPanelPath = "Canvas/上方栏位/目标/生命值";
    private const string TargetHealthFillPath = "Canvas/上方栏位/目标/生命值/生命值";
    private const string TargetHealthTextPath = "Canvas/上方栏位/目标/生命值数字";
    private const string TargetNameTextPath = "Canvas/上方栏位/目标/名字/目标名字text";

    private readonly Color targetHealthBarColor;
    private readonly Color targetNameSelfColor;
    private readonly Color targetNameAllyColor;
    private readonly Color targetNameEnemyColor;

    private RectTransform targetPanelRect;
    private Image targetHealthFillImage;
    private TMP_Text targetHealthText;
    private TMP_Text targetNameText;
    private string lastTargetUiSignature = "<unset>";
    private RectSnapshot targetHealthFillBaseRect;
    private bool cachedTargetBaseRect;

    public BattleTargetPanelService(Color healthBarColor, Color selfColor, Color allyColor, Color enemyColor)
    {
        targetHealthBarColor = healthBarColor;
        targetNameSelfColor = selfColor;
        targetNameAllyColor = allyColor;
        targetNameEnemyColor = enemyColor;
    }

    public BattleUnit HoveredTargetUnit { get; private set; }

    public BattleUnit LockedTargetUnit { get; private set; }

    public void Refresh(
        BattleGrid grid,
        Camera battleCamera,
        BattleUnit activeUnit,
        BattleUnit hoveredSkillTarget,
        bool isExplorationMode,
        Action refreshSelectionOutlines)
    {
        if (isExplorationMode)
        {
            CacheTargetPanelReferences();
            lastTargetUiSignature = "<exploration>";
            ApplyTargetPanelUi(null, activeUnit, string.Empty, 0, 0, false);
            return;
        }

        CacheTargetPanelReferences();

        BattleUnit directHoveredUnit = ResolveHoveredPanelUnit(grid, battleCamera, hoveredSkillTarget);
        if (HoveredTargetUnit != directHoveredUnit)
        {
            HoveredTargetUnit = directHoveredUnit;
            refreshSelectionOutlines?.Invoke();
        }

        BattleUnit targetUnit = directHoveredUnit ?? ResolvePersistentTargetUnit(refreshSelectionOutlines);
        string targetId = targetUnit != null && targetUnit.IsAlive
            ? (string.IsNullOrWhiteSpace(targetUnit.characterId) ? targetUnit.unitName : targetUnit.characterId)
            : string.Empty;
        int currentHealth = targetUnit != null && targetUnit.IsAlive ? Mathf.Max(0, targetUnit.currentHealth) : 0;
        int maxHealth = targetUnit != null && targetUnit.IsAlive ? Mathf.Max(0, targetUnit.GetEffectiveMaxHealth()) : 0;
        string signature = string.Concat(targetId, "|", currentHealth, "/", maxHealth);
        if (string.Equals(signature, lastTargetUiSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastTargetUiSignature = signature;
        ApplyTargetPanelUi(targetUnit, activeUnit, targetId, currentHealth, maxHealth, targetUnit != null && targetUnit.IsAlive);
    }

    public void SetLockedTargetUnit(BattleUnit unit, bool isSkillModeActive, Action refreshSelectionOutlines, Action refreshHighlights)
    {
        if (isSkillModeActive)
        {
            return;
        }

        LockedTargetUnit = unit != null && unit.IsAlive ? unit : null;
        lastTargetUiSignature = "<unset>";
        refreshSelectionOutlines?.Invoke();
        refreshHighlights?.Invoke();
    }

    public void ClearLockedTargetUnit(Action refreshSelectionOutlines, Action refreshHighlights)
    {
        if (LockedTargetUnit == null)
        {
            return;
        }

        LockedTargetUnit = null;
        lastTargetUiSignature = "<unset>";
        refreshSelectionOutlines?.Invoke();
        refreshHighlights?.Invoke();
    }

    public void NotifyUnitRemoved(BattleUnit unit, Action refreshSelectionOutlines)
    {
        bool changed = false;

        if (LockedTargetUnit == unit)
        {
            LockedTargetUnit = null;
            changed = true;
        }

        if (HoveredTargetUnit == unit)
        {
            HoveredTargetUnit = null;
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        lastTargetUiSignature = "<unset>";
        refreshSelectionOutlines?.Invoke();
    }

    private BattleUnit ResolvePersistentTargetUnit(Action refreshSelectionOutlines)
    {
        if (LockedTargetUnit != null && LockedTargetUnit.IsAlive)
        {
            return LockedTargetUnit;
        }

        if (LockedTargetUnit != null && !LockedTargetUnit.IsAlive)
        {
            LockedTargetUnit = null;
            refreshSelectionOutlines?.Invoke();
        }

        return null;
    }

    private static BattleUnit ResolveHoveredPanelUnit(BattleGrid grid, Camera battleCamera, BattleUnit hoveredSkillTarget)
    {
        if (hoveredSkillTarget != null && hoveredSkillTarget.IsAlive)
        {
            return hoveredSkillTarget;
        }

        if (BattleInputService.IsPointerBlockedByUi() || grid == null || battleCamera == null)
        {
            return null;
        }

        Plane clickPlane = grid.GetInteractionPlane();
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);
        float enter;
        if (!clickPlane.Raycast(ray, out enter))
        {
            return null;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        Vector2Int hoveredCell = grid.WorldToCell(hitPoint);
        if (!grid.IsInside(hoveredCell))
        {
            return null;
        }

        BattleUnit unit = grid.GetUnitAt(hoveredCell);
        return unit != null && unit.IsAlive ? unit : null;
    }

    private void CacheTargetPanelReferences()
    {
        if (targetPanelRect == null)
        {
            targetPanelRect = SceneHierarchyPathUtility.FindInActiveScene(TargetPanelPath) as RectTransform;
        }

        if (targetHealthFillImage == null)
        {
            Transform fillTransform = SceneHierarchyPathUtility.FindInActiveScene(TargetHealthFillPath);
            targetHealthFillImage = fillTransform != null ? fillTransform.GetComponent<Image>() : null;
            if (targetHealthFillImage != null)
            {
                targetHealthFillImage.color = targetHealthBarColor;
                CacheTargetFillBaseRect();
            }
        }

        if (targetHealthText == null)
        {
            Transform textTransform = SceneHierarchyPathUtility.FindInActiveScene(TargetHealthTextPath);
            targetHealthText = textTransform != null ? textTransform.GetComponent<TMP_Text>() : null;
            if (targetHealthText == null)
            {
                targetHealthText = FindTargetHealthTextFallback();
            }
        }

        if (targetNameText == null)
        {
            Transform nameTransform = SceneHierarchyPathUtility.FindInActiveScene(TargetNameTextPath);
            targetNameText = nameTransform != null ? nameTransform.GetComponent<TMP_Text>() : null;
        }
    }

    private void CacheTargetFillBaseRect()
    {
        if (cachedTargetBaseRect || targetHealthFillImage == null || targetHealthFillImage.rectTransform == null)
        {
            return;
        }

        targetHealthFillBaseRect = CaptureSnapshot(targetHealthFillImage.rectTransform);
        cachedTargetBaseRect = true;
    }

    private TMP_Text FindTargetHealthTextFallback()
    {
        Transform panel = SceneHierarchyPathUtility.FindInActiveScene(TargetHealthPanelPath);
        if (panel == null)
        {
            return null;
        }

        TMP_Text existing = panel.GetComponentInChildren<TMP_Text>(true);
        if (existing != null)
        {
            return existing;
        }

        GameObject textObject = new GameObject("生命值数字", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(240f, 40f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 28f;
        text.fontStyle |= FontStyles.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    private void ApplyTargetPanelUi(BattleUnit targetUnit, BattleUnit activeUnit, string targetId, int currentHealth, int maxHealth, bool visible)
    {
        if (targetPanelRect != null)
        {
            targetPanelRect.gameObject.SetActive(visible);
        }

        if (targetNameText != null)
        {
            targetNameText.text = visible ? targetId : string.Empty;
            targetNameText.color = visible ? ResolveTargetNameColor(targetUnit, activeUnit) : targetNameSelfColor;
        }

        if (targetHealthText != null)
        {
            targetHealthText.text = visible ? currentHealth + "/" + maxHealth : string.Empty;
        }

        ApplyTargetHealthBar(currentHealth, maxHealth, visible);
    }

    private Color ResolveTargetNameColor(BattleUnit targetUnit, BattleUnit activeUnit)
    {
        if (targetUnit == null || activeUnit == null)
        {
            return targetNameSelfColor;
        }

        if (targetUnit == activeUnit)
        {
            return targetNameSelfColor;
        }

        return targetUnit.team == activeUnit.team ? targetNameAllyColor : targetNameEnemyColor;
    }

    private void ApplyTargetHealthBar(int current, int max, bool visible)
    {
        if (targetHealthFillImage == null)
        {
            return;
        }

        CacheTargetFillBaseRect();
        targetHealthFillImage.enabled = visible && max > 0;
        targetHealthFillImage.color = targetHealthBarColor;

        RectTransform rectTransform = targetHealthFillImage.rectTransform;
        if (rectTransform == null)
        {
            return;
        }

        ApplySnapshot(rectTransform, targetHealthFillBaseRect);
        if (!visible || max <= 0)
        {
            rectTransform.sizeDelta = new Vector2(0f, targetHealthFillBaseRect.sizeDelta.y);
            return;
        }

        float ratio = Mathf.Clamp01((float)current / max);
        float fullWidth = Mathf.Max(0f, targetHealthFillBaseRect.sizeDelta.x);
        float targetWidth = fullWidth * ratio;
        float widthDelta = fullWidth - targetWidth;
        rectTransform.sizeDelta = new Vector2(targetWidth, targetHealthFillBaseRect.sizeDelta.y);

        Vector2 anchoredPosition = targetHealthFillBaseRect.anchoredPosition;
        anchoredPosition.x -= widthDelta * 0.5f;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private static RectSnapshot CaptureSnapshot(RectTransform rectTransform)
    {
        return new RectSnapshot
        {
            anchoredPosition = rectTransform.anchoredPosition,
            sizeDelta = rectTransform.sizeDelta,
            offsetMin = rectTransform.offsetMin,
            offsetMax = rectTransform.offsetMax,
            pivot = rectTransform.pivot,
            anchorMin = rectTransform.anchorMin,
            anchorMax = rectTransform.anchorMax,
            localScale = rectTransform.localScale
        };
    }

    private static void ApplySnapshot(RectTransform rectTransform, RectSnapshot snapshot)
    {
        rectTransform.anchorMin = snapshot.anchorMin;
        rectTransform.anchorMax = snapshot.anchorMax;
        rectTransform.pivot = snapshot.pivot;
        rectTransform.offsetMin = snapshot.offsetMin;
        rectTransform.offsetMax = snapshot.offsetMax;
        rectTransform.sizeDelta = snapshot.sizeDelta;
        rectTransform.anchoredPosition = snapshot.anchoredPosition;
        rectTransform.localScale = snapshot.localScale;
    }

    private struct RectSnapshot
    {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 offsetMin;
        public Vector2 offsetMax;
        public Vector2 pivot;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector3 localScale;
    }
}
