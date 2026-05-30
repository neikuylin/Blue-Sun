using UnityEngine;

internal static class BattleHitEffectUtility
{
    private const float DefaultDestroyDelaySeconds = 5f;

    public static void TryPlaySkillHitEffect(BattleUnit target, BattleSkillDatabase.SkillEntry skill, Camera battleCamera)
    {
        if (target == null || skill == null || skill.hitEffectPrefab == null)
        {
            return;
        }

        Transform mountPoint = FindAvatarChestMountPoint(target);
        if (mountPoint == null)
        {
            return;
        }

        GameObject instance = Object.Instantiate(skill.hitEffectPrefab, mountPoint, false);
        if (instance == null)
        {
            return;
        }

        ApplyMountedEffectScaleCompensation(instance.transform, mountPoint);
        instance.transform.localPosition = Vector3.zero;
        if (battleCamera != null)
        {
            instance.transform.rotation = battleCamera.transform.rotation;
        }
        else
        {
            instance.transform.localRotation = Quaternion.identity;
        }
        Object.Destroy(instance, ResolveDestroyDelay(instance));
    }

    private static Transform FindAvatarChestMountPoint(BattleUnit target)
    {
        if (target == null)
        {
            return null;
        }

        Animator animator = target.GetComponentInChildren<Animator>(true);
        if (animator == null)
        {
            Debug.LogWarning($"[HitEffect] {target.unitName} 没有 Animator，无法按 Avatar Chest 播放受击特效。");
            return null;
        }

        if (!animator.isHuman)
        {
            Debug.LogWarning($"[HitEffect] {target.unitName} 的 Animator 不是 Humanoid，无法按 Avatar Chest 播放受击特效。", animator);
            return null;
        }

        Transform chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        if (chest == null)
        {
            Debug.LogWarning($"[HitEffect] {target.unitName} 的 Avatar 没有绑定 Chest，无法播放受击特效。", animator);
            return null;
        }

        return chest;
    }

    private static void ApplyMountedEffectScaleCompensation(Transform instance, Transform mountPoint)
    {
        if (instance == null || mountPoint == null)
        {
            return;
        }

        Vector3 prefabLocalScale = instance.localScale;
        Vector3 parentLossyScale = mountPoint.lossyScale;
        instance.localScale = new Vector3(
            DivideScaleAxis(prefabLocalScale.x, parentLossyScale.x),
            DivideScaleAxis(prefabLocalScale.y, parentLossyScale.y),
            DivideScaleAxis(prefabLocalScale.z, parentLossyScale.z));
    }

    private static float DivideScaleAxis(float value, float parentScale)
    {
        return Mathf.Abs(parentScale) <= 0.0001f ? value : value / parentScale;
    }

    private static float ResolveDestroyDelay(GameObject instance)
    {
        if (instance == null)
        {
            return DefaultDestroyDelaySeconds;
        }

        float longestDuration = 0f;

        ParticleSystem[] particleSystems = instance.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem particleSystem = particleSystems[i];
            if (particleSystem == null)
            {
                continue;
            }

            ParticleSystem.MainModule main = particleSystem.main;
            float duration = main.duration;
            float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                ? main.startLifetime.constantMax
                : main.startLifetime.constant;
            longestDuration = Mathf.Max(longestDuration, duration + startLifetime);
        }

        AudioSource[] audioSources = instance.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioSource audioSource = audioSources[i];
            if (audioSource == null || audioSource.clip == null)
            {
                continue;
            }

            longestDuration = Mathf.Max(longestDuration, audioSource.clip.length);
        }

        return longestDuration > 0.01f ? longestDuration : DefaultDestroyDelaySeconds;
    }
}
