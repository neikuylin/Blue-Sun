using System.Collections.Generic;
using UnityEngine;

public sealed class 清空房间墙体动画控制器 : MonoBehaviour
{
    private const string RoomClearedEventId = "\u6E05\u7A7A\u623F\u95F4";

    [SerializeField] private List<Animator> 要停在第一帧的动画 = new List<Animator>();
    [SerializeField] private List<GameObject> 清空房间后开启的物体 = new List<GameObject>();
    [SerializeField] private List<GameObject> 清空房间后关闭的物体 = new List<GameObject>();
    [SerializeField] private List<AudioSource> 清空房间后开启的音频组件 = new List<AudioSource>();

    private bool hasAppliedState;
    private bool appliedClearedState;

    private void OnEnable()
    {
        EventRuntimeState.StateChanged += HandleEventStateChanged;
        ApplyState(EventRuntimeState.IsEnabled(RoomClearedEventId), true);
    }

    private void OnDisable()
    {
        EventRuntimeState.StateChanged -= HandleEventStateChanged;
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
            else
            {
                ResetToStart(animator);
            }
        }

        ApplyGameObjectState(清空房间后开启的物体, roomCleared);
        ApplyGameObjectState(清空房间后关闭的物体, !roomCleared);
        ApplyAudioSourceState(清空房间后开启的音频组件, roomCleared);
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

    private static void ResetToStart(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 0f;
        animator.Play(0, 0, 0f);
        animator.Update(0f);
        animator.enabled = false;
    }
}
