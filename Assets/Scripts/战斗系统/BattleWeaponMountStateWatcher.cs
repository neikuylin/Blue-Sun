using UnityEngine;

public sealed class BattleWeaponMountStateWatcher : MonoBehaviour
{
    private const string WeaponMountPointName = "武器挂载点";

    private Animator animator;
    private Transform mountPoint;
    private int lastStateHash = int.MinValue;
    private bool lastVisibilityApplied;
    private bool hasAppliedVisibility;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>(true);
        mountPoint = FindChildByName(transform, WeaponMountPointName) ?? FindDescendantByName(transform, WeaponMountPointName);
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (mountPoint == null)
        {
            mountPoint = FindChildByName(transform, WeaponMountPointName) ?? FindDescendantByName(transform, WeaponMountPointName);
        }

        if (animator == null || mountPoint == null || !animator.isActiveAndEnabled)
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
            mountPoint.gameObject.SetActive(shouldShowMountPoint);
            lastStateHash = stateHash;
            lastVisibilityApplied = shouldShowMountPoint;
            hasAppliedVisibility = true;
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
