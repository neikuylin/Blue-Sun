using System;
using System.Collections;
using UnityEngine;

internal sealed class 战斗技能表现服务
{
    private Coroutine hitFeelRoutine;
    private float hitFeelRestoreTimeScale = 1f;
    private float hitFeelRestoreFixedDeltaTime = 0.02f;
    private bool hitFeelActive;

    public void 恢复全局时间缩放(MonoBehaviour host, float hitFeelTimeScale)
    {
        if (host != null && hitFeelRoutine != null)
        {
            host.StopCoroutine(hitFeelRoutine);
            hitFeelRoutine = null;
        }

        if (Mathf.Approximately(Time.timeScale, hitFeelTimeScale))
        {
            Time.timeScale = hitFeelRestoreTimeScale;
        }

        if (Mathf.Approximately(Time.fixedDeltaTime, hitFeelRestoreFixedDeltaTime * hitFeelTimeScale))
        {
            Time.fixedDeltaTime = hitFeelRestoreFixedDeltaTime;
        }

        hitFeelActive = false;
    }

    public void 触发技能命中停顿(
        MonoBehaviour host,
        BattleSkillDatabase.SkillEntry skill,
        float hitFeelDurationSeconds,
        float hitFeelTimeScale,
        float defaultFixedDeltaTime)
    {
        if (host == null || skill == null || !skill.enableHitFeel)
        {
            return;
        }

        if (!hitFeelActive)
        {
            hitFeelRestoreTimeScale = Time.timeScale;
            hitFeelRestoreFixedDeltaTime = Time.fixedDeltaTime > 0f ? Time.fixedDeltaTime : defaultFixedDeltaTime;
        }

        if (hitFeelRoutine != null)
        {
            host.StopCoroutine(hitFeelRoutine);
        }

        hitFeelRoutine = host.StartCoroutine(播放命中停顿流程(hitFeelDurationSeconds, hitFeelTimeScale));
    }

    public void 播放受击反应(
        BattleUnit target,
        Camera battleCamera,
        Func<BattleUnit, AudioClip> resolveReactionSound,
        Func<BattleUnit, GameObject> resolveReactionSoundPrefab,
        Func<BattleUnit, string> resolveReactionStateName,
        Func<BattleUnit, string> resolveIdleStateName,
        Func<BattleUnit, string, bool> shouldCompensateGlobalMotionForState)
    {
        播放单位反应(
            target,
            battleCamera,
            resolveReactionSound,
            resolveReactionSoundPrefab,
            resolveReactionStateName,
            resolveIdleStateName,
            shouldCompensateGlobalMotionForState);
    }

    public IEnumerator 播放技能动画并在结算点执行(
        MonoBehaviour host,
        BattleUnit caster,
        BattleSkillDatabase.SkillEntry skill,
        Func<int, IEnumerator> resolveHitRoutine,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, string> resolveActionStateName,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, bool> resolveCompensateActionMotion,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, float> resolveActionYawOffset,
        Func<BattleSkillDatabase.SkillEntry, BattleUnit, float> resolvePostUseYawOffset,
        Func<BattleUnit, string> resolveIdleStateName,
        Func<BattleUnit, BattleSkillDatabase.SkillEntry, float, IEnumerator> createTrackedSkillAudioRoutine,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> playSkillHitAudio,
        Func<Animator, string, float, int> resolveAnimationStateTotalFrames,
        Func<BattleSkillDatabase.SkillEntry, int, int, float, float> resolveSkillResolveDelaySeconds,
        float hitFeelDurationSeconds,
        float hitFeelTimeScale,
        float defaultFixedDeltaTime)
    {
        if (caster == null)
        {
            yield break;
        }

        if (skill == null)
        {
            if (resolveHitRoutine != null)
            {
                yield return resolveHitRoutine(0);
            }

            yield break;
        }

        string actionStateName = resolveActionStateName != null ? resolveActionStateName(skill, caster) : string.Empty;
        if (string.IsNullOrWhiteSpace(actionStateName))
        {
            if (createTrackedSkillAudioRoutine != null)
            {
                yield return createTrackedSkillAudioRoutine(caster, skill, 0f);
            }

            yield return ExecuteAllHitsImmediately(caster, skill, resolveHitRoutine, playSkillHitAudio);
            yield break;
        }

        Animator animator = caster.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            if (createTrackedSkillAudioRoutine != null)
            {
                yield return createTrackedSkillAudioRoutine(caster, skill, 0f);
            }

            yield return ExecuteAllHitsImmediately(caster, skill, resolveHitRoutine, playSkillHitAudio);
            yield break;
        }

        caster.SetAnimationPositionCompensation(resolveCompensateActionMotion != null && resolveCompensateActionMotion(skill, caster));

        AnimatorStateInfo previousState = animator.GetCurrentAnimatorStateInfo(0);
        int previousStateHash = previousState.fullPathHash != 0 ? previousState.fullPathHash : previousState.shortNameHash;
        Quaternion previousRotation = caster.transform.rotation;
        float actionYawOffset = resolveActionYawOffset != null ? resolveActionYawOffset(skill, caster) : 0f;
        if (Mathf.Abs(actionYawOffset) > 0.01f)
        {
            caster.transform.rotation = previousRotation * Quaternion.Euler(0f, actionYawOffset, 0f);
        }

        animator.Play(actionStateName, 0, 0f);
        yield return null;

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(0);
        float clipDuration = currentState.length;
        if (host != null && createTrackedSkillAudioRoutine != null)
        {
            host.StartCoroutine(createTrackedSkillAudioRoutine(caster, skill, clipDuration));
        }

