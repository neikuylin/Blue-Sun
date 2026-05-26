using System;
using System.Collections.Generic;
using UnityEngine;

internal static class 装备数值服务
{
    public readonly struct 授予技能条目
    {
        public 授予技能条目(string skillId, string sourceItemId)
        {
            技能ID = skillId ?? string.Empty;
            来源物品ID = sourceItemId ?? string.Empty;
        }

        public string 技能ID { get; }
        public string 来源物品ID { get; }
    }

    public static float 获取角色武器攻击力(
        string characterId,
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        if (string.IsNullOrWhiteSpace(characterId) || equipment == null || equipment.Count == 0 || resolveItemEntry == null)
        {
            return 0f;
        }

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;
        if (statEntry == null)
        {
            return 0f;
        }

        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        return weaponEntry != null ? 计算武器攻击力(weaponEntry, statEntry) : 0f;
    }

    public static float 获取来源武器攻击力(
        string characterId,
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string sourceItemId)
    {
        if (string.IsNullOrWhiteSpace(characterId) || equipment == null || equipment.Count == 0 || resolveItemEntry == null || string.IsNullOrWhiteSpace(sourceItemId))
        {
            return 0f;
        }

        CharacterStatDatabase statDatabase = CharacterStatDatabase.LoadDefault();
        CharacterStatDatabase.StatEntry statEntry = statDatabase != null ? statDatabase.FindEntry(characterId) : null;
        if (statEntry == null)
        {
            return 0f;
        }

        ItemDatabase.ItemEntry weaponEntry = 获取来源武器条目(equipment, resolveItemEntry, sourceItemId);
        return weaponEntry != null ? 计算武器攻击力(weaponEntry, statEntry) : 0f;
    }

    public static ItemDatabase.WeaponDamageDistribution 获取角色武器伤害分布(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        if (weaponEntry == null)
        {
            return null;
        }

        if (!武器伤害分布有效(weaponEntry))
        {
            Debug.LogWarning($"[物品数据警告] 武器伤害分布未配置或总和不合法：{weaponEntry.itemId}");
            return null;
        }

        return 克隆武器伤害分布(weaponEntry.weaponDamageDistribution);
    }

    public static ItemDatabase.WeaponDamageDistribution 获取来源武器伤害分布(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string sourceItemId)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取来源武器条目(equipment, resolveItemEntry, sourceItemId);
        if (weaponEntry == null)
        {
            return null;
        }

        if (!武器伤害分布有效(weaponEntry))
        {
            Debug.LogWarning($"[物品数据警告] 武器伤害分布未配置或总和不合法：{weaponEntry.itemId}");
            return null;
        }

