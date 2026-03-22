using UnityEngine;

public static class BattleAudioUtility
{
    public sealed class PlaybackHandle
    {
        private GameObject instance;

        internal PlaybackHandle(GameObject runtimeInstance)
        {
            instance = runtimeInstance;
        }

        public bool IsValid => instance != null;

        public void Stop()
        {
            if (instance == null)
            {
                return;
            }

            Object.Destroy(instance);
            instance = null;
        }
    }

    public static void PlayOnce(AudioClip clip, GameObject soundPrefab, BattleUnit unit, Camera fallbackCamera = null, float volume = 1f)
    {
        if (soundPrefab != null)
        {
            GameObject instance = CreateSoundInstance(soundPrefab, unit, fallbackCamera);
            if (instance == null)
            {
                return;
            }

            AudioSource[] sources = instance.GetComponentsInChildren<AudioSource>(true);
            float longestDuration = 0f;
            bool hasLoop = false;
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                source.volume *= Mathf.Clamp01(volume);
                if (!source.isPlaying)
                {
                    source.Play();
                }

                if (source.loop)
                {
                    hasLoop = true;
                }
                else if (source.clip != null)
                {
                    longestDuration = Mathf.Max(longestDuration, source.clip.length);
                }
            }

            if (!hasLoop)
            {
                float destroyDelay = longestDuration > 0.01f ? longestDuration : 0.1f;
                Object.Destroy(instance, destroyDelay);
            }

            return;
        }

        if (clip == null)
        {
            return;
        }

        Vector3 worldPosition = ResolvePlaybackPosition(unit, fallbackCamera);
        AudioSource.PlayClipAtPoint(clip, worldPosition, Mathf.Clamp01(volume));
    }

    public static PlaybackHandle StartTracked(AudioClip clip, GameObject soundPrefab, BattleUnit unit, Camera fallbackCamera = null, float volume = 1f)
    {
        if (soundPrefab != null)
        {
            GameObject instance = CreateSoundInstance(soundPrefab, unit, fallbackCamera);
            if (instance == null)
            {
                return null;
            }

            AudioSource[] sources = instance.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                source.volume *= Mathf.Clamp01(volume);
                if (!source.isPlaying)
                {
                    source.Play();
                }
            }

            return new PlaybackHandle(instance);
        }

        PlayOnce(clip, null, unit, fallbackCamera, volume);
        return null;
    }

    private static GameObject CreateSoundInstance(GameObject soundPrefab, BattleUnit unit, Camera fallbackCamera)
    {
        if (soundPrefab == null)
        {
            return null;
        }

        if (unit != null)
        {
            GameObject instance = Object.Instantiate(soundPrefab, unit.transform, false);
            PrepareRuntimeSoundInstance(instance);
            return instance;
        }

        Vector3 worldPosition = ResolvePlaybackPosition(null, fallbackCamera);
        GameObject detachedInstance = Object.Instantiate(soundPrefab, worldPosition, Quaternion.identity);
        PrepareRuntimeSoundInstance(detachedInstance);
        return detachedInstance;
    }

    private static void PrepareRuntimeSoundInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.name = "__BattleRuntimeAudio";

#if UNITY_EDITOR
        instance.hideFlags = HideFlags.HideAndDontSave;
        Transform[] transforms = instance.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
            {
                transforms[i].hideFlags = HideFlags.HideAndDontSave;
            }
        }

        Component[] components = instance.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
            {
                components[i].hideFlags = HideFlags.HideAndDontSave;
            }
        }
#endif
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
