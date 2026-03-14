public static class ItemEditorLabels
{
    public static readonly string[] CategoryLabels =
    {
        "装备",
        "消耗品",
        "材料",
        "补给"
    };

    public static readonly string[] EquipmentSlotLabels =
    {
        "无",
        "主手",
        "副手",
        "主副手",
        "头盔",
        "胸甲",
        "腿甲",
        "手套",
        "鞋子",
        "饰品"
    };

    public static readonly string[] WeaponCategoryLabels =
    {
        "无",
        "单手武器",
        "双手武器"
    };

    public static readonly string[] MainOrOffHandWeaponCategoryLabels =
    {
        "无",
        "单手武器"
    };

    public static readonly string[] WeaponAttributeTypeLabels =
    {
        "无",
        "力量",
        "敏捷",
        "智力"
    };

    public static string[] GetWeaponCategoryLabels(ItemDatabase.EquipmentSlotType equipmentSlot)
    {
        return equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand
            ? MainOrOffHandWeaponCategoryLabels
            : WeaponCategoryLabels;
    }

    public static int ToWeaponCategoryPopupIndex(
        ItemDatabase.EquipmentSlotType equipmentSlot,
        ItemDatabase.WeaponCategory weaponCategory)
    {
        if (equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return weaponCategory == ItemDatabase.WeaponCategory.OneHanded ? 1 : 0;
        }

        return (int)weaponCategory;
    }

    public static ItemDatabase.WeaponCategory FromWeaponCategoryPopupIndex(
        ItemDatabase.EquipmentSlotType equipmentSlot,
        int popupIndex)
    {
        if (equipmentSlot == ItemDatabase.EquipmentSlotType.MainOrOffHand)
        {
            return popupIndex == 1
                ? ItemDatabase.WeaponCategory.OneHanded
                : ItemDatabase.WeaponCategory.None;
        }

        return (ItemDatabase.WeaponCategory)popupIndex;
    }
}
