using System.IO;
using UnityEditor;
using UnityEngine;

public sealed class SaveGameEditorWindow : EditorWindow
{
    private Vector2 scroll;
    private string loadedPreview = string.Empty;

    [MenuItem("Tools/存档/存档编辑器")]
    private static void Open()
    {
        SaveGameEditorWindow window = GetWindow<SaveGameEditorWindow>("存档编辑器");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("业务存档", EditorStyles.boldLabel);
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("路径", SaveGameService.DefaultSavePath);
        EditorGUILayout.LabelField("状态", SaveGameService.HasDefaultSaveFile() ? "存在" : "不存在");
        EditorGUILayout.Space(8f);

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("保存当前运行时"))
                {
                    SaveGameService.SaveDefaultSlot();
                    RefreshPreview();
                }

                if (GUILayout.Button("读取存档"))
                {
                    SaveGameService.LoadDefaultSlot();
                }

                if (GUILayout.Button("新游戏默认状态"))
                {
                    SaveGameService.StartNewGame();
                }
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("刷新预览"))
            {
                RefreshPreview();
            }

            if (GUILayout.Button("定位文件"))
            {
                EditorUtility.RevealInFinder(SaveGameService.DefaultSavePath);
            }

            using (new EditorGUI.DisabledScope(!SaveGameService.HasDefaultSaveFile()))
            {
                if (GUILayout.Button("删除存档"))
                {
                    SaveGameService.DeleteDefaultSlot();
                    RefreshPreview();
                }
            }
        }

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("保存、读取、新游戏默认状态需要在 Play 模式执行。窗口只负责调试当前运行时业务存档。", MessageType.Info);
        }

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("JSON 预览", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(loadedPreview, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void OnFocus()
    {
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        loadedPreview = SaveGameService.HasDefaultSaveFile()
            ? File.ReadAllText(SaveGameService.DefaultSavePath)
            : string.Empty;
        Repaint();
    }
}
