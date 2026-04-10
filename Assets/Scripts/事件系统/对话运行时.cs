using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
    private 主视角对话绑定 当前主视角绑定;
    private 副视角对话绑定 当前副视角绑定;
    private 屏幕火星特效 当前屏幕火星特效;
    private DialogueGroupDatabase.DialogueGroupEntry 当前对话组;
    private int 当前对话索引 = -1;

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

    public static void 继续当前对话()
    {
        instance?.AdvanceDialogue();
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
        HideAllViewsInScene();
        绑定屏幕火星特效();
        隐藏屏幕火星特效();

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
            BindEventTriggers(entry);
        }
    }

    private void UnbindAll()
    {
        事件触发监听项.Clear();
        上次事件状态.Clear();
        当前对话组 = null;
        当前对话索引 = -1;
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
            if (!上次事件状态.TryGetValue(item.事件ID, out bool previousValue))
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
        if (eventEntry == null || eventEntry.presentation == null || string.IsNullOrWhiteSpace(eventEntry.presentation.dialogueGroupId))
        {
            Debug.LogError($"对话运行时: 对话事件 '{eventEntry?.id ?? "<null>"}' 缺少表现配置。");
            return;
        }

        DialogueGroupDatabase groupDatabase = DialogueGroupDatabase.LoadDefault();
        if (groupDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueGroupDatabase。");
            return;
        }

        DialogueGroupDatabase.DialogueGroupEntry groupEntry = groupDatabase.FindEntry(eventEntry.presentation.dialogueGroupId);
        if (groupEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到对话组 '{eventEntry.presentation.dialogueGroupId}'。");
            return;
        }

        if (groupEntry.contentIds == null || groupEntry.contentIds.Count == 0)
        {
            Debug.LogError($"对话运行时: 对话组 '{groupEntry.id}' 没有内容ID。");
            return;
        }

        当前对话组 = groupEntry;
        当前对话索引 = 0;
        ShowCurrentDialogueEntry();
    }

    private void ShowOnMainBinding(主视角对话绑定 binding, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        ValidateBinding(binding.对话预制体, binding.立绘容器, binding.角色名字, binding.对话内容, binding.继续按钮, "主视角对话绑定");
        当前主视角绑定 = binding;
        ConfigureContinueButton(binding.继续按钮);
        Debug.Log($"对话运行时: 显示主视角对话, root={binding.gameObject.name}, id={binding.gameObject.GetInstanceID()}, continue={binding.继续按钮.name}, continueId={binding.继续按钮.GetInstanceID()}");
        binding.gameObject.SetActive(true);
        ApplyDialogueToBinding(binding.立绘容器, binding.角色名字, binding.对话内容, roleName, contentEntry);
        显示屏幕火星特效();
    }

    private void ShowOnSecondaryBinding(副视角对话绑定 binding, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        ValidateBinding(binding.对话预制体, binding.立绘容器, binding.角色名字, binding.对话内容, binding.继续按钮, "副视角对话绑定");
        当前副视角绑定 = binding;
        ConfigureContinueButton(binding.继续按钮);
        Debug.Log($"对话运行时: 显示副视角对话, root={binding.gameObject.name}, id={binding.gameObject.GetInstanceID()}, continue={binding.继续按钮.name}, continueId={binding.继续按钮.GetInstanceID()}");
        binding.gameObject.SetActive(true);
        ApplyDialogueToBinding(binding.立绘容器, binding.角色名字, binding.对话内容, roleName, contentEntry);
        显示屏幕火星特效();
    }

    private static void ValidateBinding(
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

    private void ConfigureContinueButton(GameObject continueButtonObject)
    {
        Button button = continueButtonObject.GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"对话运行时: 对象 '{continueButtonObject.name}' 缺少 Button 组件。");
            throw new InvalidOperationException(continueButtonObject.name);
        }

        button.onClick.RemoveListener(继续当前对话);
        button.onClick.AddListener(继续当前对话);
        Debug.Log($"对话运行时: 已绑定继续按钮, name={continueButtonObject.name}, id={continueButtonObject.GetInstanceID()}");
    }

    private static void ApplyDialogueToBinding(GameObject portraitContainer, GameObject roleNameObject, GameObject contentObject, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
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

        ClearPortraitContainer(portraitContainer);
        UnityEngine.Object.Instantiate(contentEntry.portraitPrefab, portraitContainer.transform, false);
        roleNameText.text = roleName;
        contentText.text = contentEntry.content ?? string.Empty;
    }

    private void AdvanceDialogue()
    {
        if (当前对话组 == null)
        {
            Debug.LogError("对话运行时: 当前没有正在播放的对话组。");
            return;
        }

        当前对话索引++;
        if (当前对话索引 >= 当前对话组.contentIds.Count)
        {
            CloseDialogue();
            return;
        }

        ShowCurrentDialogueEntry();
    }

    private void CloseDialogue()
    {
        Debug.Log("对话运行时: 点击继续按钮，执行关闭当前对话。");
        HideCurrentViews();
        当前对话组 = null;
        当前对话索引 = -1;
        隐藏屏幕火星特效();
    }

    private void HideCurrentViews()
    {
        if (当前主视角绑定 != null)
        {
            Debug.Log($"对话运行时: 隐藏主视角对话, root={当前主视角绑定.gameObject.name}, id={当前主视角绑定.gameObject.GetInstanceID()}");
            当前主视角绑定.gameObject.SetActive(false);
        }

        if (当前副视角绑定 != null)
        {
            Debug.Log($"对话运行时: 隐藏副视角对话, root={当前副视角绑定.gameObject.name}, id={当前副视角绑定.gameObject.GetInstanceID()}");
            当前副视角绑定.gameObject.SetActive(false);
        }
    }

    private void HideAllViewsInScene()
    {
        主视角对话绑定[] mainBindings = FindObjectsOfType<主视角对话绑定>(true);
        for (int i = 0; i < mainBindings.Length; i++)
        {
            if (mainBindings[i] != null)
            {
                mainBindings[i].gameObject.SetActive(false);
            }
        }

        副视角对话绑定[] secondaryBindings = FindObjectsOfType<副视角对话绑定>(true);
        for (int i = 0; i < secondaryBindings.Length; i++)
        {
            if (secondaryBindings[i] != null)
            {
                secondaryBindings[i].gameObject.SetActive(false);
            }
        }

        当前主视角绑定 = null;
        当前副视角绑定 = null;
    }

    private void ShowCurrentDialogueEntry()
    {
        if (当前对话组 == null)
        {
            Debug.LogError("对话运行时: 当前对话组为空。");
            return;
        }

        if (当前对话组.contentIds == null || 当前对话索引 < 0 || 当前对话索引 >= 当前对话组.contentIds.Count)
        {
            Debug.LogError($"对话运行时: 对话组 '{当前对话组.id}' 的索引 '{当前对话索引}' 无效。");
            return;
        }

        string contentId = 当前对话组.contentIds[当前对话索引];
        if (string.IsNullOrWhiteSpace(contentId))
        {
            Debug.LogError($"对话运行时: 对话组 '{当前对话组.id}' 的第 {当前对话索引 + 1} 条内容ID为空。");
            return;
        }

        DialogueContentDatabase contentDatabase = DialogueContentDatabase.LoadDefault();
        if (contentDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueContentDatabase。");
            return;
        }

        DialogueContentDatabase.DialogueContentEntry contentEntry = contentDatabase.FindEntry(contentId);
        if (contentEntry == null)
        {
            Debug.LogError($"对话运行时: 找不到对话内容 '{contentId}'。");
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

        HideCurrentViews();

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

    private static void ClearPortraitContainer(GameObject portraitContainer)
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

    private void 绑定屏幕火星特效()
    {
        当前屏幕火星特效 = FindObjectOfType<屏幕火星特效>(true);
    }

    private void 显示屏幕火星特效()
    {
        if (当前屏幕火星特效 == null)
        {
            绑定屏幕火星特效();
        }

        if (当前屏幕火星特效 != null)
        {
            当前屏幕火星特效.显示特效();
        }
    }

    private void 隐藏屏幕火星特效()
    {
        if (当前屏幕火星特效 == null)
        {
            绑定屏幕火星特效();
        }

        if (当前屏幕火星特效 != null)
        {
            当前屏幕火星特效.隐藏特效();
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
