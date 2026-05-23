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

    public void 刷新(SkillTooltipRuntime.Snapshot snapshot)
    {
        if (战技图标 != null)
        {
            战技图标.sprite = snapshot.icon;
            战技图标.enabled = snapshot.icon != null;
        }
        else
        {
            Debug.LogWarning("战技内容视图缺少绑定：战技图标。");
        }

        设置文本(战技名字文本, snapshot.displayName ?? string.Empty, "战技名字文本");
        设置文本(命中率文本, $"命中率：{Mathf.Max(0, snapshot.hitRate)}%", "命中率文本");
        设置文本(战技伤害文本, $"战技伤害：{snapshot.damage}", "战技伤害文本");
        设置文本(战技描述文本, snapshot.description ?? string.Empty, "战技描述文本");
        设置文本(使用者文本, $"使用者：{snapshot.ownerCharacterId}", "使用者文本");
    }

    private static void 设置文本(TMP_Text 文本, string 内容, string 字段名)
    {
        if (文本 == null)
        {
            Debug.LogWarning($"战技内容视图缺少绑定：{字段名}。");
            return;
        }

        文本.text = 内容;
    }
}
