using System.Collections.Generic;
using UnityEngine;

public sealed class RoomClearedWallAnimationController : MonoBehaviour
{
    private const string RoomClearedEventId = "\u6E05\u7A7A\u623F\u95F4";

    private readonly List<Animator> controlledAnimators = new List<Animator>();
    private bool hasAppliedState;
    private bool appliedClearedState;

    private void OnEnable()
    {
        RefreshAnimators();
        EventRuntimeState.StateChanged += HandleEventStateChanged;
        ApplyState(EventRuntimeState.IsEnabled(RoomClearedEventId), true);
    }

    private void OnDisable()
    {
        EventRuntimeState.StateChanged -= HandleEventStateChanged;
    }

    public void RefreshAnimators()
    {
        controlledAnimators.Clear();
        GetComponentsInChildren(true, controlledAnimators);
        for (int i = controlledAnimators.Count - 1; i >= 0; i--)
        {
            Animator animator = controlledAnimators[i];
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                controlledAnimators.RemoveAt(i);
            }
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

        for (int i = controlledAnimators.Count - 1; i >= 0; i--)
        {
            Animator animator = controlledAnimators[i];
            if (animator == null)
            {
                controlledAnimators.RemoveAt(i);
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
