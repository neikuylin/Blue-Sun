using UnityEditor;
using UnityEngine;

public static class 水纹半月粒子系统菜单
{
    [MenuItem("GameObject/特效/水纹半月粒子系统", false, 11)]
    private static void CreateRippleParticleSystem(MenuCommand menuCommand)
    {
        GameObject particleObject = new GameObject("水纹半月粒子系统");
        GameObjectUtility.SetParentAndAlign(particleObject, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(particleObject, "创建水纹半月粒子系统");

        particleObject.AddComponent<ParticleSystem>();
        particleObject.AddComponent<水纹半月粒子系统>();

        Selection.activeObject = particleObject;
    }
}
