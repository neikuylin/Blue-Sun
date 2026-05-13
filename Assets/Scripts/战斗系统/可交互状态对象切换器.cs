using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/可交互状态对象切换器")]
public sealed class 可交互状态对象切换器 : MonoBehaviour
{
    [SerializeField] private GameObject 普通状态对象;
    [SerializeField] private GameObject 悬浮状态对象;
    [SerializeField] private GameObject 选中状态对象;
    [SerializeField] private bool 启用互斥选中 = true;

    private static 可交互状态对象切换器 selectedSwitcher;
    private bool isHighlighted;
    private bool isSelected;

    private void OnEnable()
    {
        isHighlighted = false;
        isSelected = selectedSwitcher == this;
        应用状态();
    }

    private void OnDisable()
    {
        if (selectedSwitcher == this)
        {
            selectedSwitcher = null;
        }

        isHighlighted = false;
        isSelected = false;
        应用状态();
    }

    public void 设置悬浮(bool highlighted)
    {
        if (isHighlighted == highlighted)
        {
            return;
        }

        isHighlighted = highlighted;
        应用状态();
    }

    public void 标记为选中()
    {
        if (启用互斥选中 && selectedSwitcher != null && selectedSwitcher != this)
        {
            selectedSwitcher.取消选中();
        }

        if (启用互斥选中)
        {
            selectedSwitcher = this;
        }

        isSelected = true;
        应用状态();
    }

    public void 取消选中()
    {
        if (selectedSwitcher == this)
        {
            selectedSwitcher = null;
        }

        isSelected = false;
        应用状态();
    }

    private void 应用状态()
    {
        GameObject activeObject = 解析当前状态对象();
        设置对象激活(普通状态对象, activeObject);
        设置对象激活(悬浮状态对象, activeObject);
        设置对象激活(选中状态对象, activeObject);
    }

    private GameObject 解析当前状态对象()
    {
        if (isSelected && 选中状态对象 != null)
        {
            return 选中状态对象;
        }

        if (isHighlighted && 悬浮状态对象 != null)
        {
            return 悬浮状态对象;
        }

        return 普通状态对象;
    }

    private static void 设置对象激活(GameObject target, GameObject activeObject)
    {
        if (target != null)
        {
            target.SetActive(target == activeObject);
        }
    }
}
