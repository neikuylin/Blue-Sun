using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 对话运行时 : MonoBehaviour
{
    private sealed class 触发监听项
    {
        public string 对话事件ID = string.Empty;
        public string 事件ID = string.Empty;
        public bool 目标值;
    }

    private static 对话运行时 instance;

    private readonly List<Action> unbindActions = new List<Action>();
    private readonly List<触发监听项> 事件触发监听项 = new List<触发监听项>();
    private readonly Dictionary<string, bool> 上次事件状态 = new Dictionary<string, bool>(StringComparer.Ordinal);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
        {
            return;
        }

        GameObject go = new GameObject("对话运行时");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<对话运行时>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        RebindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindAll();
    }

    private void Update()
    {
        UpdateEventTriggers();
    }

    public static void 关闭当前对话()
    {
        instance?.CloseDialogue();
    }

    public static void 尝试触发对话事件(string 对话事件ID)
    {
        instance?.TryTriggerDialogueEvent(对话事件ID);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebindScene();
    }

    private void RebindScene()
    {
        UnbindAll();
        HideAllViews();

        DialogueEventDatabase eventDatabase = DialogueEventDatabase.LoadDefault();
        if (eventDatabase == null || eventDatabase.Entries == null)
        {
            return;
        }

        for (int i = 0; i < eventDatabase.Entries.Count; i++)
        {
            DialogueEventDatabase.DialogueEventEntry entry = eventDatabase.Entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
            {
                continue;
            }

            DialogueEventDatabase.EnsureEntry(entry);
            BindButtons(entry);
            BindEventTriggers(entry);
        }
    }

    private void UnbindAll()
    {
        for (int i = 0; i < unbindActions.Count; i++)
        {
            unbindActions[i]?.Invoke();
        }

        unbindActions.Clear();
        事件触发监听项.Clear();
        上次事件状态.Clear();
    }

    private void BindButtons(DialogueEventDatabase.DialogueEventEntry entry)
    {
        if (entry == null || entry.trigger == null || entry.trigger.buttons == null)
        {
            return;
        }

        for (int i = 0; i < entry.trigger.buttons.Count; i++)
        {
            GameObject buttonObject = entry.trigger.buttons[i];
            if (buttonObject == null)
            {
                continue;
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button == null)
            {
                Debug.LogError($"对话运行时: 对话事件 '{entry.id}' 绑定的对象 '{buttonObject.name}' 缺少 Button 组件。");
                continue;
            }

            string capturedEventId = entry.id;
            UnityAction onClick = () => TryTriggerDialogueEvent(capturedEventId);
            button.onClick.AddListener(onClick);
            unbindActions.Add(() =>
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(onClick);
                }
            });
        }
    }

    private void BindEventTriggers(DialogueEventDatabase.DialogueEventEntry entry)
    {
        if (entry == null || entry.trigger == null || entry.trigger.eventIds == null)
        {
            return;
        }

        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        for (int i = 0; i < entry.trigger.eventIds.Count; i++)
        {
            DialogueEventDatabase.TriggerEventEntry triggerEntry = entry.trigger.eventIds[i];
            if (triggerEntry == null || string.IsNullOrWhiteSpace(triggerEntry.eventId))
            {
                continue;
            }

            string eventId = triggerEntry.eventId.Trim();
            事件触发监听项.Add(new 触发监听项
            {
                对话事件ID = entry.id,
                事件ID = eventId,
                目标值 = triggerEntry.expectedValue
            });

            上次事件状态[eventId] = ResolveEventState(eventDatabase, eventId);
        }
    }

    private void UpdateEventTriggers()
    {
        if (事件触发监听项.Count == 0)
        {
            return;
        }

        EventDatabase eventDatabase = EventDatabase.LoadDefault();
        for (int i = 0; i < 事件触发监听项.Count; i++)
        {
            触发监听项 item = 事件触发监听项[i];
            if (item == null || string.IsNullOrWhiteSpace(item.事件ID))
            {
                continue;
            }

            bool currentValue = ResolveEventState(eventDatabase, item.事件ID);
            bool previousValue;
            if (!上次事件状态.TryGetValue(item.事件ID, out previousValue))
            {
                上次事件状态[item.事件ID] = currentValue;
                continue;
            }

            if (previousValue == currentValue)
            {
                continue;
            }

            上次事件状态[item.事件ID] = currentValue;
            if (currentValue == item.目标值)
            {
                TryTriggerDialogueEvent(item.对话事件ID);
            }
        }
    }

    private void TryTriggerDialogueEvent(string dialogueEventId)
    {
        if (string.IsNullOrWhiteSpace(dialogueEventId))
        {
            Debug.LogError("对话运行时: 对话事件ID为空。");
            return;
        }

        DialogueEventDatabase eventDatabase = DialogueEventDatabase.LoadDefault();
        if (eventDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueEventDatabase。");
            return;
        }

        DialogueEventDatabase.DialogueEventEntry eventEntry = eventDatabase.FindEntry(dialogueEventId);
        if (eventEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到对话事件 '{dialogueEventId}'。");
            return;
        }

        if (!EvaluateConditions(eventEntry))
        {
            return;
        }

        ShowDialogue(eventEntry);
    }

    private bool EvaluateConditions(DialogueEventDatabase.DialogueEventEntry eventEntry)
    {
        if (eventEntry == null || eventEntry.condition == null || eventEntry.condition.eventIds == null)
        {
            return true;
        }

        DialogueConditionDatabase conditionDatabase = DialogueConditionDatabase.LoadDefault();
        if (conditionDatabase == null && eventEntry.condition.eventIds.Count > 0)
        {
            Debug.LogError($"对话运行时: 对话事件 '{eventEntry.id}' 需要 DialogueConditionDatabase，但资源缺失。");
            return false;
        }

        for (int i = 0; i < eventEntry.condition.eventIds.Count; i++)
        {
            DialogueEventDatabase.ConditionEntry conditionEntry = eventEntry.condition.eventIds[i];
            if (conditionEntry == null || string.IsNullOrWhiteSpace(conditionEntry.eventId))
            {
                continue;
            }

            DialogueConditionDatabase.ConditionDefinitionEntry definition = conditionDatabase.FindEntry(conditionEntry.eventId);
            if (definition == null)
            {
                Debug.LogError($"对话运行时: 找不到条件定义 '{conditionEntry.eventId}'。");
                return false;
            }

            if (definition.number != conditionEntry.number)
            {
                return false;
            }
        }

        return true;
    }

    private void ShowDialogue(DialogueEventDatabase.DialogueEventEntry eventEntry)
    {
        if (eventEntry == null || eventEntry.presentation == null || string.IsNullOrWhiteSpace(eventEntry.presentation.dialogueContentId))
        {
            Debug.LogError($"对话运行时: 对话事件 '{eventEntry?.id ?? "<null>"}' 缺少表现配置。");
            return;
        }

        DialogueContentDatabase contentDatabase = DialogueContentDatabase.LoadDefault();
        if (contentDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueContentDatabase。");
            return;
        }

        DialogueContentDatabase.DialogueContentEntry contentEntry = contentDatabase.FindEntry(eventEntry.presentation.dialogueContentId);
        if (contentEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到对话内容 '{eventEntry.presentation.dialogueContentId}'。");
            return;
        }

        DialogueRoleNameDatabase roleNameDatabase = DialogueRoleNameDatabase.LoadDefault();
        if (roleNameDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueRoleNameDatabase。");
            return;
        }

        DialogueRoleNameDatabase.RoleNameEntry roleNameEntry = roleNameDatabase.FindEntry(contentEntry.roleNameId);
        if (roleNameEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到角色名字 '{contentEntry.roleNameId}'。");
            return;
        }

        HideAllViews();

        if (contentEntry.viewSide == DialogueContentDatabase.DialogueViewSide.Main)
        {
            主视角对话绑定 binding = FindObjectOfType<主视角对话绑定>(true);
            if (binding == null)
            {
                Debug.LogError("对话运行时: 场景中缺少 主视角对话绑定。");
                return;
            }

            ShowOnMainBinding(binding, roleNameEntry.id, contentEntry);
            return;
        }

        副视角对话绑定 secondaryBinding = FindObjectOfType<副视角对话绑定>(true);
        if (secondaryBinding == null)
        {
            Debug.LogError("对话运行时: 场景中缺少 副视角对话绑定。");
            return;
        }

        ShowOnSecondaryBinding(secondaryBinding, roleNameEntry.id, contentEntry);
    }

    private void ShowOnMainBinding(主视角对话绑定 binding, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        ValidateBinding(binding.对话预制体, binding.立绘容器, binding.角色名字, binding.对话内容, "主视角对话绑定");
        binding.gameObject.SetActive(true);
        ApplyDialogueToBinding(binding.立绘容器, binding.角色名字, binding.对话内容, roleName, contentEntry);
    }

    private void ShowOnSecondaryBinding(副视角对话绑定 binding, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        ValidateBinding(binding.对话预制体, binding.立绘容器, binding.角色名字, binding.对话内容, "副视角对话绑定");
        binding.gameObject.SetActive(true);
        ApplyDialogueToBinding(binding.立绘容器, binding.角色名字, binding.对话内容, roleName, contentEntry);
    }

    private static void ValidateBinding(GameObject prefab, GameObject portraitContainer, GameObject roleNameObject, GameObject contentObject, string bindingName)
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
    }

    private static void ApplyDialogueToBinding(GameObject portraitContainer, GameObject roleNameObject, GameObject contentObject, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        Image portraitImage = portraitContainer.GetComponent<Image>();
        if (portraitImage == null)
        {
            Debug.LogError($"对话运行时: 对象 '{portraitContainer.name}' 缺少 Image 组件。");
            throw new InvalidOperationException(portraitContainer.name);
        }

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

        portraitImage.sprite = contentEntry.portraitSprite2D;
        roleNameText.text = roleName;
        contentText.text = contentEntry.content ?? string.Empty;
    }

    private void CloseDialogue()
    {
        HideAllViews();
    }

    private void HideAllViews()
    {
        主视角对话绑定 mainBinding = FindObjectOfType<主视角对话绑定>(true);
        if (mainBinding != null)
        {
            mainBinding.gameObject.SetActive(false);
        }

        副视角对话绑定 secondaryBinding = FindObjectOfType<副视角对话绑定>(true);
        if (secondaryBinding != null)
        {
            secondaryBinding.gameObject.SetActive(false);
        }
    }

    private static bool ResolveEventState(EventDatabase database, string eventId)
    {
        if (database == null || string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        EventDatabase.EventEntry entry = database.FindEntry(eventId);
        return entry != null && entry.enabled;
    }
}
