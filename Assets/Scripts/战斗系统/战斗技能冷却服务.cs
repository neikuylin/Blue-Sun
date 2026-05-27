using System;
using System.Collections.Generic;
using UnityEngine;

internal sealed class 战斗技能冷却服务
{
    private readonly Dictionary<技能冷却键, int> 当前冷却 = new Dictionary<技能冷却键, int>();

    public int 获取剩余冷却回合(string 角色ID, string 技能ID)
    {
        if (string.IsNullOrWhiteSpace(角色ID) || string.IsNullOrWhiteSpace(技能ID))
        {
            return 0;
        }

        return 当前冷却.TryGetValue(new 技能冷却键(角色ID, 技能ID), out int 剩余冷却)
            ? Mathf.Max(0, 剩余冷却)
            : 0;
    }

    public void 记录技能使用(BattleUnit 使用者, BattleSkillDatabase.SkillEntry 技能)
    {
        if (使用者 == null || 技能 == null || 技能.cooldownTurns <= 0)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(使用者.characterId) || string.IsNullOrWhiteSpace(技能.skillId))
        {
            Debug.LogWarning("[战斗技能冷却服务] 技能冷却记录失败：使用者角色ID或技能ID为空。");
            return;
        }

        当前冷却[new 技能冷却键(使用者.characterId, 技能.skillId)] = Mathf.Max(0, 技能.cooldownTurns);
    }

    public void 推进大回合冷却()
    {
        if (当前冷却.Count == 0)
        {
            return;
        }

        List<技能冷却键> keys = new List<技能冷却键>(当前冷却.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            技能冷却键 key = keys[i];
            int nextValue = 当前冷却[key] - 1;
            if (nextValue <= 0)
            {
                当前冷却.Remove(key);
            }
            else
            {
                当前冷却[key] = nextValue;
            }
        }
    }

    public void 清空()
    {
        当前冷却.Clear();
    }

    private readonly struct 技能冷却键 : IEquatable<技能冷却键>
    {
        private readonly string 角色ID;
        private readonly string 技能ID;

        public 技能冷却键(string 角色ID, string 技能ID)
        {
            this.角色ID = 角色ID ?? string.Empty;
            this.技能ID = 技能ID ?? string.Empty;
        }

        public bool Equals(技能冷却键 other)
        {
            return string.Equals(角色ID, other.角色ID, StringComparison.Ordinal) &&
                string.Equals(技能ID, other.技能ID, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            return obj is 技能冷却键 other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((角色ID != null ? 角色ID.GetHashCode() : 0) * 397) ^
                    (技能ID != null ? 技能ID.GetHashCode() : 0);
            }
        }
    }
}
