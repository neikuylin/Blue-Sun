using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class 对话按钮触发 : MonoBehaviour
{
    [SerializeField] private string 对话事件ID = string.Empty;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"对话按钮触发: 对象 '{name}' 缺少 Button 组件。");
            enabled = false;
            return;
        }

        button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (string.IsNullOrWhiteSpace(对话事件ID))
        {
            Debug.LogError($"对话按钮触发: 对象 '{name}' 的对话事件ID为空。");
            return;
        }

        对话运行时.尝试触发对话事件(对话事件ID);
    }
}
