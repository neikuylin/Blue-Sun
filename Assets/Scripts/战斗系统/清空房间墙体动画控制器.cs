using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public sealed class 清空房间墙体动画控制器 : MonoBehaviour
{
    private const string RoomClearedEventId = "\u6E05\u7A7A\u623F\u95F4";

    [SerializeField] private List<Behaviour> 进房间时倒放后要停在第一帧的动画 = new List<Behaviour>();
    [SerializeField] private List<Behaviour> 进房间时播放的音频组件 = new List<Behaviour>();
    [SerializeField] private 房间方向按钮 切房间时响应的房间按钮;
    [SerializeField] private List<Behaviour> 切房间时播放的正向音频组件 = new List<Behaviour>();
    [SerializeField] private List<GameObject> 切房间时关闭的物体 = new List<GameObject>();
    [SerializeField] private List<GameObject> 清空房间后开启的物体 = new List<GameObject>();
    [SerializeField] private List<GameObject> 清空房间后关闭的物体 = new List<GameObject>();

    private bool hasAppliedState;
    private bool appliedClearedState;
    private Coroutine roomEnterReverseRoutine;
    private Coroutine roomEnterForwardRoutine;

    private void OnEnable()
    {
        EventRuntimeState.StateChanged += HandleEventStateChanged;
        PlayRoomEnterReverse();
        ApplyRoomClearedState(EventRuntimeState.IsEnabled(RoomClearedEventId), true);
    }

    private void OnDisable()
    {
        EventRuntimeState.StateChanged -= HandleEventStateChanged;
        if (roomEnterReverseRoutine != null)
        {
            StopCoroutine(roomEnterReverseRoutine);
            roomEnterReverseRoutine = null;
        }

        if (roomEnterForwardRoutine != null)
        {
            StopCoroutine(roomEnterForwardRoutine);
            roomEnterForwardRoutine = null;
        }

        StopReverseAudioPlayback();
    }

    private void HandleEventStateChanged(string eventId, bool enabled)
    {
        if (eventId != RoomClearedEventId)
        {
            return;
        }

        ApplyRoomClearedState(enabled, false);
    }

    public bool 是否响应切房间按钮(房间方向按钮 clickedButton)
    {
        return clickedButton != null && 切房间时响应的房间按钮 == clickedButton;
    }

    public float 播放切房间时正向动画(房间方向按钮 clickedButton)
    {
        if (!是否响应切房间按钮(clickedButton))
        {
            return 0f;
        }

        StopRoomEnterReverse();
        ApplyGameObjectState(切房间时关闭的物体, false);

        if (roomEnterForwardRoutine != null)
        {
            StopCoroutine(roomEnterForwardRoutine);
            roomEnterForwardRoutine = null;
        }

        float duration = ResolveLongestCurrentStateLength();
        播放切房间时正向音频();
        roomEnterForwardRoutine = StartCoroutine(PlayRoomEnterForwardRoutine(duration));
        return duration;
    }

    public void 播放切房间时正向音频()
    {
        for (int i = 0; i < 切房间时播放的正向音频组件.Count; i++)
        {
            PlayAudioBehaviour(切房间时播放的正向音频组件[i]);
        }
    }

    private void PlayRoomEnterReverse()
    {
        StopRoomEnterReverse();

        if (roomEnterForwardRoutine != null)
        {
            StopCoroutine(roomEnterForwardRoutine);
            roomEnterForwardRoutine = null;
        }

        for (int i = 0; i < 进房间时倒放后要停在第一帧的动画.Count; i++)
        {
            Behaviour target = 进房间时倒放后要停在第一帧的动画[i];
            if (target == null)
            {
                continue;
            }

            PlayFromEnd(target);
        }

        roomEnterReverseRoutine = StartCoroutine(PlayRoomEnterReverseRoutine());
    }

    private void StopRoomEnterReverse()
    {
        if (roomEnterReverseRoutine != null)
        {
            StopCoroutine(roomEnterReverseRoutine);
            roomEnterReverseRoutine = null;
        }

        StopReverseAudioPlayback();
    }

    private void ApplyRoomClearedState(bool roomCleared, bool force)
    {
        if (!force && hasAppliedState && appliedClearedState == roomCleared)
        {
            return;
        }

        hasAppliedState = true;
        appliedClearedState = roomCleared;
        ApplyGameObjectState(清空房间后开启的物体, roomCleared);
        ApplyGameObjectState(清空房间后关闭的物体, !roomCleared);
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

    private static void PlayAudioBehaviour(Behaviour target)
    {
        if (target == null)
        {
            return;
        }

        if (target is PlayableDirector playableDirector)
        {
            playableDirector.time = 0d;
            playableDirector.Play();
            return;
        }

        if (target is AudioSource audioSource)
        {
            audioSource.enabled = true;
            audioSource.Stop();
            audioSource.time = 0f;
            audioSource.Play();
        }
    }

    private static void StopAudioBehaviour(Behaviour target)
    {
        if (target == null)
        {
            return;
        }

        if (target is PlayableDirector playableDirector)
        {
            playableDirector.Stop();
            playableDirector.time = 0d;
            playableDirector.Evaluate();
            return;
        }

        if (target is AudioSource audioSource)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }
    }

    private void PlayReverseAudioPlayback()
    {
        for (int i = 0; i < 进房间时播放的音频组件.Count; i++)
        {
            PlayAudioBehaviour(进房间时播放的音频组件[i]);
        }
    }

    private void StopReverseAudioPlayback()
    {
        for (int i = 0; i < 进房间时播放的音频组件.Count; i++)
        {
            StopAudioBehaviour(进房间时播放的音频组件[i]);
        }
    }

    private static void PlayFromEnd(Behaviour target)
    {
        if (target is PlayableDirector playableDirector)
        {
            SetTimelineTime(playableDirector, ResolvePlayableDirectorLength(playableDirector));
            return;
        }

        if (target is Animator animator)
        {
            PlayAnimatorFromEnd(animator);
        }
    }

    private static void ResetToStart(Behaviour target)
    {
        if (target is PlayableDirector playableDirector)
        {
            ResetTimelineToStart(playableDirector);
            return;
        }

        if (target is Animator animator)
        {
            ResetAnimatorToStart(animator);
        }
    }

    private static void PlayAnimatorFromEnd(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 0f;
        animator.Play(0, 0, 1f);
        animator.Update(0f);
    }

    private static void ResetAnimatorToStart(Animator animator)
    {
        animator.enabled = true;
        animator.speed = 0f;
        animator.Play(0, 0, 0f);
        animator.Update(0f);
        animator.enabled = false;
    }

    private static void ResetTimelineToStart(PlayableDirector playableDirector)
    {
        playableDirector.Stop();
        playableDirector.time = 0d;
        playableDirector.Evaluate();
    }

    private IEnumerator PlayRoomEnterReverseRoutine()
    {
        yield return null;

        PlayReverseAudioPlayback();

        float duration = ResolveLongestCurrentStateLength();
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float timelineTime = duration - elapsed;
            SampleAnimations(timelineTime);
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < 进房间时倒放后要停在第一帧的动画.Count; i++)
        {
            Behaviour target = 进房间时倒放后要停在第一帧的动画[i];
            if (target == null)
            {
                continue;
            }

            ResetToStart(target);
        }

        StopReverseAudioPlayback();
        roomEnterReverseRoutine = null;
    }

    private IEnumerator PlayRoomEnterForwardRoutine(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            SampleAnimations(elapsed);
            elapsed += Time.deltaTime;
            yield return null;
        }

        SampleAnimations(duration);
        StopAtEnd();
        roomEnterForwardRoutine = null;
    }

    private float ResolveLongestCurrentStateLength()
    {
        float duration = 0f;
        for (int i = 0; i < 进房间时倒放后要停在第一帧的动画.Count; i++)
        {
            Behaviour target = 进房间时倒放后要停在第一帧的动画[i];
            if (target == null)
            {
                continue;
            }

            float resolvedLength = ResolveAnimationTargetLength(target);
            duration = Mathf.Max(duration, resolvedLength);
        }

        return Mathf.Max(duration, 0.01f);
    }

    private static float ResolveAnimationTargetLength(Behaviour target)
    {
        if (target is PlayableDirector playableDirector)
        {
            return ResolvePlayableDirectorLength(playableDirector);
        }

        if (target is Animator animator)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            return ResolveAnimatorLength(animator, stateInfo);
        }

        return 0f;
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

    private static float ResolvePlayableDirectorLength(PlayableDirector playableDirector)
    {
        if (playableDirector == null)
        {
            return 0f;
        }

        double duration = playableDirector.duration;
        if ((double.IsInfinity(duration) || double.IsNaN(duration) || duration <= 0d) && playableDirector.playableAsset != null)
        {
            duration = playableDirector.playableAsset.duration;
        }

        if (double.IsInfinity(duration) || double.IsNaN(duration) || duration <= 0d)
        {
            return 0f;
        }

        return (float)duration;
    }

    private void SampleAnimations(float timelineTime)
    {
        for (int i = 0; i < 进房间时倒放后要停在第一帧的动画.Count; i++)
        {
            Behaviour target = 进房间时倒放后要停在第一帧的动画[i];
            if (target == null)
            {
                continue;
            }

            SampleAnimationTarget(target, timelineTime);
        }
    }

    private void StopAtEnd()
    {
        for (int i = 0; i < 进房间时倒放后要停在第一帧的动画.Count; i++)
        {
            Behaviour target = 进房间时倒放后要停在第一帧的动画[i];
            if (target == null)
            {
                continue;
            }

            StopAnimationTargetAtEnd(target);
        }
    }

    private static void SampleAnimationTarget(Behaviour target, float timelineTime)
    {
        float targetLength = ResolveAnimationTargetLength(target);
        if (targetLength <= 0f)
        {
            return;
        }

        float targetTime = Mathf.Clamp(timelineTime, 0f, targetLength);

        if (target is PlayableDirector playableDirector)
        {
            SamplePlayableDirector(playableDirector, targetTime);
            return;
        }

        if (target is Animator animator)
        {
            animator.enabled = true;
            animator.speed = 0f;
            animator.Play(0, 0, targetTime / targetLength);
            animator.Update(0f);
        }
    }

    private static void StopAnimationTargetAtEnd(Behaviour target)
    {
        float targetLength = ResolveAnimationTargetLength(target);
        if (targetLength <= 0f)
        {
            return;
        }

        if (target is PlayableDirector playableDirector)
        {
            SamplePlayableDirector(playableDirector, targetLength);
            return;
        }

        if (target is Animator animator)
        {
            animator.enabled = true;
            animator.speed = 0f;
            animator.Play(0, 0, 1f);
            animator.Update(0f);
            animator.enabled = false;
        }
    }

    private static void SetTimelineTime(PlayableDirector playableDirector, double time)
    {
        playableDirector.Stop();
        playableDirector.time = time;
        playableDirector.Evaluate();
    }

    private static void SamplePlayableDirector(PlayableDirector playableDirector, float timelineTime)
    {
        float targetLength = ResolvePlayableDirectorLength(playableDirector);
        if (targetLength <= 0f)
        {
            return;
        }

        SetTimelineTime(playableDirector, Mathf.Clamp(timelineTime, 0f, targetLength));
    }
}
