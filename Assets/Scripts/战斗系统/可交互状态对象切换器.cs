using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/可交互状态对象切换器")]
public sealed class 可交互状态对象切换器 : MonoBehaviour
{
    public enum 状态表现方式
    {
        对象切换 = 0,
        颜色染色 = 1
    }

    [SerializeField] private 状态表现方式 表现方式 = 状态表现方式.对象切换;
    [SerializeField] private GameObject 普通状态对象;
    [SerializeField] private GameObject 悬浮状态对象;
    [SerializeField] private GameObject 选中状态对象;
    [SerializeField] private bool 包含未激活Sprite = true;
    [SerializeField] private Color 普通颜色 = Color.white;
    [SerializeField] private Color 悬浮颜色 = Color.white;
    [SerializeField] private Color 选中颜色 = Color.white;
    [SerializeField] private bool 启用互斥选中 = true;

    private SpriteRenderer[] cachedSpriteRenderers;

    private static 可交互状态对象切换器 selectedSwitcher;
    private bool isHighlighted;
    private bool isSelected;

    private void Awake()
    {
        刷新Sprite染色对象();
    }

    private void OnEnable()
    {
        刷新Sprite染色对象();
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
        if (表现方式 == 状态表现方式.颜色染色)
        {
            应用颜色状态();
            return;
        }

        应用对象切换状态();
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

    private void 应用对象切换状态()
    {
        GameObject activeObject = 解析当前状态对象();
        设置对象激活(普通状态对象, activeObject);
        设置对象激活(悬浮状态对象, activeObject);
        设置对象激活(选中状态对象, activeObject);
    }

    private void 应用颜色状态()
    {
        if (cachedSpriteRenderers == null)
        {
            刷新Sprite染色对象();
        }

        Color targetColor = 解析当前颜色();
        if (cachedSpriteRenderers == null)
        {
            return;
        }

        for (int i = 0; i < cachedSpriteRenderers.Length; i++)
        {
            SpriteRenderer renderer = cachedSpriteRenderers[i];
            if (renderer != null)
            {
                renderer.color = targetColor;
            }
        }
    }

    private Color 解析当前颜色()
    {
        if (isSelected)
        {
            return 选中颜色;
        }

        if (isHighlighted)
        {
            return 悬浮颜色;
        }

        return 普通颜色;
    }

    public void 刷新Sprite染色对象()
    {
        cachedSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(包含未激活Sprite);
    }

    private static void 设置对象激活(GameObject target, GameObject activeObject)
    {
        if (target != null)
        {
            target.SetActive(target == activeObject);
        }
    }
}
