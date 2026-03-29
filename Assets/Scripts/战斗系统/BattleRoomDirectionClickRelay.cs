using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class BattleRoomDirectionClickRelay : MonoBehaviour
{
    [SerializeField] private string targetNodeId = string.Empty;
    [SerializeField] private bool interactable = true;

    public void Configure(string nextNodeId, bool canInteract)
    {
        targetNodeId = nextNodeId ?? string.Empty;
        interactable = canInteract && !string.IsNullOrWhiteSpace(targetNodeId);
        enabled = interactable;
    }

    private void OnMouseUpAsButton()
    {
        if (!interactable || string.IsNullOrWhiteSpace(targetNodeId))
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.IsPointerOverGameObject())
        {
            return;
        }

        BattleTurnSystem turnSystem = Object.FindObjectOfType<BattleTurnSystem>();
        if (turnSystem != null && !turnSystem.IsExplorationMode)
        {
            return;
        }

        Debug.Log($"BattleRoomDirectionClickRelay: sprite click targetNode='{targetNodeId}' on '{gameObject.name}'.");
        BattleBootstrap.NavigateToNode(targetNodeId);
    }
}
