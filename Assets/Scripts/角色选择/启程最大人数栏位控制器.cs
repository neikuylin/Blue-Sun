using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("角色选择/启程最大人数栏位控制器")]
public sealed class 启程最大人数栏位控制器 : MonoBehaviour
{
    [SerializeField] private int 记录的最大人数;
    [SerializeField] private GameObject 第1个玩家栏位按钮;
    [SerializeField] private GameObject 第2个玩家栏位按钮;
    [SerializeField] private GameObject 第3个玩家栏位按钮;
    [SerializeField] private GameObject 第4个玩家栏位按钮;

    public int 只读记录的最大人数 => 记录的最大人数;

    private void OnEnable()
    {
        刷新最大人数并应用栏位();
    }

    public void 刷新最大人数并应用栏位()
    {
        string templateId = 副本选择状态.当前选择地图模板ID;
        if (string.IsNullOrWhiteSpace(templateId))
        {
            Debug.LogWarning($"{name}：启程最大人数栏位控制器没有读到副本选择记录。", this);
            return;
        }

        MapTemplateDatabase database = MapTemplateDatabase.LoadDefault();
        MapTemplateDatabase.MapTemplateEntry template = database != null ? database.FindEntry(templateId) : null;
        if (template == null)
        {
            Debug.LogWarning($"{name}：启程最大人数栏位控制器找不到地图模板：{templateId}", this);
            return;
        }

        记录的最大人数 = Mathf.Max(1, template.maxPartySize);
        应用栏位显示();
    }

    private void 应用栏位显示()
    {
        List<GameObject> buttons = 收集按钮();
        for (int i = 0; i < buttons.Count; i++)
        {
            GameObject button = buttons[i];
            if (button == null)
            {
                Debug.LogWarning($"{name}：启程最大人数栏位控制器第 {i + 1} 个玩家栏位按钮没有绑定。", this);
                continue;
            }

            button.SetActive(i < 记录的最大人数);
        }
    }

    private List<GameObject> 收集按钮()
    {
        return new List<GameObject>
        {
            第1个玩家栏位按钮,
            第2个玩家栏位按钮,
            第3个玩家栏位按钮,
            第4个玩家栏位按钮
        };
    }
}
