using UnityEditor;
using UnityEngine;

public sealed class BattleSkillEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/BattleSkillDatabase.asset";

    private static readonly string[] SkillTypeLabels =
    {
        "\u79FB\u52A8",
        "\u70B9\u9009\u6280\u80FD",
        "\u8303\u56F4\u6280\u80FD"
    };

    private Vector2 scroll;
    private SerializedObject databaseObject;

    [MenuItem("Tools/\u6280\u80FD/\u6280\u80FD\u7F16\u8F91\u5668")]
    private static void Open()
    {
        BattleSkillEditorWindow window = GetWindow<BattleSkillEditorWindow>("\u6280\u80FD\u7F16\u8F91\u5668");
        window.minSize = new Vector2(640f, 460f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleSkillDatabase database = EnsureDatabase();

        EditorGUILayout.LabelField("\u6218\u6597\u6280\u80FD\u914D\u7F6E", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "\u8FD9\u91CC\u5148\u7BA1\u7406\u6280\u80FD\u6A21\u677F\u3002\u9ED8\u8BA4\u5DF2\u5EFA\u7ACB\u201C\u79FB\u52A8\u201D\u6280\u80FD\uFF0C\u6280\u80FD\u7C7B\u578B\u5305\u542B\uFF1A\u79FB\u52A8\u3001\u70B9\u9009\u6280\u80FD\u3001\u8303\u56F4\u6280\u80FD\u3002",
            MessageType.Info);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("\u5237\u65B0"))
            {
                Repaint();
            }

            if (GUILayout.Button("\u8865\u9F50\u9ED8\u8BA4\u79FB\u52A8\u6280\u80FD"))
            {
                EnsureDefaultMoveSkill(database);
            }
        }

        EditorGUILayout.Space(6f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        DrawEntries(database);
        EditorGUILayout.EndScrollView();
    }

    private void DrawEntries(BattleSkillDatabase database)
    {
        if (database == null)
        {
            EditorGUILayout.HelpBox("\u6280\u80FD\u5E93\u8D44\u4EA7\u521B\u5EFA\u5931\u8D25\u3002", MessageType.Error);
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
            SerializedProperty skillType = entry.FindPropertyRelative("skillType");
            SerializedProperty useMoveDistanceAsRange = entry.FindPropertyRelative("useMoveDistanceAsRange");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("skillId"), new GUIContent("\u6280\u80FDID"));
                    if (GUILayout.Button("\u5220\u9664", GUILayout.Width(60f)))
                    {
                        entries.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("group"), new GUIContent("\u5206\u7EC4"));
                skillType.enumValueIndex = EditorGUILayout.Popup("\u6280\u80FD\u7C7B\u578B", skillType.enumValueIndex, SkillTypeLabels);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("cooldownTurns"), new GUIContent("\u51B7\u5374\u65F6\u95F4\uFF08\u56DE\u5408\uFF09"));

                bool rangeFromMoveDistance = EditorGUILayout.Toggle("\u5C04\u7A0B\u53D6\u79FB\u52A8\u8DDD\u79BB", useMoveDistanceAsRange.boolValue);
                useMoveDistanceAsRange.boolValue = rangeFromMoveDistance;
                if (!rangeFromMoveDistance)
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("range"), new GUIContent("\u5C04\u7A0B"));
                }
                else
                {
                    EditorGUILayout.LabelField("\u5C04\u7A0B", "\u53D6\u81EA\u89D2\u8272\u7684\u79FB\u52A8\u8DDD\u79BB");
                }

                SerializedProperty effectSize = entry.FindPropertyRelative("effectSize");
                Vector2Int size = effectSize.vector2IntValue;
                int width = EditorGUILayout.IntField("\u4F5C\u7528\u8303\u56F4\u5BBD", Mathf.Max(1, size.x));
                int height = EditorGUILayout.IntField("\u4F5C\u7528\u8303\u56F4\u9AD8", Mathf.Max(1, size.y));
                effectSize.vector2IntValue = new Vector2Int(width, height);
                EditorGUILayout.LabelField("\u4F5C\u7528\u8303\u56F4", $"{Mathf.Max(1, width)}x{Mathf.Max(1, height)}");
            }
        }

        if (GUILayout.Button("\u65B0\u589E\u6280\u80FD"))
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

    private static void ResetEntry(SerializedProperty entry)
    {
        entry.FindPropertyRelative("skillId").stringValue = string.Empty;
        entry.FindPropertyRelative("group").stringValue = "\u6280\u80FD";
        entry.FindPropertyRelative("skillType").enumValueIndex = (int)BattleSkillDatabase.SkillType.Move;
        entry.FindPropertyRelative("cooldownTurns").intValue = 0;
        entry.FindPropertyRelative("useMoveDistanceAsRange").boolValue = true;
        entry.FindPropertyRelative("range").intValue = 0;
        entry.FindPropertyRelative("effectSize").vector2IntValue = new Vector2Int(3, 3);
    }

    private static BattleSkillDatabase EnsureDatabase()
    {
        BattleSkillDatabase database = AssetDatabase.LoadAssetAtPath<BattleSkillDatabase>(AssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(AssetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        database = CreateInstance<BattleSkillDatabase>();
        database.Entries.Add(CreateDefaultMoveSkill());
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureDefaultMoveSkill(BattleSkillDatabase database)
    {
        if (database == null)
        {
            return;
        }

        if (database.FindEntry(BattleSkillDatabase.MoveSkillId) != null)
        {
            return;
        }

        database.Entries.Add(CreateDefaultMoveSkill());
        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private static BattleSkillDatabase.SkillEntry CreateDefaultMoveSkill()
    {
        return new BattleSkillDatabase.SkillEntry
        {
            skillId = BattleSkillDatabase.MoveSkillId,
            group = "\u6280\u80FD",
            skillType = BattleSkillDatabase.SkillType.Move,
            cooldownTurns = 0,
            useMoveDistanceAsRange = true,
            range = 0,
            effectSize = new Vector2Int(3, 3)
        };
    }
}
