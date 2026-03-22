using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleVitalBarBinder : MonoBehaviour
{
    private const string HealthPanelPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u751f\u547d\u503c\u9762\u677f";
    private const string ManaPanelPath = "Canvas/\u4e0b\u65b9\u680f\u4f4d/\u89d2\u8272\u64cd\u4f5c\u680f/\u9b54\u6cd5\u503c\u9762\u677f";
    private const string HealthFillName = "\u5f53\u524d\u751f\u547d\u503c";
    private const string ManaFillName = "\u5f53\u524d\u9b54\u6cd5\u503c";
    private const string HealthSlotName = "\u751f\u547d\u69fd\u4f4d";
    private const string ManaSlotName = "\u9b54\u6cd5\u69fd\u4f4d";
    private const string HealthTextName = "\u751f\u547d\u503c\u6570\u5b57";
    private const string ManaTextName = "\u9b54\u6cd5\u503c\u6570\u5b57";

    private static readonly Color HealthBarColor = new Color(0.90f, 0.18f, 0.22f, 1f);
    private static readonly Color ManaBarColor = new Color(0.20f, 0.48f, 0.95f, 1f);

    private BattleTurnSystem turnSystem;
    private Image healthSlotImage;
    private Image manaSlotImage;
    private Image healthFillImage;
    private Image manaFillImage;
    private TMP_Text healthText;
    private TMP_Text manaText;
    private string lastSignature = string.Empty;
    private BattleSceneBindings battleBindings;
    private GameObject healthPanelObject;
    private GameObject manaPanelObject;
    private CanvasGroup healthPanelCanvasGroup;
    private CanvasGroup manaPanelCanvasGroup;
    private RectSnapshot healthFillBaseRect;
    private RectSnapshot manaFillBaseRect;
    private bool cachedBaseRects;

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

    public void Initialize(BattleTurnSystem system)
    {
        turnSystem = system;
        battleBindings = BattleSceneBindings.FindInActiveScene();
        CacheReferences();
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || turnSystem == null)
        {
            return;
        }

        Refresh(force: false);
    }

    private void CacheReferences()
    {
        if (battleBindings != null)
        {
            healthSlotImage = healthSlotImage != null ? healthSlotImage : battleBindings.healthSlotImage;
            healthFillImage = healthFillImage != null ? healthFillImage : battleBindings.healthFillImage;
            healthText = healthText != null ? healthText : battleBindings.healthText;
            manaSlotImage = manaSlotImage != null ? manaSlotImage : battleBindings.manaSlotImage;
            manaFillImage = manaFillImage != null ? manaFillImage : battleBindings.manaFillImage;
            manaText = manaText != null ? manaText : battleBindings.manaText;
        }

        if (healthSlotImage == null || healthFillImage == null || healthText == null)
        {
            Transform panel = FindTransformByPath(HealthPanelPath);
            healthPanelObject = panel != null ? panel.gameObject : healthPanelObject;
            healthSlotImage = FindChildImage(panel, HealthSlotName);
            healthFillImage = FindChildImage(panel, HealthFillName);
            healthText = FindChildText(panel, HealthTextName);
            ConfigureFillImage(healthSlotImage, healthFillImage, HealthBarColor, fillFromRight: true);
        }
        else if (healthPanelObject == null)
        {
            healthPanelObject = healthSlotImage.transform.parent != null ? healthSlotImage.transform.parent.gameObject : null;
        }

        if (manaSlotImage == null || manaFillImage == null || manaText == null)
        {
            Transform panel = FindTransformByPath(ManaPanelPath);
            manaPanelObject = panel != null ? panel.gameObject : manaPanelObject;
            manaSlotImage = FindChildImage(panel, ManaSlotName);
            manaFillImage = FindChildImage(panel, ManaFillName);
            manaText = FindChildText(panel, ManaTextName);
            ConfigureFillImage(manaSlotImage, manaFillImage, ManaBarColor, fillFromRight: false);
        }
        else if (manaPanelObject == null)
        {
            manaPanelObject = manaSlotImage.transform.parent != null ? manaSlotImage.transform.parent.gameObject : null;
        }

        CacheBaseRects();
    }

    private void Refresh(bool force)
    {
        CacheReferences();

        bool shouldShowForTurn = ShouldShowForCurrentTurn();
        SetPanelVisible(healthPanelObject, ref healthPanelCanvasGroup, shouldShowForTurn);
        SetPanelVisible(manaPanelObject, ref manaPanelCanvasGroup, shouldShowForTurn);

        BattleUnit displayedUnit = ResolveDisplayedUnit();
        bool showVitals = shouldShowForTurn && displayedUnit != null && displayedUnit.IsAlive;
        int currentHealth = showVitals ? Mathf.Max(0, displayedUnit.currentHealth) : 0;
        int maxHealth = showVitals ? Mathf.Max(0, displayedUnit.maxHealth) : 0;
        int currentMana = showVitals ? Mathf.Max(0, displayedUnit.currentMana) : 0;
        int maxMana = showVitals ? Mathf.Max(0, displayedUnit.maxMana) : 0;
        string unitId = showVitals ? displayedUnit.characterId ?? string.Empty : string.Empty;
        string signature = string.Concat(unitId, "|", currentHealth, "/", maxHealth, "|", currentMana, "/", maxMana, "|", showVitals);
        if (!force && string.Equals(signature, lastSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastSignature = signature;
        ApplyBar(healthFillImage, healthSlotImage, currentHealth, maxHealth, fillFromRight: true);
        ApplyBar(manaFillImage, manaSlotImage, currentMana, maxMana, fillFromRight: false);
        ApplyText(healthText, currentHealth, maxHealth, showVitals);
        ApplyText(manaText, currentMana, maxMana, showVitals);
    }

    private bool ShouldShowForCurrentTurn()
    {
        BattleUnit activeUnit = turnSystem != null ? turnSystem.ActiveUnit : null;
        return activeUnit != null &&
            activeUnit.IsAlive &&
            activeUnit.isPlayerControlled &&
            activeUnit.team == BattleTeam.Player;
    }

    private BattleUnit ResolveDisplayedUnit()
    {
        string equipmentCharacterId = InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId;
        if (!string.IsNullOrWhiteSpace(equipmentCharacterId))
        {
            BattleUnit equipmentUnit = FindBattleUnitByCharacterId(equipmentCharacterId);
            if (equipmentUnit != null)
            {
                return equipmentUnit;
            }
        }

        BattleUnit activeUnit = turnSystem != null ? turnSystem.ActiveUnit : null;
        if (activeUnit != null && activeUnit.IsAlive)
        {
            return activeUnit;
        }

        return null;
    }

    private static BattleUnit FindBattleUnitByCharacterId(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        BattleUnit[] units = FindObjectsOfType<BattleUnit>(true);
        BattleUnit fallback = null;
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !string.Equals(unit.characterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            if (unit.gameObject.activeInHierarchy && unit.IsAlive)
            {
                return unit;
            }

            if (fallback == null)
            {
                fallback = unit;
            }
        }

        return fallback;
    }

    private static void ConfigureFillImage(Image slotImage, Image fillImage, Color color, bool fillFromRight)
    {
        if (fillImage == null)
        {
            return;
        }

        fillImage.color = color;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = fillFromRight ? 1 : 0;
        fillImage.fillClockwise = false;
        fillImage.preserveAspect = false;
    }

    private void ApplyBar(Image fillImage, Image slotImage, int current, int max, bool fillFromRight)
    {
        if (fillImage == null)
        {
            return;
        }

        ConfigureFillImage(slotImage, fillImage, fillFromRight ? HealthBarColor : ManaBarColor, fillFromRight);
        CacheBaseRects();
        fillImage.enabled = max > 0;
        float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
        fillImage.fillAmount = ratio;

        RectTransform rectTransform = fillImage.rectTransform;
        if (rectTransform == null)
        {
            return;
        }

        RectSnapshot snapshot = fillImage == healthFillImage ? healthFillBaseRect : manaFillBaseRect;
        ApplySnapshot(rectTransform, snapshot);

        float fullWidth = Mathf.Max(0f, snapshot.sizeDelta.x);
        float targetWidth = fullWidth * ratio;
        float widthDelta = fullWidth - targetWidth;
        rectTransform.sizeDelta = new Vector2(targetWidth, snapshot.sizeDelta.y);

        Vector2 anchoredPosition = snapshot.anchoredPosition;
        anchoredPosition.x += fillFromRight ? -widthDelta * 0.5f : widthDelta * 0.5f;
        rectTransform.anchoredPosition = anchoredPosition;
    }

    private void CacheBaseRects()
    {
        if (cachedBaseRects)
        {
            return;
        }

        if (healthFillImage != null)
        {
            healthFillBaseRect = CaptureSnapshot(healthFillImage.rectTransform);
        }

        if (manaFillImage != null)
        {
            manaFillBaseRect = CaptureSnapshot(manaFillImage.rectTransform);
        }

        cachedBaseRects = healthFillImage != null || manaFillImage != null;
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

    private static void ApplyText(TMP_Text text, int current, int max, bool visible)
    {
        if (text == null)
        {
            return;
        }

        text.text = visible ? current + "/" + max : string.Empty;
    }

    private static void SetPanelVisible(GameObject panelObject, ref CanvasGroup canvasGroup, bool visible)
    {
        if (panelObject == null)
        {
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = panelObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelObject.AddComponent<CanvasGroup>();
            }
        }

        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = visible;
        canvasGroup.blocksRaycasts = visible;
    }

    private static Image FindChildImage(Transform parent, string childName)
    {
        Transform child = FindChildByName(parent, childName);
        return child != null ? child.GetComponent<Image>() : null;
    }

    private static TMP_Text FindChildText(Transform parent, string childName)
    {
        Transform child = FindChildByName(parent, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        return SceneHierarchyPathUtility.FindDirectChildByName(parent, childName);
    }

    private static Transform FindTransformByPath(string path)
    {
        return SceneHierarchyPathUtility.FindInActiveScene(path);
    }
}
