using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 战技内容视图 : MonoBehaviour
{
    [SerializeField] private Image 战技图标;
    [SerializeField] private TMP_Text 战技名字文本;
    [SerializeField] private TMP_Text 命中率文本;
    [SerializeField] private TMP_Text 战技伤害文本;
    [SerializeField] private TMP_Text 战技描述文本;
    [SerializeField] private TMP_Text 使用者文本;

    public RectTransform 根节点 => transform as RectTransform;
    public Image 战技图标组件 => 战技图标;
    public TMP_Text 战技名字文本组件 => 战技名字文本;
    public TMP_Text 命中率文本组件 => 命中率文本;
    public TMP_Text 战技伤害文本组件 => 战技伤害文本;
    public TMP_Text 战技描述文本组件 => 战技描述文本;
    public TMP_Text 使用者文本组件 => 使用者文本;

    public sealed class Snapshot
    {
        public Sprite 图标;
        public string 战技名字;
        public string 命中率;
        public string 战技伤害;
        public string 战技描述;
        public string 使用者;
    }

    public void 刷新(Snapshot snapshot)
    {
        if (snapshot == null)
        {
            Debug.LogWarning("战技内容视图刷新失败：显示数据为空。");
            return;
        }

        if (战技图标 != null)
        {
            战技图标.sprite = snapshot.图标;
            战技图标.enabled = snapshot.图标 != null;
        }
        else
        {
            Debug.LogWarning("战技内容视图缺少绑定：战技图标。");
        }

        设置文本(战技名字文本, snapshot.战技名字, "战技名字文本");
        设置文本(命中率文本, snapshot.命中率, "命中率文本");
        设置文本(战技伤害文本, snapshot.战技伤害, "战技伤害文本");
        设置文本(战技描述文本, snapshot.战技描述, "战技描述文本");
        设置文本(使用者文本, snapshot.使用者, "使用者文本");
    }

    public static Snapshot 构建显示数据(SkillTooltipRuntime.Snapshot snapshot)
    {
        string 使用者 = string.IsNullOrWhiteSpace(snapshot.ownerCharacterId) ? "无" : snapshot.ownerCharacterId;
        string 来源 = string.IsNullOrWhiteSpace(snapshot.skillSource) ? BattleSkillDatabase.NoSkillSourceText : snapshot.skillSource;
        return new Snapshot
        {
            图标 = snapshot.icon,
            战技名字 = snapshot.displayName ?? string.Empty,
            命中率 = $"命中率：{Mathf.Max(0, snapshot.hitRate)}%",
            战技伤害 = $"战技伤害：{snapshot.damage}",
            战技描述 = snapshot.description ?? string.Empty,
            使用者 = $"使用者：\n{使用者}\n{来源}"
        };
    }

    private static void 设置文本(TMP_Text 文本, string 内容, string 字段名)
    {
        if (文本 == null)
        {
            Debug.LogWarning($"战技内容视图缺少绑定：{字段名}。");
            return;
        }

        文本.text = 内容 ?? string.Empty;
    }
}
