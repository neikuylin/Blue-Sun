using UnityEditor;

[CustomEditor(typeof(黑色尖条流动粒子系统))]
public sealed class 黑色尖条流动粒子系统编辑器 : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
    }
}
