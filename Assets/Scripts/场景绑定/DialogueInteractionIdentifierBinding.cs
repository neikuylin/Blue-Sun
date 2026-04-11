using System;
using UnityEngine;

[Serializable]
public sealed class DialogueInteractionIdentifierBinding
{
    public string 标识ID = string.Empty;
    public GameObject 目标对象;
    public GameObject 关闭按钮;
}
