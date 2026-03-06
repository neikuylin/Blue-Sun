using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CharacterPortraitContainerCreator
{
    private const string SlotRootName = "玩家栏位按钮";
    private const string UnselectedName = "未选择图片";
    private const string SelectedImageName = "选择图片";
    private const string PortraitDisplayName = "角色头像显示";
    private const string PortraitDisplayAltName = "角色头像容器";

    [MenuItem("Tools/角色选择/生成头像显示容器")]
    public static void CreateContainers()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("没有可用的活动场景。");
            return;
        }

        List<Transform> all = new List<Transform>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Collect(roots[i].transform, all);
        }

        int created = 0;
        int existed = 0;

        for (int i = 0; i < all.Count; i++)
        {
            Transform t = all[i];
            if (!IsNumberedSlotRoot(t.name))
            {
                continue;
            }

            Transform existing = FindChildByName(t, PortraitDisplayName);
            if (existing == null)
            {
                existing = FindChildByName(t, PortraitDisplayAltName);
            }

            if (existing != null)
            {
                existed++;
                continue;
            }

            RectTransform reference = null;
            Transform selected = FindChildByName(t, SelectedImageName);
            if (selected != null)
            {
                reference = selected as RectTransform;
            }

            if (reference == null)
            {
                Transform unselected = FindChildByName(t, UnselectedName);
                if (unselected != null)
                {
                    reference = unselected as RectTransform;
                }
            }

            GameObject go = new GameObject(PortraitDisplayName);
            Undo.RegisterCreatedObjectUndo(go, "Create Portrait Display Container");
            go.transform.SetParent(t, false);
            go.AddComponent<CanvasRenderer>();

            Image image = go.AddComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            go.SetActive(false);

            RectTransform rt = go.GetComponent<RectTransform>();
            if (reference != null)
            {
                rt.anchorMin = reference.anchorMin;
                rt.anchorMax = reference.anchorMax;
                rt.pivot = reference.pivot;
                rt.anchoredPosition = reference.anchoredPosition;
                rt.sizeDelta = reference.sizeDelta;
                rt.localScale = Vector3.one;
            }
            else
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(180f, 179f);
                rt.localScale = Vector3.one;
            }

            created++;
        }

        if (created > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
        }

        Debug.Log($"生成完成: 新建 {created} 个, 已存在 {existed} 个。");
    }

    private static bool IsNumberedSlotRoot(string name)
    {
        return name.StartsWith(SlotRootName + " (") && name.EndsWith(")");
    }

    private static void Collect(Transform root, List<Transform> all)
    {
        all.Add(root);
        for (int i = 0; i < root.childCount; i++)
        {
            Collect(root.GetChild(i), all);
        }
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform found = FindChildByName(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
