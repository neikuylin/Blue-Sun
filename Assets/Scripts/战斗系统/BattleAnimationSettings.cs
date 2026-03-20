using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "BattleAnimationSettings", menuName = "Battle/Animation Settings")]
public sealed class BattleAnimationSettings : ScriptableObject
{
    public const string DefaultResourcePath = "BattleAnimationSettings";

    public string idleStateName = string.Empty;
    public string enterBattleStateName = string.Empty;
    [FormerlySerializedAs("aimStateName")]
    public string combatArtAimStateName = string.Empty;
    public string hitReactionStateName = string.Empty;
    public string dodgeStateName = string.Empty;
    public float idleYawOffset;

    public static BattleAnimationSettings LoadDefault()
    {
        return Resources.Load<BattleAnimationSettings>(DefaultResourcePath);
    }
}
