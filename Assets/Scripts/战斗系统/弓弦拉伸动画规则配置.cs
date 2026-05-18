using UnityEngine;

[CreateAssetMenu(fileName = DefaultResourcePath, menuName = "战斗/弓弦拉伸动画规则配置")]
public sealed class 弓弦拉伸动画规则配置 : ScriptableObject
{
    public const string DefaultResourcePath = "弓弦拉伸动画规则配置";

    [System.Serializable]
    public sealed class 拉弦动画状态规则
    {
        public string 状态名 = string.Empty;
        [Range(0f, 1f)] public float 进入进度 = 1f;
        public bool 按时间回零;
        [Min(0f)] public float 回零秒数;
        [Range(0f, 1f)] public float 回零后进度;
    }

    [SerializeField] private 拉弦动画状态规则[] 拉弦动画状态规则列表 = 创建默认动画状态规则();

    public 拉弦动画状态规则[] 规则列表 => 拉弦动画状态规则列表;

    private void OnValidate()
    {
        if (拉弦动画状态规则列表 == null || 拉弦动画状态规则列表.Length == 0)
        {
            拉弦动画状态规则列表 = 创建默认动画状态规则();
        }

        for (int i = 0; i < 拉弦动画状态规则列表.Length; i++)
        {
            拉弦动画状态规则 rule = 拉弦动画状态规则列表[i];
            if (rule == null)
            {
                continue;
            }

            rule.进入进度 = Mathf.Clamp01(rule.进入进度);
            rule.回零秒数 = Mathf.Max(0f, rule.回零秒数);
            rule.回零后进度 = Mathf.Clamp01(rule.回零后进度);
        }
    }

    public static 弓弦拉伸动画规则配置 加载默认配置()
    {
        return Resources.Load<弓弦拉伸动画规则配置>(DefaultResourcePath);
    }

    private static 拉弦动画状态规则[] 创建默认动画状态规则()
    {
        return new[]
        {
            new 拉弦动画状态规则
            {
                状态名 = "射击动画",
                进入进度 = 1f,
                按时间回零 = true,
                回零秒数 = 0.04f,
                回零后进度 = 0f,
            },
            new 拉弦动画状态规则
            {
                状态名 = "射击抬手",
                进入进度 = 1f,
                按时间回零 = true,
                回零秒数 = 0.149f,
                回零后进度 = 0f,
            },
            new 拉弦动画状态规则
            {
                状态名 = "射击选目标",
                进入进度 = 1f,
                按时间回零 = false,
                回零秒数 = 0f,
                回零后进度 = 0f,
            },
        };
    }
}
