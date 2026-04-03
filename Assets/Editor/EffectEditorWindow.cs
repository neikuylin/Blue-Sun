using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class EffectEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/EffectDatabase.asset";

    private static readonly string[] StackRuleLabels =
    {
        "\u4e0d\u53ef\u53e0\u52a0",
        "\u53ef\u53e0\u52a0"
    };

    private static readonly string[] TurnOwnerLabels =
    {
        "\u65bd\u6cd5\u8005\u56de\u5408",
        "\u76ee\u6807\u56de\u5408"
    };

    private static readonly EffectDatabase.CharacterStatField[] StatFieldValues =
    {
        EffectDatabase.CharacterStatField.Strength,
        EffectDatabase.CharacterStatField.Agility,
        EffectDatabase.CharacterStatField.Intelligence,
        EffectDatabase.CharacterStatField.ActionPoints,
        EffectDatabase.CharacterStatField.HitRate,
        EffectDatabase.CharacterStatField.PhysicalResistance,
        EffectDatabase.CharacterStatField.FireResistance,
        EffectDatabase.CharacterStatField.CorruptionResistance,
        EffectDatabase.CharacterStatField.ColdResistance,
        EffectDatabase.CharacterStatField.PhysicalResistancePenetration,
        EffectDatabase.CharacterStatField.FireResistancePenetration,
        EffectDatabase.CharacterStatField.CorruptionResistancePenetration,
        EffectDatabase.CharacterStatField.ColdResistancePenetration,
        EffectDatabase.CharacterStatField.CriticalChance,
        EffectDatabase.CharacterStatField.CriticalDamage
    };

    private static readonly string[] StatFieldLabels =
    {
        "\u529b\u91cf",
        "\u654f\u6377",
        "\u667a\u529b",
        "\u884c\u52a8\u529b",
        "\u547d\u4e2d",
        "\u7269\u7406\u6297\u6027",
        "\u706b\u7130\u6297\u6027",
        "\u8150\u8d25\u6297\u6027",
        "\u5bd2\u51b7\u6297\u6027",
        "\u7269\u7406\u6297\u6027\u7a7f\u900f",
        "\u706b\u7130\u6297\u6027\u7a7f\u900f",
        "\u8150\u8d25\u6297\u6027\u7a7f\u900f",
        "\u5bd2\u51b7\u6297\u6027\u7a7f\u900f",
        "\u66b4\u51fb\u7387",
        "\u66b4\u51fb\u4f24\u5bb3"
    };

    private Vector2 scroll;
    private SerializedObject databaseObject;
    private readonly Dictionary<string, bool> entryFoldoutStates = new Dictionary<string, bool>();

    [MenuItem("Tools/\u6548\u679c/\u6548\u679c\u7f16\u8f91\u5668")]
    private static void Open()
    {
        EffectEditorWindow window = GetWindow<EffectEditorWindow>("\u6548\u679c\u7f16\u8f91\u5668");
        window.minSize = new Vector2(700f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EffectDatabase database = EnsureDatabase();

        EditorGUILayout.LabelField("\u6548\u679c\u914d\u7f6e", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u8fd9\u91cc\u5148\u53ea\u505a\u6548\u679c\u8d44\u4ea7\u548c\u7f16\u8f91\u5668\uff0c\u4e0d\u63a5\u6218\u6597\u903b\u8f91\u3002\u4f60\u53ef\u4ee5\u5148\u914d\u7f6e\u6548\u679cID\u3001\u53e0\u52a0\u89c4\u5219\u3001\u6301\u7eed\u6309\u8c01\u7684\u56de\u5408\u8ba1\u7b97\uff0c\u4ee5\u53ca\u4efb\u610f\u89d2\u8272\u5c5e\u6027\u7684\u589e\u51cf\u3002",
            MessageType.Info);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65b0"))
            {
                Repaint();
            }
        }

        EditorGUILayout.Space(6f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawEntries(database);
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntries(EffectDatabase database)
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("\u6548\u679c\u5e93\u8d44\u4ea7\u521b\u5efa\u5931\u8d25\u3002", MessageType.Error);
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
        }

        databaseObject.Update();
        SerializedProperty entries = databaseObject.FindProperty("entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty entry = entries.GetArrayElementAtIndex(i);
            SerializedProperty effectIdProperty = entry.FindPropertyRelative("effectId");
            string foldoutKey = GetEntryFoldoutKey(effectIdProperty != null ? effectIdProperty.stringValue : string.Empty, i);
            bool isExpanded = GetFoldoutState(foldoutKey);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string headerLabel = BuildHeaderLabel(effectIdProperty != null ? effectIdProperty.stringValue : string.Empty, i);
                    bool nextExpanded = EditorGUILayout.Foldout(isExpanded, headerLabel, true);
                    if (nextExpanded != isExpanded)
                    {
                        SetFoldoutState(foldoutKey, nextExpanded);
                        isExpanded = nextExpanded;
                    }

                    if (GUILayout.Button("\u5220\u9664", GUILayout.Width(60f)))
                    {
                        entryFoldoutStates.Remove(foldoutKey);
                        entries.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                if (!isExpanded)
                {
                    continue;
                }

                EditorGUILayout.PropertyField(effectIdProperty, new GUIContent("\u6548\u679cID"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"), new GUIContent("\u663e\u793a\u540d\u79f0"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("description"), new GUIContent("\u63cf\u8ff0"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("icon"), new GUIContent("\u56fe\u6807"));

                SerializedProperty stackRuleProperty = entry.FindPropertyRelative("stackRule");
                SerializedProperty durationTurnOwnerProperty = entry.FindPropertyRelative("durationTurnOwner");
                SerializedProperty durationTurnsProperty = entry.FindPropertyRelative("durationTurns");

                stackRuleProperty.enumValueIndex = EditorGUILayout.Popup("\u53e0\u52a0\u89c4\u5219", stackRuleProperty.enumValueIndex, StackRuleLabels);
                durationTurnOwnerProperty.enumValueIndex = EditorGUILayout.Popup("\u6301\u7eed\u56de\u5408\u5f52\u5c5e", durationTurnOwnerProperty.enumValueIndex, TurnOwnerLabels);
                durationTurnsProperty.intValue = Mathf.Max(0, EditorGUILayout.IntField("\u6301\u7eed\u56de\u5408\u6570", Mathf.Max(0, durationTurnsProperty.intValue)));

                EditorGUILayout.Space(4f);
                DrawStatModifiers(entry.FindPropertyRelative("statModifiers"));
            }
        }

        if (GUILayout.Button("\u65b0\u589e\u6548\u679c"))
        {
            entries.InsertArrayElementAtIndex(entries.arraySize);
            ResetEntry(entries.GetArrayElementAtIndex(entries.arraySize - 1));
        }

        if (databaseObject.ApplyModifiedProperties())
        {
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }
    }

    private static void DrawStatModifiers(SerializedProperty modifiersProperty)
    {
        if (modifiersProperty == null)
        {
            return;
        }

        EditorGUILayout.LabelField("\u5c5e\u6027\u53d8\u52a8", EditorStyles.boldLabel);
        if (modifiersProperty.arraySize == 0)
        {
            EditorGUILayout.HelpBox("\u5f53\u524d\u6ca1\u6709\u5c5e\u6027\u53d8\u52a8\u3002\u53ef\u4ee5\u65b0\u589e\u591a\u6761\uff0c\u5206\u522b\u4f5c\u7528\u5230\u4e0d\u540c\u5c5e\u6027\u3002", MessageType.None);
        }

        for (int i = 0; i < modifiersProperty.arraySize; i++)
        {
            SerializedProperty modifier = modifiersProperty.GetArrayElementAtIndex(i);
            SerializedProperty statFieldProperty = modifier.FindPropertyRelative("statField");
            SerializedProperty amountProperty = modifier.FindPropertyRelative("amount");

            using (new EditorGUILayout.HorizontalScope())
            {
                int selectedIndex = FindStatFieldIndex((EffectDatabase.CharacterStatField)statFieldProperty.enumValueIndex);
                int nextIndex = EditorGUILayout.Popup(selectedIndex, StatFieldLabels);
                statFieldProperty.enumValueIndex = (int)StatFieldValues[Mathf.Clamp(nextIndex, 0, StatFieldValues.Length - 1)];
                amountProperty.intValue = EditorGUILayout.IntField(amountProperty.intValue);

                if (GUILayout.Button("\u5220\u9664", GUILayout.Width(60f)))
                {
                    modifiersProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("\u65b0\u589e\u5c5e\u6027\u53d8\u52a8", GUILayout.Width(120f)))
            {
                int index = modifiersProperty.arraySize;
                modifiersProperty.InsertArrayElementAtIndex(index);
                SerializedProperty modifier = modifiersProperty.GetArrayElementAtIndex(index);
                modifier.FindPropertyRelative("statField").enumValueIndex = (int)EffectDatabase.CharacterStatField.Strength;
                modifier.FindPropertyRelative("amount").intValue = 0;
            }
        }
    }

    private static int FindStatFieldIndex(EffectDatabase.CharacterStatField field)
    {
        for (int i = 0; i < StatFieldValues.Length; i++)
        {
            if (StatFieldValues[i] == field)
            {
                return i;
            }
        }

        return 0;
    }

    private bool GetFoldoutState(string key)
    {
        if (entryFoldoutStates.TryGetValue(key, out bool expanded))
        {
            return expanded;
        }

        entryFoldoutStates[key] = false;
        return false;
    }

    private void SetFoldoutState(string key, bool expanded)
    {
        entryFoldoutStates[key] = expanded;
    }

    private static string GetEntryFoldoutKey(string effectId, int index)
    {
        return $"effect_{index}_{effectId}";
    }

    private static string BuildHeaderLabel(string effectId, int index)
    {
        return string.IsNullOrWhiteSpace(effectId) ? $"\u672a\u547d\u540d\u6548\u679c {index + 1}" : effectId;
    }

    private static void ResetEntry(SerializedProperty entry)
    {
        entry.FindPropertyRelative("effectId").stringValue = string.Empty;
        entry.FindPropertyRelative("displayName").stringValue = string.Empty;
        entry.FindPropertyRelative("description").stringValue = string.Empty;
        entry.FindPropertyRelative("icon").objectReferenceValue = null;
        entry.FindPropertyRelative("stackRule").enumValueIndex = (int)EffectDatabase.StackRule.NotStackable;
        entry.FindPropertyRelative("durationTurnOwner").enumValueIndex = (int)EffectDatabase.TurnOwner.Target;
        entry.FindPropertyRelative("durationTurns").intValue = 1;
        entry.FindPropertyRelative("statModifiers").ClearArray();
    }

    private static EffectDatabase EnsureDatabase()
    {
        EffectDatabase database = AssetDatabase.LoadAssetAtPath<EffectDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<EffectDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }
}
