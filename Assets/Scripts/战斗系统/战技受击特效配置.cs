using UnityEngine;

[CreateAssetMenu(fileName = "战技受击特效配置", menuName = "战斗/战技受击特效配置")]
public sealed class 战技受击特效配置 : ScriptableObject
{
    public const string DefaultResourcePath = "战技受击特效配置";

    public GameObject 物理受击特效;
    public GameObject 火焰受击特效;
    public GameObject 腐败受击特效;
    public GameObject 寒冷受击特效;

    public static 战技受击特效配置 LoadDefault()
    {
        return Resources.Load<战技受击特效配置>(DefaultResourcePath);
    }

    internal GameObject 解析受击特效(DamageAttributeType 属性)
    {
        switch (属性)
        {
            case DamageAttributeType.Fire:
                return 火焰受击特效;
            case DamageAttributeType.Corruption:
                return 腐败受击特效;
            case DamageAttributeType.Cold:
                return 寒冷受击特效;
            default:
                return 物理受击特效;
        }
    }
}
