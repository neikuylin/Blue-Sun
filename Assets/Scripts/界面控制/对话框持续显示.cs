using UnityEngine;

[DisallowMultipleComponent]
public sealed class 对话框持续显示 : MonoBehaviour
{
    private bool 已打开;

    private void Awake()
    {
        已打开 = gameObject.activeSelf;
    }

    public bool 是否已打开
    {
        get { return 已打开; }
    }

    public void 打开对话框()
    {
        if (已打开)
        {
            return;
        }

        gameObject.SetActive(true);
        已打开 = true;
    }

    public void 关闭对话框()
    {
        gameObject.SetActive(false);
        已打开 = false;
    }
}
