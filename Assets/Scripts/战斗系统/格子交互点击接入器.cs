using System;
using UnityEngine;

internal sealed class 格子交互点击接入器
{
    private 房间方向按钮 hoveredDoorButton;

    public bool 处理点击(
        Camera battleCamera,
        Action<MapTemplateDatabase.ConnectionDirection> 请求门交互,
        Func<格子物件触发器, bool> 请求物件交互)
    {
        if (!Input.GetMouseButtonDown(0) || BattleInputService.IsPointerBlockedByUi() || battleCamera == null)
        {
            return false;
        }

        交互命中 hit;
        if (!尝试查找指针下交互(battleCamera, out hit))
        {
            return false;
        }

        if (hit.门按钮 != null &&
            hit.门按钮.TryGetConnectionDirection(out MapTemplateDatabase.ConnectionDirection direction))
        {
            SetHoveredDoorButton(hit.门按钮);
            hit.门按钮.标记为选中();
            请求门交互?.Invoke(direction);
            return true;
        }

        if (hit.物件触发器 != null)
        {
            return hit.物件触发器.请求点击触发(请求物件交互);
        }

        return false;
    }

    public void 更新悬浮(Camera battleCamera)
    {
        if (BattleInputService.IsPointerBlockedByUi() || battleCamera == null)
        {
            SetHoveredDoorButton(null);
            return;
        }

        交互命中 hit;
        SetHoveredDoorButton(尝试查找指针下交互(battleCamera, out hit) ? hit.门按钮 : null);
    }

    private bool 尝试查找指针下交互(Camera battleCamera, out 交互命中 hit)
    {
        hit = default;
        bool found = false;
        Ray ray = battleCamera.ScreenPointToRay(Input.mousePosition);

        RaycastHit[] hits3D = Physics.RaycastAll(ray, float.PositiveInfinity);
        if (hits3D != null && hits3D.Length > 0)
        {
            Array.Sort(hits3D, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits3D.Length; i++)
            {
                if (尝试解析3D命中(hits3D[i], out 交互命中 candidate) &&
                    (!found || candidate.距离 < hit.距离))
                {
                    hit = candidate;
                    found = true;
                }
            }
        }

        RaycastHit2D[] hits2D = Physics2D.GetRayIntersectionAll(ray, float.PositiveInfinity);
        if (hits2D != null && hits2D.Length > 0)
        {
            Array.Sort(hits2D, (left, right) => left.distance.CompareTo(right.distance));
            for (int i = 0; i < hits2D.Length; i++)
            {
                if (尝试解析2D命中(hits2D[i], out 交互命中 candidate) &&
                    (!found || candidate.距离 < hit.距离))
                {
                    hit = candidate;
                    found = true;
                }
            }
        }

        return found;
    }

    private static bool 尝试解析3D命中(RaycastHit source, out 交互命中 hit)
    {
        hit = default;
        if (source.collider == null)
        {
            return false;
        }

        房间方向按钮 doorButton = source.collider.GetComponentInParent<房间方向按钮>();
        if (doorButton != null)
        {
            hit = 交互命中.创建门(source.distance, doorButton);
            return true;
        }

        格子物件触发器 trigger = source.collider.GetComponentInParent<格子物件触发器>();
        if (trigger != null)
        {
            hit = 交互命中.创建物件(source.distance, trigger);
            return true;
        }

        return false;
    }

    private static bool 尝试解析2D命中(RaycastHit2D source, out 交互命中 hit)
    {
        hit = default;
        if (source.collider == null)
        {
            return false;
        }

        格子物件触发器 trigger = source.collider.GetComponentInParent<格子物件触发器>();
        if (trigger != null)
        {
            hit = 交互命中.创建物件(source.distance, trigger);
            return true;
        }

        return false;
    }

    private void SetHoveredDoorButton(房间方向按钮 doorButton)
    {
        if (hoveredDoorButton == doorButton)
        {
            return;
        }

        if (hoveredDoorButton != null)
        {
            hoveredDoorButton.设置悬浮(false);
        }

        hoveredDoorButton = doorButton;

        if (hoveredDoorButton != null)
        {
            hoveredDoorButton.设置悬浮(true);
        }
    }

    private struct 交互命中
    {
        public float 距离;
        public 房间方向按钮 门按钮;
        public 格子物件触发器 物件触发器;

        public static 交互命中 创建门(float 距离, 房间方向按钮 门按钮)
        {
            return new 交互命中
            {
                距离 = 距离,
                门按钮 = 门按钮
            };
        }

        public static 交互命中 创建物件(float 距离, 格子物件触发器 物件触发器)
        {
            return new 交互命中
            {
                距离 = 距离,
                物件触发器 = 物件触发器
            };
        }
    }
}
