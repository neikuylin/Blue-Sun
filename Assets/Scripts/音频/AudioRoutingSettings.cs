using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioRoutingSettings", menuName = "音频/音频路由配置")]
public sealed class AudioRoutingSettings : ScriptableObject
{
    public const string DefaultResourcePath = "AudioRoutingSettings";

    public AudioMixer mixer;
    public AudioMixerGroup voice;
    public AudioMixerGroup skill;
    public AudioMixerGroup background;
    public AudioMixerGroup ui;
    public AudioMixerGroup bgm;

    [Range(0f, 1f)] public float voiceScale = 1f;
    [Range(0f, 1f)] public float skillScale = 0.5011872f;
    [Range(0f, 1f)] public float backgroundScale = 0.2511886f;
    [Range(0f, 1f)] public float uiScale = 0.7079458f;
    [Range(0f, 1f)] public float bgmScale = 0.3162278f;

    public static AudioRoutingSettings LoadDefault()
    {
        return Resources.Load<AudioRoutingSettings>(DefaultResourcePath);
    }
}
