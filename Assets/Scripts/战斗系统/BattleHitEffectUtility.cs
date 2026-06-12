using UnityEngine;

internal static class BattleHitEffectUtility
{
    private const float DefaultDestroyDelaySeconds = 5f;

    public static void TryPlaySkillHitEffect(BattleUnit target, BattleSkillDatabase.SkillEntry skill, Camera battleCamera, CombatDamageResult damageResult)
    {
        if (target == null || skill == null)
        {
            return;
        }

        Transform mountPoint = FindAvatarChestMountPoint(target);
        if (mountPoint == null)
        {
            return;
        }

        if (skill.group == BattleSkillDatabase.SkillGroup.CombatArt)
        {
            if (damageResult == null || damageResult.components == null || damageResult.components.Count == 0)
            {
                PlayCombatArtHitEffectBySkillDamageType(mountPoint, battleCamera, skill);
                return;
            }

            if (!PlayCombatArtHitEffects(mountPoint, battleCamera, damageResult))
            {
                PlayCombatArtHitEffectBySkillDamageType(mountPoint, battleCamera, skill);
            }
            return;
        }

        if (skill.group == BattleSkillDatabase.SkillGroup.Spell)
        {
            PlayHitEffectPrefab(skill.hitEffectPrefab, mountPoint, battleCamera);
        }
    }

    private static bool PlayCombatArtHitEffects(Transform mountPoint, Camera battleCamera, CombatDamageResult damageResult)
    {
        if (mountPoint == null || damageResult == null || damageResult.components == null || damageResult.components.Count == 0)
        {
            return false;
        }

        战技受击特效配置 config = 战技受击特效配置.LoadDefault();
        if (config == null)
        {
            return false;
        }

        bool playedAny = false;
        bool playedPhysical = false;
        bool playedFire = false;
        bool playedCorruption = false;
        bool playedCold = false;
        for (int i = 0; i < damageResult.components.Count; i++)
        {
            DamageComponent component = damageResult.components[i];
            if (component.amount <= 0f)
            {
                continue;
            }

            switch (component.attributeType)
            {
                case DamageAttributeType.Fire:
                    if (!playedFire)
                    {
                        playedAny |= PlayHitEffectPrefab(config.解析受击特效(DamageAttributeType.Fire), mountPoint, battleCamera);
                        playedFire = true;
                    }
                    break;
                case DamageAttributeType.Corruption:
                    if (!playedCorruption)
                    {
                        playedAny |= PlayHitEffectPrefab(config.解析受击特效(DamageAttributeType.Corruption), mountPoint, battleCamera);
                        playedCorruption = true;
                    }
                    break;
                case DamageAttributeType.Cold:
                    if (!playedCold)
                    {
                        playedAny |= PlayHitEffectPrefab(config.解析受击特效(DamageAttributeType.Cold), mountPoint, battleCamera);
                        playedCold = true;
                    }
                    break;
                default:
                    if (!playedPhysical)
                    {
                        playedAny |= PlayHitEffectPrefab(config.解析受击特效(DamageAttributeType.Physical), mountPoint, battleCamera);
                        playedPhysical = true;
                    }
                    break;
            }
        }

        return playedAny;
    }

    private static void PlayCombatArtHitEffectBySkillDamageType(Transform mountPoint, Camera battleCamera, BattleSkillDatabase.SkillEntry skill)
    {
        if (mountPoint == null || skill == null)
        {
            return;
        }

        战技受击特效配置 config = 战技受击特效配置.LoadDefault();
        if (config == null)
        {
            return;
        }

        PlayHitEffectPrefab(config.解析受击特效(转换技能伤害类型(skill.damageType)), mountPoint, battleCamera);
    }

    private static DamageAttributeType 转换技能伤害类型(BattleSkillDatabase.DamageType damageType)
    {
        switch (damageType)
        {
            case BattleSkillDatabase.DamageType.Fire:
                return DamageAttributeType.Fire;
            case BattleSkillDatabase.DamageType.Corruption:
                return DamageAttributeType.Corruption;
            case BattleSkillDatabase.DamageType.Cold:
                return DamageAttributeType.Cold;
            default:
                return DamageAttributeType.Physical;
        }
    }

    private static bool PlayHitEffectPrefab(GameObject prefab, Transform mountPoint, Camera battleCamera)
    {
        if (prefab == null || mountPoint == null)
        {
            return false;
        }

        GameObject instance = Object.Instantiate(prefab, mountPoint, false);
        if (instance == null)
        {
            return false;
        }

        AudioSource[] audioSources = instance.GetComponentsInChildren<AudioSource>(true);
        for (int i = 0; i < audioSources.Length; i++)
        {
            AudioRouting.ApplySkill(audioSources[i]);
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
        return true;
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
