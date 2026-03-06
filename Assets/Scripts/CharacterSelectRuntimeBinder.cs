using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectRuntimeBinder : MonoBehaviour
{
    private const string SlotRootName = "玩家栏位按钮";
    private const string AvatarButtonName = "头像按钮";
    private const string UnselectedName = "未选择图片";
    private const string PortraitDisplayName = "角色头像显示";
    private const string PortraitReferenceRootName = "栏位引用头像";
    private const string CharacterPanelName = "角色选择";

    private const string IdSolana = "solana";
    private const string IdKulus = "kulus";
    private const string IdSesha = "sesha";
    private const string IdHuman = "human";

    private static readonly Color AvailableColor = Color.white;
    private static readonly Color OccupiedColor = new Color(100f / 255f, 100f / 255f, 100f / 255f, 1f);

    private static readonly Dictionary<string, string[]> CharacterNameAliases = new Dictionary<string, string[]>
    {
        { IdSolana, new[] { "索拉娜头像", "索拉娜", "选择索拉娜头像", "选择索拉娜" } },
        { IdKulus, new[] { "库鲁斯头像", "库鲁斯", "选择库鲁斯头像", "选择库鲁斯" } },
        { IdSesha, new[] { "瑟莎头像", "瑟莎", "选择瑟莎头像", "选择瑟莎" } },
        { IdHuman, new[] { "人类头像（暂用爱丽丝）", "人类（暂用爱丽丝）", "选择人类头像", "选择人类" } },
    };

    private readonly List<SlotInfo> slots = new List<SlotInfo>();
    private readonly Dictionary<string, Sprite> portraitLibrary = new Dictionary<string, Sprite>();
    private readonly Dictionary<string, PortraitLayout> portraitLayoutLibrary = new Dictionary<string, PortraitLayout>();
    private readonly List<AvatarVisualRef> avatarVisuals = new List<AvatarVisualRef>();

    private SlotInfo currentSlot;

    private struct PortraitLayout
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;
    }

    private class SlotInfo
    {
        public List<Button> selectButtons = new List<Button>();
        public GameObject unselectedObject;
        public Image portraitImage;
        public string selectedCharacterId;
    }

    private class AvatarVisualRef
    {
        public string characterId;
        public Image[] images;
        public Color[] originalColors;
        public bool keepOriginalColor;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindObjectOfType<CharacterSelectRuntimeBinder>() != null)
        {
            return;
        }

        GameObject go = new GameObject("CharacterSelectRuntimeBinder");
        DontDestroyOnLoad(go);
        go.AddComponent<CharacterSelectRuntimeBinder>();
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        slots.Clear();
        portraitLibrary.Clear();
        portraitLayoutLibrary.Clear();
        avatarVisuals.Clear();
        currentSlot = null;

        Transform[] allTransforms = FindObjectsOfType<Transform>(true);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            if (!IsNumberedSlotRoot(tr.name))
            {
                continue;
            }

            SlotInfo slot = BuildSlotInfo(tr);
            if (slot == null || slot.portraitImage == null || slot.selectButtons.Count == 0)
            {
                continue;
            }

            slots.Add(slot);
            SlotInfo capturedSlot = slot;
            for (int b = 0; b < slot.selectButtons.Count; b++)
            {
                slot.selectButtons[b].onClick.AddListener(() => SetCurrentSlot(capturedSlot));
            }
        }

        BuildPortraitLibrary();
        BindAvatarSelectors(allTransforms);

        if (slots.Count > 0)
        {
            SetCurrentSlot(slots[0]);
        }

        Debug.Log($"CharacterSelectRuntimeBinder slots={slots.Count}, portraits={portraitLibrary.Count}, layouts={portraitLayoutLibrary.Count}");
    }

    private void SetCurrentSlot(SlotInfo slot)
    {
        currentSlot = slot;
        RefreshAvatarAvailabilityVisuals();
    }

    private void BindAvatarSelectors(Transform[] allTransforms)
    {
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            if (!IsAvatarButton(tr.name))
            {
                continue;
            }

            string characterId = ResolveCharacterId(tr.name);
            if (string.IsNullOrEmpty(characterId))
            {
                continue;
            }

            Image[] allImages = tr.GetComponentsInChildren<Image>(true);
            List<Image> childImages = new List<Image>();
            for (int ci = 0; ci < allImages.Length; ci++)
            {
                if (allImages[ci] == null)
                {
                    continue;
                }

                // Do not recolor the avatar button's own Image, only child images.
                if (allImages[ci].gameObject == tr.gameObject)
                {
                    continue;
                }

                childImages.Add(allImages[ci]);
            }

            if (childImages.Count > 0)
            {
                Image[] visualImages = childImages.ToArray();
                Color[] originals = new Color[visualImages.Length];
                for (int ci = 0; ci < visualImages.Length; ci++)
                {
                    originals[ci] = visualImages[ci].color;
                }

                avatarVisuals.Add(new AvatarVisualRef
                {
                    characterId = characterId,
                    images = visualImages,
                    originalColors = originals,
                    // 头像按钮1是不可调用项，保持它本身的暗色，不参与占用变暗逻辑。
                    keepOriginalColor = tr.name.EndsWith("1"),
                });
            }

            Button button = tr.GetComponent<Button>();
            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                string capturedId = characterId;
                button.onClick.AddListener(() => TryAssignCurrentSlot(capturedId));
            }

            Toggle toggle = tr.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.RemoveAllListeners();
                string capturedId = characterId;
                toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        TryAssignCurrentSlot(capturedId);
                    }
                });
            }
        }

        RefreshAvatarAvailabilityVisuals();
    }

    private void RefreshAvatarAvailabilityVisuals()
    {
        for (int i = 0; i < avatarVisuals.Count; i++)
        {
            AvatarVisualRef v = avatarVisuals[i];
            if (v == null || v.images == null || v.images.Length == 0)
            {
                continue;
            }

            bool occupiedByOther = IsCharacterUsedByOtherSlot(v.characterId, currentSlot);
            Color targetColor = occupiedByOther ? OccupiedColor : AvailableColor;
            for (int j = 0; j < v.images.Length; j++)
            {
                if (v.images[j] == null)
                {
                    continue;
                }

                if (v.keepOriginalColor && v.originalColors != null && j < v.originalColors.Length)
                {
                    v.images[j].color = v.originalColors[j];
                    continue;
                }

                v.images[j].color = targetColor;
            }
        }
    }

    private static bool IsNumberedSlotRoot(string name)
    {
        return name.StartsWith(SlotRootName + " (") && name.EndsWith(")");
    }

    private static bool IsAvatarButton(string name)
    {
        return name.StartsWith(AvatarButtonName);
    }

    private static SlotInfo BuildSlotInfo(Transform slotRoot)
    {
        SlotInfo info = new SlotInfo();

        Transform unselected = FindChildByName(slotRoot, UnselectedName);
        if (unselected != null)
        {
            info.unselectedObject = unselected.gameObject;

            Button[] unselectedButtons = unselected.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < unselectedButtons.Length; i++)
            {
                if (unselectedButtons[i].name.StartsWith(AvatarButtonName))
                {
                    continue;
                }

                info.selectButtons.Add(unselectedButtons[i]);
            }
        }

        Transform portraitDisplay = FindChildByName(slotRoot, PortraitDisplayName);
        if (portraitDisplay != null)
        {
            info.portraitImage = portraitDisplay.GetComponent<Image>();
            if (info.portraitImage != null)
            {
                info.portraitImage.raycastTarget = false;
            }

            CanvasGroup cg = portraitDisplay.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = portraitDisplay.gameObject.AddComponent<CanvasGroup>();
            }

            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        return info;
    }

    private void BuildPortraitLibrary()
    {
        Transform portraitRoot = FindAnyObjectByName(PortraitReferenceRootName);
        if (portraitRoot == null)
        {
            Debug.LogWarning($"Portrait source root not found: {PortraitReferenceRootName}");
            return;
        }

        Image[] images = portraitRoot.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null || img.sprite == null)
            {
                continue;
            }

            string id = ResolveCharacterIdFromObjectName(img.gameObject.name);
            if (string.IsNullOrEmpty(id) || portraitLibrary.ContainsKey(id))
            {
                continue;
            }

            portraitLibrary[id] = img.sprite;
            RectTransform rt = img.rectTransform;
            portraitLayoutLibrary[id] = new PortraitLayout
            {
                anchorMin = rt.anchorMin,
                anchorMax = rt.anchorMax,
                pivot = rt.pivot,
                anchoredPosition = rt.anchoredPosition,
                sizeDelta = rt.sizeDelta,
                localScale = rt.localScale,
            };
        }
    }

    private static string ResolveCharacterId(string avatarButtonName)
    {
        if (avatarButtonName.EndsWith("2")) return IdSolana;
        if (avatarButtonName.EndsWith("3")) return IdKulus;
        if (avatarButtonName.EndsWith("4")) return IdSesha;
        if (avatarButtonName.EndsWith("5")) return IdHuman;
        if (avatarButtonName.EndsWith("1")) return IdSolana;
        return string.Empty;
    }

    private static string ResolveCharacterIdFromObjectName(string objectName)
    {
        foreach (KeyValuePair<string, string[]> kv in CharacterNameAliases)
        {
            for (int i = 0; i < kv.Value.Length; i++)
            {
                if (objectName == kv.Value[i] || objectName.Contains(kv.Value[i]))
                {
                    return kv.Key;
                }
            }
        }

        return string.Empty;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i];
            }
        }

        return null;
    }

    private void TryAssignCurrentSlot(string characterId)
    {
        if (currentSlot == null)
        {
            return;
        }

        SlotInfo occupiedSlot = FindOtherSlotByCharacter(characterId, currentSlot);
        if (occupiedSlot != null)
        {
            ResetSlotToInitialState(occupiedSlot);
        }

        if (!portraitLibrary.TryGetValue(characterId, out Sprite portrait) || portrait == null)
        {
            Debug.LogWarning("Portrait not found for character: " + characterId);
            return;
        }

        currentSlot.selectedCharacterId = characterId;
        currentSlot.portraitImage.sprite = portrait;
        ApplyPortraitLayout(currentSlot.portraitImage, characterId);
        currentSlot.portraitImage.color = Color.white;
        currentSlot.portraitImage.preserveAspect = true;
        currentSlot.portraitImage.raycastTarget = false;
        currentSlot.portraitImage.gameObject.SetActive(true);

        if (currentSlot.unselectedObject != null)
        {
            currentSlot.unselectedObject.SetActive(false);
        }

        Transform panel = FindAnyObjectByName(CharacterPanelName);
        if (panel != null)
        {
            panel.gameObject.SetActive(false);
        }

        RefreshAvatarAvailabilityVisuals();
    }

    private void ApplyPortraitLayout(Image target, string characterId)
    {
        if (target == null)
        {
            return;
        }

        if (!portraitLayoutLibrary.TryGetValue(characterId, out PortraitLayout layout))
        {
            return;
        }

        RectTransform rt = target.rectTransform;
        rt.anchorMin = layout.anchorMin;
        rt.anchorMax = layout.anchorMax;
        rt.pivot = layout.pivot;
        rt.anchoredPosition = layout.anchoredPosition;
        rt.sizeDelta = layout.sizeDelta;
        rt.localScale = layout.localScale;
    }

    private SlotInfo FindOtherSlotByCharacter(string characterId, SlotInfo targetSlot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            SlotInfo slot = slots[i];
            if (slot == targetSlot)
            {
                continue;
            }

            if (slot.selectedCharacterId == characterId)
            {
                return slot;
            }
        }

        return null;
    }

    private static void ResetSlotToInitialState(SlotInfo slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.selectedCharacterId = null;

        if (slot.portraitImage != null)
        {
            slot.portraitImage.sprite = null;
            slot.portraitImage.gameObject.SetActive(false);
        }

        if (slot.unselectedObject != null)
        {
            slot.unselectedObject.SetActive(true);
        }
    }

    private bool IsCharacterUsedByOtherSlot(string characterId, SlotInfo targetSlot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            SlotInfo slot = slots[i];
            if (slot == targetSlot)
            {
                continue;
            }

            if (slot.selectedCharacterId == characterId)
            {
                return true;
            }
        }

        return false;
    }

    private static Transform FindAnyObjectByName(string objectName)
    {
        Transform[] all = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == objectName)
            {
                return all[i];
            }
        }

        return null;
    }
}



