using UnityEngine;

[CreateAssetMenu(fileName = "BattleAnimationSettings", menuName = "Battle/Animation Settings")]
public sealed class BattleAnimationSettings : ScriptableObject
{
    public const string DefaultResourcePath = "BattleAnimationSettings";

    [System.Serializable]
    public sealed class WeaponScopedActionOverride
    {
        public bool enabled = true;
        public ItemDatabase.WeaponCategory weaponCategory = ItemDatabase.WeaponCategory.None;
        public string stateName = string.Empty;
        public AudioClip sound;
        public GameObject soundPrefab;
        public bool compensateMotion;
        public float yawCorrection;
    }

    public string idleStateName = string.Empty;
    public AudioClip idleSound;
    public GameObject idleSoundPrefab;
    public WeaponScopedActionOverride[] idleOverrides = new WeaponScopedActionOverride[0];
    public string enterBattleStateName = string.Empty;
    public AudioClip enterBattleSound;
    public GameObject enterBattleSoundPrefab;
    public bool enterBattleCompensateMotion;
    public WeaponScopedActionOverride[] enterBattleOverrides = new WeaponScopedActionOverride[0];
    public string exitBattleStateName = string.Empty;
    public AudioClip exitBattleSound;
    public GameObject exitBattleSoundPrefab;
    public bool exitBattleCompensateMotion;
    public WeaponScopedActionOverride[] exitBattleOverrides = new WeaponScopedActionOverride[0];
    public string hitReactionStateName = string.Empty;
    public AudioClip hitReactionSound;
    public GameObject hitReactionSoundPrefab;
    public bool hitReactionCompensateMotion;
    public WeaponScopedActionOverride[] hitReactionOverrides = new WeaponScopedActionOverride[0];
    public string dodgeStateName = string.Empty;
    public AudioClip dodgeSound;
    public GameObject dodgeSoundPrefab;
    public bool dodgeCompensateMotion;
    public WeaponScopedActionOverride[] dodgeOverrides = new WeaponScopedActionOverride[0];
    public string explorationIdleStateName = string.Empty;
    public AudioClip explorationIdleSound;
    public GameObject explorationIdleSoundPrefab;
    public bool explorationIdleCompensateMotion;
    public float explorationIdleYawCorrection;
    public string explorationMoveStateName = string.Empty;
    public AudioClip explorationMoveSound;
    public GameObject explorationMoveSoundPrefab;
    public bool explorationMoveCompensateMotion;
    public float explorationMoveYawCorrection;
    public float idleYawOffset;
    [Min(0.01f)] public float modelTurn90Duration = 0.3f;
    public bool enterBattleModelTurnEnabled = true;

    public static BattleAnimationSettings LoadDefault()
    {
        return Resources.Load<BattleAnimationSettings>(DefaultResourcePath);
    }
}
