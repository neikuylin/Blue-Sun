using UnityEditor;
using UnityEngine;

public sealed class 剧情编辑器窗口 : EditorWindow
{
    private const string 资源目录 = "Assets/Resources";
    private const string 数据库路径 = 资源目录 + "/剧情数据库.asset";

    private SerializedObject 数据库对象;
    private Vector2 滚动位置;
    private string 新剧情ID = string.Empty;
    private string 新备注 = string.Empty;

    [MenuItem("Tools/剧情/剧情编辑器")]
    private static void 打开()
    {
        剧情编辑器窗口 窗口 = GetWindow<剧情编辑器窗口>("剧情编辑器");
        窗口.minSize = new Vector2(520f, 360f);
        窗口.Show();
        窗口.Focus();
    }

    private void OnGUI()
    {
        剧情数据库 数据库 = 确保数据库();
        if (数据库 == null)
        {
            EditorGUILayout.HelpBox("剧情数据库加载失败。", MessageType.Error);
            return;
        }

        if (数据库对象 == null || 数据库对象.targetObject != 数据库)
        {
            数据库对象 = new SerializedObject(数据库);
        }

        数据库对象.Update();

        EditorGUILayout.LabelField("剧情编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里先维护剧情ID和备注。后续剧情触发、条件、动作会继续接到这个数据库。", MessageType.Info);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("保存", GUILayout.Width(90f)))
            {
                数据库对象.ApplyModifiedProperties();
                保存数据库(数据库);
            }
        }

        EditorGUILayout.Space(8f);
        绘制新增面板(数据库);
        EditorGUILayout.Space(8f);
        绘制剧情列表();

        数据库对象.ApplyModifiedProperties();
    }

    private void 绘制新增面板(剧情数据库 数据库)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("新增剧情", EditorStyles.boldLabel);
            新剧情ID = EditorGUILayout.TextField("剧情ID", 新剧情ID);
            EditorGUILayout.LabelField("备注");
            新备注 = EditorGUILayout.TextArea(新备注, GUILayout.MinHeight(44f));

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(新剧情ID)))
            {
                if (GUILayout.Button("新增剧情"))
                {
                    新增剧情(数据库);
                }
            }
        }
    }

    private void 绘制剧情列表()
    {
        SerializedProperty 剧情列表属性 = 数据库对象.FindProperty("剧情列表");
        if (剧情列表属性 == null)
        {
            EditorGUILayout.HelpBox("未找到剧情列表字段。", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("现有剧情", EditorStyles.boldLabel);
        if (剧情列表属性.arraySize <= 0)
        {
            EditorGUILayout.HelpBox("还没有剧情。", MessageType.Info);
            return;
        }

        滚动位置 = EditorGUILayout.BeginScrollView(滚动位置);
        for (int i = 0; i < 剧情列表属性.arraySize; i++)
        {
            if (绘制剧情条目(剧情列表属性, i))
            {
                GUIUtility.ExitGUI();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private bool 绘制剧情条目(SerializedProperty 剧情列表属性, int 索引)
    {
        SerializedProperty 条目属性 = 剧情列表属性.GetArrayElementAtIndex(索引);
        SerializedProperty 剧情ID属性 = 条目属性.FindPropertyRelative("剧情ID");
        SerializedProperty 备注属性 = 条目属性.FindPropertyRelative("备注");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string 标题 = 剧情ID属性 != null && !string.IsNullOrWhiteSpace(剧情ID属性.stringValue)
                    ? 剧情ID属性.stringValue
                    : $"剧情 {索引 + 1}";
                EditorGUILayout.LabelField(标题, EditorStyles.boldLabel);

                if (GUILayout.Button("删除", GUILayout.Width(72f)))
                {
                    剧情列表属性.DeleteArrayElementAtIndex(索引);
                    数据库对象.ApplyModifiedProperties();
                    保存数据库((剧情数据库)数据库对象.targetObject);
                    return true;
                }
            }

            if (剧情ID属性 != null)
            {
                EditorGUILayout.PropertyField(剧情ID属性, new GUIContent("剧情ID"));
            }

            if (备注属性 != null)
            {
                EditorGUILayout.PropertyField(备注属性, new GUIContent("备注"));
            }
        }

        return false;
    }

    private void 新增剧情(剧情数据库 数据库)
    {
        SerializedProperty 剧情列表属性 = 数据库对象.FindProperty("剧情列表");
        if (剧情列表属性 == null)
        {
            return;
        }

        Undo.RecordObject(数据库, "新增剧情");
        int 新索引 = 剧情列表属性.arraySize;
        剧情列表属性.InsertArrayElementAtIndex(新索引);

        SerializedProperty 条目属性 = 剧情列表属性.GetArrayElementAtIndex(新索引);
        SerializedProperty 剧情ID属性 = 条目属性.FindPropertyRelative("剧情ID");
        SerializedProperty 备注属性 = 条目属性.FindPropertyRelative("备注");

        if (剧情ID属性 != null)
        {
            剧情ID属性.stringValue = 新剧情ID.Trim();
        }

        if (备注属性 != null)
        {
            备注属性.stringValue = 新备注.Trim();
        }

        新剧情ID = string.Empty;
        新备注 = string.Empty;
        数据库对象.ApplyModifiedProperties();
        保存数据库(数据库);
    }

    private static 剧情数据库 确保数据库()
    {
        剧情数据库 数据库 = AssetDatabase.LoadAssetAtPath<剧情数据库>(数据库路径);
        if (数据库 != null)
        {
            return 数据库;
        }

        if (!AssetDatabase.IsValidFolder(资源目录))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        数据库 = CreateInstance<剧情数据库>();
        AssetDatabase.CreateAsset(数据库, 数据库路径);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return 数据库;
    }

    private static void 保存数据库(剧情数据库 数据库)
    {
        EditorUtility.SetDirty(数据库);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
