using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 武器详情视图 : MonoBehaviour
{
    [SerializeField] private Image 背景图;
    [SerializeField] private Image 物品图标;
    [SerializeField] private TMP_Text 物品名字文本;
    [SerializeField] private TMP_Text 品质文本;
    [SerializeField] private TMP_Text 武器分类文本;
    [SerializeField] private TMP_Text 装备者文本;
    [SerializeField] private TMP_Text 攻击力文本;
    [SerializeField] private TMP_Text 固定伤害文本;
    [SerializeField] private TMP_Text 属性加成文本;
    [SerializeField] private TMP_Text 文本介绍文本;
    [SerializeField] private TMP_Text 附带技能文本;
    [SerializeField] private RectTransform 附带技能图标区域;
    [SerializeField] private RectTransform 下背景;
    [SerializeField] private TMP_Text 下文本内容;
    [SerializeField] private RectTransform 展开提示;

    private readonly List<GameObject> 已创建附带技能图标 = new List<GameObject>();

    public RectTransform 根节点 => transform as RectTransform;
    public RectTransform 下背景节点 => 下背景;
    public RectTransform 文本内容节点 => 物品名字文本 != null ? 物品名字文本.transform.parent as RectTransform : null;
    public RectTransform 展开提示节点 => 展开提示;
    public TMP_Text 下文本内容组件 => 下文本内容;
    public Image 背景图组件 => 背景图;
    public Image 物品图标组件 => 物品图标;
    public TMP_Text 物品名字文本组件 => 物品名字文本;
    public TMP_Text 品质文本组件 => 品质文本;
    public TMP_Text 武器分类文本组件 => 武器分类文本;
    public TMP_Text 装备者文本组件 => 装备者文本;
    public TMP_Text 攻击力文本组件 => 攻击力文本;
    public TMP_Text 固定伤害文本组件 => 固定伤害文本;
    public TMP_Text 属性加成文本组件 => 属性加成文本;
    public TMP_Text 文本介绍文本组件 => 文本介绍文本;
    public TMP_Text 附带技能文本组件 => 附带技能文本;
    public RectTransform 附带技能图标区域节点 => 附带技能图标区域;

    public sealed class Snapshot
    {
        public Sprite 背景Sprite;
        public Sprite 物品图标Sprite;
        public Vector2 物品图标尺寸;
        public Vector3 物品图标缩放 = Vector3.one;
        public Material 物品图标材质;
        public string 物品名字;
        public string 品质;
        public string 武器分类;
        public string 装备者;
        public string 攻击力;
        public string 固定伤害;
        public string 属性加成;
        public string 文本介绍;
        public string 下文本内容;
        public IReadOnlyList<Sprite> 附带技能图标Sprites;
    }

    public void 刷新(Snapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("武器详情视图刷新失败：显示数据为空。");
            return;
        }

        设置背景(snapshot.背景Sprite);
        设置物品图标(snapshot);
        设置文本(物品名字文本, snapshot.物品名字, "物品名字文本");
        设置文本(品质文本, snapshot.品质, "品质文本");
        设置文本(武器分类文本, snapshot.武器分类, "武器分类文本");
        设置文本(装备者文本, snapshot.装备者, "装备者文本");
        设置可隐藏文本(攻击力文本, snapshot.攻击力, "攻击力文本");
        设置可隐藏文本(固定伤害文本, snapshot.固定伤害, "固定伤害文本");
        设置文本(属性加成文本, snapshot.属性加成, "属性加成文本");
        设置文本(文本介绍文本, snapshot.文本介绍, "文本介绍文本");
        设置文本(附带技能文本, "附带技能：", "附带技能文本");
        设置下文本内容(snapshot.下文本内容);
        重建附带技能图标(snapshot.附带技能图标Sprites);
    }

    public void 设置下背景显示(bool 显示)
    {
        if (下背景 != null)
        {
            if (下背景.gameObject.activeSelf != 显示)
            {
                下背景.gameObject.SetActive(显示);
            }
        }
        else if (显示)
        {
            Debug.LogWarning("武器详情视图缺少绑定：下背景。");
        }

        bool 显示展开提示 = !显示;
        if (展开提示 != null)
        {
            if (展开提示.gameObject.activeSelf != 显示展开提示)
            {
                展开提示.gameObject.SetActive(显示展开提示);
            }
        }
        else if (显示展开提示)
        {
            Debug.LogWarning("武器详情视图缺少绑定：展开提示。");
        }
    }

    public void 清空运行时内容()
    {
        清空附带技能图标();
        设置下背景显示(false);
    }

    private void 设置背景(Sprite sprite)
    {
        if (背景图 == null)
        {
            Debug.LogWarning("武器详情视图缺少绑定：背景图。");
            return;
        }

        背景图.sprite = sprite;
        背景图.enabled = sprite != null;
    }

    private void 设置物品图标(Snapshot snapshot)
    {
        if (物品图标 == null)
        {
            Debug.LogWarning("武器详情视图缺少绑定：物品图标。");
            return;
        }

        物品图标.sprite = snapshot.物品图标Sprite;
        物品图标.preserveAspect = true;
        物品图标.rectTransform.sizeDelta = snapshot.物品图标尺寸;
        物品图标.rectTransform.localScale = snapshot.物品图标缩放;
        物品图标.material = snapshot.物品图标材质;
        物品图标.enabled = snapshot.物品图标Sprite != null;
    }

    private static void 设置文本(TMP_Text 文本, string 内容, string 字段名)
    {
        if (文本 == null)
        {
            Debug.LogWarning($"武器详情视图缺少绑定：{字段名}。");
            return;
        }

        文本.text = 内容 ?? string.Empty;
    }

    private static void 设置可隐藏文本(TMP_Text 文本, string 内容, string 字段名)
    {
        if (文本 == null)
        {
            if (!string.IsNullOrWhiteSpace(内容))
            {
                Debug.LogWarning($"武器详情视图缺少绑定：{字段名}。");
            }

            return;
        }

        bool 有内容 = !string.IsNullOrEmpty(内容);
        文本.gameObject.SetActive(有内容);
        文本.text = 有内容 ? 内容 : string.Empty;
    }

    private void 设置下文本内容(string 内容)
    {
        if (下文本内容 == null)
        {
            if (!string.IsNullOrWhiteSpace(内容))
            {
                Debug.LogWarning("武器详情视图缺少绑定：下文本内容。");
            }

            return;
        }

        bool 有内容 = !string.IsNullOrWhiteSpace(内容);
        下文本内容.gameObject.SetActive(有内容);
        下文本内容.text = 有内容 ? 内容 : string.Empty;
    }

    private void 重建附带技能图标(IReadOnlyList<Sprite> 图标Sprites)
    {
        清空附带技能图标();
        if (图标Sprites == null || 图标Sprites.Count == 0)
        {
            return;
        }

        if (附带技能图标区域 == null)
        {
            Debug.LogWarning("武器详情视图缺少绑定：附带技能图标区域。");
            return;
        }

        for (int i = 0; i < 图标Sprites.Count; i++)
        {
            Sprite sprite = 图标Sprites[i];
            if (sprite == null)
            {
                continue;
            }

            GameObject go = new GameObject($"附带技能图标_{已创建附带技能图标.Count}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.SetParent(附带技能图标区域, false);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(30f, 30f);
            rect.anchoredPosition = new Vector2(已创建附带技能图标.Count * 34f, 0f);

            Image image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            已创建附带技能图标.Add(go);
        }
    }

    private void 清空附带技能图标()
    {
        for (int i = 0; i < 已创建附带技能图标.Count; i++)
        {
            GameObject go = 已创建附带技能图标[i];
            if (go == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }

        已创建附带技能图标.Clear();
    }
}
