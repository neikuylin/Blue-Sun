using UnityEngine;

public static class ItemSoundUtility
{
    private static AudioSource fallbackAudioSource;

    public static void PlayForItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        ItemDatabase database = ItemDatabase.LoadDefault();
        ItemDatabase.ItemEntry itemEntry = database != null ? database.FindEntry(itemId) : null;
        if (itemEntry == null)
        {
            return;
        }

        PlayForCategory(itemEntry.category);
    }

    public static void PlayForCategory(ItemDatabase.ItemCategory category)
    {
        ItemSoundDatabase database = ItemSoundDatabase.LoadDefault();
        ItemSoundDatabase.CategorySoundEntry soundEntry = database != null ? database.FindEntry(category) : null;
        if (soundEntry == null || soundEntry.clip == null)
        {
            return;
        }

        AudioSource audioSource = ResolveAudioSource();
        if (audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(soundEntry.clip, Mathf.Clamp01(soundEntry.volume));
    }

    public static void PlaySkillMove()
    {
        ItemSoundDatabase database = ItemSoundDatabase.LoadDefault();
        if (database == null || database.SkillMoveClip == null)
        {
            return;
        }

        AudioSource audioSource = ResolveAudioSource();
        if (audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(database.SkillMoveClip, Mathf.Clamp01(database.SkillMoveVolume));
    }

    private static AudioSource ResolveAudioSource()
    {
        if (fallbackAudioSource != null)
        {
            return fallbackAudioSource;
        }

        GameObject runtimeObject = new GameObject("__ItemRuntimeAudio");
        Object.DontDestroyOnLoad(runtimeObject);
        fallbackAudioSource = runtimeObject.AddComponent<AudioSource>();
        fallbackAudioSource.playOnAwake = false;
        fallbackAudioSource.spatialBlend = 0f;
        fallbackAudioSource.loop = false;
        AudioRouting.ApplyUi(fallbackAudioSource);
        return fallbackAudioSource;
    }
}
