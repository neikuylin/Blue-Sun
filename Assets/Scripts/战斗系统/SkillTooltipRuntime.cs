using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class SkillTooltipRuntime : MonoBehaviour
{
    public struct Snapshot
    {
        public string skillId;
        public string displayName;
        public string description;
        public string ownerCharacterId;
        public int damage;
        public Sprite icon;
        public bool isEmpty;
    }

    private static SkillTooltipRuntime instance;

    private RectTransform tooltipRoot;
    private Image iconImage;
    private TMP_Text nameText;
    private TMP_Text damageText;
    private TMP_Text descriptionText;
    private TMP_Text ownerText;
    private GameObject runtimeTooltipInstance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(SkillTooltipRuntime));
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SkillTooltipRuntime>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void Show(Snapshot snapshot)
    {
        if (instance == null || snapshot.isEmpty)
        {
            return;
        }

        instance.ShowInternal(snapshot);
    }

    public static void Hide()
    {
        instance?.HideInternal();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        tooltipRoot = null;
        EnsureTooltipInstance();
        CacheTooltipBindings();
        HideInternal();
    }

    private void EnsureTooltipInstance()
    {
        if (runtimeTooltipInstance != null)
        {
            tooltipRoot = runtimeTooltipInstance.transform as RectTransform;
            return;
        }

        SkillTooltipPrefabDatabase database = SkillTooltipPrefabDatabase.LoadDefault();
        if (database == null || database.combatArtTooltipPrefab == null)
        {
            return;
        }

        Transform parent = FindTooltipParent();
        if (parent == null)
        {
            return;
        }

        runtimeTooltipInstance = Instantiate(database.combatArtTooltipPrefab, parent, false);
        runtimeTooltipInstance.name = "战技内容";
        runtimeTooltipInstance.SetActive(false);
        tooltipRoot = runtimeTooltipInstance.transform as RectTransform;
    }

    private void CacheTooltipBindings()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        iconImage = FindImageInRoot(tooltipRoot, "战技图标", "技能图标", "物品图标");
        Transform textRoot = FindChildByName(tooltipRoot, "文本区域") ?? FindDescendantByName(tooltipRoot, "文本区域");
        if (textRoot == null)
        {
            textRoot = tooltipRoot;
        }

        nameText = FindTextInRoot(textRoot, "战技名字", "技能名字");
        damageText = FindTextInRoot(textRoot, "战技伤害", "技能伤害");
        descriptionText = FindTextInRoot(textRoot, "战技描述", "技能描述");
        ownerText = FindTextInRoot(textRoot, "使用者", "战技使用者", "技能使用者");
    }

    private void ShowInternal(Snapshot snapshot)
    {
        EnsureTooltipInstance();
        CacheTooltipBindings();
        if (tooltipRoot == null)
        {
            return;
        }

        if (iconImage != null)
        {
            iconImage.sprite = snapshot.icon;
            iconImage.enabled = snapshot.icon != null;
        }

        if (nameText != null)
        {
            nameText.text = snapshot.displayName ?? string.Empty;
        }

        if (damageText != null)
        {
            damageText.text = $"战技伤害：{snapshot.damage}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = snapshot.description ?? string.Empty;
        }

        if (ownerText != null)
        {
            ownerText.text = $"使用者：{snapshot.ownerCharacterId}";
        }

        PositionTooltip();
        tooltipRoot.gameObject.SetActive(true);
        tooltipRoot.SetAsLastSibling();
    }

    private void HideInternal()
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.gameObject.SetActive(false);
        }
    }

    private void PositionTooltip()
    {
        if (tooltipRoot == null)
        {
            return;
        }

        RectTransform parentRect = tooltipRoot.parent as RectTransform;
        Canvas canvas = tooltipRoot.GetComponentInParent<Canvas>();
        Camera uiCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (parentRect != null &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, Input.mousePosition, uiCamera, out Vector2 localPoint))
        {
            Vector2 pivotOffset = new Vector2(
                tooltipRoot.rect.width * tooltipRoot.pivot.x,
                tooltipRoot.rect.height * tooltipRoot.pivot.y);
            tooltipRoot.anchoredPosition = localPoint + pivotOffset;
        }
    }

    private Transform FindTooltipParent()
    {
        Transform popupRoot = SceneHierarchyPathUtility.FindInActiveScene("Canvas/弹窗");
        if (popupRoot != null)
        {
            return popupRoot;
        }

        Transform canvasRoot = SceneHierarchyPathUtility.FindInActiveScene("Canvas");
        if (canvasRoot != null)
        {
            return canvasRoot;
        }

        Canvas canvas = FindObjectOfType<Canvas>(true);
        return canvas != null ? canvas.transform : null;
    }

    private static Image FindImageInRoot(Transform root, params string[] names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildByName(root, names[i]) ?? FindDescendantByName(root, names[i]);
            if (target == null)
            {
                continue;
            }

            Image image = target.GetComponent<Image>();
            if (image != null)
            {
                return image;
            }
        }

        return null;
    }

    private static TMP_Text FindTextInRoot(Transform root, params string[] names)
    {
        if (root == null || names == null)
        {
            return null;
        }

        for (int i = 0; i < names.Length; i++)
        {
            Transform target = FindChildByName(root, names[i]) ?? FindDescendantByName(root, names[i]);
            if (target == null)
            {
                continue;
            }

            TMP_Text text = target.GetComponent<TMP_Text>();
            if (text != null)
            {
                return text;
            }
        }

        return null;
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
