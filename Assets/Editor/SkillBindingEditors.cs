using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SkillBarBinding), true)]
public sealed class SkillBarBindingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Draw("skillPanel", "技能栏位");
        Draw("skillSlotArea", "技能格子区域");
        Draw("slotTemplate", "预设空格子");
        Draw("grantedMarkerSprite", "锁定图片");
        Draw("grantedMarkerPosition", "锁定图片位置");
        serializedObject.ApplyModifiedProperties();
    }

    private void Draw(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }
}

[CustomEditor(typeof(SkillWarehouseBinding), true)]
public sealed class SkillWarehouseBindingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        Draw("warehousePanel", "技能仓库");
        Draw("warehouseSlotArea", "技能格子区域");
        Draw("slotTemplate", "预设空格子");
        serializedObject.ApplyModifiedProperties();
    }

    private void Draw(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label));
        }
    }
}
