using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleSceneBindings : MonoBehaviour
{
    [Header("时间轴")]
    public Transform timelineAnchor;

    [Header("按钮")]
    public Button endTurnButton;
    public Button moveSkillButton;

    [Header("头像栏")]
    public Image currentPortrait;
    public Image secondPortrait;
    public Image thirdPortrait;
    public Image fourthPortrait;

    [Header("行动点")]
    public Transform actionPointPanel;

    [Header("生命与魔法")]
    public Image healthSlotImage;
    public Image healthFillImage;
    public TMP_Text healthText;
    public Image manaSlotImage;
    public Image manaFillImage;
    public TMP_Text manaText;

    [Header("背包")]
    public RectTransform battleBackpackContainer;
    public RectTransform battleBackpackContent;
    public RectTransform battleBackpackDragHandle;

    [Header("通用")]
    public RectTransform overlayCanvas;

    public static BattleSceneBindings FindInActiveScene()
    {
        return FindObjectOfType<BattleSceneBindings>(true);
    }
}
