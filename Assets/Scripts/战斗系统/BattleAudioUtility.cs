using UnityEngine;

public static class BattleAudioUtility
{
    public static void PlayClip(AudioClip clip, Vector3 worldPosition, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource.PlayClipAtPoint(clip, worldPosition, Mathf.Clamp01(volume));
    }

    public static void PlayClipForUnit(AudioClip clip, BattleUnit unit, Camera fallbackCamera = null, float volume = 1f)
    {
        if (clip == null)
        {
            return;
        }

        Vector3 worldPosition = ResolvePlaybackPosition(unit, fallbackCamera);
        PlayClip(clip, worldPosition, volume);
    }

    private static Vector3 ResolvePlaybackPosition(BattleUnit unit, Camera fallbackCamera)
    {
        if (unit != null)
        {
            return unit.transform.position;
        }

        if (fallbackCamera != null)
        {
            return fallbackCamera.transform.position;
        }

        Camera mainCamera = Camera.main;
        return mainCamera != null ? mainCamera.transform.position : Vector3.zero;
    }
}
