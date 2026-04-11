using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BattleCharacterBindings", menuName = "战斗/角色模型绑定库")]
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
        public Vector3 modelScale = Vector3.one;
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
