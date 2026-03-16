using UnityEngine;

[CreateAssetMenu(fileName = "CombatArtTooltipPrefabDatabase", menuName = "战斗/战技内容预制体数据库")]
public sealed class SkillTooltipPrefabDatabase : ScriptableObject
{
    public const string DefaultResourcePath = "CombatArtTooltipPrefabDatabase";

    public GameObject combatArtTooltipPrefab;

    public static SkillTooltipPrefabDatabase LoadDefault()
    {
        return Resources.Load<SkillTooltipPrefabDatabase>(DefaultResourcePath);
    }
}
