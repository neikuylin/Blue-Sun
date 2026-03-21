using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ReplayToggleStateOnOpen : MonoBehaviour
{
    [SerializeField] private Toggle sourceToggle;
    [SerializeField] private List<Toggle> replayToggles = new List<Toggle>();

    private Coroutine replayRoutine;

    private void Awake()
    {
        if (sourceToggle == null)
        {
            sourceToggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (sourceToggle != null)
        {
            sourceToggle.onValueChanged.AddListener(HandleSourceToggleChanged);
        }
    }

    private void OnDisable()
    {
        if (sourceToggle != null)
        {
            sourceToggle.onValueChanged.RemoveListener(HandleSourceToggleChanged);
        }

        if (replayRoutine != null)
        {
            StopCoroutine(replayRoutine);
            replayRoutine = null;
        }
    }

    private void HandleSourceToggleChanged(bool isOn)
    {
        if (!isOn)
        {
            return;
        }

        if (replayRoutine != null)
        {
            StopCoroutine(replayRoutine);
        }

        replayRoutine = StartCoroutine(ReplayStateNextFrame());
    }

    private IEnumerator ReplayStateNextFrame()
    {
        yield return null;

        for (int i = 0; i < replayToggles.Count; i++)
        {
            Toggle toggle = replayToggles[i];
            if (toggle == null)
            {
                continue;
            }

            toggle.onValueChanged.Invoke(toggle.isOn);
        }

        replayRoutine = null;
    }
}
