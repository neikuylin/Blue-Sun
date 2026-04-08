using System;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class 当前ID属性面板绑定 : MonoBehaviour
{
    [Header("文本引用")]
    [SerializeField] private TMP_Text 当前ID文本;
    [SerializeField] private TMP_Text 角色名文本;
    [SerializeField] private TMP_Text 生命值文本;
    [SerializeField] private TMP_Text 魔法值文本;
    [SerializeField] private TMP_Text 攻击力文本;
    [SerializeField] private TMP_Text 法术伤害文本;
    [SerializeField] private TMP_Text 力量文本;
    [SerializeField] private TMP_Text 敏捷文本;
    [SerializeField] private TMP_Text 智力文本;
    [SerializeField] private TMP_Text 命中率文本;
    [SerializeField] private TMP_Text 闪避率文本;
    [SerializeField] private TMP_Text 物理抗性文本;
    [SerializeField] private TMP_Text 火焰抗性文本;
    [SerializeField] private TMP_Text 腐败抗性文本;
    [SerializeField] private TMP_Text 寒冷抗性文本;
    [SerializeField] private TMP_Text 物理穿透文本;
    [SerializeField] private TMP_Text 火焰穿透文本;
    [SerializeField] private TMP_Text 腐败穿透文本;
    [SerializeField] private TMP_Text 寒冷穿透文本;
    [SerializeField] private TMP_Text 暴击率文本;
    [SerializeField] private TMP_Text 暴击伤害文本;

    [Header("文本前缀")]
    [SerializeField] private string 当前ID前缀 = string.Empty;
    [SerializeField] private string 角色名前缀 = string.Empty;
    [SerializeField] private string 生命值前缀 = "生命值:";
    [SerializeField] private string 魔法值前缀 = "魔法值:";
    [SerializeField] private string 攻击力前缀 = "攻击力:";
    [SerializeField] private string 法术伤害前缀 = "法术伤害:";
    [SerializeField] private string 力量前缀 = "力量:";
    [SerializeField] private string 敏捷前缀 = "敏捷:";
    [SerializeField] private string 智力前缀 = "智力:";
    [SerializeField] private string 命中率前缀 = "命中率:";
    [SerializeField] private string 闪避率前缀 = "闪避率:";
    [SerializeField] private string 物理抗性前缀 = "物理抗性:";
    [SerializeField] private string 火焰抗性前缀 = "火焰抗性:";
    [SerializeField] private string 腐败抗性前缀 = "腐败抗性:";
    [SerializeField] private string 寒冷抗性前缀 = "寒冷抗性:";
    [SerializeField] private string 物理穿透前缀 = "物理穿透:";
    [SerializeField] private string 火焰穿透前缀 = "火焰穿透:";
    [SerializeField] private string 腐败穿透前缀 = "腐败穿透:";
    [SerializeField] private string 寒冷穿透前缀 = "寒冷穿透:";
    [SerializeField] private string 暴击率前缀 = "暴击率:";
    [SerializeField] private string 暴击伤害前缀 = "暴击伤害:";

    [Header("数据源")]
    [SerializeField] private CharacterStatDatabase 属性库;
    [SerializeField] private BattleCharacterBindingDatabase 角色绑定库;

    private const string 火球技能ID = "火球";
    private const float 默认火球属性倍率 = 0.8f;

    private string 上次签名 = string.Empty;

    private void Awake()
    {
        确保数据库();
    }

    private void OnEnable()
    {
        确保数据库();
        刷新(true);
    }

    private void LateUpdate()
    {
        刷新(false);
    }

    private void 确保数据库()
    {
        if (属性库 == null)
        {
            属性库 = CharacterStatDatabase.LoadDefault();
        }

        if (角色绑定库 == null)
        {
            角色绑定库 = BattleCharacterBindingDatabase.LoadDefault();
        }
    }

    private void 刷新(bool 强制刷新)
    {
        string 当前ID = 界面ID列表.当前ID ?? string.Empty;
        CharacterStatDatabase.StatEntry 属性 = 属性库 != null ? 属性库.FindEntry(当前ID) : null;
        BattleUnit 战斗单位 = 查找战斗单位(当前ID);

        float 攻击力 = string.IsNullOrWhiteSpace(当前ID)
            ? 0f
            : InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(当前ID);
        int 法术伤害 = 计算法术伤害(当前ID, 战斗单位, 属性);
        int 暴击率 = 属性 != null
            ? 属性.ResolveCriticalChance() + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalChanceBonus(当前ID)
            : -1;
        int 暴击伤害 = 属性 != null
            ? 属性.ResolveCriticalDamage() + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalDamageBonus(当前ID)
            : -1;

        string 签名 = string.Concat(
            当前ID, "|",
            当前生命值(战斗单位, 属性), "/", 最大生命值(战斗单位, 属性), "|",
            当前魔法值(战斗单位, 属性), "/", 最大魔法值(战斗单位, 属性), "|",
            属性 != null ? 属性.strength : -1, "|",
            属性 != null ? 属性.agility : -1, "|",
            属性 != null ? 属性.intelligence : -1, "|",
            属性 != null ? 属性.ResolveHitRate() : -1, "|",
            属性 != null ? 属性.ResolveDodgeRate() : -1, "|",
            属性 != null ? 属性.ResolvePhysicalResistance() : -1, "|",
            属性 != null ? 属性.ResolveFireResistance() : -1, "|",
            属性 != null ? 属性.ResolveCorruptionResistance() : -1, "|",
            属性 != null ? 属性.ResolveColdResistance() : -1, "|",
            属性 != null ? 属性.ResolvePhysicalResistancePenetration() : -1, "|",
            属性 != null ? 属性.ResolveFireResistancePenetration() : -1, "|",
            属性 != null ? 属性.ResolveCorruptionResistancePenetration() : -1, "|",
            属性 != null ? 属性.ResolveColdResistancePenetration() : -1, "|",
            暴击率, "|",
            暴击伤害, "|",
            Mathf.RoundToInt(攻击力 * 100f), "|",
            法术伤害);

        if (!强制刷新 && string.Equals(上次签名, 签名, StringComparison.Ordinal))
        {
            return;
        }

        上次签名 = 签名;
        写入文本(当前ID, 战斗单位, 属性, 攻击力, 法术伤害, 暴击率, 暴击伤害);
    }

    private void 写入文本(
        string 当前ID,
        BattleUnit 战斗单位,
        CharacterStatDatabase.StatEntry 属性,
        float 攻击力,
        int 法术伤害,
        int 暴击率,
        int 暴击伤害)
    {
        设文本(当前ID文本, 拼文本(当前ID前缀, 当前ID));
        设文本(角色名文本, 拼文本(角色名前缀, 角色显示名(当前ID)));
        设文本(生命值文本, 拼文本(生命值前缀, 生命值显示文本(战斗单位, 属性)));
        设文本(魔法值文本, 拼文本(魔法值前缀, 魔法值显示文本(战斗单位, 属性)));
        设文本(攻击力文本, 拼文本(攻击力前缀, string.IsNullOrWhiteSpace(当前ID) ? string.Empty : Mathf.RoundToInt(攻击力).ToString()));
        设文本(法术伤害文本, 拼文本(法术伤害前缀, 属性 != null ? 法术伤害.ToString() : string.Empty));
        设文本(力量文本, 拼文本(力量前缀, 属性 != null ? 属性.strength.ToString() : string.Empty));
        设文本(敏捷文本, 拼文本(敏捷前缀, 属性 != null ? 属性.agility.ToString() : string.Empty));
        设文本(智力文本, 拼文本(智力前缀, 属性 != null ? 属性.intelligence.ToString() : string.Empty));
        设文本(命中率文本, 拼文本(命中率前缀, 属性 != null ? 属性.ResolveHitRate() + "%" : string.Empty));
        设文本(闪避率文本, 拼文本(闪避率前缀, 属性 != null ? 属性.ResolveDodgeRate() + "%" : string.Empty));
        设文本(物理抗性文本, 拼文本(物理抗性前缀, 属性 != null ? 属性.ResolvePhysicalResistance() + "%" : string.Empty));
        设文本(火焰抗性文本, 拼文本(火焰抗性前缀, 属性 != null ? 属性.ResolveFireResistance() + "%" : string.Empty));
        设文本(腐败抗性文本, 拼文本(腐败抗性前缀, 属性 != null ? 属性.ResolveCorruptionResistance() + "%" : string.Empty));
        设文本(寒冷抗性文本, 拼文本(寒冷抗性前缀, 属性 != null ? 属性.ResolveColdResistance() + "%" : string.Empty));
        设文本(物理穿透文本, 拼文本(物理穿透前缀, 属性 != null ? 属性.ResolvePhysicalResistancePenetration() + "%" : string.Empty));
        设文本(火焰穿透文本, 拼文本(火焰穿透前缀, 属性 != null ? 属性.ResolveFireResistancePenetration() + "%" : string.Empty));
        设文本(腐败穿透文本, 拼文本(腐败穿透前缀, 属性 != null ? 属性.ResolveCorruptionResistancePenetration() + "%" : string.Empty));
        设文本(寒冷穿透文本, 拼文本(寒冷穿透前缀, 属性 != null ? 属性.ResolveColdResistancePenetration() + "%" : string.Empty));
        设文本(暴击率文本, 拼文本(暴击率前缀, 属性 != null ? 暴击率 + "%" : string.Empty));
        设文本(暴击伤害文本, 拼文本(暴击伤害前缀, 属性 != null ? 暴击伤害 + "%" : string.Empty));
    }

    private string 角色显示名(string 当前ID)
    {
        if (string.IsNullOrWhiteSpace(当前ID))
        {
            return string.Empty;
        }

        BattleCharacterBindingDatabase.BindingEntry 绑定 = 角色绑定库 != null ? 角色绑定库.FindBinding(当前ID) : null;
        return 绑定 != null && !string.IsNullOrWhiteSpace(绑定.displayName) ? 绑定.displayName : 当前ID;
    }

    private static BattleUnit 查找战斗单位(string 当前ID)
    {
        if (string.IsNullOrWhiteSpace(当前ID))
        {
            return null;
        }

        BattleUnit[] 单位列表 = FindObjectsOfType<BattleUnit>(true);
        BattleUnit 后备 = null;
        for (int i = 0; i < 单位列表.Length; i++)
        {
            BattleUnit 单位 = 单位列表[i];
            if (单位 == null || !string.Equals(单位.characterId, 当前ID, StringComparison.Ordinal))
            {
                continue;
            }

            if (单位.gameObject.activeInHierarchy && 单位.IsAlive)
            {
                return 单位;
            }

            if (后备 == null)
            {
                后备 = 单位;
            }
        }

        return 后备;
    }

    private static int 当前生命值(BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (战斗单位 != null)
        {
            return Mathf.Max(0, 战斗单位.currentHealth);
        }

        return 属性 != null ? Mathf.Max(0, 属性.ResolveMaxHealth()) : 0;
    }

    private static int 最大生命值(BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (战斗单位 != null)
        {
            return Mathf.Max(0, 战斗单位.maxHealth);
        }

        return 属性 != null ? Mathf.Max(0, 属性.ResolveMaxHealth()) : 0;
    }

    private static int 当前魔法值(BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (战斗单位 != null)
        {
            return Mathf.Max(0, 战斗单位.currentMana);
        }

        return 属性 != null ? Mathf.Max(0, 属性.ResolveMaxMana()) : 0;
    }

    private static int 最大魔法值(BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (战斗单位 != null)
        {
            return Mathf.Max(0, 战斗单位.maxMana);
        }

        return 属性 != null ? Mathf.Max(0, 属性.ResolveMaxMana()) : 0;
    }

    private static string 生命值显示文本(BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (战斗单位 == null && 属性 == null)
        {
            return string.Empty;
        }

        return 当前生命值(战斗单位, 属性) + "/" + 最大生命值(战斗单位, 属性);
    }

    private static string 魔法值显示文本(BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (战斗单位 == null && 属性 == null)
        {
            return string.Empty;
        }

        return 当前魔法值(战斗单位, 属性) + "/" + 最大魔法值(战斗单位, 属性);
    }

    private static int 计算法术伤害(string 当前ID, BattleUnit 战斗单位, CharacterStatDatabase.StatEntry 属性)
    {
        if (string.IsNullOrWhiteSpace(当前ID) || 属性 == null)
        {
            return 0;
        }

        BattleSkillDatabase 技能库 = BattleSkillDatabase.LoadDefault();
        BattleSkillDatabase.SkillEntry 火球技能 = 技能库 != null ? 技能库.FindEntry(火球技能ID) : null;
        float 智力值 = 战斗单位 != null
            ? Mathf.Max(0, 战斗单位.GetEffectiveIntelligence())
            : Mathf.Max(0, 属性.intelligence);
        float 固定伤害 = 火球技能 != null ? Mathf.Max(0, 火球技能.fixedDamage) : 0f;
        float 属性倍率 = 火球技能 != null
            ? Mathf.Max(0f, 火球技能.attributeMultiplier)
            : 默认火球属性倍率;
        float 法杖倍率 = InventoryShortcutRuntimeBinder.GetCharacterStaffDamageMultiplier(当前ID);
        float 结果 = (固定伤害 + (属性倍率 * 智力值)) * Mathf.Max(0f, 法杖倍率);
        return Mathf.Max(0, Mathf.RoundToInt(结果));
    }

    private static string 拼文本(string 前缀, string 值)
    {
        return string.Concat(前缀 ?? string.Empty, 值 ?? string.Empty);
    }

    private static void 设文本(TMP_Text 目标, string 内容)
    {
        if (目标 == null)
        {
            return;
        }

        目标.text = 内容 ?? string.Empty;
    }
}
