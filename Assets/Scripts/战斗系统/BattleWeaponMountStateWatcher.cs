using UnityEngine;

public sealed class BattleWeaponMountStateWatcher : MonoBehaviour
{
    private const string LeftWeaponMountPointName = "武器挂载点（左）";
    private const string RightWeaponMountPointName = "武器挂载点（右）";

    private Animator animator;
    private readonly System.Collections.Generic.List<Transform> mountPoints = new System.Collections.Generic.List<Transform>();
    private int lastStateHash = int.MinValue;
    private bool lastVisibilityApplied;
    private bool hasAppliedVisibility;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        RefreshMountPoints();
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (mountPoints.Count == 0)
        {
            RefreshMountPoints();
        }

        if (animator == null || mountPoints.Count == 0 || !animator.isActiveAndEnabled)
        {
            return;
        }

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        int stateHash = stateInfo.fullPathHash != 0 ? stateInfo.fullPathHash : stateInfo.shortNameHash;
        bool shouldHide = IsStateMatch(stateInfo, BattleAnimationSettingsResolver.ResolveExplorationIdleStateName()) ||
            IsStateMatch(stateInfo, BattleAnimationSettingsResolver.ResolveExplorationMoveStateName());

        bool shouldShowMountPoint = !shouldHide;
        if (!hasAppliedVisibility || stateHash != lastStateHash || shouldShowMountPoint != lastVisibilityApplied)
        {
            for (int i = 0; i < mountPoints.Count; i++)
            {
                Transform mountPoint = mountPoints[i];
                if (mountPoint != null)
                {
                    mountPoint.gameObject.SetActive(shouldShowMountPoint);
                }
            }

            lastStateHash = stateHash;
            lastVisibilityApplied = shouldShowMountPoint;
            hasAppliedVisibility = true;
        }
    }

    private void RefreshMountPoints()
    {
        mountPoints.Clear();
        AddMountPoint(FindChildByName(transform, LeftWeaponMountPointName) ?? FindDescendantByName(transform, LeftWeaponMountPointName));
        AddMountPoint(FindChildByName(transform, RightWeaponMountPointName) ?? FindDescendantByName(transform, RightWeaponMountPointName));
    }

    private void AddMountPoint(Transform mountPoint)
    {
        if (mountPoint != null && !mountPoints.Contains(mountPoint))
        {
            mountPoints.Add(mountPoint);
        }
    }

    private static bool IsStateMatch(AnimatorStateInfo stateInfo, string stateName)
    {
        if (string.IsNullOrWhiteSpace(stateName))
        {
            return false;
        }

        int shortHash = Animator.StringToHash(stateName);
        if (stateInfo.shortNameHash == shortHash || stateInfo.fullPathHash == shortHash)
        {
            return true;
        }

        return stateInfo.IsName(stateName);
    }

    private static Transform FindChildByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null && string.Equals(child.name, childName, System.StringComparison.Ordinal))
            {
                return child;
            }
        }

        return null;
    }

    private static Transform FindDescendantByName(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        int childCount = parent.childCount;
        for (int i = 0; i < childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child == null)
            {
                continue;
            }

            if (string.Equals(child.name, childName, System.StringComparison.Ordinal))
            {
                return child;
            }

            Transform nested = FindDescendantByName(child, childName);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }
}
