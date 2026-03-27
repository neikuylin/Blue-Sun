using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "AttackPowerTextSpriteDatabase", menuName = "背包/攻击力文本图标数据库")]
public sealed class AttackPowerTextSpriteDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "AttackPowerTextSpriteDatabase";

    public TMP_SpriteAsset spriteAsset;

    public static AttackPowerTextSpriteDatabase LoadDefault()
    {
        return Resources.Load<AttackPowerTextSpriteDatabase>(DefaultResourcePath);
    }
}
