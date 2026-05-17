using System;
using UnityEngine;

public sealed class 装备武器测试 : MonoBehaviour
{
    private const string 左手武器挂载点名称 = "武器挂载点（左）";
    private const string 右手武器挂载点名称 = "武器挂载点（右）";
    private const string 运行时武器模型名称 = "__RuntimeWeaponModel";
    private const float 默认描边宽度 = 0.025f;

    [SerializeField] private ItemDatabase 物品数据库;
    [SerializeField] private BattleCharacterBindingDatabase 战斗角色绑定库;
    [SerializeField] private string 角色ID = string.Empty;
    [SerializeField] private string 武器物品ID = string.Empty;
    [SerializeField] private GameObject 直接武器模型预制体;
    [SerializeField] private ItemDatabase.WeaponCategory 直接武器类型 = ItemDatabase.WeaponCategory.OneHanded;
    [SerializeField] private bool 生成前应用战斗模型倍率 = true;
    [SerializeField] private bool 应用战斗黑色描边 = true;
    [SerializeField] private bool 已记录原始缩放;
    [SerializeField] private Vector3 原始本地缩放 = Vector3.one;

    public ItemDatabase 数据库 => 物品数据库;
    public BattleCharacterBindingDatabase 角色绑定库 => 战斗角色绑定库;
    public string 当前角色ID => 角色ID;
    public string 当前武器物品ID => 武器物品ID;

    public void 设置物品数据库(ItemDatabase database)
    {
        物品数据库 = database;
    }

    public void 设置战斗角色绑定库(BattleCharacterBindingDatabase database)
    {
        战斗角色绑定库 = database;
    }

    public bool 生成测试装备(out string result)
    {
        Transform leftMountPoint = FindWeaponMountPoint(transform, 左手武器挂载点名称);
        Transform rightMountPoint = FindWeaponMountPoint(transform, 右手武器挂载点名称);
        清理测试装备();

        if (生成前应用战斗模型倍率 && !应用战斗模型倍率(out result))
        {
            return false;
        }

        if (!ResolveWeaponPrefab(out GameObject weaponPrefab, out ItemDatabase.WeaponCategory weaponCategory, out result))
        {
            return false;
        }

        Transform mountPoint = ResolveWeaponMountPoint(weaponCategory, leftMountPoint, rightMountPoint);
        if (mountPoint == null)
        {
            result = weaponCategory == ItemDatabase.WeaponCategory.Bow || weaponCategory == ItemDatabase.WeaponCategory.Staff
                ? $"没有找到{左手武器挂载点名称}。"
                : $"没有找到{右手武器挂载点名称}。";
            return false;
        }

        GameObject instance = Instantiate(weaponPrefab, mountPoint, false);
        instance.name = 运行时武器模型名称;
        ApplyMountedModelScaleCompensation(instance.transform, mountPoint);

        if (应用战斗黑色描边)
        {
            BattleUnitOutlineBuilder.Apply(instance, Color.black, 默认描边宽度);
            BattleUnit battleUnit = GetComponent<BattleUnit>();
            if (battleUnit != null)
            {
                battleUnit.RefreshOutlineBindings();
            }
        }

        result = $"已在{mountPoint.name}下生成测试武器模型：{weaponPrefab.name}。";
        return true;
    }

    public bool 应用战斗模型倍率(out string result)
    {
        BattleCharacterBindingDatabase.BindingEntry binding = ResolveCharacterBinding();
        if (binding == null)
        {
            result = "没有选择有效的战斗角色绑定。";
            return false;
        }

        if (!已记录原始缩放)
        {
            原始本地缩放 = transform.localScale;
            已记录原始缩放 = true;
        }

        Vector3 configuredScale = binding.modelScale;
        if (configuredScale == Vector3.zero)
        {
            configuredScale = Vector3.one;
        }

        transform.localScale = new Vector3(
            原始本地缩放.x * configuredScale.x,
            原始本地缩放.y * configuredScale.y,
            原始本地缩放.z * configuredScale.z);

        result = $"已应用“{ResolveCharacterLabel(binding)}”的战斗模型倍率：{configuredScale}。";
        return true;
    }

