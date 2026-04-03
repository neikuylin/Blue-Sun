using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JourneyAttributeDetailBinder : MonoBehaviour
{
    private const string CharacterNamePath = "\u6587\u672c\u533a\u57df/\u89d2\u8272ID";
    private const string HealthPath = "\u6587\u672c\u533a\u57df/\u751f\u547d\u503c";
    private const string ManaPath = "\u6587\u672c\u533a\u57df/\u9b54\u6cd5\u503c";
    private const string AttackPowerPath = "\u6587\u672c\u533a\u57df/\u653b\u51fb\u529b";
    private const string SpellDamagePath = "\u6587\u672c\u533a\u57df/\u6cd5\u672f\u4f24\u5bb3";
    private const string StrengthPath = "\u6587\u672c\u533a\u57df/\u529b\u91cf";
    private const string AgilityPath = "\u6587\u672c\u533a\u57df/\u654f\u6377";
    private const string IntelligencePath = "\u6587\u672c\u533a\u57df/\u667a\u529b";
    private const string HitRatePath = "\u6587\u672c\u533a\u57df/\u547d\u4e2d\u7387";
    private const string DodgeRatePath = "\u6587\u672c\u533a\u57df/\u95ea\u907f\u7387";
    private const string PhysicalResistancePath = "\u6587\u672c\u533a\u57df/\u7269\u7406\u6297\u6027";
    private const string FireResistancePath = "\u6587\u672c\u533a\u57df/\u706b\u7130\u6297\u6027";
    private const string CorruptionResistancePath = "\u6587\u672c\u533a\u57df/\u8150\u8d25\u6297\u6027";
    private const string ColdResistancePath = "\u6587\u672c\u533a\u57df/\u5bd2\u51b7\u6297\u6027";
    private const string PhysicalResistancePenetrationPath = "\u6587\u672c\u533a\u57df/\u7269\u7406\u6297\u6027\u7A7F\u900F";
    private const string FireResistancePenetrationPath = "\u6587\u672c\u533a\u57df/\u706b\u7130\u6297\u6027\u7A7F\u900F";
    private const string CorruptionResistancePenetrationPath = "\u6587\u672c\u533a\u57df/\u8150\u8d25\u6297\u6027\u7A7F\u900F";
    private const string ColdResistancePenetrationPath = "\u6587\u672c\u533a\u57df/\u5bd2\u51b7\u6297\u6027\u7A7F\u900F";
    private const string CriticalChancePath = "\u6587\u672c\u533a\u57df/\u66b4\u51fb\u7387";
    private const string CriticalDamagePath = "\u6587\u672c\u533a\u57df/\u66b4\u51fb\u4f24\u5bb3";

    [SerializeField] private Component characterNameText;
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
    [SerializeField] private Component physicalResistancePenetrationText;
    [SerializeField] private Component fireResistancePenetrationText;
    [SerializeField] private Component corruptionResistancePenetrationText;
    [SerializeField] private Component coldResistancePenetrationText;
    [SerializeField] private Component criticalChanceText;
    [SerializeField] private Component criticalDamageText;

    [SerializeField] private CharacterStatDatabase statDatabase;
    [SerializeField] private BattleCharacterBindingDatabase characterBindingDatabase;

    private const string FireballSkillId = "\u706b\u7403";
    private const float DefaultFireballAttributeMultiplier = 0.8f;

    private string lastSignature = string.Empty;

    private void Reset()
    {
        AutoBind();
    }

    private void Awake()
    {
        EnsureDatabases();
    }

    private void OnEnable()
    {
        AutoBindMissingReferences();
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh(force: false);
    }

    [ContextMenu("\u81ea\u52a8\u7ed1\u5b9a")]
    private void AutoBind()
    {
        characterNameText = FindTextByPath(CharacterNamePath);
        healthText = FindTextByPath(HealthPath);
        manaText = FindTextByPath(ManaPath);
        attackPowerText = FindTextByPath(AttackPowerPath);
        spellDamageText = FindTextByPath(SpellDamagePath);
        strengthText = FindTextByPath(StrengthPath);
        agilityText = FindTextByPath(AgilityPath);
        intelligenceText = FindTextByPath(IntelligencePath);
        hitRateText = FindTextByPath(HitRatePath);
        dodgeRateText = FindTextByPath(DodgeRatePath);
        physicalResistanceText = FindTextByPath(PhysicalResistancePath);
        fireResistanceText = FindTextByPath(FireResistancePath);
        corruptionResistanceText = FindTextByPath(CorruptionResistancePath);
        coldResistanceText = FindTextByPath(ColdResistancePath);
        physicalResistancePenetrationText = FindTextByPath(PhysicalResistancePenetrationPath);
        fireResistancePenetrationText = FindTextByPath(FireResistancePenetrationPath);
        corruptionResistancePenetrationText = FindTextByPath(CorruptionResistancePenetrationPath);
        coldResistancePenetrationText = FindTextByPath(ColdResistancePenetrationPath);
        criticalChanceText = FindTextByPath(CriticalChancePath);
        criticalDamageText = FindTextByPath(CriticalDamagePath);
        EnsureDatabases();
    }

    private void AutoBindMissingReferences()
    {
        if (characterNameText == null)
        {
            characterNameText = FindTextByPath(CharacterNamePath);
        }

        if (strengthText == null)
        {
            strengthText = FindTextByPath(StrengthPath);
        }

        if (healthText == null)
        {
            healthText = FindTextByPath(HealthPath);
        }

        if (manaText == null)
        {
            manaText = FindTextByPath(ManaPath);
        }

        if (attackPowerText == null)
        {
            attackPowerText = FindTextByPath(AttackPowerPath);
        }

        if (spellDamageText == null)
        {
            spellDamageText = FindTextByPath(SpellDamagePath);
        }

        if (agilityText == null)
        {
            agilityText = FindTextByPath(AgilityPath);
        }

        if (intelligenceText == null)
        {
            intelligenceText = FindTextByPath(IntelligencePath);
        }

        if (hitRateText == null)
        {
            hitRateText = FindTextByPath(HitRatePath);
        }

        if (dodgeRateText == null)
        {
            dodgeRateText = FindTextByPath(DodgeRatePath);
        }

        if (physicalResistanceText == null)
        {
            physicalResistanceText = FindTextByPath(PhysicalResistancePath);
        }

        if (fireResistanceText == null)
        {
            fireResistanceText = FindTextByPath(FireResistancePath);
        }

        if (corruptionResistanceText == null)
        {
            corruptionResistanceText = FindTextByPath(CorruptionResistancePath);
        }

        if (coldResistanceText == null)
        {
            coldResistanceText = FindTextByPath(ColdResistancePath);
        }

        if (physicalResistancePenetrationText == null)
        {
            physicalResistancePenetrationText = FindTextByPath(PhysicalResistancePenetrationPath);
        }

        if (fireResistancePenetrationText == null)
        {
            fireResistancePenetrationText = FindTextByPath(FireResistancePenetrationPath);
        }

        if (corruptionResistancePenetrationText == null)
        {
            corruptionResistancePenetrationText = FindTextByPath(CorruptionResistancePenetrationPath);
        }

        if (coldResistancePenetrationText == null)
        {
            coldResistancePenetrationText = FindTextByPath(ColdResistancePenetrationPath);
        }

        if (criticalChanceText == null)
        {
            criticalChanceText = FindTextByPath(CriticalChancePath);
        }

        if (criticalDamageText == null)
        {
            criticalDamageText = FindTextByPath(CriticalDamagePath);
        }

        EnsureDatabases();
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
        string characterId = ResolveCurrentCharacterId();
        BattleUnit battleUnit = ResolveDisplayedBattleUnit(characterId);
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;
        float attackPower = string.IsNullOrWhiteSpace(characterId) ? 0f : InventoryShortcutRuntimeBinder.GetCharacterWeaponAttackPower(characterId);
        int spellDamage = CalculateDisplayedSpellDamage(characterId, battleUnit, statEntry);
        int hitRate = battleUnit != null
            ? battleUnit.HitRate
            : statEntry != null ? statEntry.ResolveHitRate() : -1;
        int dodgeRate = battleUnit != null
            ? battleUnit.DodgeRate
            : statEntry != null ? statEntry.ResolveDodgeRate() : -1;
        int criticalChance = statEntry != null
            ? statEntry.ResolveCriticalChance() + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalChanceBonus(characterId)
            : -1;
        int criticalDamage = statEntry != null
            ? statEntry.ResolveCriticalDamage() + InventoryShortcutRuntimeBinder.GetCharacterWeaponCriticalDamageBonus(characterId)
            : -1;
        string signature = BuildSignature(characterId, battleUnit, statEntry, attackPower, spellDamage, hitRate, dodgeRate, criticalChance, criticalDamage);
        if (!force && string.Equals(lastSignature, signature, System.StringComparison.Ordinal))
        {
            return;
        }

        lastSignature = signature;
        ApplyCharacter(characterId, battleUnit, statEntry, attackPower, spellDamage, hitRate, dodgeRate, criticalChance, criticalDamage);
    }

    private static string ResolveCurrentCharacterId()
    {
        string equipmentCharacterId = InventoryShortcutRuntimeBinder.CurrentEquipmentCharacterId;
        if (!string.IsNullOrWhiteSpace(equipmentCharacterId))
        {
            Debug.Log($"[JourneyAttributeDetailBinder] ResolveCurrentCharacterId source=equipment value='{equipmentCharacterId}'");
            return equipmentCharacterId;
        }

        BattleTurnSystem battleTurnSystem = FindObjectOfType<BattleTurnSystem>(true);
        if (battleTurnSystem != null)
        {
            if (battleTurnSystem.ActiveUnit != null)
            {
                string activeUnitCharacterId = battleTurnSystem.ActiveUnit.characterId;
                if (!string.IsNullOrWhiteSpace(activeUnitCharacterId))
                {
                    Debug.Log($"[JourneyAttributeDetailBinder] ResolveCurrentCharacterId source=active-turn value='{activeUnitCharacterId}'");
                    return activeUnitCharacterId;
                }
            }

            Debug.Log("[JourneyAttributeDetailBinder] ResolveCurrentCharacterId source=active-turn value=''");
            return string.Empty;
        }

        string journeyCharacterId = ResolveJourneySelectedCharacterId();
        Debug.Log($"[JourneyAttributeDetailBinder] ResolveCurrentCharacterId source=journey-selection value='{journeyCharacterId}'");
        return journeyCharacterId;
    }

    private static string ResolveJourneySelectedCharacterId()
    {
        CharacterSlotView[] slots = FindObjectsOfType<CharacterSlotView>(true);
        for (int i = 0; i < slots.Length; i++)
        {
            CharacterSlotView slot = slots[i];
            if (slot == null)
            {
                continue;
            }

            for (int j = 0; j < slot.selectToggles.Count; j++)
            {
                Toggle toggle = slot.selectToggles[j];
                if (toggle != null && toggle.isOn)
                {
                    string resolvedId = CharacterSelectionState.ResolveCharacterId(slot);
                    if (!string.IsNullOrWhiteSpace(resolvedId))
                    {
                        return resolvedId;
                    }
                }
            }
        }

        return string.Empty;
    }

    private static BattleUnit ResolveDisplayedBattleUnit(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return null;
        }

        BattleTurnSystem battleTurnSystem = FindObjectOfType<BattleTurnSystem>(true);
        if (battleTurnSystem != null && battleTurnSystem.ActiveUnit != null &&
            string.Equals(battleTurnSystem.ActiveUnit.characterId, characterId, System.StringComparison.Ordinal))
        {
            return battleTurnSystem.ActiveUnit;
        }

        BattleUnit[] units = FindObjectsOfType<BattleUnit>(true);
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit != null && string.Equals(unit.characterId, characterId, System.StringComparison.Ordinal))
            {
                return unit;
            }
        }

        return null;
    }

    private static string BuildSignature(
        string characterId,
        BattleUnit battleUnit,
        CharacterStatDatabase.StatEntry statEntry,
        float attackPower,
        int spellDamage,
        int hitRate,
        int dodgeRate,
        int criticalChance,
        int criticalDamage)
    {
        return string.Concat(
            characterId ?? string.Empty,
            "|",
            battleUnit != null ? battleUnit.currentHealth : -1,
            "/",
            battleUnit != null ? battleUnit.maxHealth : -1,
            "|",
            battleUnit != null ? battleUnit.currentMana : -1,
            "/",
            battleUnit != null ? battleUnit.maxMana : -1,
            "|",
            battleUnit != null ? battleUnit.currentActionPoints : -1,
            "|",
            spellDamage,
            "|",
            hitRate,
            "|",
            dodgeRate,
            "|",
            statEntry != null ? statEntry.strength : -1,
            "|",
            statEntry != null ? statEntry.agility : -1,
            "|",
            statEntry != null ? statEntry.intelligence : -1,
            "|",
            statEntry != null ? statEntry.ResolvePhysicalResistance() : -1,
            "|",
            statEntry != null ? statEntry.ResolveFireResistance() : -1,
            "|",
            statEntry != null ? statEntry.ResolveCorruptionResistance() : -1,
            "|",
            statEntry != null ? statEntry.ResolveColdResistance() : -1,
            "|",
            statEntry != null ? statEntry.ResolvePhysicalResistancePenetration() : -1,
            "|",
            statEntry != null ? statEntry.ResolveFireResistancePenetration() : -1,
            "|",
            statEntry != null ? statEntry.ResolveCorruptionResistancePenetration() : -1,
            "|",
            statEntry != null ? statEntry.ResolveColdResistancePenetration() : -1,
            "|",
            criticalChance,
            "|",
            criticalDamage,
            "|",
            Mathf.RoundToInt(attackPower * 100f));
    }

    private void ApplyCharacter(
        string characterId,
        BattleUnit battleUnit,
        CharacterStatDatabase.StatEntry statEntry,
        float attackPower,
        int spellDamage,
        int hitRate,
        int dodgeRate,
        int criticalChance,
        int criticalDamage)
    {
        bool hasBattleValues = battleUnit != null;
        string healthValue = statEntry != null
            ? hasBattleValues
                ? "生命值:" + Mathf.Max(0, battleUnit.currentHealth) + "/" + Mathf.Max(0, battleUnit.maxHealth)
                : "生命值:" + statEntry.ResolveMaxHealth()
            : "生命值:";
        string manaValue = statEntry != null
            ? hasBattleValues
                ? "魔法值:" + Mathf.Max(0, battleUnit.currentMana) + "/" + Mathf.Max(0, battleUnit.maxMana)
                : "魔法值:" + statEntry.ResolveMaxMana()
            : "魔法值:";

        SetText(characterNameText, ResolveDisplayName(characterId));
        SetText(healthText, healthValue);
        SetText(manaText, manaValue);
        SetText(attackPowerText, attackPower > 0f ? "\u653b\u51fb\u529b:" + Mathf.RoundToInt(attackPower) : "\u653b\u51fb\u529b:\u65e0\u6b66\u5668");
        SetText(spellDamageText, statEntry != null ? "\u6cd5\u672f\u4f24\u5bb3:" + spellDamage : "\u6cd5\u672f\u4f24\u5bb3:");
        SetText(strengthText, statEntry != null ? "\u529b\u91cf:" + statEntry.strength : "\u529b\u91cf:");
        SetText(agilityText, statEntry != null ? "\u654f\u6377:" + statEntry.agility : "\u654f\u6377:");
        SetText(intelligenceText, statEntry != null ? "\u667a\u529b:" + statEntry.intelligence : "\u667a\u529b:");
        SetText(hitRateText, hitRate >= 0 ? "\u547d\u4e2d:" + hitRate + "%" : "\u547d\u4e2d:");
        SetText(dodgeRateText, dodgeRate >= 0 ? "\u95ea\u907f:" + dodgeRate + "%" : "\u95ea\u907f:");
        SetText(physicalResistanceText, statEntry != null ? "\u7269\u7406\u6297\u6027:" + statEntry.ResolvePhysicalResistance() + "%" : "\u7269\u7406\u6297\u6027:");
        SetText(fireResistanceText, statEntry != null ? "\u706b\u7130\u6297\u6027:" + statEntry.ResolveFireResistance() + "%" : "\u706b\u7130\u6297\u6027:");
        SetText(corruptionResistanceText, statEntry != null ? "\u8150\u8d25\u6297\u6027:" + statEntry.ResolveCorruptionResistance() + "%" : "\u8150\u8d25\u6297\u6027:");
        SetText(coldResistanceText, statEntry != null ? "\u5bd2\u51b7\u6297\u6027:" + statEntry.ResolveColdResistance() + "%" : "\u5bd2\u51b7\u6297\u6027:");
        SetText(physicalResistancePenetrationText, statEntry != null ? "\u7269\u7406\u6297\u6027\u7A7F\u900F:" + statEntry.ResolvePhysicalResistancePenetration() + "%" : "\u7269\u7406\u6297\u6027\u7A7F\u900F:");
        SetText(fireResistancePenetrationText, statEntry != null ? "\u706b\u7130\u6297\u6027\u7A7F\u900F:" + statEntry.ResolveFireResistancePenetration() + "%" : "\u706b\u7130\u6297\u6027\u7A7F\u900F:");
        SetText(corruptionResistancePenetrationText, statEntry != null ? "\u8150\u8d25\u6297\u6027\u7A7F\u900F:" + statEntry.ResolveCorruptionResistancePenetration() + "%" : "\u8150\u8d25\u6297\u6027\u7A7F\u900F:");
        SetText(coldResistancePenetrationText, statEntry != null ? "\u5bd2\u51b7\u6297\u6027\u7A7F\u900F:" + statEntry.ResolveColdResistancePenetration() + "%" : "\u5bd2\u51b7\u6297\u6027\u7A7F\u900F:");
        SetText(criticalChanceText, statEntry != null ? "\u66b4\u51fb\u7387:" + criticalChance + "%" : "\u66b4\u51fb\u7387:");
        SetText(criticalDamageText, statEntry != null ? "\u66b4\u51fb\u4f24\u5bb3:" + criticalDamage + "%" : "\u66b4\u51fb\u4f24\u5bb3:");
    }

    private string ResolveDisplayName(string characterId)
    {
        if (!string.IsNullOrWhiteSpace(characterId) && characterBindingDatabase != null)
        {
            BattleCharacterBindingDatabase.BindingEntry binding = characterBindingDatabase.FindBinding(characterId);
            if (binding != null && !string.IsNullOrWhiteSpace(binding.displayName))
            {
                return binding.displayName;
            }
        }

        return string.IsNullOrWhiteSpace(characterId) ? string.Empty : characterId;
    }

    private static int CalculateDisplayedSpellDamage(
        string characterId,
        BattleUnit battleUnit,
        CharacterStatDatabase.StatEntry statEntry)
    {
        if (string.IsNullOrWhiteSpace(characterId) || statEntry == null)
        {
            return 0;
        }

        BattleSkillDatabase skillDatabase = BattleSkillDatabase.LoadDefault();
        BattleSkillDatabase.SkillEntry fireballSkill = skillDatabase != null ? skillDatabase.FindEntry(FireballSkillId) : null;
        float intelligence = battleUnit != null
            ? Mathf.Max(0, battleUnit.GetEffectiveIntelligence())
            : Mathf.Max(0, statEntry.intelligence);
        float fixedDamage = fireballSkill != null ? Mathf.Max(0, fireballSkill.fixedDamage) : 0f;
        float attributeMultiplier = fireballSkill != null
            ? Mathf.Max(0f, fireballSkill.attributeMultiplier)
            : DefaultFireballAttributeMultiplier;
        float staffMultiplier = InventoryShortcutRuntimeBinder.GetCharacterStaffDamageMultiplier(characterId);
        float damage = (fixedDamage + (attributeMultiplier * intelligence)) * Mathf.Max(0f, staffMultiplier);
        return Mathf.Max(0, Mathf.RoundToInt(damage));
    }

    private Component FindTextByPath(string path)
    {
        Transform target = transform.Find(path);
        if (target == null)
        {
            return null;
        }

        TMP_Text tmp = target.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            return tmp;
        }

        Text legacyText = target.GetComponent<Text>();
        if (legacyText != null)
        {
            return legacyText;
        }

        return null;
    }

    private static void SetText(Component target, string value)
    {
        if (target is TMP_Text tmp)
        {
            tmp.text = value ?? string.Empty;
            return;
        }

        if (target is Text legacyText)
        {
            legacyText.text = value ?? string.Empty;
        }
    }
}
