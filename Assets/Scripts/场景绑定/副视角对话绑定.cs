using System.Collections.Generic;
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

    [Header("交互按钮")]
    public GameObject 交互按钮容器;
    public GameObject 交互按钮模板;
    public List<GameObject> 交互按钮槽位 = new List<GameObject>();

    [Header("标识内容绑定")]
    public List<DialogueInteractionIdentifierBinding> 标识内容绑定 = new List<DialogueInteractionIdentifierBinding>();
}
