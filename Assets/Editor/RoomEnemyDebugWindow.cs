using UnityEditor;
using UnityEngine;

public sealed class RoomEnemyDebugWindow : EditorWindow
{
    [MenuItem("Tools/战斗/房间敌人调试器")]
    private static void Open()
    {
        RoomEnemyDebugWindow window = GetWindow<RoomEnemyDebugWindow>("房间敌人");
        window.minSize = new Vector2(560f, 240f);
        window.Show();
        window.Focus();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("房间敌人调试器", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "这个窗口原来直接编辑 BattleBootstrap.enemySpawns，但运行时已经不再使用这条链路。\n\n现在请改用 Tools/战斗/遭遇战编辑器，直接维护 RoomEnemyPresetDatabase 里的遭遇战预设。",
            MessageType.Info);

        EditorGUILayout.Space(8f);
        if (GUILayout.Button("打开遭遇战编辑器", GUILayout.Height(32f)))
        {
            EditorApplication.ExecuteMenuItem("Tools/战斗/遭遇战编辑器");
        }
    }
}
