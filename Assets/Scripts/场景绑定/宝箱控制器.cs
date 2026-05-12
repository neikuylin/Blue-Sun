using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Playables;

[DisallowMultipleComponent]
public sealed class 宝箱控制器 : MonoBehaviour, IPointerClickHandler
{
    [Header("宝箱")]
    public int 宝箱序列号;

    [Header("界面")]
    public string 宝箱内容路径 = "Canvas/宝箱内容";

    [Header("表现")]
    public Animator 动画器;
    public string 打开动画触发器 = "打开";
    public PlayableDirector 音效;

    public void 打开宝箱()
    {
        确保宝箱序列号();

        宝箱内容绑定 content = 获取宝箱内容();
        if (content != null)
        {
            content.打开宝箱内容(宝箱序列号);
        }

        播放打开表现();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        打开宝箱();
    }

    private void OnMouseDown()
    {
        打开宝箱();
    }

    private void 确保宝箱序列号()
    {
        if (宝箱序列号 > 0)
        {
            return;
        }

        宝箱序列号 = InventoryShortcutRuntimeBinder.RegisterChestInstance();
    }

    private 宝箱内容绑定 获取宝箱内容()
    {
        Transform byPath = 查找场景路径(宝箱内容路径);
        if (byPath != null)
        {
            宝箱内容绑定 binding = byPath.GetComponent<宝箱内容绑定>();
            if (binding != null)
            {
                return binding;
            }

            return byPath.GetComponentInChildren<宝箱内容绑定>(true);
        }

        宝箱内容绑定[] contents = FindObjectsOfType<宝箱内容绑定>(true);
        return contents.Length > 0 ? contents[0] : null;
    }

    private static Transform 查找场景路径(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target != null && 构建场景路径(target) == path)
            {
                return target;
            }
        }

        return null;
    }

    private static string 构建场景路径(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    private void 播放打开表现()
    {
        if (动画器 != null && !string.IsNullOrWhiteSpace(打开动画触发器))
        {
            动画器.SetTrigger(打开动画触发器);
        }

        if (音效 != null)
        {
            音效.Play();
        }
    }
}
