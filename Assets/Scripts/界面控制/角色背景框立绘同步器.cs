using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class 角色背景框立绘同步器 : MonoBehaviour
{
    private string lastCurrentId = string.Empty;

    private void OnEnable()
    {
        Refresh(force: true);
    }

    private void OnTransformChildrenChanged()
    {
        Refresh(force: true);
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh(bool force = false)
    {
        string currentId = 界面ID列表.当前ID ?? string.Empty;
        if (!force && string.Equals(lastCurrentId, currentId, StringComparison.Ordinal))
        {
            return;
        }

        lastCurrentId = currentId;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null)
            {
                continue;
            }

            bool shouldBeActive = !string.IsNullOrWhiteSpace(currentId) &&
                                  string.Equals(child.name, currentId, StringComparison.Ordinal);
            if (child.gameObject.activeSelf != shouldBeActive)
            {
                child.gameObject.SetActive(shouldBeActive);
            }
        }
    }
}
