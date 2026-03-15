using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class BattleSceneBindings : MonoBehaviour
{
    [Header("Timeline")]
    public Transform timelineAnchor;

    [Header("Buttons")]
    public Button endTurnButton;
    public Button moveSkillButton;
    public Button previousSkillPageButton;
    public Button nextSkillPageButton;

    [Header("Portraits")]
    public Image currentPortrait;
    public Image secondPortrait;
    public Image thirdPortrait;
    public Image fourthPortrait;

    [Header("Action Points")]
    public Transform actionPointPanel;

    [Header("Vitals")]
    public Image healthSlotImage;
    public Image healthFillImage;
    public TMP_Text healthText;
    public Image manaSlotImage;
    public Image manaFillImage;
    public TMP_Text manaText;

    [Header("Backpack")]
    public RectTransform battleBackpackContainer;
    public RectTransform battleBackpackContent;
    public RectTransform battleBackpackDragHandle;

    [Header("Equipment")]
    public RectTransform equipmentContainer;
    public Image leftPanelPortraitImage;
    public RectTransform leftPanelSkillSlotContainer;

    [Header("Overlay")]
    public RectTransform overlayCanvas;

    [Header("Paged Skills")]
    public List<Button> skillPageButtons = new List<Button>();
    public List<Image> skillPageIcons = new List<Image>();
    public TMP_Text spellCurrentPageText;
    public TMP_Text spellTotalPageText;

    public static BattleSceneBindings FindInActiveScene()
    {
        return FindObjectOfType<BattleSceneBindings>(true);
    }
}
