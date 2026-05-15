using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public sealed class 清空房间墙体动画控制器 : MonoBehaviour
{
    private const string RoomClearedEventId = "\u6E05\u7A7A\u623F\u95F4";

    [SerializeField] private List<Animator> 要停在第一帧的动画 = new List<Animator>();
    [SerializeField] private List<GameObject> 清空房间后开启的物体 = new List<GameObject>();
    [SerializeField] private List<GameObject> 清空房间后关闭的物体 = new List<GameObject>();
    [SerializeField] private List<Behaviour> 清空房间后开启的音频组件 = new List<Behaviour>();

    private bool hasAppliedState;
    private bool appliedClearedState;
    private Coroutine roomEnterReverseRoutine;

    private void OnEnable()
    {
        EventRuntimeState.StateChanged += HandleEventStateChanged;
        ApplyState(EventRuntimeState.IsEnabled(RoomClearedEventId), true);
    }

    private void OnDisable()
    {
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

        hasAppliedState = true;
        appliedClearedState = roomCleared;
        if (roomEnterReverseRoutine != null)
        {
            StopCoroutine(roomEnterReverseRoutine);
            roomEnterReverseRoutine = null;
        }

        bool playRoomEnterReverse = !roomCleared && force;

        for (int i = 0; i < 要停在第一帧的动画.Count; i++)
        {
            Animator animator = 要停在第一帧的动画[i];
            if (animator == null)
            {
                continue;
            }

            if (roomCleared)
            {
                PlayFromStart(animator);
            }
            else if (force)
            {
                PlayFromEnd(animator);
            }
            else
            {
                ResetToStart(animator);
            }
        }

        ApplyGameObjectState(清空房间后开启的物体, roomCleared);
        ApplyGameObjectState(清空房间后关闭的物体, !roomCleared);
        ApplyAudioSourceState(清空房间后开启的音频组件, roomCleared);

        if (playRoomEnterReverse)
        {
            roomEnterReverseRoutine = StartCoroutine(PlayRoomEnterReverseRoutine());
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

    private static void ApplyAudioSourceState(List<Behaviour> targets, bool enabled)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            Behaviour target = targets[i];
            if (target == null)
            {
                continue;
            }

            if (target is PlayableDirector playableDirector)
            {
                ApplyPlayableDirectorState(playableDirector, enabled);
                continue;
            }

            target.enabled = enabled;
        }
    }

    private static void ApplyPlayableDirectorState(PlayableDirector target, bool play)
    {
        if (play)
        {
            target.time = 0d;
            target.Play();
            return;
        }

        target.Stop();
        target.time = 0d;
        target.Evaluate();
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
        yield return null;

        float duration = ResolveLongestCurrentStateLength();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float normalizedTime = 1f - Mathf.Clamp01(elapsed / duration);
            SampleAnimations(normalizedTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < 要停在第一帧的动画.Count; i++)
        {
            Animator animator = 要停在第一帧的动画[i];
            if (animator == null)
            {
                continue;
            }

            ResetToStart(animator);
        }

        roomEnterReverseRoutine = null;
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
        }
    }
}
