using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/摄像机边界参考图")]
public sealed class 摄像机边界参考图 : MonoBehaviour
{
    public enum 边界方向
    {
        北 = 0,
        南 = 1,
        东 = 2,
        西 = 3
    }

    [InspectorName("边界方向")]
    [SerializeField] private 边界方向 direction = 边界方向.北;
    [InspectorName("目标Sprite物体")]
    [SerializeField] private GameObject targetSpriteObject;
    [InspectorName("位置偏移")]
    [SerializeField] private float positionOffset;

    public 边界方向 Direction => direction;

    public bool TryCalculateBoundaryValue(Camera cameraToUse, out float value)
    {
        value = 0f;
        if (cameraToUse == null)
        {
            Debug.LogError("摄像机边界参考图：缺少摄像机。", this);
            return false;
        }

        if (targetSpriteObject == null)
        {
            Debug.LogError("摄像机边界参考图：目标Sprite物体为空。", this);
            return false;
        }

        SpriteRenderer spriteRenderer = targetSpriteObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"摄像机边界参考图：目标Sprite物体 '{targetSpriteObject.name}' 没有 SpriteRenderer。", this);
            return false;
        }

        if (spriteRenderer.sprite == null)
        {
            Debug.LogError($"摄像机边界参考图：目标Sprite物体 '{targetSpriteObject.name}' 没有 Sprite。", this);
            return false;
        }

        Vector3 axis = IsHorizontalBoundary(direction) ? cameraToUse.transform.up : cameraToUse.transform.right;
        Bounds bounds = spriteRenderer.sprite.bounds;
        bool useMax = direction == 边界方向.北 || direction == 边界方向.东;
        float resolvedValue = useMax ? float.NegativeInfinity : float.PositiveInfinity;

        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                Vector3 localCorner = new Vector3(
                    x == 0 ? bounds.min.x : bounds.max.x,
                    y == 0 ? bounds.min.y : bounds.max.y,
                    0f);
                Vector3 worldCorner = spriteRenderer.transform.TransformPoint(localCorner);
                float projected = Vector3.Dot(worldCorner, axis);
                resolvedValue = useMax
                    ? Mathf.Max(resolvedValue, projected)
                    : Mathf.Min(resolvedValue, projected);
            }
        }

        value = resolvedValue + ResolveSignedOffset();
        return true;
    }

    private float ResolveSignedOffset()
    {
        return direction == 边界方向.北 || direction == 边界方向.东
            ? positionOffset
            : -positionOffset;
    }

    private static bool IsHorizontalBoundary(边界方向 direction)
    {
        return direction == 边界方向.北 || direction == 边界方向.南;
    }
}
