using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 打开时重放Toggle状态 : MonoBehaviour
{
    [FormerlySerializedAs("sourceToggle")]
    [SerializeField] private Toggle 来源Toggle;
    [FormerlySerializedAs("defaultToggleOnOpen")]
    [SerializeField] private Toggle 打开时默认Toggle;
    [FormerlySerializedAs("replayToggles")]
    [SerializeField] private List<Toggle> 重放Toggle列表 = new List<Toggle>();

    private Coroutine 重放协程;

    private void Awake()
    {
        if (来源Toggle == null)
        {
            来源Toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (来源Toggle != null)
        {
            来源Toggle.onValueChanged.AddListener(处理来源Toggle变化);
        }
    }

    private void OnDisable()
    {
        if (来源Toggle != null)
        {
            来源Toggle.onValueChanged.RemoveListener(处理来源Toggle变化);
        }

        if (重放协程 != null)
        {
            StopCoroutine(重放协程);
            重放协程 = null;
        }
    }

    private void 处理来源Toggle变化(bool isOn)
    {
        if (!isOn)
        {
            return;
        }

        if (重放协程 != null)
        {
            StopCoroutine(重放协程);
        }

        重放协程 = StartCoroutine(下一帧重放状态());
    }

    private IEnumerator 下一帧重放状态()
    {
        yield return null;

        if (打开时默认Toggle != null)
        {
            if (!打开时默认Toggle.isOn)
            {
                打开时默认Toggle.isOn = true;
            }
            else
            {
                打开时默认Toggle.onValueChanged.Invoke(true);
            }

            重放协程 = null;
            yield break;
        }

        for (int i = 0; i < 重放Toggle列表.Count; i++)
        {
            Toggle toggle = 重放Toggle列表[i];
            if (toggle == null)
            {
                continue;
            }

            toggle.onValueChanged.Invoke(toggle.isOn);
        }

        重放协程 = null;
    }
}
