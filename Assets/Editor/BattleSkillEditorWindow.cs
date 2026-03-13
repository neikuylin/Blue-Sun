using UnityEditor;
using UnityEngine;

public sealed class BattleSkillEditorWindow : EditorWindow
{
    private const string AssetFolder = "Assets/Resources";
    private const string AssetPath = AssetFolder + "/BattleSkillDatabase.asset";

    private static readonly string[] SkillGroupLabels =
    {
        "特殊",
        "战技",
        "法术"
    };

    private static readonly string[] SkillTypeLabels =
    {
        "点选技能",
        "范围技能"
    };

    private static readonly string[] CastTargetLabels =
    {
        "自己（当前回合角色）",
        "敌人",
        "队友（除当前回合角色）",
        "全部（所有有ID的战场单位）"
    };

    private Vector2 scroll;
    private SerializedObject databaseObject;

    [MenuItem("Tools/技能/技能编辑器")]
    private static void Open()
    {
        BattleSkillEditorWindow window = GetWindow<BattleSkillEditorWindow>("技能编辑器");
        window.minSize = new Vector2(680f, 480f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        BattleSkillDatabase database = EnsureDatabase();

        EditorGUILayout.LabelField("战斗技能配置", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "技能分组固定为：特殊、战技、法术。移动技能保留技能ID“移动”，但现在属于“特殊 + 范围技能”。",
            MessageType.Info);
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新"))
            {
                Repaint();
            }

            if (GUILayout.Button("补齐默认移动技能"))
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
            EditorGUILayout.HelpBox("技能库资产创建失败。", MessageType.Error);
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
            SerializedProperty group = entry.FindPropertyRelative("group");
            SerializedProperty skillType = entry.FindPropertyRelative("skillType");
            SerializedProperty useMoveDistanceAsRange = entry.FindPropertyRelative("useMoveDistanceAsRange");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("skillId"), new GUIContent("技能ID"));
                    if (GUILayout.Button("删除", GUILayout.Width(60f)))
                    {
                        entries.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                group.enumValueIndex = EditorGUILayout.Popup("分组", group.enumValueIndex, SkillGroupLabels);
                skillType.enumValueIndex = EditorGUILayout.Popup("技能类型", skillType.enumValueIndex, SkillTypeLabels);
                SerializedProperty castTarget = entry.FindPropertyRelative("castTarget");
                castTarget.enumValueIndex = EditorGUILayout.Popup("施法对象", castTarget.enumValueIndex, CastTargetLabels);
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("icon"), new GUIContent("技能图标"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("damageMultiplier"), new GUIContent("战技伤害倍率"));

                bool rangeFromMoveDistance = EditorGUILayout.Toggle("射程取移动距离", useMoveDistanceAsRange.boolValue);
                useMoveDistanceAsRange.boolValue = rangeFromMoveDistance;
                if (!rangeFromMoveDistance)
                {
                    EditorGUILayout.PropertyField(entry.FindPropertyRelative("range"), new GUIContent("射程"));
                }
                else
                {
                    EditorGUILayout.LabelField("射程", "取自角色的移动距离");
                }

                EditorGUILayout.PropertyField(entry.FindPropertyRelative("cooldownTurns"), new GUIContent("冷却时间（回合）"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("manaCost"), new GUIContent("魔法消耗（MP）"));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("actionPointCost"), new GUIContent("行动力消耗（AP）"));

                if ((BattleSkillDatabase.SkillType)skillType.enumValueIndex == BattleSkillDatabase.SkillType.Area)
                {
                    SerializedProperty effectSize = entry.FindPropertyRelative("effectSize");
                    Vector2Int size = effectSize.vector2IntValue;
                    int width = EditorGUILayout.IntField("作用范围宽", Mathf.Max(1, size.x));
                    int height = EditorGUILayout.IntField("作用范围高", Mathf.Max(1, size.y));
                    effectSize.vector2IntValue = new Vector2Int(width, height);
                    EditorGUILayout.LabelField("作用范围", $"{Mathf.Max(1, width)}x{Mathf.Max(1, height)}");
                }
            }
        }

        if (GUILayout.Button("新增技能"))
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
        entry.FindPropertyRelative("group").enumValueIndex = (int)BattleSkillDatabase.SkillGroup.CombatArt;
        entry.FindPropertyRelative("skillType").enumValueIndex = (int)BattleSkillDatabase.SkillType.Target;
        entry.FindPropertyRelative("castTarget").enumValueIndex = (int)BattleSkillDatabase.CastTarget.Enemy;
        entry.FindPropertyRelative("icon").objectReferenceValue = null;
        entry.FindPropertyRelative("damageMultiplier").floatValue = 1f;
        entry.FindPropertyRelative("actionPointCost").intValue = 1;
        entry.FindPropertyRelative("manaCost").intValue = 0;
        entry.FindPropertyRelative("cooldownTurns").intValue = 0;
        entry.FindPropertyRelative("useMoveDistanceAsRange").boolValue = false;
        entry.FindPropertyRelative("range").intValue = 1;
        entry.FindPropertyRelative("effectSize").vector2IntValue = new Vector2Int(1, 1);
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

        BattleSkillDatabase.SkillEntry moveSkill = database.FindEntry(BattleSkillDatabase.MoveSkillId);
        if (moveSkill != null)
        {
            moveSkill.group = BattleSkillDatabase.SkillGroup.Special;
            moveSkill.skillType = BattleSkillDatabase.SkillType.Area;
            moveSkill.useMoveDistanceAsRange = true;
            moveSkill.effectSize = new Vector2Int(3, 3);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
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
            group = BattleSkillDatabase.SkillGroup.Special,
            skillType = BattleSkillDatabase.SkillType.Area,
            castTarget = BattleSkillDatabase.CastTarget.Self,
            icon = null,
            damageMultiplier = 1f,
            actionPointCost = 1,
            manaCost = 0,
            cooldownTurns = 0,
            useMoveDistanceAsRange = true,
            range = 0,
            effectSize = new Vector2Int(3, 3)
        };
    }
}
