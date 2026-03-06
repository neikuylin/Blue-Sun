using UnityEngine;

public class OpenCharacterSelect : MonoBehaviour
{
    public GameObject characterPanel;   //角色选择框
    public Vector2 offset = new Vector2(100, 0);  //偏移

    public void OpenPanel()
    {
        RectTransform slot = GetComponent<RectTransform>();
        RectTransform panel = characterPanel.GetComponent<RectTransform>();

        //设置位置
        panel.position = slot.position + (Vector3)offset;

        //显示面板
        characterPanel.SetActive(true);
    }
}