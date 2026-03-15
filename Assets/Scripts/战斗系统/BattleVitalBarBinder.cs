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
            healthSlotImage = FindChildImage(panel, HealthSlotName);
            healthFillImage = FindChildImage(panel, HealthFillName);
            healthText = FindChildText(panel, HealthTextName);
            ConfigureFillImage(healthSlotImage, healthFillImage, HealthBarColor, fillFromRight: true);
        }

        if (manaSlotImage == null || manaFillImage == null || manaText == null)
        {
            Transform panel = FindTransformByPath(ManaPanelPath);
            manaSlotImage = FindChildImage(panel, ManaSlotName);
            manaFillImage = FindChildImage(panel, ManaFillName);
            manaText = FindChildText(panel, ManaTextName);
            ConfigureFillImage(manaSlotImage, manaFillImage, ManaBarColor, fillFromRight: false);
        }
    }

    private void Refresh(bool force)
    {
        CacheReferences();

        BattleUnit activeUnit = turnSystem.ActiveUnit;
        bool showVitals = activeUnit != null && activeUnit.IsAlive && activeUnit.isPlayerControlled;
        int currentHealth = showVitals ? Mathf.Max(0, activeUnit.currentHealth) : 0;
        int maxHealth = showVitals ? Mathf.Max(0, activeUnit.maxHealth) : 0;
        int currentMana = showVitals ? Mathf.Max(0, activeUnit.currentMana) : 0;
        int maxMana = showVitals ? Mathf.Max(0, activeUnit.maxMana) : 0;
        string unitId = showVitals ? activeUnit.characterId ?? string.Empty : string.Empty;
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

    private static void ApplyBar(Image fillImage, Image slotImage, int current, int max, bool fillFromRight)
    {
        if (fillImage == null)
        {
            return;
        }

        ConfigureFillImage(slotImage, fillImage, fillFromRight ? HealthBarColor : ManaBarColor, fillFromRight);
        fillImage.enabled = max > 0;
        fillImage.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
    }

    private static void ApplyText(TMP_Text text, int current, int max, bool visible)
    {
        if (text == null)
        {
            return;
        }

        text.text = visible ? current + "/" + max : string.Empty;
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
