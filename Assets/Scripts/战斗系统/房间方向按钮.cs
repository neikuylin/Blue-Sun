using UnityEngine;

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

    private void OnMouseUpAsButton()
    {
        if (!TryConvertDirection(direction, out MapTemplateDatabase.ConnectionDirection resolvedDirection))
        {
            return;
        }

        BattleBootstrap.NavigateToDirection(resolvedDirection);
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
