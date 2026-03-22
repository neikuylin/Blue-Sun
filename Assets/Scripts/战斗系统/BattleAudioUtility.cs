using UnityEngine;

public static class BattleAudioUtility
{
    private static readonly System.Collections.Generic.List<RuntimeAudioController> activeControllers = new System.Collections.Generic.List<RuntimeAudioController>();
    private static float globalPitchScale = 1f;

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

    internal sealed class RuntimeAudioController : MonoBehaviour
    {
        private AudioSource[] sources;
        private float[] baseVolumes;
        private float[] basePitches;

        public void Initialize(AudioSource[] runtimeSources)
        {
            sources = runtimeSources ?? System.Array.Empty<AudioSource>();
            baseVolumes = new float[sources.Length];
            basePitches = new float[sources.Length];
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                baseVolumes[i] = source.volume;
                basePitches[i] = source.pitch;
            }

            ApplyPitchScale(globalPitchScale);
        }

        public void ApplyVolumeScale(float volumeScale)
        {
            float clampedVolume = Mathf.Clamp01(volumeScale);
            if (sources == null)
            {
                return;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                source.volume = baseVolumes[i] * clampedVolume;
            }
        }

        public void ApplyPitchScale(float pitchScale)
        {
            float clampedPitch = Mathf.Max(0.01f, pitchScale);
            if (sources == null)
            {
                return;
            }

            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                if (source == null)
                {
                    continue;
                }

                source.pitch = basePitches[i] * clampedPitch;
            }
        }

        private void OnEnable()
        {
            if (!activeControllers.Contains(this))
            {
                activeControllers.Add(this);
            }

            ApplyPitchScale(globalPitchScale);
        }

        private void OnDisable()
        {
            activeControllers.Remove(this);
        }

        private void OnDestroy()
        {
            activeControllers.Remove(this);
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

        CreateClipSoundInstance(clip, unit, fallbackCamera, volume);
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

    private static GameObject CreateClipSoundInstance(AudioClip clip, BattleUnit unit, Camera fallbackCamera, float volume)
    {
        if (clip == null)
        {
            return null;
        }

        Vector3 worldPosition = ResolvePlaybackPosition(unit, fallbackCamera);
        GameObject instance = new GameObject("__BattleRuntimeAudio");
        instance.transform.position = worldPosition;
        if (unit != null)
        {
            instance.transform.SetParent(unit.transform, true);
        }

        AudioSource source = instance.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.spatialBlend = 1f;
        source.playOnAwake = false;

        PrepareRuntimeSoundInstance(instance);
        source.Play();
        Object.Destroy(instance, Mathf.Max(clip.length, 0.1f));
        return instance;
    }

    private static void PrepareRuntimeSoundInstance(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        instance.name = "__BattleRuntimeAudio";
        RuntimeAudioController controller = instance.GetComponent<RuntimeAudioController>();
        if (controller == null)
        {
            controller = instance.AddComponent<RuntimeAudioController>();
        }

        controller.Initialize(instance.GetComponentsInChildren<AudioSource>(true));

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

    public static void SetGlobalPitchScale(float pitchScale)
    {
        globalPitchScale = Mathf.Max(0.01f, pitchScale);
        for (int i = activeControllers.Count - 1; i >= 0; i--)
        {
            RuntimeAudioController controller = activeControllers[i];
            if (controller == null)
            {
                activeControllers.RemoveAt(i);
                continue;
            }

            controller.ApplyPitchScale(globalPitchScale);
        }
    }

    public static void ResetGlobalPitchScale()
    {
        SetGlobalPitchScale(1f);
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
