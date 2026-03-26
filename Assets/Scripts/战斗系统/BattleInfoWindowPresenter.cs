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

    private static readonly Color DefaultInfoTextColor = new Color32(160, 160, 160, 255);

    private readonly List<string> detailMessages = new List<string>();

    public void ShowMessage(string message)
    {
        string resolvedMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : message;

        TMP_Text briefTarget = briefInfoText != null ? briefInfoText : ResolveBriefInfoText();
        if (briefTarget != null)
        {
            briefTarget.color = DefaultInfoTextColor;
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

        target.enableWordWrapping = true;
        target.overflowMode = TextOverflowModes.Overflow;
        target.raycastTarget = false;
        target.color = DefaultInfoTextColor;
        target.text = BuildVisibleDetailText(target);
    }

    private string BuildVisibleDetailText(TMP_Text target)
    {
        if (detailMessages.Count == 0)
        {
            return string.Empty;
        }

        float maxHeight = Mathf.Max(0f, target.rectTransform.rect.height);
        if (maxHeight <= 0f)
        {
            return string.Join("\n", detailMessages);
        }

        string bestText = detailMessages[detailMessages.Count - 1];
        for (int startIndex = detailMessages.Count - 1; startIndex >= 0; startIndex--)
        {
            string candidate = string.Join("\n", detailMessages.GetRange(startIndex, detailMessages.Count - startIndex));
            target.text = candidate;
            target.ForceMeshUpdate();

            if (target.preferredHeight <= maxHeight)
            {
                bestText = candidate;
            }
            else
            {
                break;
            }
        }

        return bestText;
    }

    private void Awake()
    {
        ResolveBriefInfoText();
        ResolveDetailInfoText();
    }
}
