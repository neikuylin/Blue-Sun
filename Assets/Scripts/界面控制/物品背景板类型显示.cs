using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[AddComponentMenu("界面/物品背景板类型显示")]
public sealed class 物品背景板类型显示 : MonoBehaviour
{
    public enum 显示类型
    {
        未识别,
        仓库,
        技能仓库,
        属性,
        背包,
        宝箱
    }

    [Header("文字")]
    [SerializeField] private GameObject 类型文字对象;

    [Header("大类入口")]
    [SerializeField] private GameObject 技能仓库入口对象;
    [SerializeField] private GameObject 属性入口对象;

    [Header("物品细分")]
    [SerializeField] private GameObject 物品区域绑定对象;

    [Header("图片")]
    [SerializeField] private GameObject 仓库图片对象;
    [SerializeField] private GameObject 技能仓库图片对象;
    [SerializeField] private GameObject 属性图片对象;
    [SerializeField] private GameObject 背包图片对象;
    [SerializeField] private GameObject 宝箱图片对象;

    private 显示类型 已应用类型 = 显示类型.未识别;
    private bool 已应用过;

    public 显示类型 当前显示类型 => 解析显示类型();

    private void Reset()
    {
        按子物体名称自动绑定();
    }

    private void OnEnable()
    {
        刷新显示();
    }

    private void LateUpdate()
    {
        刷新显示();
    }

    public void 刷新显示()
    {
        显示类型 类型 = 解析显示类型();
        if (!已应用过 || 已应用类型 != 类型)
        {
            应用显示类型(类型);
            已应用类型 = 类型;
            已应用过 = true;
        }
    }

    public void 按子物体名称自动绑定()
    {
        绑定子物体(ref 类型文字对象, "物品类型text");
        绑定子物体(ref 类型文字对象, "格子类型text");
        绑定子物体(ref 仓库图片对象, "仓库");
        绑定子物体(ref 技能仓库图片对象, "技能仓库");
        绑定子物体(ref 属性图片对象, "属性");
        绑定子物体(ref 背包图片对象, "背包");
        绑定子物体(ref 宝箱图片对象, "宝箱");
    }

    public static string 获取显示名称(显示类型 类型)
    {
        switch (类型)
        {
            case 显示类型.仓库:
                return "仓库";
            case 显示类型.技能仓库:
                return "技能仓库";
            case 显示类型.属性:
                return "属性";
            case 显示类型.背包:
                return "背包";
            case 显示类型.宝箱:
                return "宝箱";
            default:
                return string.Empty;
        }
    }

    private 显示类型 解析显示类型()
    {
        if (入口Toggle已选中(技能仓库入口对象))
        {
            return 显示类型.技能仓库;
        }

        if (入口Toggle已选中(属性入口对象))
        {
            return 显示类型.属性;
        }

        return 解析物品显示类型(获取物品绑定());
    }

    private 显示类型 解析物品显示类型(物品格子区域绑定 物品绑定)
    {
        if (物品绑定 == null)
        {
            return 显示类型.未识别;
        }

        switch (物品绑定.数据来源)
        {
            case 物品格子区域绑定.数据来源类型.仓库:
                return 显示类型.仓库;
            case 物品格子区域绑定.数据来源类型.背包:
                return 显示类型.背包;
            case 物品格子区域绑定.数据来源类型.宝箱:
                return 显示类型.宝箱;
            default:
                return 显示类型.未识别;
        }
    }

    private 物品格子区域绑定 获取物品绑定()
    {
        if (物品区域绑定对象 == null)
        {
            return null;
        }

        return 物品区域绑定对象.GetComponent<物品格子区域绑定>();
    }

    private void 应用显示类型(显示类型 类型)
    {
        TMP_Text 类型文字 = 类型文字对象 != null ? 类型文字对象.GetComponent<TMP_Text>() : null;
        if (类型文字 != null)
        {
            类型文字.text = 获取显示名称(类型);
        }

        设置图片显示(仓库图片对象, 类型 == 显示类型.仓库);
        设置图片显示(技能仓库图片对象, 类型 == 显示类型.技能仓库);
        设置图片显示(属性图片对象, 类型 == 显示类型.属性);
        设置图片显示(背包图片对象, 类型 == 显示类型.背包);
        设置图片显示(宝箱图片对象, 类型 == 显示类型.宝箱);
    }

    private static bool 入口Toggle已选中(GameObject 入口对象)
    {
        Toggle toggle = 查找Toggle(入口对象);
        return toggle != null && toggle.isOn;
    }

    private static Toggle 查找Toggle(GameObject 根物体)
    {
        if (根物体 == null)
        {
            return null;
        }

        Toggle toggle = 根物体.GetComponent<Toggle>();
        if (toggle != null)
        {
            return toggle;
        }

        return 根物体.GetComponentInChildren<Toggle>(true);
    }

    private void 设置图片显示(GameObject 图片对象, bool 显示)
    {
        if (图片对象 != null && 图片对象.activeSelf != 显示)
        {
            图片对象.SetActive(显示);
        }
    }

    private void 绑定子物体(ref GameObject 字段, string 子物体名称)
    {
        GameObject 子物体 = 查找子物体(gameObject, 子物体名称);
        if (子物体 != null)
        {
            字段 = 子物体;
        }
    }

    private static GameObject 查找子物体(GameObject 根物体, string 子物体名称)
    {
        if (根物体 == null || string.IsNullOrEmpty(子物体名称))
        {
            return null;
        }

        Transform[] 所有子物体 = 根物体.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < 所有子物体.Length; i++)
        {
            Transform 子物体 = 所有子物体[i];
            if (子物体 != null && 子物体.name == 子物体名称)
            {
                return 子物体.gameObject;
            }
        }

        return null;
    }
}
