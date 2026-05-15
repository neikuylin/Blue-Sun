using UnityEngine;

[CreateAssetMenu(fileName = "BattleMusicSettings", menuName = "Battle/Music Settings")]
public sealed class BattleMusicSettings : ScriptableObject
{
    public const string DefaultResourcePath = "BattleMusicSettings";

    public AudioClip combatMusic;
    public AudioClip explorationMusic;
    [Range(0f, 1f)] public float volume = 1f;
    [Min(0f)] public float fadeInSeconds = 1.5f;
    [Min(0f)] public float fadeOutSeconds = 1f;

    public static BattleMusicSettings LoadDefault()
    {
        return Resources.Load<BattleMusicSettings>(DefaultResourcePath);
    }
}
