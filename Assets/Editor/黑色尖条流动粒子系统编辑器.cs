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
    }

    private void OnSceneGUI()
    {
        黑色尖条流动粒子系统 flow = (黑色尖条流动粒子系统)target;
        Transform flowTransform = flow.transform;
        Vector2 yRange = flow.范围Y;
        float minX = Mathf.Min(flow.范围起点X, flow.范围终点X);
        float maxX = Mathf.Max(flow.范围起点X, flow.范围终点X);
        float minY = Mathf.Min(yRange.x, yRange.y);
        float maxY = Mathf.Max(yRange.x, yRange.y);

        rangeHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
        rangeHandle.center = new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f);
        rangeHandle.size = new Vector3(maxX - minX, maxY - minY, 0.01f);
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
                float halfWidth = size.x * 0.5f;
                float halfHeight = size.y * 0.5f;

                flow.Editor设置范围(
                    center.x - halfWidth,
                    center.x + halfWidth,
                    new Vector2(center.y - halfHeight, center.y + halfHeight));
            }
        }
    }
}
