using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class Toggle目标显隐 : MonoBehaviour
{
    [FormerlySerializedAs("toggle")]
    [SerializeField] private Toggle 控制Toggle;
    [FormerlySerializedAs("target")]
    [SerializeField] private GameObject 目标物体;
    [FormerlySerializedAs("extraTargets")]
    [SerializeField] private List<GameObject> 额外目标物体 = new List<GameObject>();
    [FormerlySerializedAs("reverseTarget")]
    [SerializeField] private GameObject 反向目标物体;
    [FormerlySerializedAs("extraReverseTargets")]
    [SerializeField] private List<GameObject> 额外反向目标物体 = new List<GameObject>();
    private bool 已有上次应用状态;
    private bool 上次应用状态;

    public GameObject 获取首个正向目标()
    {
        if (目标物体 != null)
        {
            return 目标物体;
        }

        for (int i = 0; i < 额外目标物体.Count; i++)
        {
            if (额外目标物体[i] != null)
            {
                return 额外目标物体[i];
            }
        }

        return null;
    }

    private void Awake()
    {
        if (控制Toggle == null)
        {
            控制Toggle = GetComponent<Toggle>();
        }
    }

    private void OnEnable()
    {
        if (控制Toggle != null)
        {
            控制Toggle.onValueChanged.AddListener(应用状态);
        }

        刷新显示状态();
    }

    private void OnDisable()
    {
        if (控制Toggle != null)
        {
            控制Toggle.onValueChanged.RemoveListener(应用状态);
        }
    }

    private void LateUpdate()
    {
        刷新显示状态();
    }

    private void 刷新显示状态()
    {
        if (控制Toggle == null)
        {
            控制Toggle = GetComponent<Toggle>();
        }

        if (控制Toggle == null)
        {
            return;
        }

        bool isOn = 控制Toggle.isOn;
        if (已有上次应用状态 && 上次应用状态 == isOn)
        {
            return;
        }

        应用状态(isOn);
    }

    private void 应用状态(bool isOn)
    {
        已有上次应用状态 = true;
        上次应用状态 = isOn;

        if (目标物体 != null)
        {
            目标物体.SetActive(isOn);
        }

        for (int i = 0; i < 额外目标物体.Count; i++)
        {
            GameObject extraTarget = 额外目标物体[i];
            if (extraTarget != null)
            {
                extraTarget.SetActive(isOn);
            }
        }

        if (反向目标物体 != null)
        {
            反向目标物体.SetActive(!isOn);
        }

        for (int i = 0; i < 额外反向目标物体.Count; i++)
        {
            GameObject extraReverseTarget = 额外反向目标物体[i];
            if (extraReverseTarget != null)
            {
                extraReverseTarget.SetActive(!isOn);
            }
        }
    }
}
