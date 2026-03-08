using System;
using UnityEngine;

public class OpenCharacterSelect : MonoBehaviour
{
    public static event Action<OpenCharacterSelect> PanelOpened;
    public static event Action<OpenCharacterSelect> PanelClosed;

    public GameObject characterPanel;
    public Vector2 offset = new Vector2(100, 0);

    private bool isPanelOpen;

    private void OnEnable()
    {
        isPanelOpen = characterPanel != null && characterPanel.activeInHierarchy;
    }

    public void OpenPanel()
    {
        if (characterPanel == null)
        {
            return;
        }

        if (isPanelOpen)
        {
            isPanelOpen = false;
            characterPanel.SetActive(false);
            PanelClosed?.Invoke(this);
            return;
        }

        RectTransform slot = GetComponent<RectTransform>();
        RectTransform panel = characterPanel.GetComponent<RectTransform>();
        if (slot != null && panel != null)
        {
            panel.position = slot.position + (Vector3)offset;
        }

        isPanelOpen = true;
        characterPanel.SetActive(true);
        PanelOpened?.Invoke(this);
    }

    public void MarkClosedFromOutside()
    {
        isPanelOpen = false;
    }
}
