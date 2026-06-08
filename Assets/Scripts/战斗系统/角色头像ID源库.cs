using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "角色头像ID源库", menuName = "角色/角色头像ID源库")]
public sealed class 角色头像ID源库 : ScriptableObject
{
    public const string 默认资源路径 = "角色头像ID源库";

    [Serializable]
    public struct 头像布局
    {
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector3 localScale;

        public CharacterSelectionState.PortraitLayout 转为角色选择布局()
        {
            return new CharacterSelectionState.PortraitLayout
            {
                anchorMin = anchorMin,
                anchorMax = anchorMax,
                pivot = pivot,
                anchoredPosition = anchoredPosition,
                sizeDelta = sizeDelta,
                localScale = localScale
            };
        }
    }

    [Serializable]
    public sealed class 条目
    {
        public string characterId = string.Empty;
        public Sprite portraitSprite;
        public 头像布局 portraitLayout = 默认布局();
    }

    [SerializeField] private List<条目> entries = new List<条目>();

    public List<条目> Entries => entries;

    public 条目 查找(string 角色ID)
    {
        if (string.IsNullOrWhiteSpace(角色ID))
        {
            return null;
        }

        string resolvedId = 角色ID.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            条目 entry = entries[i];
            if (entry != null && string.Equals(entry.characterId, resolvedId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public static 角色头像ID源库 加载默认库()
    {
        return Resources.Load<角色头像ID源库>(默认资源路径);
    }

    public static 头像布局 默认布局()
    {
        return new 头像布局
        {
            anchorMin = new Vector2(0.5f, 0.5f),
            anchorMax = new Vector2(0.5f, 0.5f),
            pivot = new Vector2(0.5f, 0.5f),
            anchoredPosition = Vector2.zero,
            sizeDelta = Vector2.zero,
            localScale = Vector3.one
        };
    }
}
