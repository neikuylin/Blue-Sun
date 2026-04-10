using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 屏幕火星特效 : MonoBehaviour
{
    [Header("容器")]
    [SerializeField] private RectTransform 火星容器;

    [Header("生成")]
    [SerializeField] private int 火星数量 = 48;
    [SerializeField] private Vector2 生成区域尺寸 = new Vector2(520f, 320f);
    [SerializeField] private Vector2 生成偏移 = new Vector2(420f, -260f);

    [Header("运动")]
    [SerializeField] private Vector2 横向速度范围 = new Vector2(-120f, -55f);
    [SerializeField] private Vector2 纵向速度范围 = new Vector2(70f, 145f);
    [SerializeField] private float 飘动强度 = 18f;
    [SerializeField] private float 飘动频率 = 0.9f;
    [SerializeField] private Vector2 生命周期范围 = new Vector2(3.8f, 6.4f);

    [Header("外观")]
    [SerializeField] private Vector2 尺寸范围 = new Vector2(8f, 22f);
    [SerializeField] private Gradient 生命周期颜色;
    [SerializeField] private Vector2 透明度范围 = new Vector2(0.4f, 0.9f);

    private readonly List<火星状态> 火星列表 = new List<火星状态>();
    private Texture2D 火星纹理;
    private Sprite 火星精灵;
    private bool 需要重建;
    private bool 需要刷新表现;
    private bool 已显示;

    private sealed class 火星状态
    {
        public RectTransform rectTransform;
        public Image image;
        public Vector2 起始位置;
        public Vector2 位置;
        public Vector2 速度;
        public float 尺寸;
        public float 生命周期;
        public float 已经过时间;
        public float 飘动种子;
        public float 透明度;
    }

    private void Reset()
    {
        生命周期颜色 = 创建默认渐变();
    }

    private void Awake()
    {
        if (生命周期颜色 == null || 生命周期颜色.colorKeys == null || 生命周期颜色.colorKeys.Length == 0)
        {
            生命周期颜色 = 创建默认渐变();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        需要重建 = true;
        需要刷新表现 = true;
    }

    private void OnEnable()
    {
        已显示 = gameObject.activeSelf;

        if (火星容器 == null)
        {
            Debug.LogError("屏幕火星特效: 火星容器未绑定。");
            return;
        }

        火星纹理 = 创建火星纹理();
        火星精灵 = Sprite.Create(
            火星纹理,
            new Rect(0f, 0f, 火星纹理.width, 火星纹理.height),
            new Vector2(0.5f, 0.5f),
            100f);

        清空火星();
        创建火星();
    }

    private void OnDisable()
    {
        已显示 = false;
        清空火星();

        if (火星精灵 != null)
        {
            Destroy(火星精灵);
            火星精灵 = null;
        }

        if (火星纹理 != null)
        {
            Destroy(火星纹理);
            火星纹理 = null;
        }
    }

    public void 显示特效()
    {
        已显示 = true;
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        需要重建 = true;
        需要刷新表现 = true;
    }

    public void 隐藏特效()
    {
        已显示 = false;
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (需要重建)
        {
            立即重建();
        }
        else if (需要刷新表现)
        {
            立即刷新表现();
        }

        float deltaTime = Time.deltaTime;
        for (int i = 0; i < 火星列表.Count; i++)
        {
            火星状态 火星 = 火星列表[i];
            火星.已经过时间 += deltaTime;

            if (火星.已经过时间 >= 火星.生命周期)
            {
                重生火星(火星, preserveTransform: true);
            }

            float 生命周期进度 = 火星.已经过时间 / 火星.生命周期;
            float 飘动X = Mathf.Sin((Time.time + 火星.飘动种子) * 飘动频率) * 飘动强度;
            float 飘动Y = Mathf.Cos((Time.time + 火星.飘动种子 * 1.37f) * 飘动频率) * (飘动强度 * 0.55f);

            火星.位置 += 火星.速度 * deltaTime;
            火星.rectTransform.anchoredPosition = 火星.位置 + new Vector2(飘动X, 飘动Y);

            float 尺寸倍率 = 计算尺寸倍率(生命周期进度);
            火星.rectTransform.sizeDelta = Vector2.one * (火星.尺寸 * 尺寸倍率);

            Color color = 生命周期颜色.Evaluate(生命周期进度);
            color.a *= 火星.透明度;
            火星.image.color = color;
        }
    }

    private void 立即重建()
    {
        if (火星容器 == null)
        {
            需要重建 = false;
            需要刷新表现 = false;
            return;
        }

        if (生命周期颜色 == null || 生命周期颜色.colorKeys == null || 生命周期颜色.colorKeys.Length == 0)
        {
            生命周期颜色 = 创建默认渐变();
        }

        if (火星纹理 == null)
        {
            火星纹理 = 创建火星纹理();
        }

        if (火星精灵 == null)
        {
            火星精灵 = Sprite.Create(
                火星纹理,
                new Rect(0f, 0f, 火星纹理.width, 火星纹理.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        清空火星();
        创建火星();
        需要重建 = false;
        需要刷新表现 = false;
    }

    private void 立即刷新表现()
    {
        for (int i = 0; i < 火星列表.Count; i++)
        {
            火星状态 火星 = 火星列表[i];
            if (火星 == null || 火星.rectTransform == null || 火星.image == null)
            {
                continue;
            }

            火星.尺寸 = Mathf.Clamp(火星.尺寸, 尺寸范围.x, 尺寸范围.y);
            火星.透明度 = Mathf.Clamp(火星.透明度, 透明度范围.x, 透明度范围.y);
        }

        需要刷新表现 = false;
    }

    private void 创建火星()
    {
        火星列表.Clear();
        for (int i = 0; i < 火星数量; i++)
        {
            GameObject 火星物体 = new GameObject("火星", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            火星物体.transform.SetParent(火星容器, false);

            RectTransform rectTransform = 火星物体.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Image image = 火星物体.GetComponent<Image>();
            image.sprite = 火星精灵;
            image.raycastTarget = false;
            image.maskable = false;

            火星状态 火星 = new 火星状态
            {
                rectTransform = rectTransform,
                image = image
            };

            重生火星(火星, preserveTransform: false);
            火星列表.Add(火星);
        }
    }

    private void 重生火星(火星状态 火星, bool preserveTransform)
    {
        float 半宽 = 火星容器.rect.width * 0.5f;
        float 半高 = 火星容器.rect.height * 0.5f;

        float 生成X = 半宽 + 生成偏移.x + Random.Range(-生成区域尺寸.x * 0.5f, 生成区域尺寸.x * 0.5f);
        float 生成Y = -半高 + 生成偏移.y + Random.Range(-生成区域尺寸.y * 0.5f, 生成区域尺寸.y * 0.5f);

        火星.起始位置 = new Vector2(生成X, 生成Y);
        火星.位置 = 火星.起始位置;
        火星.速度 = new Vector2(
            Random.Range(横向速度范围.x, 横向速度范围.y),
            Random.Range(纵向速度范围.x, 纵向速度范围.y));
        火星.尺寸 = Random.Range(尺寸范围.x, 尺寸范围.y);
        火星.生命周期 = Random.Range(生命周期范围.x, 生命周期范围.y);
        火星.已经过时间 = preserveTransform ? 0f : Random.Range(0f, 火星.生命周期);
        火星.飘动种子 = Random.Range(0f, 1000f);
        火星.透明度 = Random.Range(透明度范围.x, 透明度范围.y);

        火星.rectTransform.anchoredPosition = 火星.位置;
        火星.rectTransform.sizeDelta = Vector2.one * 火星.尺寸;
        火星.image.color = 生命周期颜色.Evaluate(0f);
    }

    private void 清空火星()
    {
        for (int i = 0; i < 火星列表.Count; i++)
        {
            火星状态 火星 = 火星列表[i];
            if (火星 != null && 火星.rectTransform != null)
            {
                Destroy(火星.rectTransform.gameObject);
            }
        }

        火星列表.Clear();
    }

    private static float 计算尺寸倍率(float 生命周期进度)
    {
        if (生命周期进度 < 0.18f)
        {
            return Mathf.Lerp(0.25f, 1f, 生命周期进度 / 0.18f);
        }

        if (生命周期进度 < 0.78f)
        {
            return Mathf.Lerp(1f, 0.82f, (生命周期进度 - 0.18f) / 0.6f);
        }

        return Mathf.Lerp(0.82f, 0.14f, (生命周期进度 - 0.78f) / 0.22f);
    }

    private static Gradient 创建默认渐变()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.92f, 0.72f), 0f),
                new GradientColorKey(new Color(1f, 0.54f, 0.18f), 0.45f),
                new GradientColorKey(new Color(0.58f, 0.12f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.82f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    private static Texture2D 创建火星纹理()
    {
        const int size = 64;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "屏幕火星特效纹理";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha * alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
