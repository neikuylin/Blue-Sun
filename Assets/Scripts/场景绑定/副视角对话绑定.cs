using UnityEngine;

[DisallowMultipleComponent]
public sealed class 副视角对话绑定 : MonoBehaviour
{
    [Header("预制体")]
    public GameObject 对话预制体;

    [Header("绑定")]
    public GameObject 立绘容器;
    public GameObject 角色名字;
    public GameObject 对话内容;
    public GameObject 继续按钮;
}
