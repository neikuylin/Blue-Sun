using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class UISoundTrigger : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Toggle toggle;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip toggleOnClip;
    [SerializeField] private AudioClip toggleOffClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private static AudioSource fallbackAudioSource;

    private void Reset()
    {
        button = GetComponent<Button>();
        toggle = GetComponent<Toggle>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (button != null)
        {
            button.onClick.AddListener(HandleButtonClick);
        }

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }
    }

    private void OnDisable()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleButtonClick);
        }

        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }

    private void HandleButtonClick()
    {
        PlayClip(clickClip);
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        AudioClip clip = isOn ? toggleOnClip : toggleOffClip;
        if (clip == null)
        {
            clip = clickClip;
        }

        PlayClip(clip);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        AudioSource source = ResolveAudioSource();
        if (source == null)
        {
            return;
        }

        source.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private AudioSource ResolveAudioSource()
    {
        if (audioSource != null)
        {
            return audioSource;
        }

        if (fallbackAudioSource == null)
        {
            GameObject runtimeObject = new GameObject("__UIRuntimeAudio");
            DontDestroyOnLoad(runtimeObject);
            fallbackAudioSource = runtimeObject.AddComponent<AudioSource>();
            fallbackAudioSource.playOnAwake = false;
            fallbackAudioSource.spatialBlend = 0f;
            fallbackAudioSource.loop = false;
        }

        return fallbackAudioSource;
    }
}
