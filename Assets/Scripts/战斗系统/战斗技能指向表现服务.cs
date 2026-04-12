using System;
using System.Collections;
using UnityEngine;

internal sealed class 战斗技能指向表现服务
{
    private string currentSkillTargetingStateName = string.Empty;
    private float currentSkillTargetingYawOffset;
    private bool skillTargetSelectionReady;
    private Coroutine skillTargetingIntroRoutine;
    private BattleUnit skillModeRotationAnchorUnit;
    private Quaternion skillModeRotationAnchorRotation = Quaternion.identity;
    private bool hasSkillModeRotationAnchor;

    public bool 技能目标选择已就绪
    {
        get { return skillTargetSelectionReady; }
    }

    public void 缓存技能模式旋转锚点(BattleUnit unit, bool wasSkillModeActive)
    {
        if (wasSkillModeActive || unit == null)
        {
            return;
        }

        skillModeRotationAnchorUnit = unit;
        skillModeRotationAnchorRotation = unit.transform.rotation;
        hasSkillModeRotationAnchor = true;
    }

    public void 清空技能模式状态(MonoBehaviour host, bool shouldRestoreRotation)
    {
        currentSkillTargetingStateName = string.Empty;
        currentSkillTargetingYawOffset = 0f;
        skillTargetSelectionReady = false;

        if (host != null && skillTargetingIntroRoutine != null)
        {
            host.StopCoroutine(skillTargetingIntroRoutine);
            skillTargetingIntroRoutine = null;
        }

        if (shouldRestoreRotation &&
            hasSkillModeRotationAnchor &&
            skillModeRotationAnchorUnit != null &&
            skillModeRotationAnchorUnit.IsAlive &&
            !skillModeRotationAnchorUnit.IsMoving)
        {
            skillModeRotationAnchorUnit.transform.rotation = skillModeRotationAnchorRotation;
        }

        skillModeRotationAnchorUnit = null;
        skillModeRotationAnchorRotation = Quaternion.identity;
        hasSkillModeRotationAnchor = false;
    }

    public void 开始技能指向引导(
        MonoBehaviour host,
        BattleUnit unit,
        BattleSkillDatabase.SkillEntry skill,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, string> resolveRaiseHandStateName,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, float> resolveRaiseHandYawOffset,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, string> resolveTargetSelectionStateName,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, float> resolveTargetSelectionYawOffset,
        Func<BattleUnit, string> resolveIdleStateName,
        Func<Vector3?> tryGetMouseWorldPoint,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, bool> isStillSameSkill)
    {
        if (host == null)
        {
            return;
        }

        if (skillTargetingIntroRoutine != null)
        {
            host.StopCoroutine(skillTargetingIntroRoutine);
            skillTargetingIntroRoutine = null;
        }

        skillTargetingIntroRoutine = host.StartCoroutine(播放技能指向引导流程(
            unit,
            skill,
            resolveRaiseHandStateName,
            resolveRaiseHandYawOffset,
            resolveTargetSelectionStateName,
            resolveTargetSelectionYawOffset,
            resolveIdleStateName,
            tryGetMouseWorldPoint,
            isStillSameSkill));
    }

    public void 更新技能指向朝向(bool isSkillModeActive, BattleUnit activeUnit, Vector3 worldPosition)
    {
        if (!isSkillModeActive || activeUnit == null || !activeUnit.IsAlive || activeUnit.IsMoving)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(currentSkillTargetingStateName))
        {
            return;
        }

        activeUnit.FaceToward(worldPosition);
        if (Mathf.Abs(currentSkillTargetingYawOffset) > 0.01f)
        {
            activeUnit.transform.rotation = activeUnit.transform.rotation * Quaternion.Euler(0f, currentSkillTargetingYawOffset, 0f);
        }
    }

    private IEnumerator 播放技能指向引导流程(
        BattleUnit unit,
        BattleSkillDatabase.SkillEntry skill,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, string> resolveRaiseHandStateName,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, float> resolveRaiseHandYawOffset,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, string> resolveTargetSelectionStateName,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, float> resolveTargetSelectionYawOffset,
        Func<BattleUnit, string> resolveIdleStateName,
        Func<Vector3?> tryGetMouseWorldPoint,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, bool> isStillSameSkill)
    {
        currentSkillTargetingStateName = string.Empty;
        currentSkillTargetingYawOffset = 0f;
        skillTargetSelectionReady = false;

        if (unit == null || skill == null || !unit.IsAlive)
        {
            skillTargetingIntroRoutine = null;
            yield break;
        }

        string raiseHandStateName = resolveRaiseHandStateName != null
            ? resolveRaiseHandStateName(skill, unit)
            : string.Empty;
        if (!string.IsNullOrWhiteSpace(raiseHandStateName))
        {
            unit.SetAnimationPositionCompensation(false);
            unit.PlayAnimationState(raiseHandStateName);
            currentSkillTargetingStateName = raiseHandStateName;
            currentSkillTargetingYawOffset = resolveRaiseHandYawOffset != null
                ? resolveRaiseHandYawOffset(skill, unit)
                : 0f;

            Vector3? raiseHandHitPoint = tryGetMouseWorldPoint != null ? tryGetMouseWorldPoint() : null;
            if (raiseHandHitPoint.HasValue)
            {
                更新技能指向朝向(true, unit, raiseHandHitPoint.Value);
            }

            Animator animator = unit.GetComponentInChildren<Animator>(true);
            if (animator != null && animator.runtimeAnimatorController != null && animator.isActiveAndEnabled)
            {
                yield return null;
                float duration = animator.GetCurrentAnimatorStateInfo(0).length;
                if (duration > 0.01f)
                {
                    yield return new WaitForSeconds(duration);
                }
            }
        }

        if (isStillSameSkill != null && !isStillSameSkill(unit, skill))
        {
            skillTargetingIntroRoutine = null;
            yield break;
        }

        string targetSelectionStateName = resolveTargetSelectionStateName != null
            ? resolveTargetSelectionStateName(skill, unit)
            : string.Empty;
        if (string.IsNullOrWhiteSpace(targetSelectionStateName))
        {
            string idleStateName = resolveIdleStateName != null ? resolveIdleStateName(unit) : string.Empty;
            if (!string.IsNullOrWhiteSpace(idleStateName))
            {
                unit.PlayAnimationState(idleStateName);
            }

            skillTargetSelectionReady = true;
            skillTargetingIntroRoutine = null;
            yield break;
        }

        unit.SetAnimationPositionCompensation(false);
        unit.PlayAnimationState(targetSelectionStateName);
        currentSkillTargetingStateName = targetSelectionStateName;
        currentSkillTargetingYawOffset = resolveTargetSelectionYawOffset != null
            ? resolveTargetSelectionYawOffset(skill, unit)
            : 0f;
        skillTargetSelectionReady = true;
        skillTargetingIntroRoutine = null;
    }
}
