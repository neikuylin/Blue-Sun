using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class ToggleTargetActive : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject target;
    [SerializeField] private List<GameObject> extraTargets = new List<GameObject>();

    private void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(ApplyState);
            ApplyState(toggle.isOn);
        }
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(ApplyState);
        }
    }

    private void ApplyState(bool isOn)
    {
        if (target != null)
        {
            target.SetActive(isOn);
        }

        for (int i = 0; i < extraTargets.Count; i++)
        {
            GameObject extraTarget = extraTargets[i];
            if (extraTarget != null)
            {
                extraTarget.SetActive(isOn);
            }
        }
    }
}
