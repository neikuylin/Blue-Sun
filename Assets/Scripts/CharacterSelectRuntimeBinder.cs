using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectRuntimeBinder : MonoBehaviour
{
    private const string SlotRootName = "玩家栏位按钮";
    private const string AvatarButtonName = "头像按钮";
    private const string UnselectedName = "未选择图片";
    private const string SelectedImageName = "选择图片";
    private const string PortraitDisplayName = "角色头像显示";
    private const string PortraitDisplayAltName = "角色头像容器";
    private const string PortraitReferenceRootName = "栏位引用头像";
    private const string CharacterPanelName = "角色选择";

    private const string IdSolana = "solana";
    private const string IdKulus = "kulus";
    private const string IdSesha = "sesha";
    private const string IdHuman = "human";

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
        public string name;
        public Transform root;
        public List<Button> selectButtons = new List<Button>();
        public GameObject unselectedObject;
        public Image portraitImage;
        public string selectedCharacterId;
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
            if (slot == null || slot.portraitImage == null)
            {
                continue;
            }

            slots.Add(slot);
            SlotInfo capturedSlot = slot;
            for (int b = 0; b < slot.selectButtons.Count; b++)
            {
                slot.selectButtons[b].onClick.AddListener(() => currentSlot = capturedSlot);
            }
        }

        BuildPortraitLibrary(allTransforms);
        BindAvatarSelectors(allTransforms);

        if (slots.Count > 0)
        {
            currentSlot = slots[0];
        }

        Debug.Log($"CharacterSelectRuntimeBinder slots={slots.Count}, portraits={portraitLibrary.Count}, layouts={portraitLayoutLibrary.Count}");
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

            Button button = tr.GetComponent<Button>();
            Toggle toggle = tr.GetComponent<Toggle>();

            if (button == null && toggle == null)
            {
                button = EnsureButton(tr);
            }

            if (button != null)
            {
                button.onClick = new Button.ButtonClickedEvent();
                string capturedId = characterId;
                button.onClick.AddListener(() => TryAssignCurrentSlot(capturedId));
            }

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
        info.name = slotRoot.name;
        info.root = slotRoot;

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

        Transform selectedState = FindChildByName(slotRoot, SelectedImageName);
        RectTransform refRect = null;
        if (selectedState != null)
        {
            refRect = selectedState.GetComponent<RectTransform>();
        }
        if (refRect == null && unselected != null)
        {
            refRect = unselected.GetComponent<RectTransform>();
        }

        info.portraitImage = EnsurePortraitDisplay(slotRoot, refRect);
        HideLegacyAvatarChildren(slotRoot);
        return info;
    }

    private static Image EnsurePortraitDisplay(Transform slotRoot, RectTransform referenceRect)
    {
        Transform existing = FindPortraitDisplay(slotRoot);
        Image img;
        bool createdNow = false;

        if (existing != null)
        {
            img = existing.GetComponent<Image>();
            if (img == null)
            {
                img = existing.gameObject.AddComponent<Image>();
            }
        }
        else
        {
            GameObject go = new GameObject(PortraitDisplayName);
            go.transform.SetParent(slotRoot, false);
            go.AddComponent<CanvasRenderer>();
            img = go.AddComponent<Image>();
            createdNow = true;
            Debug.LogWarning($"[{slotRoot.name}] missing '{PortraitDisplayName}', created temporary one. You can create it in edit mode and position it manually.");
        }

        RectTransform rt = img.rectTransform;
        if (createdNow && referenceRect != null)
        {
            rt.anchorMin = referenceRect.anchorMin;
            rt.anchorMax = referenceRect.anchorMax;
            rt.pivot = referenceRect.pivot;
            rt.anchoredPosition = referenceRect.anchoredPosition;
            rt.sizeDelta = referenceRect.sizeDelta;
            rt.localScale = Vector3.one;
        }
        else if (createdNow)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(180f, 179f);
            rt.localScale = Vector3.one;
        }

        img.raycastTarget = false;
        img.preserveAspect = true;
        img.gameObject.SetActive(false);
        return img;
    }

    private static Transform FindPortraitDisplay(Transform slotRoot)
    {
        Transform t = FindChildByName(slotRoot, PortraitDisplayName);
        if (t != null)
        {
            return t;
        }

        return FindChildByName(slotRoot, PortraitDisplayAltName);
    }

    private void BuildPortraitLibrary(Transform[] allTransforms)
    {
        Transform portraitRoot = FindAnyObjectByName(PortraitReferenceRootName);
        if (portraitRoot != null)
        {
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

        if (portraitLibrary.Count > 0)
        {
            return;
        }

        // Fallback for old scene setup.
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];
            string id = ResolveCharacterIdFromObjectName(tr.name);
            if (string.IsNullOrEmpty(id) || portraitLibrary.ContainsKey(id))
            {
                continue;
            }

            Image img = tr.GetComponent<Image>();
            if (img == null || img.sprite == null)
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

    private static Button EnsureButton(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Button btn = root.GetComponent<Button>();
        if (btn == null)
        {
            btn = root.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
        }

        if (btn.targetGraphic == null)
        {
            Image img = root.GetComponent<Image>();
            if (img == null)
            {
                img = root.GetComponentInChildren<Image>(true);
            }

            if (img != null)
            {
                btn.targetGraphic = img;
            }
        }

        return btn;
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

        if (IsCharacterUsedByOtherSlot(characterId, currentSlot))
        {
            Debug.Log("Character already selected in another slot: " + characterId);
            return;
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

    private static void HideLegacyAvatarChildren(Transform slotRoot)
    {
        Transform[] children = slotRoot.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            string name = children[i].name;
            if (!name.Contains("头像"))
            {
                continue;
            }

            if (name.StartsWith(AvatarButtonName))
            {
                continue;
            }

            children[i].gameObject.SetActive(false);
        }
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