        return 克隆武器伤害分布(weaponEntry.weaponDamageDistribution);
    }

    public static int 获取角色武器暴击率加成(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        return weaponEntry != null ? Mathf.Max(0, weaponEntry.criticalChanceBonus) : 0;
    }

    public static int 获取来源武器暴击率加成(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string sourceItemId)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取来源武器条目(equipment, resolveItemEntry, sourceItemId);
        return weaponEntry != null ? Mathf.Max(0, weaponEntry.criticalChanceBonus) : 0;
    }

    public static int 获取角色武器暴击伤害加成(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        return weaponEntry != null ? Mathf.Max(0, weaponEntry.criticalDamageBonus) : 0;
    }

    public static int 获取来源武器暴击伤害加成(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string sourceItemId)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取来源武器条目(equipment, resolveItemEntry, sourceItemId);
        return weaponEntry != null ? Mathf.Max(0, weaponEntry.criticalDamageBonus) : 0;
    }

    public static int 获取角色武器抗性穿透(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        ItemDatabase.ResistanceModifierType resistanceType)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        if (weaponEntry == null || weaponEntry.weaponResistancePenetrations == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < weaponEntry.weaponResistancePenetrations.Count; i++)
        {
            ItemDatabase.WeaponResistancePenetrationEntry entry = weaponEntry.weaponResistancePenetrations[i];
            if (entry == null || entry.resistanceType != resistanceType)
            {
                continue;
            }

            total += Mathf.Max(0, entry.value);
        }

        return total;
    }

    public static int 获取来源武器抗性穿透(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string sourceItemId,
        ItemDatabase.ResistanceModifierType resistanceType)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取来源武器条目(equipment, resolveItemEntry, sourceItemId);
        if (weaponEntry == null || weaponEntry.weaponResistancePenetrations == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < weaponEntry.weaponResistancePenetrations.Count; i++)
        {
            ItemDatabase.WeaponResistancePenetrationEntry entry = weaponEntry.weaponResistancePenetrations[i];
            if (entry == null || entry.resistanceType != resistanceType)
            {
                continue;
            }

            total += Mathf.Max(0, entry.value);
        }

        return total;
    }

    public static ItemDatabase.WeaponCategory 获取角色已装备武器类型(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        return weaponEntry != null ? weaponEntry.weaponCategory : ItemDatabase.WeaponCategory.None;
    }

    public static float 获取角色法杖伤害倍率(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        ItemDatabase.ItemEntry weaponEntry = 获取最佳武器条目(equipment, resolveItemEntry);
        if (weaponEntry == null || weaponEntry.weaponCategory != ItemDatabase.WeaponCategory.Staff)
        {
            return 1f;
        }

        return Mathf.Max(0f, weaponEntry.staffDamageMultiplier);
    }

    public static List<string> 构建授予技能列表(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        Func<int, ItemDatabase.EquipmentSlotType> resolveEquipmentSlotType = null)
    {
        List<授予技能条目> entries = 构建授予技能条目列表(equipment, resolveItemEntry, resolveEquipmentSlotType);
        List<string> result = new List<string>(entries.Count);
        for (int i = 0; i < entries.Count; i++)
        {
            result.Add(entries[i].技能ID);
        }

        return result;
    }

    public static List<授予技能条目> 构建授予技能条目列表(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        Func<int, ItemDatabase.EquipmentSlotType> resolveEquipmentSlotType = null)
    {
        List<授予技能条目> result = new List<授予技能条目>();
        if (equipment == null || resolveItemEntry == null)
        {
            return result;
        }

        ItemDatabase.ItemEntry mainHandWeapon = null;
        ItemDatabase.ItemEntry offHandWeapon = null;
        for (int i = 0; i < equipment.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData slot = equipment[i];
            if (string.IsNullOrWhiteSpace(slot.itemId))
            {
                continue;
            }

            if (slot.isFootprintExtension)
            {
                continue;
            }

            ItemDatabase.ItemEntry itemEntry = resolveItemEntry(slot.itemId);
            if (itemEntry == null)
            {
                continue;
            }

            ItemDatabase.EquipmentSlotType actualSlotType = resolveEquipmentSlotType != null
                ? resolveEquipmentSlotType(i)
                : ItemDatabase.EquipmentSlotType.None;
            if (actualSlotType == ItemDatabase.EquipmentSlotType.MainHand && 是攻击力武器条目(itemEntry))
            {
                mainHandWeapon = itemEntry;
            }
            else if (actualSlotType == ItemDatabase.EquipmentSlotType.OffHand && 是攻击力武器条目(itemEntry))
            {
                offHandWeapon = itemEntry;
            }

            if (actualSlotType == ItemDatabase.EquipmentSlotType.OffHand &&
                itemEntry.weaponCategory == ItemDatabase.WeaponCategory.OneHanded)
            {
                continue;
            }

            if (itemEntry.grantedSkillIds == null)
            {
                continue;
            }

            for (int s = 0; s < itemEntry.grantedSkillIds.Count; s++)
            {
                string skillId = itemEntry.grantedSkillIds[s];
                if (string.IsNullOrWhiteSpace(skillId))
                {
                    continue;
                }

                if (string.Equals(skillId, BattleSkillDatabase.DualWieldNormalAttackSkillId, StringComparison.Ordinal))
                {
                    continue;
                }

                result.Add(new 授予技能条目(skillId, slot.itemId));
            }
        }

        if (mainHandWeapon != null &&
            offHandWeapon != null &&
            mainHandWeapon.weaponCategory == ItemDatabase.WeaponCategory.OneHanded &&
            offHandWeapon.weaponCategory == ItemDatabase.WeaponCategory.OneHanded)
        {
            result.Add(new 授予技能条目(BattleSkillDatabase.DualWieldNormalAttackSkillId, string.Empty));
        }

        return result;
    }

    public static string 查找授予技能来源物品(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string skillId)
    {
        if (equipment == null || resolveItemEntry == null || string.IsNullOrWhiteSpace(skillId))
        {
            return string.Empty;
        }

        for (int i = 0; i < equipment.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData slot = equipment[i];
            if (string.IsNullOrWhiteSpace(slot.itemId))
            {
                continue;
            }

            ItemDatabase.ItemEntry itemEntry = resolveItemEntry(slot.itemId);
            if (itemEntry == null || itemEntry.grantedSkillIds == null)
            {
                continue;
            }

            for (int s = 0; s < itemEntry.grantedSkillIds.Count; s++)
            {
                if (string.Equals(itemEntry.grantedSkillIds[s], skillId, StringComparison.Ordinal))
                {
                    return slot.itemId;
                }
            }
        }

        return string.Empty;
    }

    public static ItemDatabase.ItemEntry 获取最佳武器条目(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry)
    {
        if (equipment == null || resolveItemEntry == null)
        {
            return null;
        }

        ItemDatabase.ItemEntry weaponEntry = null;
        int bestPriority = int.MaxValue;
        for (int i = 0; i < equipment.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData slot = equipment[i];
            if (string.IsNullOrWhiteSpace(slot.itemId))
            {
                continue;
            }

            ItemDatabase.ItemEntry entry = resolveItemEntry(slot.itemId);
            if (!是攻击力武器条目(entry))
            {
                continue;
            }

            int priority = entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainHand ? 0 :
                entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand ? 1 : int.MaxValue;
            if (weaponEntry == null || priority < bestPriority)
            {
                weaponEntry = entry;
                bestPriority = priority;
            }
        }

        return weaponEntry;
    }

    public static ItemDatabase.ItemEntry 获取来源武器条目(
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment,
        Func<string, ItemDatabase.ItemEntry> resolveItemEntry,
        string sourceItemId)
    {
        if (equipment == null || resolveItemEntry == null || string.IsNullOrWhiteSpace(sourceItemId))
        {
            return null;
        }

        for (int i = 0; i < equipment.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData slot = equipment[i];
            if (slot.isFootprintExtension || !string.Equals(slot.itemId, sourceItemId, StringComparison.Ordinal))
            {
                continue;
            }

            ItemDatabase.ItemEntry entry = resolveItemEntry(slot.itemId);
            return 是攻击力武器条目(entry) ? entry : null;
        }

        return null;
    }

    public static ItemDatabase.WeaponDamageDistribution 克隆武器伤害分布(ItemDatabase.WeaponDamageDistribution distribution)
    {
        if (distribution == null)
        {
            return null;
        }

        return new ItemDatabase.WeaponDamageDistribution
        {
            physical = distribution.physical,
            fire = distribution.fire,
            corruption = distribution.corruption,
            cold = distribution.cold
        };
    }

    public static bool 武器伤害分布有效(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.weaponDamageDistribution == null)
        {
            return false;
        }

        ItemDatabase.WeaponDamageDistribution distribution = entry.weaponDamageDistribution;
        return distribution.physical >= 0 &&
            distribution.fire >= 0 &&
            distribution.corruption >= 0 &&
            distribution.cold >= 0 &&
            distribution.Total == 100;
    }

    public static float 计算武器攻击力(ItemDatabase.ItemEntry entry, CharacterStatDatabase.StatEntry statEntry)
    {
        if (!是攻击力武器条目(entry) || statEntry == null)
        {
            return 0f;
        }

        float attackPower = Mathf.Max(0f, entry.fixedDamage);
        if (entry.weaponAttributeMultipliers == null)
        {
            return attackPower;
        }

        for (int i = 0; i < entry.weaponAttributeMultipliers.Count; i++)
        {
            ItemDatabase.WeaponAttributeMultiplierEntry multiplier = entry.weaponAttributeMultipliers[i];
            if (multiplier == null || multiplier.attributeType == ItemDatabase.WeaponAttributeType.None)
            {
                continue;
            }

            attackPower += 获取角色属性值(statEntry, multiplier.attributeType) * multiplier.multiplier;
        }

        return attackPower;
    }

    public static bool 是攻击力武器条目(ItemDatabase.ItemEntry entry)
    {
        if (entry == null || entry.category != ItemDatabase.ItemCategory.Equipment)
        {
            return false;
        }

        if (entry.weaponCategory == ItemDatabase.WeaponCategory.None)
        {
            return false;
        }

        return entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainHand ||
            entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand;
    }

    public static float 获取角色属性值(
        CharacterStatDatabase.StatEntry statEntry,
        ItemDatabase.WeaponAttributeType attributeType)
    {
        if (statEntry == null)
        {
            return 0f;
        }

        switch (attributeType)
        {
            case ItemDatabase.WeaponAttributeType.Strength:
                return statEntry.strength;
            case ItemDatabase.WeaponAttributeType.Agility:
                return statEntry.agility;
            case ItemDatabase.WeaponAttributeType.Intelligence:
                return statEntry.intelligence;
            default:
                return 0f;
        }
    }
}
