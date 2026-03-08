using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToggleLayerByState : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private RectTransform target;
    [SerializeField] private RectTransform background;

    public void Configure(Toggle toggleRef, RectTransform targetRef, RectTransform backgroundRef)
    {
        toggle = toggleRef;
        target = targetRef;
        background = backgroundRef;
        ApplyLayer(toggle != null && toggle.isOn);
    }

    private void Reset()
    {
        toggle = GetComponent<Toggle>();
        target = GetComponent<RectTransform>();
    }

    private void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        if (target == null)
        {
            target = GetComponent<RectTransform>();
        }
    }

    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleValueChanged);
            ApplyLayer(toggle.isOn);
        }
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        }
    }

    private void OnToggleValueChanged(bool isOn)
    {
        ApplyLayer(isOn);
    }

    private void ApplyLayer(bool isOn)
    {
        if (target == null || background == null)
        {
            return;
        }

        Transform parent = background.parent;
        if (parent == null)
        {
            return;
        }

        if (target.parent != parent)
        {
            target.SetParent(parent, true);
        }

        int bgIndex = background.GetSiblingIndex();
        int maxIndex = Mathf.Max(0, parent.childCount - 1);

        int desiredIndex = isOn
            ? Mathf.Min(maxIndex, bgIndex + 1)
            : Mathf.Max(0, bgIndex - 1);

        target.SetSiblingIndex(desiredIndex);
    }
}
