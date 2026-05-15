using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BattleMusicRuntime : MonoBehaviour
{
    private const string RuntimeObjectName = "BattleMusicRuntime";
    private const string BattleSceneName = "战斗副本";

    private static BattleMusicRuntime instance;

    private AudioSource currentSource;
    private AudioSource standbySource;
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
        if (currentSource != null && standbySource != null)
        {
            return;
        }

        AudioSource[] sources = GetComponents<AudioSource>();
        currentSource = sources.Length > 0 ? sources[0] : gameObject.AddComponent<AudioSource>();
        standbySource = sources.Length > 1 ? sources[1] : gameObject.AddComponent<AudioSource>();

        ConfigureSource(currentSource);
        ConfigureSource(standbySource);
    }

    private static void ConfigureSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.loop = true;
    }

    private void Play(AudioClip nextClip, float targetVolume, float fadeInSeconds, float fadeOutSeconds)
    {
        EnsureSource();

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (currentSource.clip == nextClip && currentSource.isPlaying)
        {
            StopAndClear(standbySource);
            if (Mathf.Approximately(currentSource.volume, targetVolume))
            {
                return;
            }

            fadeRoutine = StartCoroutine(FadeVolume(currentSource, currentSource.volume, targetVolume, fadeInSeconds));
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

        if ((currentSource.clip == null || !currentSource.isPlaying) &&
            (standbySource.clip == null || !standbySource.isPlaying))
        {
            return;
        }

        fadeRoutine = StartCoroutine(FadeOutAndClear(fadeOutSeconds));
    }

    private IEnumerator SwitchMusic(AudioClip nextClip, float targetVolume, float fadeInSeconds, float fadeOutSeconds)
    {
        AudioSource fadingOutSource = currentSource;
        AudioSource fadingInSource = standbySource;

        fadingInSource.Stop();
        fadingInSource.clip = nextClip;
        fadingInSource.volume = 0f;
        fadingInSource.loop = true;
        fadingInSource.Play();

        currentSource = fadingInSource;
        standbySource = fadingOutSource;

        float fadeOutStartVolume = fadingOutSource != null ? fadingOutSource.volume : 0f;
        float fadeInElapsed = 0f;
        float fadeOutElapsed = 0f;
        bool fadeInFinished = fadeInSeconds <= 0f;
        bool fadeOutFinished = fadeOutSeconds <= 0f || fadingOutSource == null || fadingOutSource.clip == null || !fadingOutSource.isPlaying;

        if (fadeInFinished)
        {
            fadingInSource.volume = targetVolume;
        }

        if (fadeOutFinished && fadingOutSource != null)
        {
            fadingOutSource.volume = 0f;
        }

        while (!fadeInFinished || !fadeOutFinished)
        {
            float deltaTime = Time.unscaledDeltaTime;

            if (!fadeInFinished)
            {
                fadeInElapsed += deltaTime;
                float t = Mathf.Clamp01(fadeInElapsed / fadeInSeconds);
                fadingInSource.volume = Mathf.Lerp(0f, targetVolume, t);
                fadeInFinished = t >= 1f;
            }

            if (!fadeOutFinished)
            {
                fadeOutElapsed += deltaTime;
                float t = Mathf.Clamp01(fadeOutElapsed / fadeOutSeconds);
                fadingOutSource.volume = Mathf.Lerp(fadeOutStartVolume, 0f, t);
                fadeOutFinished = t >= 1f;
            }

            yield return null;
        }

        fadingInSource.volume = targetVolume;
        if (fadingOutSource != null)
        {
            fadingOutSource.Stop();
            fadingOutSource.clip = null;
            fadingOutSource.volume = 0f;
        }

        fadeRoutine = null;
    }

    private IEnumerator FadeVolume(AudioSource source, float startVolume, float targetVolume, float duration)
    {
        yield return FadeSourceVolume(source, startVolume, targetVolume, duration);
        fadeRoutine = null;
    }

    private IEnumerator FadeOutAndClear(float duration)
    {
        AudioSource firstSource = currentSource;
        AudioSource secondSource = standbySource;
        float firstStartVolume = firstSource != null ? firstSource.volume : 0f;
        float secondStartVolume = secondSource != null ? secondSource.volume : 0f;
        float elapsed = 0f;

        if (duration <= 0f)
        {
            StopAndClear(firstSource);
            StopAndClear(secondSource);
            fadeRoutine = null;
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (firstSource != null)
            {
                firstSource.volume = Mathf.Lerp(firstStartVolume, 0f, t);
            }

            if (secondSource != null)
            {
                secondSource.volume = Mathf.Lerp(secondStartVolume, 0f, t);
            }

            yield return null;
        }

        StopAndClear(firstSource);
        StopAndClear(secondSource);
        fadeRoutine = null;
    }

    private IEnumerator FadeSourceVolume(AudioSource source, float startVolume, float targetVolume, float duration)
    {
        if (duration <= 0f)
        {
            if (source != null)
            {
                source.volume = targetVolume;
            }

            yield break;
        }

        float elapsed = 0f;
        while (source != null && elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);
            yield return null;
        }

        if (source != null)
        {
            source.volume = targetVolume;
        }
    }

    private static void StopAndClear(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
        source.volume = 0f;
    }
}
