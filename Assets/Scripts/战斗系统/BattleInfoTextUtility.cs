using System.Collections.Generic;
using UnityEngine;

public static class BattleInfoTextUtility
{
    public const string PlayerInfoColorHex = "#33CC66";
    public const string EnemyInfoColorHex = "#E14B4B";
    public const string NeutralInfoColorHex = "#A0A0A0";
    public const string PhysicalInfoColorHex = "#FFFFFF";
    public const string FireInfoColorHex = "#FF4D4D";
    public const string CorruptionInfoColorHex = "#33CC66";
    public const string ColdInfoColorHex = "#4DA6FF";

    public static string FormatTargetSkillMessage(BattleUnit caster, BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        string casterName = ResolveBattleInfoUnitName(caster, richText: true);
        string targetName = ResolveBattleInfoUnitName(target, richText: true);
        string skillName = ResolveBattleInfoSkillName(skill);
        return $"{casterName}对{targetName}使用了{skillName}";
    }

    public static string FormatAreaSkillMessage(BattleUnit caster, string skillName, Vector2Int targetCell, List<string> targetNames)
    {
        string casterName = ResolveBattleInfoUnitName(caster, richText: true);
        if (targetNames != null && targetNames.Count == 1)
        {
            return $"{casterName}对{targetNames[0]}使用了{skillName}";
        }

        if (targetNames != null && targetNames.Count > 1)
        {
            return $"{casterName}对{string.Join("、", targetNames)}使用了{skillName}";
        }

        return $"{casterName}在{targetCell}使用了{skillName}";
    }

    public static string ResolveBattleInfoUnitName(BattleUnit unit, bool richText = false)
    {
        string baseName;
        if (unit == null)
        {
            baseName = "未知目标";
        }
        else if (!string.IsNullOrWhiteSpace(unit.unitName))
        {
            baseName = unit.unitName;
        }
        else if (!string.IsNullOrWhiteSpace(unit.characterId))
        {
            baseName = unit.characterId;
        }
        else
        {
            baseName = unit.name;
        }

        if (!richText)
        {
            return baseName;
        }

        string colorHex = unit != null && unit.team == BattleTeam.Enemy ? EnemyInfoColorHex : PlayerInfoColorHex;
        return WrapBattleInfoColor(baseName, colorHex);
    }

    public static string ResolveBattleInfoSkillName(BattleSkillDatabase.SkillEntry skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.skillId))
        {
            return "技能";
        }

        return skill.skillId;
    }

    public static string WrapBattleInfoColor(string content, string colorHex)
    {
        string safeContent = string.IsNullOrWhiteSpace(content) ? string.Empty : content;
        if (string.IsNullOrWhiteSpace(safeContent) || string.IsNullOrWhiteSpace(colorHex))
        {
            return safeContent;
        }

        return $"<color={colorHex}>{safeContent}</color>";
    }
}
