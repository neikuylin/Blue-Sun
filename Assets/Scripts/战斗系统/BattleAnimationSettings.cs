using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BattleAnimationSettings", menuName = "Battle/Animation Settings")]
public sealed class BattleAnimationSettings : ScriptableObject
{
    public const string DefaultResourcePath = "BattleAnimationSettings";

    public string idleStateName = string.Empty;
    public AudioClip idleSound;
    public string enterBattleStateName = string.Empty;
    public AudioClip enterBattleSound;
    [FormerlySerializedAs("aimStateName")]
    [FormerlySerializedAs("combatArtAimStateName")]
    public string combatArtLeftAimStateName = string.Empty;
    public AudioClip combatArtLeftAimSound;
    public string combatArtRightAimStateName = string.Empty;
    public AudioClip combatArtRightAimSound;
    public string hitReactionStateName = string.Empty;
    public AudioClip hitReactionSound;
    public string dodgeStateName = string.Empty;
    public AudioClip dodgeSound;
    public float idleYawOffset;

    public static BattleAnimationSettings LoadDefault()
    {
        return Resources.Load<BattleAnimationSettings>(DefaultResourcePath);
    }
}
