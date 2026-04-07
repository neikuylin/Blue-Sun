using System;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-40)]
public sealed class CampCharacterEventBinder : MonoBehaviour
{
    private const string SceneName = "\u8425\u5730";
    private const string CanvasPath = "Canvas";
    private const string EventPrefix = "\u8425\u5730\u89d2\u8272\uff1a";

    private EventDatabase eventDatabase;
    private string lastSignature = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneName)
        {
            return;
        }

        if (FindObjectOfType<CampCharacterEventBinder>() != null)
        {
            return;
        }

        Transform canvasTransform = SceneHierarchyPathUtility.Find(activeScene, CanvasPath);
        if (canvasTransform == null)
        {
            Debug.LogWarning("CampCharacterEventBinder: missing Canvas in camp scene.");
            return;
        }

        canvasTransform.gameObject.AddComponent<CampCharacterEventBinder>();
    }

    private void Awake()
    {
        Refresh(force: true);
    }

    private void OnEnable()
    {
        Refresh(force: true);
    }

    private void Update()
    {
        Refresh(force: false);
    }

    private void Refresh(bool force)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (SceneManager.GetActiveScene().name != SceneName)
        {
            return;
        }

        if (eventDatabase == null)
        {
            eventDatabase = EventDatabase.LoadDefault();
        }

        if (eventDatabase == null)
        {
            return;
        }

        string signature = BuildSignature(eventDatabase);
        if (!force && string.Equals(lastSignature, signature, StringComparison.Ordinal))
        {
            return;
        }

        lastSignature = signature;
        ApplyCampCharacterVisibility(eventDatabase);
    }

    private void ApplyCampCharacterVisibility(EventDatabase database)
    {
        if (database == null || database.Entries == null)
        {
            return;
        }

        for (int i = 0; i < database.Entries.Count; i++)
        {
            EventDatabase.EventEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.eventId))
            {
                continue;
            }

            if (!entry.eventId.StartsWith(EventPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            string characterName = entry.eventId.Substring(EventPrefix.Length).Trim();
            if (string.IsNullOrWhiteSpace(characterName))
            {
                continue;
            }

            Transform target = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath + "/" + characterName);
            if (target == null)
            {
                Debug.LogWarning($"CampCharacterEventBinder: missing target 'Canvas/{characterName}' for event '{entry.eventId}'.");
                continue;
            }

            if (target.gameObject.activeSelf != entry.enabled)
            {
                target.gameObject.SetActive(entry.enabled);
            }
        }
    }

    private static string BuildSignature(EventDatabase database)
    {
        if (database == null || database.Entries == null)
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < database.Entries.Count; i++)
        {
            EventDatabase.EventEntry entry = database.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.eventId))
            {
                continue;
            }

            if (!entry.eventId.StartsWith(EventPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            builder.Append(entry.eventId);
            builder.Append('=');
            builder.Append(entry.enabled ? '1' : '0');
            builder.Append(';');
        }

        return builder.ToString();
    }
}
