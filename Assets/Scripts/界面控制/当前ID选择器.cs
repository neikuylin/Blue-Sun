using UnityEngine;

[DisallowMultipleComponent]
public sealed class 当前ID选择器 : MonoBehaviour
{
    [SerializeField] private string 当前ID = string.Empty;

    public void 设置当前ID()
    {
        界面ID列表.设置营地当前ID(当前ID);
    }
}
