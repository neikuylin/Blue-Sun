using UnityEditor;
using UnityEngine;

public sealed class DialogueContentEditorWindow : EditorWindow
{
    private const string ResourceFolder = "Assets/Resources";
    private const string AssetPath = ResourceFolder + "/DialogueContentDatabase.asset";

    private Vector2 dialogueScroll;
    private Vector2 contentScroll;
    private string newDialogueId = "dlg_001";
    private string selectedDialogueId = string.Empty;

    [MenuItem("Tools/事件/对话内容编辑器")]
    private static void Open()
    {
        DialogueContentEditorWindow window = GetWindow<DialogueContentEditorWindow>("对话内容编辑器");
        window.minSize = new Vector2(1100f, 680f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        DialogueContentDatabase database = EnsureDatabase();
        if (database == null)
        {
            EditorGUILayout.HelpBox("对话内容数据库创建失败。", MessageType.Error);
            return;
        }

        EnsureValidSelection(database);

        using (new EditorGUILayout.HorizontalScope())
        {
            DrawDialogueList(database);
            DrawDialogueInspector(database);
        }
    }

    private void DrawDialogueList(DialogueContentDatabase database)
    {
        using (new EditorGUILayout.VerticalScope("box", GUILayout.Width(300f), GUILayout.ExpandHeight(true)))
        {
            EditorGUILayout.LabelField("对话列表", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("这里维护对话条目本身。右侧编辑节点和台词内容。", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                newDialogueId = EditorGUILayout.TextField(newDialogueId);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(newDialogueId)))
                {
                    if (GUILayout.Button("新增", GUILayout.Width(72f)))
                    {
                        Undo.RecordObject(database, "新增对话条目");
                        DialogueContentDatabase.DialogueEntry entry = database.GetOrCreateEntry(newDialogueId);
                        if (entry != null)
                        {
                            selectedDialogueId = entry.dialogueId;
                            EditorUtility.SetDirty(database);
                            SaveAsset(database);
                        }
                    }
                }
            }

            dialogueScroll = EditorGUILayout.BeginScrollView(dialogueScroll);
            for (int i = 0; i < database.Entries.Count; i++)
            {
                DialogueContentDatabase.DialogueEntry entry = database.Entries[i];
                if (entry == null)
                {
                    continue;
                }

                string label = string.IsNullOrWhiteSpace(entry.displayName)
                    ? entry.dialogueId
                    : $"{entry.displayName} ({entry.dialogueId})";
                bool isSelected = string.Equals(selectedDialogueId, entry.dialogueId, System.StringComparison.Ordinal);
                GUIStyle style = isSelected ? EditorStyles.miniButtonMid : EditorStyles.miniButton;
                if (GUILayout.Button(label, style, GUILayout.Height(32f)))
                {
                    selectedDialogueId = entry.dialogueId;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawDialogueInspector(DialogueContentDatabase database)
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
        {
            DialogueContentDatabase.DialogueEntry entry = database.FindEntry(selectedDialogueId);
            if (entry == null)
            {
                EditorGUILayout.HelpBox("先在左侧选择或创建一个对话条目。", MessageType.Info);
                return;
            }

            contentScroll = EditorGUILayout.BeginScrollView(contentScroll);
            DrawDialogueHeader(database, entry);
            EditorGUILayout.Space(8f);
            DrawNodeList(database, entry);
            EditorGUILayout.EndScrollView();
        }
    }

    private void DrawDialogueHeader(DialogueContentDatabase database, DialogueContentDatabase.DialogueEntry entry)
    {
        EditorGUILayout.LabelField("对话定义", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("这里只做词条整理和可视化录入，不做实际播放逻辑。", MessageType.None);

        using (new EditorGUI.ChangeCheckScope())
        {
            entry.dialogueId = EditorGUILayout.TextField("对话ID", entry.dialogueId);
            entry.displayName = EditorGUILayout.TextField("显示名字", entry.displayName);
            entry.openingNodeId = EditorGUILayout.TextField("起始节点ID", entry.openingNodeId);
            entry.description = EditorGUILayout.TextField("说明", entry.description);
            DrawStringList("标签", entry.tags);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("新增节点", GUILayout.Width(88f)))
                {
                    Undo.RecordObject(database, "新增对话节点");
                    entry.nodes.Add(new DialogueContentDatabase.DialogueNodeEntry
                    {
                        nodeId = $"node_{entry.nodes.Count + 1:000}"
                    });
                    SaveAsset(database);
                }

                if (GUILayout.Button("删除当前对话", GUILayout.Width(110f)))
                {
                    if (EditorUtility.DisplayDialog("删除对话", $"确定删除对话 {entry.dialogueId} 吗？", "删除", "取消"))
                    {
                        Undo.RecordObject(database, "删除对话");
                        database.Entries.Remove(entry);
                        selectedDialogueId = database.Entries.Count > 0 ? database.Entries[0].dialogueId : string.Empty;
                        SaveAsset(database);
                        GUIUtility.ExitGUI();
                    }
                }
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(database);
                SaveAsset(database);
            }
        }
    }

    private void DrawNodeList(DialogueContentDatabase database, DialogueContentDatabase.DialogueEntry entry)
    {
        EditorGUILayout.LabelField($"节点列表 ({entry.nodes.Count})", EditorStyles.boldLabel);
        for (int i = 0; i < entry.nodes.Count; i++)
        {
            DialogueContentDatabase.DialogueNodeEntry node = entry.nodes[i];
            if (node == null)
            {
                continue;
            }

            DialogueContentDatabase.EnsureNode(node);
            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string title = string.IsNullOrWhiteSpace(node.nodeId) ? $"节点 {i + 1}" : node.nodeId;
                    EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                    if (GUILayout.Button("删除节点", GUILayout.Width(88f)))
                    {
                        Undo.RecordObject(database, "删除对话节点");
                        entry.nodes.RemoveAt(i);
                        SaveAsset(database);
                        GUIUtility.ExitGUI();
                    }
                }

                node.nodeId = EditorGUILayout.TextField("节点ID", node.nodeId);
                node.nodeType = (DialogueContentDatabase.NodeType)EditorGUILayout.EnumPopup("节点类型", node.nodeType);
                node.speakerId = EditorGUILayout.TextField("说话人ID", node.speakerId);
                node.speakerName = EditorGUILayout.TextField("说话人显示名", node.speakerName);
                node.content = EditorGUILayout.TextField("台词内容", node.content, GUILayout.MinHeight(60f));
                node.portraitSprite = (Sprite)EditorGUILayout.ObjectField("头像 Sprite", node.portraitSprite, typeof(Sprite), false);
                node.dialoguePrefab = (GameObject)EditorGUILayout.ObjectField("对话预制体", node.dialoguePrefab, typeof(GameObject), false);
                node.voiceClip = (AudioClip)EditorGUILayout.ObjectField("语音", node.voiceClip, typeof(AudioClip), false);
                node.nextNodeId = EditorGUILayout.TextField("默认下一节点ID", node.nextNodeId);
                node.note = EditorGUILayout.TextField("备注", node.note);
                DrawStringList("标签", node.tags);

                if (node.nodeType == DialogueContentDatabase.NodeType.Choice)
                {
                    DrawChoiceList(database, node);
                }
            }
        }
    }

