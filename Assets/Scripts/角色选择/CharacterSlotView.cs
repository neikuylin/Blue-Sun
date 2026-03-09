using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class CharacterSlotView : MonoBehaviour
{
    [Header("主角栏位勾选后固定显示玩家背景")]
    public bool isMainSlot;

    [Header("槽位默认角色ID（不点头像也可用于驱动背景）")]
    public string slotCharacterId;

    [Header("切换当前栏位用")]
    public List<Button> selectButtons = new List<Button>();
    public List<Toggle> selectToggles = new List<Toggle>();

    [Header("空栏位显示")]
    public GameObject unselectedObject;

    [Header("角色头像显示")]
    public Image portraitImage;

    [HideInInspector]
    public string selectedCharacterId;
}
