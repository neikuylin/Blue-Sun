using UnityEditor;
using UnityEngine;

public static class 进战流程模型旋转菜单
{
    private const string 菜单路径 = "Tools/模型/进战流程模型旋转";

    [MenuItem(菜单路径)]
    private static void 切换()
    {
        BattleAnimationSettings 设置 = BattleAnimationSettings.LoadDefault();
        if (设置 == null)
        {
            Debug.LogError("进战流程模型旋转：找不到 Resources/BattleAnimationSettings 配置。");
            return;
        }

        Undo.RecordObject(设置, "切换进战流程模型旋转");
        设置.enterBattleModelTurnEnabled = !设置.enterBattleModelTurnEnabled;
        EditorUtility.SetDirty(设置);
        AssetDatabase.SaveAssets();
        Menu.SetChecked(菜单路径, 设置.enterBattleModelTurnEnabled);

        Debug.Log(设置.enterBattleModelTurnEnabled
            ? "已开启进战流程中的模型旋转。"
            : "已关闭进战流程中的模型旋转。");
    }

    [MenuItem(菜单路径, true)]
    private static bool 验证菜单()
    {
        Menu.SetChecked(
            菜单路径,
            BattleAnimationSettingsResolver.ResolveEnterBattleModelTurnEnabled());
        return true;
    }
}
