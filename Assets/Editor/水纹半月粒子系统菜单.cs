using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class 水纹半月粒子系统菜单
{
    [MenuItem("GameObject/特效/水纹半月粒子系统", false, 11)]
    private static void CreateRippleParticleSystem(MenuCommand menuCommand)
    {
        GameObject particleObject = new GameObject("水纹半月粒子系统");
        GameObject parentObject = ResolveCreationParent(menuCommand);
        if (parentObject != null)
        {
            if (parentObject.scene.IsValid())
            {
                SceneManager.MoveGameObjectToScene(particleObject, parentObject.scene);
            }

            particleObject.transform.SetParent(parentObject.transform, false);
            GameObjectUtility.EnsureUniqueNameForSibling(particleObject);
        }
        else
        {
            GameObjectUtility.SetParentAndAlign(particleObject, null);
        }

        Undo.RegisterCreatedObjectUndo(particleObject, "创建水纹半月粒子系统");

        particleObject.AddComponent<ParticleSystem>();
        particleObject.AddComponent<水纹半月粒子系统>();

        Selection.activeObject = particleObject;
    }

    private static GameObject ResolveCreationParent(MenuCommand menuCommand)
    {
        PrefabStage prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
        if (prefabStage != null && prefabStage.prefabContentsRoot != null)
        {
            if (menuCommand.context is GameObject prefabContextObject &&
                prefabContextObject.scene == prefabStage.scene)
            {
                return prefabContextObject;
            }

            if (Selection.activeGameObject != null &&
                Selection.activeGameObject.scene == prefabStage.scene)
            {
                return Selection.activeGameObject;
            }

            return prefabStage.prefabContentsRoot;
        }

        if (menuCommand.context is GameObject contextObject)
        {
            return contextObject;
        }

        if (Selection.activeGameObject != null)
        {
            return Selection.activeGameObject;
        }

        return null;
    }
}
