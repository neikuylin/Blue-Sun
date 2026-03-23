using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class UISoundTrigger : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private Selectable selectable;
    private static AudioSource fallbackAudioSource;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        PlayClick();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        PlayClick();
    }

    private void PlayClick()
    {
        if (selectable != null && !selectable.IsInteractable())
        {
            return;
        }

        PlayClip(clickClip);
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
