using UnityEditor;
using UnityEngine;

public static class 花瓣飞舞粒子系统菜单
{
    [MenuItem("GameObject/特效/花瓣下落粒子系统", false, 10)]
    private static void CreatePetalParticleSystem(MenuCommand menuCommand)
    {
        GameObject particleObject = new GameObject("花瓣下落粒子系统");
        GameObjectUtility.SetParentAndAlign(particleObject, menuCommand.context as GameObject);
        Undo.RegisterCreatedObjectUndo(particleObject, "创建花瓣下落粒子系统");

        particleObject.AddComponent<ParticleSystem>();
        particleObject.AddComponent<花瓣飞舞粒子系统>();

        Selection.activeObject = particleObject;
    }
}
