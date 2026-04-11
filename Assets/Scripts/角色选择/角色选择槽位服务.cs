using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

internal static class 角色选择槽位服务
{
    public static CharacterSlotView 查找当前激活槽位(CharacterSlotView[] slots)
    {
        if (slots == null)
        {
            return null;
        }

        for (int i = 0; i < slots.Length; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            for (int j = 0; j < slot.selectToggles.Count; j++)
            {
                Toggle toggle = slot.selectToggles[j];
                if (toggle != null && toggle.isOn)
                {
                    return slot;
                }
            }
        }

        return null;
    }

    public static string 解析角色ID(CharacterSlotView slot)
    {
        if (slot == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(slot.selectedCharacterId))
        {
            return slot.selectedCharacterId;
        }

        if (!string.IsNullOrEmpty(slot.slotCharacterId))
        {
            return slot.slotCharacterId;
        }

        return string.Empty;
    }

    public static Sprite 解析立绘图片(CharacterSlotView slot)
    {
        Image portraitImage = 解析立绘组件(slot, true);
        if (portraitImage != null)
        {
            return portraitImage.sprite;
        }

        portraitImage = 解析立绘组件(slot, false);
        return portraitImage != null ? portraitImage.sprite : null;
    }

    public static CharacterSelectionState.PortraitLayout 解析立绘布局(CharacterSlotView slot)
    {
        CharacterSelectionState.PortraitLayout result = new CharacterSelectionState.PortraitLayout
        {
            anchorMin = new Vector2(0.5f, 0.5f),
            anchorMax = new Vector2(0.5f, 0.5f),
            pivot = new Vector2(0.5f, 0.5f),
            anchoredPosition = Vector2.zero,
            sizeDelta = Vector2.zero,
            localScale = Vector3.one
        };

        Image portraitImage = 解析立绘组件(slot, true) ?? 解析立绘组件(slot, false);
        if (portraitImage == null)
        {
            return result;
        }

        RectTransform rectTransform = portraitImage.rectTransform;
        if (rectTransform == null)
        {
            return result;
        }

        result.anchorMin = rectTransform.anchorMin;
        result.anchorMax = rectTransform.anchorMax;
        result.pivot = rectTransform.pivot;
        result.anchoredPosition = rectTransform.anchoredPosition;
        result.sizeDelta = rectTransform.sizeDelta;
        result.localScale = rectTransform.localScale;
        return result;
    }

    public static List<CharacterSlotView> 排序槽位(IEnumerable<CharacterSlotView> slots)
    {
        List<CharacterSlotView> result = new List<CharacterSlotView>();
        if (slots == null)
        {
            return result;
        }

        foreach (CharacterSlotView slot in slots)
        {
            if (slot != null)
            {
                result.Add(slot);
            }
        }

        result.Sort(比较槽位顺序);
        return result;
    }

    private static Image 解析立绘组件(CharacterSlotView slot, bool requireActive)
    {
        if (slot == null)
        {
            return null;
        }

        if (slot.portraitImage != null && slot.portraitImage.sprite != null)
        {
            if (!requireActive || slot.portraitImage.gameObject.activeInHierarchy)
            {
                return slot.portraitImage;
            }
        }

        Image[] childImages = slot.GetComponentsInChildren<Image>(true);
        return 查找优先立绘组件(childImages, requireActive);
    }

    private static Image 查找优先立绘组件(Image[] images, bool requireActive)
    {
        if (images == null)
        {
            return null;
        }

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
            {
                continue;
            }

            if (requireActive && !image.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (image.name.Contains("头像", StringComparison.Ordinal))
            {
                return image;
            }
        }

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
            {
                continue;
            }

            if (requireActive && !image.gameObject.activeInHierarchy)
            {
                continue;
            }

            return image;
        }

        return null;
    }

    private static int 比较槽位顺序(CharacterSlotView left, CharacterSlotView right)
    {
        if (ReferenceEquals(left, right))
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        string leftPath = 构建层级路径(left.transform);
        string rightPath = 构建层级路径(right.transform);
        return string.Compare(leftPath, rightPath, StringComparison.Ordinal);
    }

    private static string 构建层级路径(Transform target)
    {
        if (target == null)
        {
            return string.Empty;
        }

        List<string> segments = new List<string>();
        Transform current = target;
        while (current != null)
        {
            segments.Add(current.GetSiblingIndex().ToString("D4") + "_" + current.name);
            current = current.parent;
        }

        segments.Reverse();
        return string.Join("/", segments);
    }
}
