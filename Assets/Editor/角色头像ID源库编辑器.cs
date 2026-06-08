using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(角色头像ID源库))]
public sealed class 角色头像ID源库编辑器 : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty entries = serializedObject.FindProperty("entries");
        EditorGUILayout.LabelField("角色头像ID源库", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("战斗角色栏按角色ID读取这里的头像和布局。没有登记就没有头像。", MessageType.Info);

        if (entries != null)
        {
            for (int i = 0; i < entries.arraySize; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                DrawEntry(entries, entry, i);
                EditorGUILayout.Space(4f);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新增头像ID源"))
                {
                    int index = entries.arraySize;
                    entries.InsertArrayElementAtIndex(index);
                    SerializedProperty entry = entries.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("characterId").stringValue = string.Empty;
                    entry.FindPropertyRelative("portraitSprite").objectReferenceValue = null;
                    ResetLayout(entry.FindPropertyRelative("portraitLayout"));
                }
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawEntry(SerializedProperty entries, SerializedProperty entry, int index)
    {
        SerializedProperty characterId = entry.FindPropertyRelative("characterId");
        SerializedProperty portraitSprite = entry.FindPropertyRelative("portraitSprite");
        SerializedProperty portraitLayout = entry.FindPropertyRelative("portraitLayout");

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"头像 {index + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("删除", GUILayout.Width(56f)))
                {
                    entries.DeleteArrayElementAtIndex(index);
                    GUIUtility.ExitGUI();
                }
            }

            EditorGUILayout.PropertyField(characterId, new GUIContent("角色ID"));
            EditorGUILayout.PropertyField(portraitSprite, new GUIContent("头像图片"));
            DrawLayout(portraitLayout);
        }
    }

    private static void DrawLayout(SerializedProperty layout)
    {
        if (layout == null)
        {
            return;
        }

        EditorGUILayout.LabelField("头像布局", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(layout.FindPropertyRelative("anchorMin"), new GUIContent("锚点最小值"));
        EditorGUILayout.PropertyField(layout.FindPropertyRelative("anchorMax"), new GUIContent("锚点最大值"));
        EditorGUILayout.PropertyField(layout.FindPropertyRelative("pivot"), new GUIContent("轴心"));
        EditorGUILayout.PropertyField(layout.FindPropertyRelative("anchoredPosition"), new GUIContent("锚定位置"));
        EditorGUILayout.PropertyField(layout.FindPropertyRelative("sizeDelta"), new GUIContent("尺寸"));
        EditorGUILayout.PropertyField(layout.FindPropertyRelative("localScale"), new GUIContent("缩放"));
    }

    private static void ResetLayout(SerializedProperty layout)
    {
        if (layout == null)
        {
            return;
        }

        layout.FindPropertyRelative("anchorMin").vector2Value = new Vector2(0.5f, 0.5f);
        layout.FindPropertyRelative("anchorMax").vector2Value = new Vector2(0.5f, 0.5f);
        layout.FindPropertyRelative("pivot").vector2Value = new Vector2(0.5f, 0.5f);
        layout.FindPropertyRelative("anchoredPosition").vector2Value = Vector2.zero;
        layout.FindPropertyRelative("sizeDelta").vector2Value = Vector2.zero;
        layout.FindPropertyRelative("localScale").vector3Value = Vector3.one;
    }
}
