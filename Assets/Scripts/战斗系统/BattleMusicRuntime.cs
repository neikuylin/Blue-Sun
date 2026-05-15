using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleMusicRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "BattleMusicRuntime";
    private const string BattleSceneName = "战斗副本";

    private static BattleMusicRuntime instance;

    private AudioSource musicSource;
    private Coroutine fadeRoutine;

    public static void RefreshForMode(bool isExplorationMode)
    {
        BattleMusicSettings settings = BattleMusicSettings.LoadDefault();
        if (settings == null)
        {
            StopUsingSettings();
            return;
        }

        AudioClip nextClip = isExplorationMode ? settings.explorationMusic : settings.combatMusic;
        if (nextClip == null)
        {
            Stop(Mathf.Max(0f, settings.fadeOutSeconds));
            return;
        }

        EnsureInstance().Play(
            nextClip,
            Mathf.Clamp01(settings.volume),
            Mathf.Max(0f, settings.fadeInSeconds),
            Mathf.Max(0f, settings.fadeOutSeconds));
    }

    public static void StopUsingSettings()
    {
        BattleMusicSettings settings = BattleMusicSettings.LoadDefault();
        Stop(settings != null ? Mathf.Max(0f, settings.fadeOutSeconds) : 0f);
    }

    public static void Stop(float fadeOutSeconds)
    {
        if (instance == null)
        {
            return;
        }

        instance.FadeOutAndStop(Mathf.Max(0f, fadeOutSeconds));
    }

    private static BattleMusicRuntime EnsureInstance()
    {
        if (instance != null)
        {
            return instance;
        }

        GameObject runtimeObject = new GameObject(RuntimeObjectName);
        DontDestroyOnLoad(runtimeObject);
        instance = runtimeObject.AddComponent<BattleMusicRuntime>();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureSource();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.Equals(scene.name, BattleSceneName, System.StringComparison.Ordinal))
        {
            return;
        }

        StopUsingSettings();
    }

    private void EnsureSource()
    {
        if (musicSource != null)
        {
            return;
        }

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
        }

        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        musicSource.loop = true;
    }

    private void Play(AudioClip nextClip, float targetVolume, float fadeInSeconds, float fadeOutSeconds)
    {
        EnsureSource();

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (musicSource.clip == nextClip && musicSource.isPlaying)
        {
            if (Mathf.Approximately(musicSource.volume, targetVolume))
            {
                return;
            }

            fadeRoutine = StartCoroutine(FadeVolume(musicSource.volume, targetVolume, fadeInSeconds));
            return;
        }

        fadeRoutine = StartCoroutine(SwitchMusic(nextClip, targetVolume, fadeInSeconds, fadeOutSeconds));
    }

    private void FadeOutAndStop(float fadeOutSeconds)
    {
        EnsureSource();

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (musicSource.clip == null && !musicSource.isPlaying)
        {
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAndClear(fadeOutSeconds));
    }

    private IEnumerator SwitchMusic(AudioClip nextClip, float targetVolume, float fadeInSeconds, float fadeOutSeconds)
    {
        if (musicSource.clip != null && musicSource.isPlaying)
        {
            yield return FadeSourceVolume(musicSource.volume, 0f, fadeOutSeconds);
            musicSource.Stop();
            musicSource.clip = null;
        }

        musicSource.clip = nextClip;
        musicSource.volume = 0f;
        musicSource.loop = true;
        musicSource.Play();

        yield return FadeSourceVolume(0f, targetVolume, fadeInSeconds);
        fadeRoutine = null;
    }

    private IEnumerator FadeVolume(float startVolume, float targetVolume, float duration)
    {
        yield return FadeSourceVolume(startVolume, targetVolume, duration);
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndClear(float duration)
    {
        yield return FadeSourceVolume(musicSource.volume, 0f, duration);
        if (musicSource != null)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }

        fadeRoutine = null;
    }

    private IEnumerator FadeSourceVolume(float startVolume, float targetVolume, float duration)
    {
        if (duration <= 0f)
        {
            if (musicSource != null)
            {
                musicSource.volume = targetVolume;
            }

            yield break;
        }

        float elapsed = 0f;
        while (musicSource != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (musicSource != null)
        {
            musicSource.volume = targetVolume;
        }
    }
}
