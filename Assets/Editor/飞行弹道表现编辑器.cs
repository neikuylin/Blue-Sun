using UnityEditor;

[CustomEditor(typeof(飞行弹道表现))]
public sealed class 飞行弹道表现编辑器 : Editor
{
    private SerializedProperty 飞行速度;
    private SerializedProperty 出发武器挂载点;
    private SerializedProperty 火焰粒子尾焰;
    private SerializedProperty 尾焰后拖速度;
    private SerializedProperty 清除粒子重力;

    private void OnEnable()
    {
        飞行速度 = serializedObject.FindProperty("飞行速度");
        出发武器挂载点 = serializedObject.FindProperty("出发武器挂载点");
        火焰粒子尾焰 = serializedObject.FindProperty("火焰粒子尾焰");
        尾焰后拖速度 = serializedObject.FindProperty("尾焰后拖速度");
        清除粒子重力 = serializedObject.FindProperty("清除粒子重力");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(飞行速度);
        EditorGUILayout.PropertyField(出发武器挂载点);
        EditorGUILayout.Space(6f);
        EditorGUILayout.PropertyField(火焰粒子尾焰);

        if (火焰粒子尾焰.boolValue)
        {
            EditorGUILayout.PropertyField(尾焰后拖速度);
            EditorGUILayout.PropertyField(清除粒子重力);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
