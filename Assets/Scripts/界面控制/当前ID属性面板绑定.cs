using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 当前ID属性面板绑定 : MonoBehaviour
{
    [Header("角色属性文本")]
    [SerializeField] private Component currentIdText;
    [SerializeField] private Component displayNameText;
    [SerializeField] private Component healthText;
    [SerializeField] private Component manaText;
    [SerializeField] private Component attackPowerText;
    [SerializeField] private Component spellDamageText;
    [SerializeField] private Component strengthText;
    [SerializeField] private Component agilityText;
    [SerializeField] private Component intelligenceText;
    [SerializeField] private Component hitRateText;
    [SerializeField] private Component dodgeRateText;
    [SerializeField] private Component physicalResistanceText;
    [SerializeField] private Component fireResistanceText;
    [SerializeField] private Component corruptionResistanceText;
    [SerializeField] private Component coldResistanceText;
    [SerializeField] private Component physicalPenetrationText;
    [SerializeField] private Component firePenetrationText;
    [SerializeField] private Component corruptionPenetrationText;
    [SerializeField] private Component coldPenetrationText;
    [SerializeField] private Component criticalChanceText;
    [SerializeField] private Component criticalDamageText;

    [Header("文本前缀")]
    [SerializeField] private string currentIdPrefix = string.Empty;
    [SerializeField] private string displayNamePrefix = string.Empty;
    [SerializeField] private string healthPrefix = "生命值:";
    [SerializeField] private string manaPrefix = "魔法值:";
    [SerializeField] private string attackPowerPrefix = "攻击力:";
    [SerializeField] private string spellDamagePrefix = "法术伤害:";
    [SerializeField] private string strengthPrefix = "力量:";
    [SerializeField] private string agilityPrefix = "敏捷:";
    [SerializeField] private string intelligencePrefix = "智力:";
    [SerializeField] private string hitRatePrefix = "命中率:";
    [SerializeField] private string dodgeRatePrefix = "闪避率:";
    [SerializeField] private string physicalResistancePrefix = "物理抗性:";
    [SerializeField] private string fireResistancePrefix = "火焰抗性:";
    [SerializeField] private string corruptionResistancePrefix = "腐败抗性:";
    [SerializeField] private string coldResistancePrefix = "寒冷抗性:";
    [SerializeField] private string physicalPenetrationPrefix = "物理穿透:";
    [SerializeField] private string firePenetrationPrefix = "火焰穿透:";
    [SerializeField] private string corruptionPenetrationPrefix = "腐败穿透:";
    [SerializeField] private string coldPenetrationPrefix = "寒冷穿透:";
    [SerializeField] private string criticalChancePrefix = "暴击率:";
    [SerializeField] private string criticalDamagePrefix = "暴击伤害:";

    [Header("数据源")]
    [SerializeField] private CharacterStatDatabase statDatabase;
    [SerializeField] private BattleCharacterBindingDatabase characterBindingDatabase;

    private const string FireballSkillId = "火球";
    private const float DefaultFireballAttributeMultiplier = 0.8f;

    private string lastSignature = string.Empty;

    private void Awake()
    {
        EnsureDatabases();
    }

    private void OnEnable()
    {
        EnsureDatabases();
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
    }

    private void EnsureDatabases()
    {
        if (statDatabase == null)
        {
            statDatabase = CharacterStatDatabase.LoadDefault();
        }

        if (characterBindingDatabase == null)
        {
            characterBindingDatabase = BattleCharacterBindingDatabase.LoadDefault();
        }
    }

    private void Refresh(bool force)
    {
        string characterId = 界面ID列表.当前ID ?? string.Empty;
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;
        BattleUnit unit = FindBattleUnitByCharacterId(characterId);

        float attackPower = string.IsNullOrWhiteSpace(characterId)
            ? 0f
            : InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(characterId);

        int spellDamage = CalculateSpellDamage(characterId, unit, statEntry);
        int criticalChance = statEntry != null
            ? statEntry.ResolveCriticalChance() + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalChanceBonus(characterId)
            : -1;
        int criticalDamage = statEntry != null
            ? statEntry.ResolveCriticalDamage() + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalDamageBonus(characterId)
            : -1;

        string signature = string.Concat(
            characterId, "|",
            GetCurrentHealth(unit, statEntry), "/", GetMaxHealth(unit, statEntry), "|",
            GetCurrentMana(unit, statEntry), "/", GetMaxMana(unit, statEntry), "|",
            statEntry != null ? statEntry.strength : -1, "|",
            statEntry != null ? statEntry.agility : -1, "|",
            statEntry != null ? statEntry.intelligence : -1, "|",
            statEntry != null ? statEntry.ResolveHitRate() : -1, "|",
            statEntry != null ? statEntry.ResolveDodgeRate() : -1, "|",
            statEntry != null ? statEntry.ResolvePhysicalResistance() : -1, "|",
            statEntry != null ? statEntry.ResolveFireResistance() : -1, "|",
            statEntry != null ? statEntry.ResolveCorruptionResistance() : -1, "|",
            statEntry != null ? statEntry.ResolveColdResistance() : -1, "|",
            statEntry != null ? statEntry.ResolvePhysicalResistancePenetration() : -1, "|",
            statEntry != null ? statEntry.ResolveFireResistancePenetration() : -1, "|",
            statEntry != null ? statEntry.ResolveCorruptionResistancePenetration() : -1, "|",
            statEntry != null ? statEntry.ResolveColdResistancePenetration() : -1, "|",
            criticalChance, "|",
            criticalDamage, "|",
            Mathf.RoundToInt(attackPower * 100f), "|",
            spellDamage);

        if (!force && string.Equals(lastSignature, signature, StringComparison.Ordinal))
        {
            return;
        }

        lastSignature = signature;
        ApplyTexts(characterId, unit, statEntry, attackPower, spellDamage, criticalChance, criticalDamage);
    }

    private void ApplyTexts(
        string characterId,
        BattleUnit unit,
        CharacterStatDatabase.StatEntry statEntry,
        float attackPower,
        int spellDamage,
        int criticalChance,
        int criticalDamage)
    {
        SetText(currentIdText, Join(currentIdPrefix, characterId));
        SetText(displayNameText, Join(displayNamePrefix, ResolveDisplayName(characterId)));
        SetText(healthText, Join(healthPrefix, BuildHealthText(unit, statEntry)));
        SetText(manaText, Join(manaPrefix, BuildManaText(unit, statEntry)));
        SetText(attackPowerText, Join(attackPowerPrefix, string.IsNullOrWhiteSpace(characterId) ? string.Empty : Mathf.RoundToInt(attackPower).ToString()));
        SetText(spellDamageText, Join(spellDamagePrefix, statEntry != null ? spellDamage.ToString() : string.Empty));
        SetText(strengthText, Join(strengthPrefix, statEntry != null ? statEntry.strength.ToString() : string.Empty));
        SetText(agilityText, Join(agilityPrefix, statEntry != null ? statEntry.agility.ToString() : string.Empty));
        SetText(intelligenceText, Join(intelligencePrefix, statEntry != null ? statEntry.intelligence.ToString() : string.Empty));
        SetText(hitRateText, Join(hitRatePrefix, statEntry != null ? statEntry.ResolveHitRate() + "%" : string.Empty));
        SetText(dodgeRateText, Join(dodgeRatePrefix, statEntry != null ? statEntry.ResolveDodgeRate() + "%" : string.Empty));
        SetText(physicalResistanceText, Join(physicalResistancePrefix, statEntry != null ? statEntry.ResolvePhysicalResistance() + "%" : string.Empty));
        SetText(fireResistanceText, Join(fireResistancePrefix, statEntry != null ? statEntry.ResolveFireResistance() + "%" : string.Empty));
        SetText(corruptionResistanceText, Join(corruptionResistancePrefix, statEntry != null ? statEntry.ResolveCorruptionResistance() + "%" : string.Empty));
        SetText(coldResistanceText, Join(coldResistancePrefix, statEntry != null ? statEntry.ResolveColdResistance() + "%" : string.Empty));
        SetText(physicalPenetrationText, Join(physicalPenetrationPrefix, statEntry != null ? statEntry.ResolvePhysicalResistancePenetration() + "%" : string.Empty));
        SetText(firePenetrationText, Join(firePenetrationPrefix, statEntry != null ? statEntry.ResolveFireResistancePenetration() + "%" : string.Empty));
        SetText(corruptionPenetrationText, Join(corruptionPenetrationPrefix, statEntry != null ? statEntry.ResolveCorruptionResistancePenetration() + "%" : string.Empty));
        SetText(coldPenetrationText, Join(coldPenetrationPrefix, statEntry != null ? statEntry.ResolveColdResistancePenetration() + "%" : string.Empty));
        SetText(criticalChanceText, Join(criticalChancePrefix, statEntry != null ? criticalChance + "%" : string.Empty));
        SetText(criticalDamageText, Join(criticalDamagePrefix, statEntry != null ? criticalDamage + "%" : string.Empty));
    }

    private string ResolveDisplayName(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return string.Empty;
        }

        BattleCharacterBindingDatabase.BindingEntry binding =
            characterBindingDatabase != null ? characterBindingDatabase.FindBinding(characterId) : null;
        return binding != null && !string.IsNullOrWhiteSpace(binding.displayName) ? binding.displayName : characterId;
    }

    private static string Join(string prefix, string value)
    {
        return string.Concat(prefix ?? string.Empty, value ?? string.Empty);
    }

    private static void SetText(Component target, string value)
    {
        if (target is TMP_Text tmp)
        {
            tmp.text = value ?? string.Empty;
            return;
        }

        if (target is Text legacy)
        {
            legacy.text = value ?? string.Empty;
        }
    }

    private static int CalculateSpellDamage(
        string characterId,
        BattleUnit unit,
        CharacterStatDatabase.StatEntry statEntry)
    {
        if (string.IsNullOrWhiteSpace(characterId) || statEntry == null)
        {
            return 0;
        }

        BattleSkillDatabase skillDatabase = BattleSkillDatabase.LoadDefault();
        BattleSkillDatabase.SkillEntry fireballSkill =
            skillDatabase != null ? skillDatabase.FindEntry(FireballSkillId) : null;
        float intelligence = unit != null
            ? Mathf.Max(0, unit.GetEffectiveIntelligence())
            : Mathf.Max(0, statEntry.intelligence);
        float fixedDamage = fireballSkill != null ? Mathf.Max(0, fireballSkill.fixedDamage) : 0f;
        float attributeMultiplier = fireballSkill != null
            ? Mathf.Max(0f, fireballSkill.attributeMultiplier)
            : DefaultFireballAttributeMultiplier;
        float staffMultiplier = InventoryShortcutRuntimeBinder.GetCharacterStaffDamageMultiplier(characterId);
        float damage = (fixedDamage + (attributeMultiplier * intelligence)) * Mathf.Max(0f, staffMultiplier);
        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    private static BattleUnit FindBattleUnitByCharacterId(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        BattleUnit[] units = FindObjectsOfType<BattleUnit>(true);
        BattleUnit fallback = null;
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !string.Equals(unit.characterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            if (unit.gameObject.activeInHierarchy && unit.IsAlive)
            {
                return unit;
            }

            if (fallback == null)
            {
                fallback = unit;
            }
        }

        return fallback;
    }

    private static int GetCurrentHealth(BattleUnit unit, CharacterStatDatabase.StatEntry statEntry)
    {
        if (unit != null)
        {
            return Mathf.Max(0, unit.currentHealth);
        }

        return statEntry != null ? Mathf.Max(0, statEntry.ResolveMaxHealth()) : 0;
    }

    private static int GetMaxHealth(BattleUnit unit, CharacterStatDatabase.StatEntry statEntry)
    {
        if (unit != null)
        {
            return Mathf.Max(0, unit.maxHealth);
        }

        return statEntry != null ? Mathf.Max(0, statEntry.ResolveMaxHealth()) : 0;
    }

    private static int GetCurrentMana(BattleUnit unit, CharacterStatDatabase.StatEntry statEntry)
    {
        if (unit != null)
        {
            return Mathf.Max(0, unit.currentMana);
        }

        return statEntry != null ? Mathf.Max(0, statEntry.ResolveMaxMana()) : 0;
    }

    private static int GetMaxMana(BattleUnit unit, CharacterStatDatabase.StatEntry statEntry)
    {
        if (unit != null)
        {
            return Mathf.Max(0, unit.maxMana);
        }

        return statEntry != null ? Mathf.Max(0, statEntry.ResolveMaxMana()) : 0;
    }

    private static string BuildHealthText(BattleUnit unit, CharacterStatDatabase.StatEntry statEntry)
    {
        if (unit == null && statEntry == null)
        {
            return string.Empty;
        }

        return GetCurrentHealth(unit, statEntry) + "/" + GetMaxHealth(unit, statEntry);
    }

    private static string BuildManaText(BattleUnit unit, CharacterStatDatabase.StatEntry statEntry)
    {
        if (unit == null && statEntry == null)
        {
            return string.Empty;
        }

        return GetCurrentMana(unit, statEntry) + "/" + GetMaxMana(unit, statEntry);
    }
}
