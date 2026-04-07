using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
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
                button = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true)
            };

            if (entry.toggle != null)
            {
                string capturedId = entry.characterId;
                entry.toggle.onValueChanged.AddListener(isOn =>
                {
                    if (isOn)
                    {
                        TrySelectCharacter(capturedId);
                    }
                });
            }

            if (entry.button != null)
            {
                string capturedId = entry.characterId;
                entry.button.onClick.AddListener(() => TrySelectCharacter(capturedId));
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

    private static void TrySelectCharacter(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        BattleTurnSystem battleTurnSystem = UnityEngine.Object.FindObjectOfType<BattleTurnSystem>(true);
        if (battleTurnSystem != null)
        {
            InventoryShortcutRuntimeBinder.SetDisplayedEquipmentCharacter(characterId);
            return;
        }

        if (string.Equals(SceneManager.GetActiveScene().name, "营地", StringComparison.Ordinal))
        {
            界面ID列表.设置营地当前ID(characterId);
            return;
        }

        CharacterSlotView[] slots = UnityEngine.Object.FindObjectsOfType<CharacterSlotView>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            string resolvedCharacterId = CharacterSelectionState.ResolveCharacterId(slot);
            if (!string.Equals(resolvedCharacterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            for (int j = 0; j < slot.selectToggles.Count; j++)
            {
                Toggle toggle = slot.selectToggles[j];
                if (toggle == null)
                {
                    continue;
                }

                if (!toggle.isOn)
                {
                    toggle.isOn = true;
                }
                else
                {
                    toggle.onValueChanged.Invoke(true);
                }

                return;
            }
        }
    }
}
