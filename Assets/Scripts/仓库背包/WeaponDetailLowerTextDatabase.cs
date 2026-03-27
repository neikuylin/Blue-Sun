using UnityEngine;

[CreateAssetMenu(fileName = "WeaponDetailLowerTextDatabase", menuName = "背包/武器详细下文本数据库")]
public sealed class WeaponDetailLowerTextDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "WeaponDetailLowerTextDatabase";

    public string criticalChanceFormat = "+暴击率：x%";
    public string criticalDamageFormat = "+暴击伤害：x%";

    public static WeaponDetailLowerTextDatabase LoadDefault()
    {
        return Resources.Load<WeaponDetailLowerTextDatabase>(DefaultResourcePath);
    }
}
