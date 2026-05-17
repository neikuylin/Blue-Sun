using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 武器模型挂载服务
{
    private const string LeftWeaponMountPointName = "武器挂载点（左）";
    private const string RightWeaponMountPointName = "武器挂载点（右）";
    private const string RuntimeWeaponModelName = "__RuntimeWeaponModel";
    private const float DefaultOutlineWidth = 0.025f;

    internal sealed class Context
    {
        public Func<BattleUnit[]> FindBattleUnits;
        public Func<string, List<InventoryShortcutRuntimeBinder.ItemSlotData>> GetEquipmentDataForCharacter;
        public Func<string, ItemDatabase.ItemEntry> ResolveItemEntry;
        public Func<Transform, string, Transform> FindChildByName;
        public Func<Transform, string, Transform> FindDescendantByName;
    }

    public void RefreshAllRuntimeWeaponModels(Context context)
    {
        BattleUnit[] units = context.FindBattleUnits != null ? context.FindBattleUnits() : Array.Empty<BattleUnit>();
        for (int i = 0; i < units.Length; i++)
        {
            RefreshRuntimeWeaponModel(context, units[i]);
        }
    }

    public void RefreshRuntimeWeaponModelForCharacter(Context context, string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return;
        }

        BattleUnit[] units = context.FindBattleUnits != null ? context.FindBattleUnits() : Array.Empty<BattleUnit>();
        for (int i = 0; i < units.Length; i++)
        {
            BattleUnit unit = units[i];
            if (unit == null || !string.Equals(unit.characterId, characterId, StringComparison.Ordinal))
            {
                continue;
            }

            RefreshRuntimeWeaponModel(context, unit);
        }
    }

    private void RefreshRuntimeWeaponModel(Context context, BattleUnit unit)
    {
        if (unit == null)
        {
            return;
        }

        Transform leftMountPoint = FindWeaponMountPoint(context, unit.transform, LeftWeaponMountPointName);
        Transform rightMountPoint = FindWeaponMountPoint(context, unit.transform, RightWeaponMountPointName);
        ClearRuntimeWeaponModel(leftMountPoint);
        ClearRuntimeWeaponModel(rightMountPoint);

        ItemDatabase.ItemEntry weaponEntry = ResolveEquippedWeaponModelEntry(context, unit.characterId);
        if (weaponEntry == null || weaponEntry.weaponModelPrefab == null)
        {
            return;
        }

        Transform mountPoint = ResolveWeaponMountPoint(weaponEntry, leftMountPoint, rightMountPoint);
        if (mountPoint == null)
        {
            return;
        }

        GameObject instance = UnityEngine.Object.Instantiate(weaponEntry.weaponModelPrefab, mountPoint, false);
        instance.name = RuntimeWeaponModelName;
        ApplyMountedModelScaleCompensation(instance.transform, mountPoint);
        BattleUnitOutlineBuilder.Apply(instance, Color.black, DefaultOutlineWidth);
        unit.RefreshOutlineBindings();
    }

    private static void ClearRuntimeWeaponModel(Transform mountPoint)
    {
        if (mountPoint == null)
        {
            return;
        }

        for (int i = mountPoint.childCount - 1; i >= 0; i--)
        {
            Transform child = mountPoint.GetChild(i);
            if (child == null || !string.Equals(child.name, RuntimeWeaponModelName, StringComparison.Ordinal))
            {
                continue;
            }

            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    private static void ApplyMountedModelScaleCompensation(Transform instance, Transform mountPoint)
    {
        if (instance == null || mountPoint == null)
        {
            return;
        }

        Vector3 prefabLocalPosition = instance.localPosition;
        Vector3 prefabLocalScale = instance.localScale;
        Vector3 parentLossyScale = mountPoint.lossyScale;
        instance.localPosition = new Vector3(
            DivideScaleAxis(prefabLocalPosition.x, parentLossyScale.x),
            DivideScaleAxis(prefabLocalPosition.y, parentLossyScale.y),
            DivideScaleAxis(prefabLocalPosition.z, parentLossyScale.z));
        instance.localScale = new Vector3(
            DivideScaleAxis(prefabLocalScale.x, parentLossyScale.x),
            DivideScaleAxis(prefabLocalScale.y, parentLossyScale.y),
            DivideScaleAxis(prefabLocalScale.z, parentLossyScale.z));
    }

    private static float DivideScaleAxis(float value, float parentScale)
    {
        return Mathf.Abs(parentScale) <= 0.0001f ? value : value / parentScale;
    }

    private static Transform FindWeaponMountPoint(Context context, Transform root, string mountPointName)
    {
        Transform child = context.FindChildByName != null ? context.FindChildByName(root, mountPointName) : null;
        return child ?? (context.FindDescendantByName != null ? context.FindDescendantByName(root, mountPointName) : null);
    }

    private static Transform ResolveWeaponMountPoint(
        ItemDatabase.ItemEntry weaponEntry,
        Transform leftMountPoint,
        Transform rightMountPoint)
    {
        if (weaponEntry == null)
        {
            return null;
        }

        switch (weaponEntry.weaponCategory)
        {
            case ItemDatabase.WeaponCategory.Bow:
            case ItemDatabase.WeaponCategory.Staff:
                return leftMountPoint;
            case ItemDatabase.WeaponCategory.OneHanded:
            case ItemDatabase.WeaponCategory.TwoHanded:
            default:
                return rightMountPoint;
        }
    }

    private static ItemDatabase.ItemEntry ResolveEquippedWeaponModelEntry(Context context, string characterId)
    {
        List<InventoryShortcutRuntimeBinder.ItemSlotData> equipment =
            context.GetEquipmentDataForCharacter != null ? context.GetEquipmentDataForCharacter(characterId) : null;
        if (equipment == null || equipment.Count == 0)
        {
            return null;
        }

        ItemDatabase.ItemEntry bestEntry = null;
        int bestPriority = int.MaxValue;
        for (int i = 0; i < equipment.Count; i++)
        {
            InventoryShortcutRuntimeBinder.ItemSlotData slot = equipment[i];
            if (string.IsNullOrWhiteSpace(slot.itemId))
            {
                continue;
            }

            ItemDatabase.ItemEntry entry = context.ResolveItemEntry != null ? context.ResolveItemEntry(slot.itemId) : null;
            if (entry == null ||
                entry.category != ItemDatabase.ItemCategory.Equipment ||
                entry.weaponModelPrefab == null ||
                !ItemDatabase.SupportsWeaponModelPrefab(entry.equipmentSlot))
            {
                continue;
            }

            int priority = entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainHand ? 0 :
                entry.equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand ? 1 : int.MaxValue;
            if (bestEntry == null || priority < bestPriority)
            {
                bestEntry = entry;
                bestPriority = priority;
            }
        }

        return bestEntry;
    }
}
