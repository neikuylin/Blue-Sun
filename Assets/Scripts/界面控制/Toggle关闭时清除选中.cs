using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Toggle关闭时清除选中 : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleValueChanged);
        }
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleValueChanged);
        }
    }

    private void HandleToggleValueChanged(bool isOn)
    {
        if (isOn)
        {
            return;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return;
        }

        if (eventSystem.currentSelectedGameObject == gameObject)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
