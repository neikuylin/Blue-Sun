using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[FilePath("UserSettings/ButtonSoundEditorState.asset", FilePathAttribute.Location.ProjectFolder)]
internal sealed class ButtonSoundEditorState : ScriptableSingleton<ButtonSoundEditorState>
{
    [Serializable]
    internal sealed class SoundEntry
    {
        public GameObject targetObject;
        public AudioClip clickClip;
        public float volume = 1f;
    }

    [SerializeField] internal List<SoundEntry> entries = new List<SoundEntry>();

    internal void EnsureInitialized()
    {
        if (entries == null)
        {
            entries = new List<SoundEntry>();
        }

        if (entries.Count == 0)
        {
            entries.Add(new SoundEntry());
        }
    }

    internal void SaveState()
    {
        Save(true);
    }
}

public sealed class ButtonSoundEditorWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Tools/音效/按钮")]
    private static void Open()
    {
        ButtonSoundEditorWindow window = GetWindow<ButtonSoundEditorWindow>("按钮音效");
        window.minSize = new Vector2(420f, 320f);
        window.Show();
        window.Focus();
    }

    private void OnSelectionChange()
    {
        Repaint();
    }

    private void OnEnable()
    {
        ButtonSoundEditorState.instance.EnsureInitialized();
    }

    private void OnDisable()
    {
        ButtonSoundEditorState.instance.SaveState();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("按钮/开关音效编辑器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "每条配置只需要一个 GameObject、一个点击音效和一个音量。目标对象上有 Button 或 Toggle 都可以统一使用这条点击音效。",
            MessageType.Info);

        EditorGUILayout.Space(6f);
        if (GUILayout.Button("新增按钮音效"))
        {
            ButtonSoundEditorState.instance.entries.Add(new ButtonSoundEditorState.SoundEntry
            {
                targetObject = Selection.activeGameObject
            });
            ButtonSoundEditorState.instance.SaveState();
        }

        using (new EditorGUI.DisabledScope(ButtonSoundEditorState.instance.entries.Count == 0))
        {
            if (GUILayout.Button("应用全部"))
            {
                ApplyAll();
            }
        }

        if (GUILayout.Button("从当前场景读取已有音效"))
        {
            ImportFromOpenScenes();
        }

        EditorGUILayout.Space(6f);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        for (int i = 0; i < ButtonSoundEditorState.instance.entries.Count; i++)
        {
            DrawEntry(ButtonSoundEditorState.instance.entries[i], i);
        }
        EditorGUILayout.EndScrollView();

        if (GUI.changed)
        {
            ButtonSoundEditorState.instance.SaveState();
        }
    }

    private void DrawEntry(ButtonSoundEditorState.SoundEntry entry, int index)
    {
        if (entry == null)
        {
            return;
        }

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField($"按钮音效 {index + 1}", EditorStyles.boldLabel);
            entry.targetObject = (GameObject)EditorGUILayout.ObjectField("GameObject", entry.targetObject, typeof(GameObject), true);
            entry.clickClip = (AudioClip)EditorGUILayout.ObjectField("点击音效", entry.clickClip, typeof(AudioClip), false);
            entry.volume = EditorGUILayout.Slider("音量", entry.volume, 0f, 1f);

            Component targetComponent = ResolveTargetComponent(entry.targetObject);
            if (entry.targetObject != null && targetComponent == null)
            {
                EditorGUILayout.HelpBox("这个对象上没有 Button 或 Toggle 组件。", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(targetComponent == null))
                {
                    if (GUILayout.Button("应用"))
                    {
                        ApplyToTarget(entry);
                    }
                }

                if (GUILayout.Button("读取"))
                {
                    LoadFromExisting(entry);
                }

                if (GUILayout.Button("删除"))
                {
                    RemoveFromTarget(entry);
                    ButtonSoundEditorState.instance.entries.RemoveAt(index);
                    if (ButtonSoundEditorState.instance.entries.Count == 0)
                    {
                        ButtonSoundEditorState.instance.entries.Add(new ButtonSoundEditorState.SoundEntry());
                    }

                    ButtonSoundEditorState.instance.SaveState();
                    GUIUtility.ExitGUI();
                }
            }
        }
    }

    private static Component ResolveTargetComponent(GameObject targetObject)
    {
        if (targetObject == null)
        {
            return null;
        }

        Button button = targetObject.GetComponent<Button>();
        if (button != null)
        {
            return button;
        }

        return targetObject.GetComponent<Toggle>();
    }

    private static void LoadFromExisting(ButtonSoundEditorState.SoundEntry entry)
    {
        if (entry == null || entry.targetObject == null)
        {
            return;
        }

        UISoundTrigger trigger = entry.targetObject.GetComponent<UISoundTrigger>();
        if (trigger == null)
        {
            return;
        }

        SerializedObject serializedObject = new SerializedObject(trigger);
        entry.clickClip = serializedObject.FindProperty("clickClip").objectReferenceValue as AudioClip;
        entry.volume = serializedObject.FindProperty("volume").floatValue;
    }

    private static void ApplyToTarget(ButtonSoundEditorState.SoundEntry entry)
    {
        GameObject targetObject = entry != null ? entry.targetObject : null;
        if (targetObject == null)
        {
            return;
        }

        Button button = targetObject.GetComponent<Button>();
        Toggle toggle = targetObject.GetComponent<Toggle>();
        if (button == null && toggle == null)
        {
            return;
        }

        UISoundTrigger trigger = targetObject.GetComponent<UISoundTrigger>();
        if (trigger == null)
        {
            trigger = Undo.AddComponent<UISoundTrigger>(targetObject);
        }

        Undo.RecordObject(trigger, "Apply UI Sound Trigger");
        SerializedObject serializedObject = new SerializedObject(trigger);
        serializedObject.Update();
        serializedObject.FindProperty("button").objectReferenceValue = button;
        serializedObject.FindProperty("toggle").objectReferenceValue = toggle;
        serializedObject.FindProperty("audioSource").objectReferenceValue = null;
        serializedObject.FindProperty("clickClip").objectReferenceValue = entry.clickClip;
        serializedObject.FindProperty("toggleOnClip").objectReferenceValue = null;
        serializedObject.FindProperty("toggleOffClip").objectReferenceValue = null;
        serializedObject.FindProperty("volume").floatValue = entry.volume;
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(trigger);
    }

    private static void RemoveFromTarget(ButtonSoundEditorState.SoundEntry entry)
    {
        GameObject targetObject = entry != null ? entry.targetObject : null;
        if (targetObject == null)
        {
            return;
        }

        UISoundTrigger trigger = targetObject.GetComponent<UISoundTrigger>();
        if (trigger == null)
        {
            return;
        }

        Undo.DestroyObjectImmediate(trigger);
        EditorUtility.SetDirty(targetObject);
    }

    private void ApplyAll()
    {
        for (int i = 0; i < ButtonSoundEditorState.instance.entries.Count; i++)
        {
            ApplyToTarget(ButtonSoundEditorState.instance.entries[i]);
        }

        ButtonSoundEditorState.instance.SaveState();
    }

    private void ImportFromOpenScenes()
    {
        UISoundTrigger[] triggers = Resources.FindObjectsOfTypeAll<UISoundTrigger>();
        Dictionary<GameObject, ButtonSoundEditorState.SoundEntry> importedEntries =
            new Dictionary<GameObject, ButtonSoundEditorState.SoundEntry>();

        for (int i = 0; i < triggers.Length; i++)
        {
            UISoundTrigger trigger = triggers[i];
            if (trigger == null)
            {
                continue;
            }

            GameObject targetObject = trigger.gameObject;
            if (targetObject == null)
            {
                continue;
            }

            if (!targetObject.scene.IsValid() || !targetObject.scene.isLoaded)
            {
                continue;
            }

            if (EditorUtility.IsPersistent(targetObject))
            {
                continue;
            }

            SerializedObject serializedObject = new SerializedObject(trigger);
            AudioClip clickClip = serializedObject.FindProperty("clickClip").objectReferenceValue as AudioClip;
            float volume = serializedObject.FindProperty("volume").floatValue;

            importedEntries[targetObject] = new ButtonSoundEditorState.SoundEntry
            {
                targetObject = targetObject,
                clickClip = clickClip,
                volume = volume
            };
        }

        ButtonSoundEditorState.instance.entries.Clear();
        foreach (ButtonSoundEditorState.SoundEntry importedEntry in importedEntries.Values)
        {
            ButtonSoundEditorState.instance.entries.Add(importedEntry);
        }

        if (ButtonSoundEditorState.instance.entries.Count == 0)
        {
            ButtonSoundEditorState.instance.entries.Add(new ButtonSoundEditorState.SoundEntry());
        }

        ButtonSoundEditorState.instance.SaveState();
        Repaint();
    }
}