        int totalFrames = resolveAnimationStateTotalFrames != null
            ? resolveAnimationStateTotalFrames(animator, actionStateName, clipDuration)
            : 0;
        int hitCount = skill.ResolveHitCount();
        float actionStartTime = Time.time;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            float resolveDelay = resolveSkillResolveDelaySeconds != null
                ? resolveSkillResolveDelaySeconds(skill, hitIndex, totalFrames, clipDuration)
                : clipDuration;
            if (resolveDelay < 0f)
            {
                continue;
            }

            float elapsedActionTime = Mathf.Max(0f, Time.time - actionStartTime);
            float waitDuration = Mathf.Max(0f, resolveDelay - elapsedActionTime);
            if (waitDuration > 0.01f)
            {
                yield return new WaitForSeconds(waitDuration);
            }

            if (!skill.useProjectile)
            {
                触发技能命中停顿(host, skill, hitFeelDurationSeconds, hitFeelTimeScale, defaultFixedDeltaTime);
                playSkillHitAudio?.Invoke(caster, skill);
            }

            if (resolveHitRoutine != null)
            {
                yield return resolveHitRoutine(hitIndex);
            }
        }

        float remainingDuration = Mathf.Max(0f, clipDuration - Mathf.Max(0f, Time.time - actionStartTime));
        if (remainingDuration > 0.01f)
        {
            yield return new WaitForSeconds(remainingDuration);
        }

        string idleStateName = resolveIdleStateName != null ? resolveIdleStateName(caster) : string.Empty;
        Quaternion postSkillIdleRotation = currentRotationWithOffsets(
            caster.transform.rotation,
            actionYawOffset,
            resolvePostUseYawOffset != null ? resolvePostUseYawOffset(skill, caster) : 0f);
        if (!string.IsNullOrWhiteSpace(idleStateName) && animator.isActiveAndEnabled)
        {
            animator.Play(idleStateName, 0, 0f);
            caster.transform.rotation = postSkillIdleRotation;
        }
        else if (previousStateHash != 0 && animator.isActiveAndEnabled)
        {
            animator.Play(previousStateHash, 0, 0f);
            caster.transform.rotation = postSkillIdleRotation;
        }

        caster.SetAnimationPositionCompensation(false);
    }

    private static IEnumerator ExecuteAllHitsImmediately(
        BattleUnit caster,
        BattleSkillDatabase.SkillEntry skill,
        Func<int, IEnumerator> resolveHitRoutine,
        Action<BattleUnit, BattleSkillDatabase.SkillEntry> playSkillHitAudio)
    {
        int hitCount = skill != null ? skill.ResolveHitCount() : 1;
        for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
        {
            if (skill == null || !skill.useProjectile)
            {
                playSkillHitAudio?.Invoke(caster, skill);
            }

            if (resolveHitRoutine != null)
            {
                yield return resolveHitRoutine(hitIndex);
            }
        }
    }

    private IEnumerator 播放命中停顿流程(float hitFeelDurationSeconds, float hitFeelTimeScale)
    {
        hitFeelActive = true;
        Time.timeScale = hitFeelTimeScale;
        Time.fixedDeltaTime = hitFeelRestoreFixedDeltaTime * hitFeelTimeScale;

        yield return new WaitForSecondsRealtime(hitFeelDurationSeconds);

        if (Mathf.Approximately(Time.timeScale, hitFeelTimeScale))
        {
            Time.timeScale = hitFeelRestoreTimeScale;
        }

        if (Mathf.Approximately(Time.fixedDeltaTime, hitFeelRestoreFixedDeltaTime * hitFeelTimeScale))
        {
            Time.fixedDeltaTime = hitFeelRestoreFixedDeltaTime;
        }

        hitFeelActive = false;
        hitFeelRoutine = null;
    }

    private static void 播放单位反应(
        BattleUnit target,
        Camera battleCamera,
        Func<BattleUnit, AudioClip> resolveReactionSound,
        Func<BattleUnit, GameObject> resolveReactionSoundPrefab,
        Func<BattleUnit, string> resolveReactionStateName,
        Func<BattleUnit, string> resolveIdleStateName,
        Func<BattleUnit, string, bool> shouldCompensateGlobalMotionForState)
    {
        if (target == null)
        {
            return;
        }

        BattleAudioUtility.PlayOnce(
            resolveReactionSound != null ? resolveReactionSound(target) : null,
            resolveReactionSoundPrefab != null ? resolveReactionSoundPrefab(target) : null,
            target,
            battleCamera);

        string stateName = resolveReactionStateName != null ? resolveReactionStateName(target) : string.Empty;
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            return;
        }

        string idleStateName = resolveIdleStateName != null ? resolveIdleStateName(target) : string.Empty;
        bool compensateMotion = shouldCompensateGlobalMotionForState != null &&
            shouldCompensateGlobalMotionForState(target, stateName);
        target.PlayAnimationStateForCurrentClipDuration(
            stateName,
            idleStateName,
            compensateMotion);
    }

    private static Quaternion currentRotationWithOffsets(Quaternion currentRotation, float actionYawOffset, float postUseYawOffset)
    {
        float idleYawOffset = BattleAnimationSettingsResolver.ResolveIdleYawOffset();
        return currentRotation * Quaternion.Euler(0f, idleYawOffset - actionYawOffset + postUseYawOffset, 0f);
    }
}
