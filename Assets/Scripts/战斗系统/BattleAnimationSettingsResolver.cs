using System;
using UnityEngine;

public static class BattleAnimationSettingsResolver
{
    public static string ResolveIdleStateName(string characterId)
    {
        return ResolveStateName(characterId, settings => settings.idleOverrides);
    }

    public static AudioClip ResolveIdleSound(string characterId)
    {
        return ResolveSound(characterId, settings => settings.idleOverrides);
    }

    public static GameObject ResolveIdleSoundPrefab(string characterId)
    {
        return ResolveSoundPrefab(characterId, settings => settings.idleOverrides);
    }

    public static bool ResolveIdleCompensateMotion(string characterId)
    {
        return ResolveCompensateMotion(characterId, settings => settings.idleOverrides);
    }

    public static string ResolveEnterBattleStateName(string characterId)
    {
        return ResolveStateName(characterId, settings => settings.enterBattleOverrides);
    }

    public static AudioClip ResolveEnterBattleSound(string characterId)
    {
        return ResolveSound(characterId, settings => settings.enterBattleOverrides);
    }

    public static GameObject ResolveEnterBattleSoundPrefab(string characterId)
    {
        return ResolveSoundPrefab(characterId, settings => settings.enterBattleOverrides);
    }

    public static bool ResolveEnterBattleCompensateMotion(string characterId)
    {
        return ResolveCompensateMotion(characterId, settings => settings.enterBattleOverrides);
    }

    public static string ResolveExplorationIdleStateName()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.explorationIdleStateName : string.Empty;
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
        return settings != null ? settings.explorationMoveStateName : string.Empty;
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

    public static string ResolveExitBattleStateName(string characterId)
    {
        return ResolveStateName(characterId, settings => settings.exitBattleOverrides);
    }

    public static AudioClip ResolveExitBattleSound(string characterId)
    {
        return ResolveSound(characterId, settings => settings.exitBattleOverrides);
    }

    public static GameObject ResolveExitBattleSoundPrefab(string characterId)
    {
        return ResolveSoundPrefab(characterId, settings => settings.exitBattleOverrides);
    }

    public static bool ResolveExitBattleCompensateMotion(string characterId)
    {
        return ResolveCompensateMotion(characterId, settings => settings.exitBattleOverrides);
    }

    public static float ResolveIdleYawOffset()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? settings.idleYawOffset : 0f;
    }

    public static float ResolveModelTurn90Duration()
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        return settings != null ? Mathf.Max(0.01f, settings.modelTurn90Duration) : 0.3f;
    }

    public static string ResolveHitReactionStateName(string characterId)
    {
        return ResolveStateName(characterId, settings => settings.hitReactionOverrides);
    }

    public static bool ResolveHitReactionCompensateMotion(string characterId)
    {
        return ResolveCompensateMotion(characterId, settings => settings.hitReactionOverrides);
    }

    public static AudioClip ResolveHitReactionSound(string characterId)
    {
        return ResolveSound(characterId, settings => settings.hitReactionOverrides);
    }

    public static GameObject ResolveHitReactionSoundPrefab(string characterId)
    {
        return ResolveSoundPrefab(characterId, settings => settings.hitReactionOverrides);
    }

    public static string ResolveDodgeStateName(string characterId)
    {
        return ResolveStateName(characterId, settings => settings.dodgeOverrides);
    }

    public static bool ResolveDodgeCompensateMotion(string characterId)
    {
        return ResolveCompensateMotion(characterId, settings => settings.dodgeOverrides);
    }

    public static AudioClip ResolveDodgeSound(string characterId)
    {
        return ResolveSound(characterId, settings => settings.dodgeOverrides);
    }

    public static GameObject ResolveDodgeSoundPrefab(string characterId)
    {
        return ResolveSoundPrefab(characterId, settings => settings.dodgeOverrides);
    }

    public static bool ShouldCompensateGlobalMotionForState(string stateName, string characterId)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        string hitStateName = ResolveHitReactionStateName(characterId);
        if (string.Equals(stateName, hitStateName, StringComparison.Ordinal))
        {
            return ResolveHitReactionCompensateMotion(characterId);
        }

        string dodgeStateName = ResolveDodgeStateName(characterId);
        if (string.Equals(stateName, dodgeStateName, StringComparison.Ordinal))
        {
            return ResolveDodgeCompensateMotion(characterId);
        }

        return false;
    }

    private static string ResolveStateName(
        string characterId,
        Func<BattleAnimationSettings, BattleAnimationSettings.WeaponScopedActionOverride[]> selector)
    {
        BattleAnimationSettings.WeaponScopedActionOverride entry = FindEnabledOverride(characterId, selector);
        return entry != null ? entry.stateName : string.Empty;
    }

    private static AudioClip ResolveSound(
        string characterId,
        Func<BattleAnimationSettings, BattleAnimationSettings.WeaponScopedActionOverride[]> selector)
    {
        BattleAnimationSettings.WeaponScopedActionOverride entry = FindEnabledOverride(characterId, selector);
        return entry != null ? entry.sound : null;
    }

    private static GameObject ResolveSoundPrefab(
        string characterId,
        Func<BattleAnimationSettings, BattleAnimationSettings.WeaponScopedActionOverride[]> selector)
    {
        BattleAnimationSettings.WeaponScopedActionOverride entry = FindEnabledOverride(characterId, selector);
        return entry != null ? entry.soundPrefab : null;
    }

    private static bool ResolveCompensateMotion(
        string characterId,
        Func<BattleAnimationSettings, BattleAnimationSettings.WeaponScopedActionOverride[]> selector)
    {
        BattleAnimationSettings.WeaponScopedActionOverride entry = FindEnabledOverride(characterId, selector);
        return entry != null && entry.compensateMotion;
    }

    private static BattleAnimationSettings.WeaponScopedActionOverride FindEnabledOverride(
        string characterId,
        Func<BattleAnimationSettings, BattleAnimationSettings.WeaponScopedActionOverride[]> selector)
    {
        BattleAnimationSettings settings = BattleAnimationSettings.LoadDefault();
        if (settings == null || selector == null)
        {
            return null;
        }

        BattleAnimationSettings.WeaponScopedActionOverride[] entries = selector(settings);
        if (entries == null || entries.Length == 0)
        {
            return null;
        }

        ItemDatabase.WeaponCategory weaponCategory = ResolveWeaponCategory(characterId);
        for (int i = 0; i < entries.Length; i++)
        {
            BattleAnimationSettings.WeaponScopedActionOverride entry = entries[i];
            if (entry == null || !entry.enabled || entry.weaponCategory != weaponCategory)
            {
                continue;
            }

            return entry;
        }

        return null;
    }

    private static ItemDatabase.WeaponCategory ResolveWeaponCategory(string characterId)
    {
        if (string.IsNullOrWhiteSpace(characterId))
        {
            return ItemDatabase.WeaponCategory.None;
        }

        return InventoryShortcutRuntimeBinder.GetCharacterEquippedWeaponCategory(characterId);
    }
}
