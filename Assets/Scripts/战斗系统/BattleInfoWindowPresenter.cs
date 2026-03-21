using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
public sealed class BattleInfoWindowPresenter : MonoBehaviour
{
    private const string DefaultBriefTextPath = "放大状态/放大后信息";
    private const string DefaultDetailTextPath = "放大状态/详细信息";

    [Header("简略信息")]
    [FormerlySerializedAs("messageText")]
    [SerializeField] private TMP_Text briefInfoText;

    [Header("详细信息")]
    [SerializeField] private TMP_Text detailInfoText;

    private readonly List<string> detailMessages = new List<string>();

    public void ShowMessage(string message)
    {
        string resolvedMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message;

        TMP_Text briefTarget = briefInfoText != null ? briefInfoText : ResolveBriefInfoText();
        if (briefTarget != null)
        {
            briefTarget.text = resolvedMessage;
        }

        if (string.IsNullOrWhiteSpace(resolvedMessage))
        {
            return;
        }

        TMP_Text detailTarget = detailInfoText != null ? detailInfoText : ResolveDetailInfoText();
        if (detailTarget == null)
        {
            return;
        }

        detailMessages.Add(resolvedMessage);
        ApplyDetailMessages(detailTarget);
    }

    public void Clear()
    {
        if (briefInfoText != null || ResolveBriefInfoText() != null)
        {
            briefInfoText.text = string.Empty;
        }

        detailMessages.Clear();
        if (detailInfoText != null || ResolveDetailInfoText() != null)
        {
            detailInfoText.text = string.Empty;
        }
    }

    public static BattleInfoWindowPresenter FindInActiveScene()
    {
        return FindObjectOfType<BattleInfoWindowPresenter>(true);
    }

    private TMP_Text ResolveBriefInfoText()
    {
        if (briefInfoText != null)
        {
            return briefInfoText;
        }

        Transform child = transform.Find(DefaultBriefTextPath);
        if (child == null)
        {
            return null;
        }

        briefInfoText = child.GetComponent<TMP_Text>();
        return briefInfoText;
    }

    private TMP_Text ResolveDetailInfoText()
    {
        if (detailInfoText != null)
        {
            return detailInfoText;
        }

        Transform child = transform.Find(DefaultDetailTextPath);
        if (child == null)
        {
            return null;
        }

        detailInfoText = child.GetComponent<TMP_Text>();
        return detailInfoText;
    }

    private void ApplyDetailMessages(TMP_Text target)
    {
        if (target == null)
        {
            return;
        }

        target.text = string.Join("\n", detailMessages);
        target.ForceMeshUpdate();

        float maxHeight = Mathf.Max(0f, target.rectTransform.rect.height);
        while (detailMessages.Count > 0 && maxHeight > 0f && target.preferredHeight > maxHeight)
        {
            detailMessages.RemoveAt(0);
            target.text = string.Join("\n", detailMessages);
            target.ForceMeshUpdate();
        }
    }

    private void Awake()
    {
        ResolveBriefInfoText();
        ResolveDetailInfoText();
    }
}
