using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleVitalBarBinder : MonoBehaviour
{
    private const string HealthPanelPath = "Canvas/下方栏位/角色操作栏/生命值面板";
    private const string ManaPanelPath = "Canvas/下方栏位/角色操作栏/魔法值面板";
    private const string HealthFillName = "当前生命值";
    private const string ManaFillName = "当前魔法值";
    private const string HealthSlotName = "生命槽位";
    private const string ManaSlotName = "魔法槽位";
    private const string HealthTextName = "生命值数字";
    private const string ManaTextName = "魔法值数字";

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

    public void Initialize(BattleTurnSystem system)
    {
        turnSystem = system;
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

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && string.Equals(roots[i].name, segments[0], StringComparison.Ordinal))
            {
                current = roots[i].transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
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
