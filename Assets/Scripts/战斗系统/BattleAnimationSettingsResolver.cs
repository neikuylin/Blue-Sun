using System;
using UnityEngine;

public static class BattleAnimationSettingsResolver
{
    public static string ResolveIdleStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.idleStateName : string.Empty;
    }

    public static string ResolveEnterBattleStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.enterBattleStateName : string.Empty;
    }

    public static AudioClip ResolveEnterBattleSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.enterBattleSound : null;
    }

    public static GameObject ResolveEnterBattleSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.enterBattleSoundPrefab : null;
    }

    public static bool ResolveEnterBattleCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.enterBattleCompensateMotion;
    }

    public static string ResolveExplorationIdleStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        if (settings == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(settings.explorationIdleStateName)
            ? settings.idleStateName
            : settings.explorationIdleStateName;
    }

    public static AudioClip ResolveExplorationIdleSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.explorationIdleSound : null;
    }

    public static GameObject ResolveExplorationIdleSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.explorationIdleSoundPrefab : null;
    }

    public static bool ResolveExplorationIdleCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.explorationIdleCompensateMotion;
    }

    public static string ResolveExplorationMoveStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        if (settings == null)
        {
            return string.Empty;
        }

        return string.IsNullOrWhiteSpace(settings.explorationMoveStateName)
            ? settings.idleStateName
            : settings.explorationMoveStateName;
    }

    public static AudioClip ResolveExplorationMoveSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.explorationMoveSound : null;
    }

    public static GameObject ResolveExplorationMoveSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.explorationMoveSoundPrefab : null;
    }

    public static bool ResolveExplorationMoveCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.explorationMoveCompensateMotion;
    }

    public static string ResolveExitBattleStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.exitBattleStateName : string.Empty;
    }

    public static AudioClip ResolveExitBattleSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.exitBattleSound : null;
    }

    public static GameObject ResolveExitBattleSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.exitBattleSoundPrefab : null;
    }

    public static bool ResolveExitBattleCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.exitBattleCompensateMotion;
    }

    public static string ResolveCombatArtLeftAimStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.combatArtLeftAimStateName : string.Empty;
    }

    public static bool ResolveCombatArtLeftAimCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.combatArtLeftAimCompensateMotion;
    }

    public static AudioClip ResolveCombatArtLeftAimSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.combatArtLeftAimSound : null;
    }

    public static GameObject ResolveCombatArtLeftAimSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.combatArtLeftAimSoundPrefab : null;
    }

    public static string ResolveCombatArtRightAimStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.combatArtRightAimStateName : string.Empty;
    }

    public static bool ResolveCombatArtRightAimCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.combatArtRightAimCompensateMotion;
    }

    public static AudioClip ResolveCombatArtRightAimSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.combatArtRightAimSound : null;
    }

    public static GameObject ResolveCombatArtRightAimSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.combatArtRightAimSoundPrefab : null;
    }

    public static float ResolveIdleYawOffset()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.idleYawOffset : 0f;
    }

    public static string ResolveHitReactionStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.hitReactionStateName : string.Empty;
    }

    public static bool ResolveHitReactionCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.hitReactionCompensateMotion;
    }

    public static AudioClip ResolveHitReactionSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.hitReactionSound : null;
    }

    public static GameObject ResolveHitReactionSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.hitReactionSoundPrefab : null;
    }

    public static string ResolveDodgeStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.dodgeStateName : string.Empty;
    }

    public static bool ResolveDodgeCompensateMotion()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null && settings.dodgeCompensateMotion;
    }

    public static AudioClip ResolveDodgeSound()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.dodgeSound : null;
    }

    public static GameObject ResolveDodgeSoundPrefab()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.dodgeSoundPrefab : null;
    }

    public static void ResolveCombatArtAimAudioForState(string stateName, out AudioClip clip, out GameObject soundPrefab)
    {
        clip = null;
        soundPrefab = null;

        if (string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        string leftStateName = ResolveCombatArtLeftAimStateName();
        if (string.Equals(stateName, leftStateName, StringComparison.Ordinal))
        {
            clip = ResolveCombatArtLeftAimSound();
            soundPrefab = ResolveCombatArtLeftAimSoundPrefab();
            return;
        }

        string rightStateName = ResolveCombatArtRightAimStateName();
        if (string.Equals(stateName, rightStateName, StringComparison.Ordinal))
        {
            clip = ResolveCombatArtRightAimSound();
            soundPrefab = ResolveCombatArtRightAimSoundPrefab();
        }
    }

    public static bool ShouldCompensateGlobalMotionForState(string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        string leftStateName = ResolveCombatArtLeftAimStateName();
        if (string.Equals(stateName, leftStateName, StringComparison.Ordinal))
        {
            return ResolveCombatArtLeftAimCompensateMotion();
        }

        string rightStateName = ResolveCombatArtRightAimStateName();
        if (string.Equals(stateName, rightStateName, StringComparison.Ordinal))
        {
            return ResolveCombatArtRightAimCompensateMotion();
        }

        string hitStateName = ResolveHitReactionStateName();
        if (string.Equals(stateName, hitStateName, StringComparison.Ordinal))
        {
            return ResolveHitReactionCompensateMotion();
        }

        string dodgeStateName = ResolveDodgeStateName();
        if (string.Equals(stateName, dodgeStateName, StringComparison.Ordinal))
        {
            return ResolveDodgeCompensateMotion();
        }

        return false;
    }
}
