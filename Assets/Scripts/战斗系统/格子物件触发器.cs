using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/格子物件触发器")]
public sealed class 格子物件触发器 : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private string 触发器名称 = string.Empty;
    [SerializeField] private List<格子模板数据库.CellPosition> 触发格列表 = new List<格子模板数据库.CellPosition>();
    [SerializeField] private UnityEvent 到达后触发 = new UnityEvent();

    private Action 运行时触发动作;
    private int 最近点击帧 = -1;

    public string 名称 => string.IsNullOrWhiteSpace(触发器名称) ? name : 触发器名称.Trim();

    public IReadOnlyList<格子模板数据库.CellPosition> 触发格 => 触发格列表;

    public void 初始化(string 名称, IEnumerable<格子模板数据库.CellPosition> 触发格)
    {
        触发器名称 = string.IsNullOrWhiteSpace(名称) ? name : 名称.Trim();
        if (触发格列表 == null)
        {
            触发格列表 = new List<格子模板数据库.CellPosition>();
        }
        else
        {
            触发格列表.Clear();
        }

        if (触发格 == null)
        {
            return;
        }

        foreach (格子模板数据库.CellPosition cell in 触发格)
        {
            触发格列表.Add(cell);
        }
    }

    public void 设置运行时触发动作(Action action)
    {
        运行时触发动作 = action;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        请求点击触发();
    }

    private void OnMouseDown()
    {
        请求点击触发();
    }

    public bool 请求点击触发()
    {
        if (最近点击帧 == Time.frameCount)
        {
            return true;
        }

        最近点击帧 = Time.frameCount;
        if (BattleInputService.IsPointerBlockedByUi())
        {
            return true;
        }

        播放点击反馈();
        BattleTurnSystem turnSystem = FindObjectOfType<BattleTurnSystem>();
        if (turnSystem == null)
        {
            return false;
        }

        return turnSystem.TryTriggerGridInteraction(this);
    }

    public bool 包含触发格(Vector2Int cell)
    {
        if (触发格列表 == null)
        {
            return false;
        }

        for (int i = 0; i < 触发格列表.Count; i++)
        {
            if (触发格列表[i].ToVector2Int() == cell)
            {
                return true;
            }
        }

        return false;
    }

    public void 执行到达触发()
    {
        到达后触发?.Invoke();
        运行时触发动作?.Invoke();
    }

    public void 播放点击反馈()
    {
        // 先保留统一入口，后续战斗中弹窗或物件点击动效都接这里。
        Debug.Log($"格子物件触发器：点击了 '{名称}'。", this);
    }
}
