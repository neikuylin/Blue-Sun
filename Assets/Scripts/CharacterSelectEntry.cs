using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSelectEntry : MonoBehaviour
{
    [Header("角色ID（可用中文）")]
    public string characterId;

    [Header("选择头像按钮")]
    public Button selectButton;

    [Header("头像源（用于填入栏位头像）")]
    public Image portraitSource;

    [Header("背景框立绘（该角色激活时显示）")]
    public List<GameObject> backgroundPortraits = new List<GameObject>();
}
