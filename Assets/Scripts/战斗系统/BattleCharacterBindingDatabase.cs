using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(fileName = "BattleCharacterBindings", menuName = "\u6218\u6597/\u89d2\u8272\u6a21\u578b\u7ed1\u5b9a\u5e93")]
public sealed class BattleCharacterBindingDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "BattleCharacterBindings";

    [Serializable]
    public sealed class BindingEntry
    {
        public string characterId;
        public string displayName;
        public GameObject modelPrefab;
        public RuntimeAnimatorController animatorController;
        public Vector2Int cellOffset = Vector2Int.zero;
        public Vector3 worldOffset = Vector3.zero;
        public bool useAutoVisualAnchor = true;
    }

    [SerializeField] private List<BindingEntry> entries = new List<BindingEntry>();

    public List<BindingEntry> Entries => entries;

    public BindingEntry FindBinding(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            BindingEntry entry = entries[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.characterId, characterId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    public static BattleCharacterBindingDatabase LoadDefault()
    {
        return Resources.Load<BattleCharacterBindingDatabase>(DefaultResourcePath);
    }
}
