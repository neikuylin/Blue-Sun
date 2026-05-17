using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器挂载点生成器))]
public sealed class 武器挂载点生成器编辑器 : Editor
{
    private SerializedProperty 当前模板索引;
    private SerializedProperty 模板列表;
    private bool[] foldouts;

    private void OnEnable()
    {
        当前模板索引 = serializedObject.FindProperty("当前模板索引");
        模板列表 = serializedObject.FindProperty("模板列表");
        同步折叠状态数组();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        同步折叠状态数组();

        绘制当前模板选择();
        绘制操作按钮();
        绘制模板列表();

        serializedObject.ApplyModifiedProperties();
    }

    private void 绘制当前模板选择()
    {
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
                serializedObject.ApplyModifiedProperties();
                生成或更新();
                serializedObject.Update();
            }

            if (GUILayout.Button("保存当前模型挂载点"))
            {
                serializedObject.ApplyModifiedProperties();
                保存当前模型();
                serializedObject.Update();
                同步折叠状态数组();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("右手镜像到左手挂载点"))
            {
                serializedObject.ApplyModifiedProperties();
                右手镜像到左手挂载点();
                serializedObject.Update();
            }

            if (GUILayout.Button("右手镜像到当前模板左手"))
            {
                serializedObject.ApplyModifiedProperties();
                右手镜像到当前模板左手();
                serializedObject.Update();
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("左手镜像到右手挂载点"))
            {
                serializedObject.ApplyModifiedProperties();
                左手镜像到右手挂载点();
                serializedObject.Update();
            }

            if (GUILayout.Button("左手镜像到当前模板右手"))
            {
                serializedObject.ApplyModifiedProperties();
                左手镜像到当前模板右手();
                serializedObject.Update();
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
    }

    private void 生成或更新()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "生成或更新武器挂载点");

        bool success = generator.生成或更新武器挂载点(out string result);
        标记已修改(generator);

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

        bool success = generator.保存当前模型挂载点(out string result);
        标记已修改(generator);

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
        标记已修改(generator);

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

        bool success = generator.用右手镜像当前模板到左手(out string result);
        标记已修改(generator);

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
        标记已修改(generator);

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

        bool success = generator.用左手镜像当前模板到右手(out string result);
        标记已修改(generator);

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }

    private static void 标记已修改(武器挂载点生成器 generator)
    {
        EditorUtility.SetDirty(generator);
        EditorUtility.SetDirty(generator.gameObject);

        if (PrefabUtility.IsPartOfPrefabInstance(generator.gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(generator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(generator.gameObject);
        }
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
