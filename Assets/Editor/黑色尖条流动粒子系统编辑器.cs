using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

[CustomEditor(typeof(黑色尖条流动粒子系统))]
public sealed class 黑色尖条流动粒子系统编辑器 : Editor
{
    private readonly BoxBoundsHandle rangeHandle = new BoxBoundsHandle();

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("设为正方形范围"))
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is 黑色尖条流动粒子系统 flow)
                {
                    Undo.RecordObject(flow, "设为正方形范围");
                    flow.设为正方形范围();
                }
            }
        }
    }

    private void OnSceneGUI()
    {
        黑色尖条流动粒子系统 flow = target as 黑色尖条流动粒子系统;
        if (flow == null)
        {
            return;
        }

        Transform flowTransform = flow.transform;
        Vector2 yRange = flow.范围Y;
        float minX = Mathf.Min(flow.范围起点X, flow.范围终点X);
        float maxX = Mathf.Max(flow.范围起点X, flow.范围终点X);
        float minY = Mathf.Min(yRange.x, yRange.y);
        float maxY = Mathf.Max(yRange.x, yRange.y);

        rangeHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        rangeHandle.center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        rangeHandle.size = new Vector3(Mathf.Max(0.01f, maxX - minX), Mathf.Max(0.01f, maxY - minY), 0.01f);
        rangeHandle.handleColor = flow.Scene范围边框颜色;
        rangeHandle.wireframeColor = flow.Scene范围边框颜色;

        using (new Handles.DrawingScope(flowTransform.localToWorldMatrix))
        {
            EditorGUI.BeginChangeCheck();
            rangeHandle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(flow, "调整黑色尖条流动范围");

                Vector3 center = rangeHandle.center;
                Vector3 size = rangeHandle.size;
                float halfWidth = Mathf.Max(0.01f, size.x) * 0.5f;
                float halfHeight = Mathf.Max(0.01f, size.y) * 0.5f;

                float newMinX = center.x - halfWidth;
                float newMaxX = center.x + halfWidth;
                float newMinY = center.y - halfHeight;
                float newMaxY = center.y + halfHeight;

                if (flow.范围起点X <= flow.范围终点X)
                {
                    flow.Editor设置范围(newMinX, newMaxX, new Vector2(newMinY, newMaxY));
                }
                else
                {
                    flow.Editor设置范围(newMaxX, newMinX, new Vector2(newMinY, newMaxY));
                }

                SceneView.RepaintAll();
            }
        }
    }
}
