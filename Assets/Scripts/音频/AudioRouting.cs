using UnityEngine;
using UnityEngine.Audio;

public static class AudioRouting
{
    public static void ApplyVoice(AudioSource source)
    {
        Apply(source, settings => settings.voice, settings => settings.voiceScale);
    }

    public static void ApplySkill(AudioSource source)
    {
        Apply(source, settings => settings.skill, settings => settings.skillScale);
    }

    public static void ApplyBackground(AudioSource source)
    {
        Apply(source, settings => settings.background, settings => settings.backgroundScale);
    }

    public static void ApplyUi(AudioSource source)
    {
        Apply(source, settings => settings.ui, settings => settings.uiScale);
    }

    public static void ApplyBgm(AudioSource source)
    {
        Apply(source, settings => settings.bgm, settings => settings.bgmScale);
    }

    public static float GetBgmScale()
    {
        AudioRoutingSettings settings = AudioRoutingSettings.LoadDefault();
        return settings != null ? Mathf.Clamp01(settings.bgmScale) : 1f;
    }

    private static void Apply(
        AudioSource source,
        System.Func<AudioRoutingSettings, AudioMixerGroup> groupSelector,
        System.Func<AudioRoutingSettings, float> scaleSelector)
    {
        if (source == null)
        {
            return;
        }

        AudioRoutingSettings settings = AudioRoutingSettings.LoadDefault();
        if (settings == null)
        {
            return;
        }

        AudioMixerGroup targetGroup = groupSelector(settings);
        if (source.outputAudioMixerGroup == targetGroup)
        {
            return;
        }

        source.outputAudioMixerGroup = targetGroup;
        source.volume *= Mathf.Clamp01(scaleSelector(settings));
    }
}
