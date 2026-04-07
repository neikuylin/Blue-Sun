using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 小头像界面ID同步器 : MonoBehaviour
{
    private sealed class Entry
    {
        public string characterId;
        public GameObject root;
        public Toggle toggle;
        public Button button;
        public 当前ID选择器 selector;
    }

    private readonly List<Entry> entries = new List<Entry>();
    private string lastCurrentId = string.Empty;
    private string lastSelectableSignature = string.Empty;

    private void OnEnable()
    {
        RebindEntries();
        RefreshState(force: true);
    }

    private void OnTransformChildrenChanged()
    {
        RebindEntries();
        RefreshState(force: true);
    }

    private void LateUpdate()
    {
        RefreshState();
    }

    private void RebindEntries()
    {
        entries.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            Entry entry = new Entry
            {
                characterId = child.name,
                root = child.gameObject,
                toggle = child.GetComponent<Toggle>() ?? child.GetComponentInChildren<Toggle>(true),
                button = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true),
                selector = child.GetComponent<当前ID选择器>() ?? child.GetComponentInChildren<当前ID选择器>(true)
            };

            if (entry.toggle != null && entry.selector != null)
            {
                当前ID选择器 capturedSelector = entry.selector;
                entry.toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        capturedSelector.设置当前ID();
                    }
                });
            }

            entries.Add(entry);
        }

        lastCurrentId = string.Empty;
        lastSelectableSignature = string.Empty;
    }

    private void RefreshState(bool force = false)
    {
        if (entries.Count == 0)
        {
            return;
        }

        string currentId = 界面ID列表.当前ID ?? string.Empty;
        List<string> selectableIds = 界面ID列表.可选ID;
        string selectableSignature = selectableIds.Count == 0 ? string.Empty : string.Join("|", selectableIds);
        if (!force &&
            string.Equals(lastCurrentId, currentId, StringComparison.Ordinal) &&
            string.Equals(lastSelectableSignature, selectableSignature, StringComparison.Ordinal))
        {
            return;
        }

        lastCurrentId = currentId;
        lastSelectableSignature = selectableSignature;

        HashSet<string> selectableLookup = new HashSet<string>(selectableIds, StringComparer.Ordinal);
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry == null || entry.root == null)
            {
                continue;
            }

            bool isSelectable = selectableLookup.Contains(entry.characterId);
            if (entry.root.activeSelf != isSelectable)
            {
                entry.root.SetActive(isSelectable);
            }

            if (entry.toggle != null)
            {
                bool shouldBeOn = isSelectable && string.Equals(entry.characterId, currentId, StringComparison.Ordinal);
                if (entry.toggle.isOn != shouldBeOn)
                {
                    entry.toggle.SetIsOnWithoutNotify(shouldBeOn);
                }
            }
        }
    }
}
