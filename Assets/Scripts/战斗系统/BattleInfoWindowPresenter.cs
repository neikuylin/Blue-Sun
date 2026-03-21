using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BattleInfoWindowPresenter : MonoBehaviour
{
    private const string DefaultTextPath = "放大状态/放大后信息";

    [SerializeField] private TMP_Text messageText;

    public void ShowMessage(string message)
    {
        TMP_Text target = messageText != null ? messageText : ResolveMessageText();
        if (target == null)
        {
            return;
        }

        target.text = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
    }

    public void Clear()
    {
        ShowMessage(string.Empty);
    }

    public static BattleInfoWindowPresenter FindInActiveScene()
    {
        return FindObjectOfType<BattleInfoWindowPresenter>(true);
    }

    private TMP_Text ResolveMessageText()
    {
        if (messageText != null)
        {
            return messageText;
        }

        Transform child = transform.Find(DefaultTextPath);
        if (child == null)
        {
            return null;
        }

        messageText = child.GetComponent<TMP_Text>();
        return messageText;
    }

    private void Awake()
    {
        ResolveMessageText();
    }
}
