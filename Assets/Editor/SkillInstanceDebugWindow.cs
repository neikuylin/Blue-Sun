using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class SkillInstanceDebugWindow : EditorWindow
{
    private Vector2 scroll;

    [MenuItem("Tools/技能/现有技能实例")]
    private static void Open()
    {
        SkillInstanceDebugWindow window = GetWindow<SkillInstanceDebugWindow>();
        window.titleContent = new GUIContent("现有技能实例");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("现有技能实例", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "这里读取的是运行时当前技能栏正在显示的技能实例，不是 BattleSkillDatabase 里的定义本体。",
            MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("请先进入 Play 模式后再查看当前技能实例。", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("刷新", GUILayout.Width(80f)))
            {
                Repaint();
            }
        }

        List<BattleSkillPaginationBinder.SkillInstanceSnapshot> snapshots =
            BattleSkillPaginationBinder.GetCurrentSkillSnapshots();

        EditorGUILayout.Space(8f);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        if (snapshots == null || snapshots.Count == 0)
        {
            EditorGUILayout.HelpBox("当前没有可读取的技能实例。", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < snapshots.Count; i++)
            {
                BattleSkillPaginationBinder.SkillInstanceSnapshot snapshot = snapshots[i];
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    EditorGUILayout.LabelField($"槽位 {snapshot.index}");
                    if (snapshot.isEmpty)
                    {
                        EditorGUILayout.LabelField("状态", "空");
                        continue;
                    }

                    EditorGUILayout.LabelField("技能ID", snapshot.skillId);
                    EditorGUILayout.LabelField("技能名字", snapshot.displayName);
                    EditorGUILayout.LabelField("技能描述", snapshot.description);
                    EditorGUILayout.LabelField("使用者", snapshot.ownerCharacterId);
                    EditorGUILayout.LabelField("来源", snapshot.source);
                    EditorGUILayout.LabelField("技能倍率", snapshot.damageMultiplier.ToString("0.##"));
                    EditorGUILayout.LabelField("技能伤害", snapshot.damage.ToString());
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }
}
