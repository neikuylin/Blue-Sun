using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHierarchyPathUtility
{
    public static Transform FindInActiveScene(string path)
    {
        return Find(SceneManager.GetActiveScene(), path);
    }

    public static Transform Find(Scene scene, string path)
    {
        if (!scene.IsValid() || !scene.isLoaded || string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        string[] segments = path.Split('/');
        if (segments.Length == 0)
        {
            return null;
        }

        GameObject[] roots = scene.GetRootGameObjects();
        Transform current = null;
        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            if (root != null && string.Equals(root.name, segments[0], StringComparison.Ordinal))
            {
                current = root.transform;
                break;
            }
        }

        if (current == null)
        {
            return null;
        }

        for (int i = 1; i < segments.Length; i++)
        {
            current = FindDirectChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    public static Transform FindDirectChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }
}
