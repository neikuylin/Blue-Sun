using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(武器挂载点生成器))]
public sealed class 武器挂载点生成器编辑器 : Editor
{
    private SerializedProperty 左手本地位置;
    private SerializedProperty 左手本地欧拉角;
    private SerializedProperty 右手本地位置;
    private SerializedProperty 右手本地欧拉角;

    private void OnEnable()
    {
        左手本地位置 = serializedObject.FindProperty("左手本地位置");
        左手本地欧拉角 = serializedObject.FindProperty("左手本地欧拉角");
        右手本地位置 = serializedObject.FindProperty("右手本地位置");
        右手本地欧拉角 = serializedObject.FindProperty("右手本地欧拉角");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("索拉娜武器挂载点模板", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(左手本地位置, new GUIContent("左手本地位置"));
        EditorGUILayout.PropertyField(左手本地欧拉角, new GUIContent("左手本地旋转"));
        EditorGUILayout.PropertyField(右手本地位置, new GUIContent("右手本地位置"));
        EditorGUILayout.PropertyField(右手本地欧拉角, new GUIContent("右手本地旋转"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("生成或更新武器挂载点"))
        {
            生成或更新();
        }
    }

    private void 生成或更新()
    {
        武器挂载点生成器 generator = (武器挂载点生成器)target;
        Undo.RegisterFullObjectHierarchyUndo(generator.gameObject, "生成或更新武器挂载点");

        bool success = generator.生成或更新武器挂载点(out string result);
        EditorUtility.SetDirty(generator.gameObject);

        if (PrefabUtility.IsPartOfPrefabInstance(generator.gameObject))
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(generator.gameObject);
        }

        if (success)
        {
            Debug.Log(result, generator.gameObject);
            return;
        }

        Debug.LogError(result, generator.gameObject);
    }
}