    private static void DrawChoiceList(DialogueContentDatabase database, DialogueContentDatabase.DialogueNodeEntry node)
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("选项列表", EditorStyles.boldLabel);
        for (int i = 0; i < node.choices.Count; i++)
        {
            DialogueContentDatabase.DialogueChoiceEntry choice = node.choices[i];
            if (choice == null)
            {
                continue;
            }

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(string.IsNullOrWhiteSpace(choice.choiceId) ? $"选项 {i + 1}" : choice.choiceId, EditorStyles.boldLabel);
                    if (GUILayout.Button("删除", GUILayout.Width(72f)))
                    {
                        Undo.RecordObject(database, "删除选项");
                        node.choices.RemoveAt(i);
                        SaveAsset(database);
                        GUIUtility.ExitGUI();
                    }
                }

                choice.choiceId = EditorGUILayout.TextField("选项ID", choice.choiceId);
                choice.text = EditorGUILayout.TextField("选项文本", choice.text);
                choice.nextNodeId = EditorGUILayout.TextField("跳转节点ID", choice.nextNodeId);
            }
        }

        if (GUILayout.Button("新增选项", GUILayout.Width(88f)))
        {
            Undo.RecordObject(database, "新增选项");
            node.choices.Add(new DialogueContentDatabase.DialogueChoiceEntry
            {
                choiceId = $"choice_{node.choices.Count + 1:000}"
            });
            SaveAsset(database);
        }
    }

    private static void DrawStringList(string label, System.Collections.Generic.List<string> values)
    {
        if (values == null)
        {
            return;
        }

        EditorGUILayout.LabelField(label);
        for (int i = 0; i < values.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                values[i] = EditorGUILayout.TextField(values[i]);
                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    values.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
            }
        }

        if (GUILayout.Button($"+ 新增{label}", GUILayout.Width(100f)))
        {
            values.Add(string.Empty);
        }
    }

    private void EnsureValidSelection(DialogueContentDatabase database)
    {
        if (!string.IsNullOrWhiteSpace(selectedDialogueId) && database.FindEntry(selectedDialogueId) != null)
        {
            return;
        }

        selectedDialogueId = database.Entries.Count > 0 && database.Entries[0] != null
            ? database.Entries[0].dialogueId
            : string.Empty;
    }

    private static DialogueContentDatabase EnsureDatabase()
    {
        DialogueContentDatabase database = AssetDatabase.LoadAssetAtPath<DialogueContentDatabase>(AssetPath);
        if (database != null)
        {
            for (int i = 0; i < database.Entries.Count; i++)
            {
                DialogueContentDatabase.EnsureEntry(database.Entries[i]);
            }

            return database;
        }

        EnsureResourceFolder();
        database = CreateInstance<DialogueContentDatabase>();
        AssetDatabase.CreateAsset(database, AssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return database;
    }

    private static void EnsureResourceFolder()
    {
        if (!AssetDatabase.IsValidFolder(ResourceFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
    }

    private static void SaveAsset(ScriptableObject asset)
    {
        if (asset == null)
        {
            return;
        }

        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }
}
