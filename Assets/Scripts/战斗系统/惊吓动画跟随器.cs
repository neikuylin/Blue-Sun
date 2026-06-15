using System.Collections;
using UnityEngine;

public sealed class 惊吓动画跟随器 : MonoBehaviour
{
    public void 初始化()
    {
        StartCoroutine(等待动画结束后销毁());
    }

    private IEnumerator 等待动画结束后销毁()
    {
        yield return null;

        Animator 动画器 = GetComponentInChildren<Animator>(true);
        float 动画时长 = 读取动画时长(动画器);
        if (动画时长 > 0.01f)
        {
            yield return new WaitForSeconds(动画时长);
        }

        Destroy(gameObject);
    }

    private static float 读取动画时长(Animator 动画器)
    {
        if (动画器 == null || 动画器.runtimeAnimatorController == null)
        {
            return 0f;
        }

        AnimatorStateInfo 状态 = 动画器.GetCurrentAnimatorStateInfo(0);
        float 播放速度 = Mathf.Abs(动画器.speed * 状态.speed);
        if (状态.length > 0.01f)
        {
            return 状态.length / Mathf.Max(0.01f, 播放速度);
        }

        float 最长时长 = 0f;
        AnimationClip[] 动画片段 = 动画器.runtimeAnimatorController.animationClips;
        for (int i = 0; i < 动画片段.Length; i++)
        {
            AnimationClip 动画片段资源 = 动画片段[i];
            if (动画片段资源 != null)
            {
                最长时长 = Mathf.Max(最长时长, 动画片段资源.length);
            }
        }

        return 最长时长;
    }
}
