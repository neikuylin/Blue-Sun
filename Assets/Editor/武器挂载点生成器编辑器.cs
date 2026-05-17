using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器挂载点生成器))]
public sealed class 武器挂载点生成器编辑器 : Editor
{
    private const string DatabasePath = "Assets/Resources/武器挂载点模板数据库.asset";

    private SerializedProperty 模板数据库;
    private SerializedProperty 当前模板索引;
    private SerializedObject databaseObject;
    private SerializedProperty 模板列表;
    private bool[] foldouts;

    private void OnEnable()
    {
        模板数据库 = serializedObject.FindProperty("模板数据库");
        当前模板索引 = serializedObject.FindProperty("当前模板索引");
        确保数据库();
        绑定数据库对象();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        确保数据库();
        绑定数据库对象();

        if (databaseObject == null)
        {
            EditorGUILayout.HelpBox($"无法加载模板数据库：{DatabasePath}", MessageType.Error);
            serializedObject.ApplyModifiedProperties();
            return;
        }

        databaseObject.Update();
        同步折叠状态数组();

        绘制数据库引用();
        绘制当前模板选择();
        绘制操作按钮();
        绘制模板列表();

        databaseObject.ApplyModifiedProperties();
        serializedObject.ApplyModifiedProperties();
    }

    private void 绘制数据库引用()
    {
        EditorGUILayout.LabelField("共享模板数据库", EditorStyles.boldLabel);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.PropertyField(模板数据库, new GUIContent("模板数据库"));
        }
    }

    private void 绘制当前模板选择()
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("武器挂载点模板", EditorStyles.boldLabel);

        if (模板列表.arraySize == 0)
        {
            EditorGUILayout.HelpBox("当前没有保存任何模型模板。", MessageType.Warning);
            return;
        }

        当前模板索引.intValue = Mathf.Clamp(当前模板索引.intValue, 0, 模板列表.arraySize - 1);
        string[] options = new string[模板列表.arraySize];
        for (int i = 0; i < 模板列表.arraySize; i++)
        {
            SerializedProperty template = 模板列表.GetArrayElementAtIndex(i);
            SerializedProperty name = template.FindPropertyRelative("模型名称");
            options[i] = string.IsNullOrWhiteSpace(name.stringValue) ? $"未命名模型 {i + 1}" : name.stringValue;
        }

        当前模板索引.intValue = EditorGUILayout.Popup("当前使用模板", 当前模板索引.intValue, options);
    }

    private void 绘制操作按钮()
    {
        EditorGUILayout.Space(8f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("生成或更新武器挂载点"))
            {
                应用序列化修改();
                生成或更新();
                重新加载序列化对象();
            }

            if (GUILayout.Button("保存当前模型挂载点"))
            {
                应用序列化修改();
                保存当前模型();
                重新加载序列化对象();
                同步折叠状态数组();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("右手镜像到左手挂载点"))
            {
                应用序列化修改();
                右手镜像到左手挂载点();
                重新加载序列化对象();
            }

            if (GUILayout.Button("右手镜像到当前模板左手"))
            {
                应用序列化修改();
                右手镜像到当前模板左手();
                重新加载序列化对象();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("左手镜像到右手挂载点"))
            {
                应用序列化修改();
                左手镜像到右手挂载点();
                重新加载序列化对象();
            }

            if (GUILayout.Button("左手镜像到当前模板右手"))
            {
                应用序列化修改();
                左手镜像到当前模板右手();
                重新加载序列化对象();
            }
        }
    }

    private void 绘制模板列表()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("已保存模型模板", EditorStyles.boldLabel);

        for (int i = 0; i < 模板列表.arraySize; i++)
        {
            SerializedProperty template = 模板列表.GetArrayElementAtIndex(i);
            SerializedProperty name = template.FindPropertyRelative("模型名称");
            string title = string.IsNullOrWhiteSpace(name.stringValue) ? $"未命名模型 {i + 1}" : name.stringValue;

            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], title, true);
            if (!foldouts[i])
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.PropertyField(name, new GUIContent("模型名称"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("左手本地位置"), new GUIContent("左手本地位置"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("左手本地欧拉角"), new GUIContent("左手本地旋转"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("右手本地位置"), new GUIContent("右手本地位置"));
                EditorGUILayout.PropertyField(template.FindPropertyRelative("右手本地欧拉角"), new GUIContent("右手本地旋转"));

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("设为当前模板"))
                    {
                        当前模板索引.intValue = i;
                    }

                    if (GUILayout.Button("删除模板"))
                    {
                        删除模板(i);
                        return;
                    }
                }
            }
        }
    }

    private void 删除模板(int index)
    {
        模板列表.DeleteArrayElementAtIndex(index);
        if (模板列表.arraySize == 0)
        {
            当前模板索引.intValue = 0;
        }
        else
        {
            当前模板索引.intValue = Mathf.Clamp(当前模板索引.intValue, 0, 模板列表.arraySize - 1);
        }

        同步折叠状态数组();
        标记数据库已修改();
    }

    private void 生成或更新()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "生成或更新武器挂载点");

        bool success = generator.生成或更新武器挂载点(out string result);
        标记生成器已修改(generator);

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private void 保存当前模型()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "保存当前模型武器挂载点");
        Undo.RecordObject((UnityEngine.Object)模板数据库.objectReferenceValue, "保存当前模型武器挂载点模板");

        bool success = generator.保存当前模型挂载点(out string result);
        标记生成器已修改(generator);
        标记数据库已修改();

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private void 右手镜像到左手挂载点()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "右手镜像到左手挂载点");

        bool success = generator.用右手镜像当前挂载点到左手(out string result);
        标记生成器已修改(generator);

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private void 右手镜像到当前模板左手()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "右手镜像到当前模板左手");
        Undo.RecordObject((UnityEngine.Object)模板数据库.objectReferenceValue, "右手镜像到当前模板左手");

        bool success = generator.用右手镜像当前模板到左手(out string result);
        标记生成器已修改(generator);
        标记数据库已修改();

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private void 左手镜像到右手挂载点()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "左手镜像到右手挂载点");

        bool success = generator.用左手镜像当前挂载点到右手(out string result);
        标记生成器已修改(generator);

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private void 左手镜像到当前模板右手()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "左手镜像到当前模板右手");
        Undo.RecordObject((UnityEngine.Object)模板数据库.objectReferenceValue, "左手镜像到当前模板右手");

        bool success = generator.用左手镜像当前模板到右手(out string result);
        标记生成器已修改(generator);
        标记数据库已修改();

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private void 确保数据库()
    {
        武器挂载点模板数据库 database = 模板数据库.objectReferenceValue as 武器挂载点模板数据库;
        if (database == null)
        {
            database = AssetDatabase.LoadAssetAtPath<武器挂载点模板数据库>(DatabasePath);
        }

        if (database == null)
        {
            string directory = Path.GetDirectoryName(DatabasePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            database = CreateInstance<武器挂载点模板数据库>();
            AssetDatabase.CreateAsset(database, DatabasePath);
            AssetDatabase.SaveAssets();
        }

        if (模板数据库.objectReferenceValue != database)
        {
            模板数据库.objectReferenceValue = database;
            serializedObject.ApplyModifiedProperties();
        }

        ((武器挂载点生成器)target).设置模板数据库(database);
    }

    private void 绑定数据库对象()
    {
        武器挂载点模板数据库 database = 模板数据库.objectReferenceValue as 武器挂载点模板数据库;
        if (database == null)
        {
            databaseObject = null;
            模板列表 = null;
            return;
        }

        if (databaseObject == null || databaseObject.targetObject != database)
        {
            databaseObject = new SerializedObject(database);
            模板列表 = databaseObject.FindProperty("模板列表");
            foldouts = null;
        }
    }

    private void 应用序列化修改()
    {
        databaseObject?.ApplyModifiedProperties();
        serializedObject.ApplyModifiedProperties();
    }

    private void 重新加载序列化对象()
    {
        serializedObject.Update();
        databaseObject?.Update();
    }

    private static void 标记生成器已修改(武器挂载点生成器 generator)
    {
        EditorUtility.SetDirty(generator);
        EditorUtility.SetDirty(generator.gameObject);

        if (PrefabUtility.IsPartOfPrefabInstance(generator.gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(generator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(generator.gameObject);
        }
    }

    private void 标记数据库已修改()
    {
        UnityEngine.Object database = 模板数据库.objectReferenceValue;
        if (database == null)
        {
            return;
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();
    }

    private void 同步折叠状态数组()
    {
        if (模板列表 == null)
        {
            foldouts = new bool[0];
            return;
        }

        if (foldouts != null && foldouts.Length == 模板列表.arraySize)
        {
            return;
        }

        bool[] nextFoldouts = new bool[模板列表.arraySize];
        for (int i = 0; i < nextFoldouts.Length; i++)
        {
            nextFoldouts[i] = foldouts != null && i < foldouts.Length ? foldouts[i] : false;
        }

        foldouts = nextFoldouts;
    }
}
