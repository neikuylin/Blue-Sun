using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("战斗/房间方向按钮")]
public sealed class 房间方向按钮 : MonoBehaviour
{
    public enum Direction
    {
        东 = 0,
        南 = 1,
        西 = 2,
        北 = 3
    }

    [SerializeField, InspectorName("方向")] private Direction direction = Direction.东;
    [SerializeField, InspectorName("普通状态图片对象")] private Image normalImageObject;
    [SerializeField, InspectorName("悬浮状态图片对象")] private Image highlightedImageObject;
    [SerializeField, InspectorName("选中状态图片对象")] private Image selectedImageObject;

    private static 房间方向按钮 selectedButton;
    private bool isHighlighted;
    private bool isSelected;

    private void OnEnable()
    {
        isHighlighted = false;
        isSelected = selectedButton == this;
        ApplyVisualState();
    }

    private void OnDisable()
    {
        if (selectedButton == this)
        {
            selectedButton = null;
        }

        isHighlighted = false;
        isSelected = false;
        ApplyVisualState();
    }

    public void 标记为选中()
    {
        if (selectedButton != null && selectedButton != this)
        {
            selectedButton.取消选中();
        }

        selectedButton = this;
        isSelected = true;
        ApplyVisualState();
    }

    public void 设置悬浮(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        ApplyVisualState();
    }

    public bool TryGetConnectionDirection(out MapTemplateDatabase.ConnectionDirection resolvedDirection)
    {
        return TryConvertDirection(direction, out resolvedDirection);
    }

    private void 取消选中()
    {
        isSelected = false;
        ApplyVisualState();
    }

    private void ApplyVisualState()
    {
        Image activeImageObject = ResolveActiveObject();
        SetImageObjectActive(normalImageObject, activeImageObject);
        SetImageObjectActive(highlightedImageObject, activeImageObject);
        SetImageObjectActive(selectedImageObject, activeImageObject);
    }

    private Image ResolveActiveObject()
    {
        if (isSelected && selectedImageObject != null)
        {
            return selectedImageObject;
        }

        if (isHighlighted && highlightedImageObject != null)
        {
            return highlightedImageObject;
        }

        return normalImageObject;
    }

    private static void SetImageObjectActive(Image target, Image activeImageObject)
    {
        if (target != null)
        {
            target.gameObject.SetActive(target == activeImageObject);
        }
    }

    private static bool TryConvertDirection(
        Direction value,
        out MapTemplateDatabase.ConnectionDirection resolvedDirection)
    {
        switch (value)
        {
            case Direction.东:
                resolvedDirection = MapTemplateDatabase.ConnectionDirection.East;
                return true;
            case Direction.南:
                resolvedDirection = MapTemplateDatabase.ConnectionDirection.South;
                return true;
            case Direction.西:
                resolvedDirection = MapTemplateDatabase.ConnectionDirection.West;
                return true;
            case Direction.北:
                resolvedDirection = MapTemplateDatabase.ConnectionDirection.North;
                return true;
            default:
                Debug.LogError($"房间方向按钮：未知方向 '{value}'。");
                resolvedDirection = default;
                return false;
        }
    }
}
