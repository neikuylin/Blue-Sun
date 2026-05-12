using UnityEngine;

[DisallowMultipleComponent]
public sealed class 宝箱内容绑定 : MonoBehaviour
{
    [Header("宝箱内容")]
    public 物品格子区域绑定 宝箱格子区域;

    public void 打开宝箱内容(int 宝箱序列号)
    {
        if (宝箱序列号 <= 0)
        {
            return;
        }

        gameObject.SetActive(true);

        物品格子区域绑定 binding = 获取宝箱格子区域();
        if (binding != null)
        {
            binding.数据来源 = 物品格子区域绑定.数据来源类型.宝箱;
            binding.宝箱序列号 = 宝箱序列号;
        }

        InventoryShortcutRuntimeBinder.OpenChest(宝箱序列号);
    }

    public void 关闭宝箱内容()
    {
        gameObject.SetActive(false);
    }

    private 物品格子区域绑定 获取宝箱格子区域()
    {
        if (宝箱格子区域 != null)
        {
            return 宝箱格子区域;
        }

        宝箱格子区域 = GetComponentInChildren<物品格子区域绑定>(true);
        return 宝箱格子区域;
    }
}