    public void 恢复原始缩放()
    {
        if (!已记录原始缩放)
        {
            return;
        }

        transform.localScale = 原始本地缩放;
        已记录原始缩放 = false;
    }

    public void 清理测试装备并恢复缩放()
    {
        清理测试装备();
        恢复原始缩放();
    }

    public void 清理测试装备()
    {
        ClearRuntimeWeaponModel(FindWeaponMountPoint(transform, 左手武器挂载点名称));
        ClearRuntimeWeaponModel(FindWeaponMountPoint(transform, 右手武器挂载点名称));
    }

    private BattleCharacterBindingDatabase.BindingEntry ResolveCharacterBinding()
    {
        BattleCharacterBindingDatabase database =
            战斗角色绑定库 != null ? 战斗角色绑定库 : BattleCharacterBindingDatabase.LoadDefault();
        if (database == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(角色ID))
        {
            return database.FindBinding(角色ID);
        }

        return null;
    }

    private static string ResolveCharacterLabel(BattleCharacterBindingDatabase.BindingEntry binding)
    {
        if (binding == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(binding.displayName) ? binding.characterId : binding.displayName;
    }

    private bool ResolveWeaponPrefab(
        out GameObject weaponPrefab,
        out ItemDatabase.WeaponCategory weaponCategory,
        out string result)
    {
        if (直接武器模型预制体 != null)
        {
            weaponPrefab = 直接武器模型预制体;
            weaponCategory = 直接武器类型;
            result = string.Empty;
            return true;
        }

        ItemDatabase database = 物品数据库 != null ? 物品数据库 : ItemDatabase.LoadDefault();
        if (database == null)
        {
            weaponPrefab = null;
            weaponCategory = ItemDatabase.WeaponCategory.None;
            result = "没有指定物品数据库，也没有找到 Resources/ItemDatabase。";
            return false;
        }

        ItemDatabase.ItemEntry entry = database.FindEntry(武器物品ID);
        if (entry == null)
        {
            weaponPrefab = null;
            weaponCategory = ItemDatabase.WeaponCategory.None;
            result = "没有选择有效的武器物品。";
            return false;
        }

        if (entry.category != ItemDatabase.ItemCategory.Equipment ||
            entry.weaponModelPrefab == null ||
            !ItemDatabase.SupportsWeaponModelPrefab(entry.equipmentSlot))
        {
            weaponPrefab = null;
            weaponCategory = ItemDatabase.WeaponCategory.None;
            result = $"物品“{entry.displayName}”没有可用于战斗挂载的武器模型。";
            return false;
        }

        weaponPrefab = entry.weaponModelPrefab;
        weaponCategory = entry.weaponCategory;
        result = string.Empty;
        return true;
    }

    private static Transform ResolveWeaponMountPoint(
        ItemDatabase.WeaponCategory weaponCategory,
        Transform leftMountPoint,
        Transform rightMountPoint)
    {
        switch (weaponCategory)
        {
            case ItemDatabase.WeaponCategory.Bow:
            case ItemDatabase.WeaponCategory.Staff:
                return leftMountPoint;
            case ItemDatabase.WeaponCategory.OneHanded:
            case ItemDatabase.WeaponCategory.TwoHanded:
            case ItemDatabase.WeaponCategory.None:
            default:
                return rightMountPoint;
        }
    }

    private static Transform FindWeaponMountPoint(Transform root, string mountPointName)
    {
        Transform child = FindChildByName(root, mountPointName);
        return child ?? FindDescendantByName(root, mountPointName);
    }

    private static Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child != null && string.Equals(child.name, targetName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform root, string targetName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, targetName, StringComparison.Ordinal))
            {
                return child;
            }

            Transform descendant = FindDescendantByName(child, targetName);
            if (descendant != null)
            {
                return descendant;
            }
        }

        return null;
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
            if (child == null || !string.Equals(child.name, 运行时武器模型名称, StringComparison.Ordinal))
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
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
}
