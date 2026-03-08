using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSelectEntry : MonoBehaviour
{
    [Header("角色ID（可用中文）")]
    public string characterId;

    [Header("选择头像（按钮或Toggle）")]
    public Button selectButton;
    public Toggle selectToggle;

    [Header("头像源（用于填入栏位头像）")]
    public Image portraitSource;

    [Header("背景框立绘（该角色激活时显示）")]
    public List<GameObject> backgroundPortraits = new List<GameObject>();

    [Header("可用性视觉（可留空，默认取按钮下全部Graphic）")]
    public List<Graphic> availabilityVisuals = new List<Graphic>();

    [Header("保持原色（勾选后不做灰显）")]
    public bool keepOriginalColor;
}
