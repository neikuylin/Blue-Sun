using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public sealed class 清空房间墙体动画控制器 : MonoBehaviour
{
    private const string RoomClearedEventId = "\u6E05\u7A7A\u623F\u95F4";

    [SerializeField] private List<Animator> 要停在第一帧的动画 = new List<Animator>();
    [SerializeField] private List<GameObject> 清空房间后开启的物体 = new List<GameObject>();
    [SerializeField] private List<GameObject> 清空房间后关闭的物体 = new List<GameObject>();
    [SerializeField] private List<AudioSource> 清空房间后开启的音频组件 = new List<AudioSource>();

    private readonly StringBuilder debugSummary = new StringBuilder();
    private bool hasAppliedState;
    private bool appliedClearedState;
    private Coroutine roomEnterReverseRoutine;
    private int reverseDebugFrame;

    private void OnEnable()
    {
        EventRuntimeState.StateChanged += HandleEventStateChanged;
        ApplyState(EventRuntimeState.IsEnabled(RoomClearedEventId), true);
    }

    private void OnDisable()
    {
        AppendDebug("OnDisable: 控制器被关闭，停止倒播协程。");
        FlushDebugSummary();
        EventRuntimeState.StateChanged -= HandleEventStateChanged;
        if (roomEnterReverseRoutine != null)
        {
            StopCoroutine(roomEnterReverseRoutine);
            roomEnterReverseRoutine = null;
        }
    }

    private void HandleEventStateChanged(string eventId, bool enabled)
    {
        if (eventId != RoomClearedEventId)
        {
            return;
        }

        ApplyState(enabled, false);
    }

    private void ApplyState(bool roomCleared, bool force)
    {
        if (!force && hasAppliedState && appliedClearedState == roomCleared)
        {
            return;
        }

        Debug.ClearDeveloperConsole();
        debugSummary.Length = 0;
        AppendDebug($"ApplyState: roomCleared={roomCleared}, force={force}, activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}, animatorCount={要停在第一帧的动画.Count}.");

        hasAppliedState = true;
        appliedClearedState = roomCleared;
        if (roomEnterReverseRoutine != null)
        {
            AppendDebug("ApplyState: 发现旧倒播协程，先停止。");
            StopCoroutine(roomEnterReverseRoutine);
            roomEnterReverseRoutine = null;
        }

        bool playRoomEnterReverse = !roomCleared && force;
        AppendDebug($"ApplyState: playRoomEnterReverse={playRoomEnterReverse}.");

        for (int i = 0; i < 要停在第一帧的动画.Count; i++)
        {
            Animator animator = 要停在第一帧的动画[i];
            if (animator == null)
            {
                AppendDebug($"Animator[{i}]: null.");
                continue;
            }

            if (roomCleared)
            {
                AppendDebug($"Animator[{i}] '{animator.name}': 房间已清空，正播。");
                PlayFromStart(animator);
            }
            else if (force)
            {
                AppendDebug($"Animator[{i}] '{animator.name}': 进房间，先停到末帧。");
                PlayFromEnd(animator);
                AppendAnimatorState($"Animator[{i}] '{animator.name}' 末帧后", animator);
            }
            else
            {
                AppendDebug($"Animator[{i}] '{animator.name}': 未清空事件，重置到第一帧。");
                ResetToStart(animator);
            }
        }

        ApplyGameObjectState(清空房间后开启的物体, roomCleared);
        ApplyGameObjectState(清空房间后关闭的物体, !roomCleared);
        ApplyAudioSourceState(清空房间后开启的音频组件, roomCleared);
        AppendDebug($"ApplyState: 物体开关处理完成，activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}.");

        if (playRoomEnterReverse)
        {
            AppendDebug("ApplyState: 末尾启动倒播协程。");
            roomEnterReverseRoutine = StartCoroutine(PlayRoomEnterReverseRoutine());
        }
        else
        {
            FlushDebugSummary();
        }
    }

    private static void ApplyGameObjectState(List<GameObject> targets, bool active)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject target = targets[i];
            if (target == null)
            {
                continue;
            }

            target.SetActive(active);
        }
    }

    private static void ApplyAudioSourceState(List<AudioSource> targets, bool enabled)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            AudioSource target = targets[i];
            if (target == null)
            {
                continue;
            }

            target.enabled = enabled;
        }
    }

    private static void PlayFromStart(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 1f;
        animator.Play(0, 0, 0f);
        animator.Update(0f);
    }

    private static void PlayFromEnd(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 0f;
        animator.Play(0, 0, 1f);
        animator.Update(0f);
    }

    private static void ResetToStart(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 0f;
        animator.Play(0, 0, 0f);
        animator.Update(0f);
        animator.enabled = false;
    }

    private IEnumerator PlayRoomEnterReverseRoutine()
    {
        AppendDebug("倒播协程: 已进入，等待一帧。");
        FlushDebugSummary();
        yield return null;

        AppendDebug($"倒播协程: 等待一帧后恢复，activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}.");
        float duration = ResolveLongestCurrentStateLength();
        AppendDebug($"倒播协程: duration={duration}.");
        float elapsed = 0f;
        reverseDebugFrame = 0;

        while (elapsed < duration)
        {
            float normalizedTime = 1f - Mathf.Clamp01(elapsed / duration);
            SampleAnimations(normalizedTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        AppendDebug("倒播协程: 采样结束，重置并停在第一帧。");
        for (int i = 0; i < 要停在第一帧的动画.Count; i++)
        {
            Animator animator = 要停在第一帧的动画[i];
            if (animator == null)
            {
                continue;
            }

            ResetToStart(animator);
            AppendAnimatorState($"Animator[{i}] '{animator.name}' 重置后", animator);
        }

        roomEnterReverseRoutine = null;
        FlushDebugSummary();
    }

    private float ResolveLongestCurrentStateLength()
    {
        float duration = 0f;
        for (int i = 0; i < 要停在第一帧的动画.Count; i++)
        {
            Animator animator = 要停在第一帧的动画[i];
            if (animator == null)
            {
                continue;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float resolvedLength = ResolveAnimatorLength(animator, stateInfo);
            AppendDebug($"Animator[{i}] '{animator.name}' 状态: fullPathHash={stateInfo.fullPathHash}, shortNameHash={stateInfo.shortNameHash}, stateLength={stateInfo.length}, resolvedLength={resolvedLength}, normalizedTime={stateInfo.normalizedTime}, enabled={animator.enabled}, speed={animator.speed}.");
            duration = Mathf.Max(duration, resolvedLength);
        }

        return Mathf.Max(duration, 0.01f);
    }

    private static float ResolveAnimatorLength(Animator animator, AnimatorStateInfo stateInfo)
    {
        if (!float.IsInfinity(stateInfo.length) && !float.IsNaN(stateInfo.length) && stateInfo.length > 0f)
        {
            return stateInfo.length;
        }

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        if (controller == null || controller.animationClips == null || controller.animationClips.Length == 0)
        {
            return 0f;
        }

        float length = 0f;
        AnimationClip[] clips = controller.animationClips;
        for (int i = 0; i < clips.Length; i++)
        {
            AnimationClip clip = clips[i];
            if (clip == null)
            {
                continue;
            }

            length = Mathf.Max(length, clip.length);
        }

        return length;
    }

    private void SampleAnimations(float normalizedTime)
    {
        for (int i = 0; i < 要停在第一帧的动画.Count; i++)
        {
            Animator animator = 要停在第一帧的动画[i];
            if (animator == null)
            {
                continue;
            }

            animator.Play(0, 0, normalizedTime);
            animator.Update(0f);
            if (reverseDebugFrame < 5 || normalizedTime <= 0.01f)
            {
                AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
                AppendDebug($"采样[{reverseDebugFrame}] Animator[{i}] '{animator.name}': input={normalizedTime}, actual={stateInfo.normalizedTime}, fullPathHash={stateInfo.fullPathHash}, enabled={animator.enabled}, speed={animator.speed}.");
            }
        }

        reverseDebugFrame++;
    }

    private void AppendAnimatorState(string label, Animator animator)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        AppendDebug($"{label}: fullPathHash={stateInfo.fullPathHash}, shortNameHash={stateInfo.shortNameHash}, length={stateInfo.length}, normalizedTime={stateInfo.normalizedTime}, enabled={animator.enabled}, speed={animator.speed}.");
    }

    private void AppendDebug(string message)
    {
        if (debugSummary.Length > 0)
        {
            debugSummary.Append(" | ");
        }

        debugSummary.Append(message);
    }

    private void FlushDebugSummary()
    {
        if (debugSummary.Length == 0)
        {
            return;
        }

        Debug.Log($"清空房间墙体动画调试汇总：{debugSummary}", this);
    }
}
