using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BattleAnimationSettings", menuName = "Battle/Animation Settings")]
public sealed class BattleAnimationSettings : ScriptableObject
{
    public const string DefaultResourcePath = "BattleAnimationSettings";

    public string idleStateName = string.Empty;
    public AudioClip idleSound;
    public GameObject idleSoundPrefab;
    public string enterBattleStateName = string.Empty;
    public AudioClip enterBattleSound;
    public GameObject enterBattleSoundPrefab;
    [FormerlySerializedAs("aimStateName")]
    [FormerlySerializedAs("combatArtAimStateName")]
    public string combatArtLeftAimStateName = string.Empty;
    public AudioClip combatArtLeftAimSound;
    public GameObject combatArtLeftAimSoundPrefab;
    public string combatArtRightAimStateName = string.Empty;
    public AudioClip combatArtRightAimSound;
    public GameObject combatArtRightAimSoundPrefab;
    public string hitReactionStateName = string.Empty;
    public AudioClip hitReactionSound;
    public GameObject hitReactionSoundPrefab;
    public string dodgeStateName = string.Empty;
    public AudioClip dodgeSound;
    public GameObject dodgeSoundPrefab;
    public float idleYawOffset;

    public static BattleAnimationSettings LoadDefault()
    {
        return Resources.Load<BattleAnimationSettings>(DefaultResourcePath);
    }
}
