using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectRuntimeBinder : MonoBehaviour
{
    private const string SlotRootName = "玩家栏位按钮";
    private const string AvatarButtonName = "头像按钮";
    private const string UnselectedName = "未选择图片";
    private const string CharacterPanelName = "角色选择";

    private const string SolanaAvatarName = "索拉娜头像";
    private const string KulusAvatarName = "库鲁斯头像";
    private const string SeshaAvatarName = "瑟莎头像";
    private const string HumanAvatarName = "人类头像（暂用爱丽丝）";

    private static readonly string[] CharacterAvatarNames =
    {
        SolanaAvatarName,
        KulusAvatarName,
        SeshaAvatarName,
        HumanAvatarName,
    };

    private readonly List<SlotInfo> slots = new List<SlotInfo>();
    private SlotInfo currentSlot;

    private class SlotInfo
    {
        public Transform root;
        public List<Button> selectButtons = new List<Button>();
        public GameObject unselectedObject;
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
        currentSlot = null;

        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform tr = allTransforms[i];

            if (IsNumberedSlotRoot(tr.name))
            {
                SlotInfo slot = BuildSlotInfo(tr);
                if (slot != null && slot.selectButtons.Count > 0)
                {
                    slots.Add(slot);
                    SlotInfo capturedSlot = slot;
                    for (int b = 0; b < slot.selectButtons.Count; b++)
                    {
                        slot.selectButtons[b].onClick.AddListener(() => currentSlot = capturedSlot);
                    }
                }

                continue;
            }

            if (IsAvatarButton(tr.name))
            {
                Button avatarButton = tr.GetComponent<Button>();
                if (avatarButton == null)
                {
                    continue;
                }

                // Clear scene hardcoded onClick bindings to avoid cross-slot side effects.
                avatarButton.onClick = new Button.ButtonClickedEvent();

                Button capturedButton = avatarButton;
                avatarButton.onClick.AddListener(() => ApplyAvatar(capturedButton));
            }
        }

        if (slots.Count > 0)
        {
            currentSlot = slots[0];
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
        info.root = slotRoot;

        Button[] buttons = slotRoot.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name.StartsWith(AvatarButtonName))
            {
                continue;
            }

            info.selectButtons.Add(buttons[i]);
        }

        Transform unselected = FindChildByName(slotRoot, UnselectedName);
        if (unselected != null)
        {
            info.unselectedObject = unselected.gameObject;
        }

        return info;
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

    private void ApplyAvatar(Button avatarButton)
    {
        if (currentSlot == null)
        {
            return;
        }

        string avatarObjectName = ResolveAvatarObjectName(avatarButton.name);
        if (string.IsNullOrEmpty(avatarObjectName))
        {
            return;
        }

        SetAllCharacterAvatarsInactive(currentSlot.root);
        bool activated = SetAvatarActiveByName(currentSlot.root, avatarObjectName);
        if (!activated)
        {
            return;
        }

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

    private static string ResolveAvatarObjectName(string buttonName)
    {
        if (buttonName.EndsWith("2")) return SolanaAvatarName;
        if (buttonName.EndsWith("3")) return KulusAvatarName;
        if (buttonName.EndsWith("4")) return SeshaAvatarName;
        if (buttonName.EndsWith("5")) return HumanAvatarName;
        if (buttonName.EndsWith("1")) return SolanaAvatarName;
        return string.Empty;
    }

    private static void SetAllCharacterAvatarsInactive(Transform slotRoot)
    {
        for (int i = 0; i < CharacterAvatarNames.Length; i++)
        {
            Transform t = FindChildByName(slotRoot, CharacterAvatarNames[i]);
            if (t != null)
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    private static bool SetAvatarActiveByName(Transform slotRoot, string avatarName)
    {
        Transform t = FindChildByName(slotRoot, avatarName);
        if (t == null)
        {
            return false;
        }

        t.gameObject.SetActive(true);
        return true;
    }

    private static Transform FindAnyObjectByName(string objectName)
    {
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            if (allTransforms[i].name == objectName)
            {
                return allTransforms[i];
            }
        }

        return null;
    }
}
