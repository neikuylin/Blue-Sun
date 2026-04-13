using UnityEngine;

internal static class BattleHitEffectUtility
{
    private const string HitMountPointName = "\u53D7\u51FB\u6302\u8F7D\u70B9";
    private const float DefaultDestroyDelaySeconds = 5f;

    public static void TryPlaySkillHitEffect(BattleUnit target, BattleSkillDatabase.SkillEntry skill)
    {
        if (target == null || skill == null || skill.hitEffectPrefab == null)
        {
            return;
        }

        Transform mountPoint = FindHitMountPoint(target.transform);
        if (mountPoint == null)
        {
            Debug.LogWarning($"[HitEffect] {target.unitName} missing mount point '{HitMountPointName}', skip hit effect for skill '{skill.skillId}'.");
            return;
        }

        Debug.Log($"[HitEffect] {target.unitName} found mount point '{HitMountPointName}', play hit effect for skill '{skill.skillId}'.");
        GameObject instance = Object.Instantiate(skill.hitEffectPrefab, mountPoint, false);
        if (instance == null)
        {
            return;
        }

        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        Object.Destroy(instance, ResolveDestroyDelay(instance));
    }

    private static Transform FindHitMountPoint(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform current = transforms[i];
            if (current != null && string.Equals(current.name, HitMountPointName, System.StringComparison.Ordinal))
            {
                return current;
            }
        }

        return null;
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
