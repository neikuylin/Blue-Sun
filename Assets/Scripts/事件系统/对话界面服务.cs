using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class 对话界面服务
{
    public static void 校验对话绑定(
        GameObject prefab,
        GameObject portraitContainer,
        GameObject roleNameObject,
        GameObject contentObject,
        GameObject continueButtonObject,
        string bindingName)
    {
        if (prefab == null)
        {
            Debug.LogError($"{bindingName}: 对话预制体未绑定。");
            throw new InvalidOperationException(bindingName);
        }

        if (portraitContainer == null)
        {
            Debug.LogError($"{bindingName}: 立绘容器未绑定。");
            throw new InvalidOperationException(bindingName);
        }

        if (roleNameObject == null)
        {
            Debug.LogError($"{bindingName}: 角色名字未绑定。");
            throw new InvalidOperationException(bindingName);
        }

        if (contentObject == null)
        {
            Debug.LogError($"{bindingName}: 对话内容未绑定。");
            throw new InvalidOperationException(bindingName);
        }

        if (continueButtonObject == null)
        {
            Debug.LogError($"{bindingName}: 继续按钮未绑定。");
            throw new InvalidOperationException(bindingName);
        }
    }

    public static void 配置交互(
        List<GameObject> 已生成交互按钮,
        GameObject continueButtonObject,
        GameObject interactionContainerObject,
        GameObject interactionButtonTemplateObject,
        List<GameObject> interactionSlotObjects,
        DialogueContentDatabase.DialogueContentEntry contentEntry,
        Action 清理已生成交互按钮,
        Action<DialogueContentDatabase.InteractionEntry> handleInteraction,
        Action 继续当前对话)
    {
        DialogueContentDatabase.EnsureEntry(contentEntry);
        清理已生成交互按钮?.Invoke();

        bool hasInteractions = 存在可见交互(contentEntry);
        设置继续按钮显隐(continueButtonObject, !hasInteractions);
        设置交互容器显隐(interactionContainerObject, hasInteractions);

        if (!hasInteractions)
        {
            配置继续按钮(continueButtonObject, 继续当前对话);
            return;
        }

        校验交互绑定(interactionContainerObject, interactionButtonTemplateObject, interactionSlotObjects, contentEntry);
        int visibleInteractionIndex = 0;
        for (int i = 0; i < contentEntry.interactions.Count; i++)
        {
            DialogueContentDatabase.InteractionEntry interaction = contentEntry.interactions[i];
            if (interaction == null || string.IsNullOrWhiteSpace(interaction.buttonText))
            {
                continue;
            }

            创建交互按钮(已生成交互按钮, interactionSlotObjects[visibleInteractionIndex], interactionButtonTemplateObject, interaction, handleInteraction);
            visibleInteractionIndex++;
        }
    }

    public static void 应用对话内容(GameObject portraitContainer, GameObject roleNameObject, GameObject contentObject, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        TMP_Text roleNameText = roleNameObject.GetComponent<TMP_Text>();
        if (roleNameText == null)
        {
            Debug.LogError($"对话运行时: 对象 '{roleNameObject.name}' 缺少 TMP_Text 组件。");
            throw new InvalidOperationException(roleNameObject.name);
        }

        TMP_Text contentText = contentObject.GetComponent<TMP_Text>();
        if (contentText == null)
        {
            Debug.LogError($"对话运行时: 对象 '{contentObject.name}' 缺少 TMP_Text 组件。");
            throw new InvalidOperationException(contentObject.name);
        }

        if (contentEntry.portraitPrefab == null)
        {
            Debug.LogError("对话运行时: 立绘Prefab未绑定。");
            throw new InvalidOperationException("立绘Prefab");
        }

        清空立绘容器(portraitContainer);
        UnityEngine.Object.Instantiate(contentEntry.portraitPrefab, portraitContainer.transform, false);
        roleNameText.text = roleName;
        contentText.text = contentEntry.content ?? string.Empty;
    }

    public static 对话框持续显示 解析持续显示(GameObject rootObject, string bindingName)
    {
        对话框持续显示 持续显示 = rootObject.GetComponent<对话框持续显示>();
        if (持续显示 == null)
        {
            Debug.LogError($"{bindingName}: 缺少 对话框持续显示。");
            throw new InvalidOperationException(bindingName);
        }

        return 持续显示;
    }

    public static void 设置按钮可交互(GameObject buttonObject, bool interactable)
    {
        if (buttonObject == null)
        {
            return;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    public static void 设置容器可交互(GameObject containerObject, bool interactable)
    {
        if (containerObject == null)
        {
            return;
        }

        CanvasGroup canvasGroup = containerObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = containerObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.interactable = interactable;
        canvasGroup.blocksRaycasts = interactable;
    }

    public static void 清空立绘容器(GameObject portraitContainer)
    {
        if (portraitContainer == null)
        {
            return;
        }

        Transform portraitTransform = portraitContainer.transform;
        for (int i = portraitTransform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.Destroy(portraitTransform.GetChild(i).gameObject);
        }
    }

    public static void 设置继续按钮显隐(GameObject continueButtonObject, bool visible)
    {
        if (continueButtonObject != null)
        {
            continueButtonObject.SetActive(visible);
        }
    }

    public static void 设置交互容器显隐(GameObject interactionContainerObject, bool visible)
    {
        if (interactionContainerObject != null)
        {
            interactionContainerObject.SetActive(visible);
        }
    }

    private static bool 存在可见交互(DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        if (contentEntry == null || contentEntry.interactions == null)
        {
            return false;
        }

        for (int i = 0; i < contentEntry.interactions.Count; i++)
        {
            DialogueContentDatabase.InteractionEntry interaction = contentEntry.interactions[i];
            if (interaction != null && !string.IsNullOrWhiteSpace(interaction.buttonText))
            {
                return true;
            }
        }

        return false;
    }

    private static void 校验交互绑定(
        GameObject interactionContainerObject,
        GameObject interactionButtonTemplateObject,
        List<GameObject> interactionSlotObjects,
        DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        if (interactionContainerObject == null)
        {
            Debug.LogError("对话运行时: 缺少交互按钮容器绑定。");
            throw new InvalidOperationException("交互按钮容器");
        }

        if (contentEntry == null || contentEntry.interactions == null)
        {
            return;
        }

        if (interactionButtonTemplateObject == null)
        {
            Debug.LogError("对话运行时: 缺少交互按钮模板绑定。");
            throw new InvalidOperationException("交互按钮模板");
        }

        if (interactionSlotObjects == null || interactionSlotObjects.Count == 0)
        {
            Debug.LogError("对话运行时: 缺少交互按钮槽位绑定。");
            throw new InvalidOperationException("交互按钮槽位");
        }

        int visibleInteractionCount = 0;
        for (int i = 0; i < contentEntry.interactions.Count; i++)
        {
            DialogueContentDatabase.InteractionEntry interaction = contentEntry.interactions[i];
            if (interaction == null || string.IsNullOrWhiteSpace(interaction.buttonText))
            {
                continue;
            }

            visibleInteractionCount++;
            if (interaction.interactionType == DialogueContentDatabase.InteractionType.Button &&
                string.IsNullOrWhiteSpace(interaction.identifierId))
            {
                Debug.LogError($"对话运行时: 按钮交互 '{interaction.buttonText}' 缺少标识ID。");
                throw new InvalidOperationException("标识ID");
            }
        }

        if (visibleInteractionCount > interactionSlotObjects.Count)
        {
            Debug.LogError($"对话运行时: 当前对话需要 {visibleInteractionCount} 个交互按钮槽位，但只绑定了 {interactionSlotObjects.Count} 个。");
            throw new InvalidOperationException("交互按钮槽位数量");
        }

        for (int i = 0; i < visibleInteractionCount; i++)
        {
            if (interactionSlotObjects[i] != null)
            {
                continue;
            }

            Debug.LogError($"对话运行时: 第 {i + 1} 个交互按钮槽位未绑定。");
            throw new InvalidOperationException("交互按钮槽位");
        }
    }

    private static void 创建交互按钮(
        List<GameObject> 已生成交互按钮,
        GameObject interactionSlotObject,
        GameObject interactionButtonTemplateObject,
        DialogueContentDatabase.InteractionEntry interaction,
        Action<DialogueContentDatabase.InteractionEntry> handleInteraction)
    {
        if (interaction == null)
        {
            return;
        }

        GameObject buttonInstance = UnityEngine.Object.Instantiate(interactionButtonTemplateObject, interactionSlotObject.transform, false);
        buttonInstance.name = $"交互按钮_{interaction.buttonText}";
        buttonInstance.SetActive(true);
        已生成交互按钮?.Add(buttonInstance);

        应用交互文本(buttonInstance, interaction.buttonText);
        绑定交互点击(buttonInstance, interaction, handleInteraction);
    }

    private static void 应用交互文本(GameObject buttonInstance, string buttonTextValue)
    {
        TMP_Text buttonText = buttonInstance.GetComponentInChildren<TMP_Text>(true);
        if (buttonText != null)
        {
            buttonText.text = buttonTextValue;
        }
    }

    private static void 绑定交互点击(GameObject buttonInstance, DialogueContentDatabase.InteractionEntry interaction, Action<DialogueContentDatabase.InteractionEntry> handleInteraction)
    {
        Button button = buttonInstance.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"对话运行时: 交互按钮模板 '{buttonInstance.name}' 缺少 Button 组件。");
            throw new InvalidOperationException(buttonInstance.name);
        }

        DialogueContentDatabase.InteractionEntry capturedInteraction = interaction;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate { handleInteraction?.Invoke(capturedInteraction); });
    }

    private static void 配置继续按钮(GameObject continueButtonObject, Action continueDialogue)
    {
        Button button = continueButtonObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"对话运行时: 对象 '{continueButtonObject.name}' 缺少 Button 组件。");
            throw new InvalidOperationException(continueButtonObject.name);
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(delegate { continueDialogue?.Invoke(); });
    }
}
