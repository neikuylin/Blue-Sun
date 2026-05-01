using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class 对话运行时 : MonoBehaviour
{
    private enum 对话显示视角
    {
        无,
        主视角,
        副视角
    }

    private static 对话运行时 instance;
    private 主视角对话绑定 当前主视角绑定;
    private 副视角对话绑定 当前副视角绑定;
    private 屏幕火星特效 当前屏幕火星特效;
    private DialogueGroupDatabase.DialogueGroupEntry 当前对话组;
    private int 当前对话索引 = -1;
    private 对话显示视角 当前显示视角 = 对话显示视角.无;

    private readonly List<对话事件服务.触发监听项> 事件触发监听项 = new List<对话事件服务.触发监听项>();
    private readonly Dictionary<string, bool> 上次事件状态 = new Dictionary<string, bool>(StringComparer.Ordinal);
    private readonly List<GameObject> 已生成交互按钮 = new List<GameObject>();
    private GameObject 当前打开标识目标;
    private Button 当前标识关闭按钮;
    private AudioSource 对话语音播放器;

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
        EnsureDialogueVoiceAudioSource();
        RebindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopDialogueVoice();
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
        EnsureDialogueVoiceAudioSource();
        RebindScene();
    }

    private void RebindScene()
    {
        UnbindAll();
        HideAllViewsInScene();
        绑定屏幕火星特效();
        if (当前屏幕火星特效 != null)
        {
            隐藏屏幕火星特效();
        }

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

            BindEventTriggers(entry);
        }
    }

    private void UnbindAll()
    {
        StopDialogueVoice();
        关闭当前标识内容();
        事件触发监听项.Clear();
        上次事件状态.Clear();
        当前对话组 = null;
        当前对话索引 = -1;
        当前显示视角 = 对话显示视角.无;
        ClearGeneratedInteractionButtons();
    }

    private void BindEventTriggers(DialogueEventDatabase.DialogueEventEntry entry)
    {
        对话事件服务.绑定事件触发(entry, 事件触发监听项, 上次事件状态);
    }

    private void UpdateEventTriggers()
    {
        对话事件服务.更新事件触发(事件触发监听项, 上次事件状态, TryTriggerDialogueEvent);
    }

    private void TryTriggerDialogueEvent(string dialogueEventId)
    {
        DialogueEventDatabase.DialogueEventEntry eventEntry = 对话事件服务.获取对话事件(dialogueEventId);
        if (eventEntry == null)
        {
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
        return 对话事件服务.满足条件(eventEntry);
    }

    private void ShowDialogue(DialogueEventDatabase.DialogueEventEntry eventEntry)
    {
        DialogueGroupDatabase.DialogueGroupEntry groupEntry = 对话事件服务.获取对话组(eventEntry);
        if (groupEntry == null)
        {
            return;
        }

        当前对话组 = groupEntry;
        当前对话索引 = 0;
        ShowCurrentDialogueEntry();
    }

    private void ShowOnMainBinding(主视角对话绑定 binding, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        对话界面服务.校验对话绑定(binding.对话预制体, binding.立绘容器, binding.角色名字, binding.对话内容, binding.继续按钮, "主视角对话绑定");
        当前主视角绑定 = binding;
        对话框持续显示 持续显示 = 对话界面服务.解析持续显示(binding.gameObject, "主视角对话绑定");
        持续显示.打开对话框();
        当前显示视角 = 对话显示视角.主视角;
        对话界面服务.应用对话内容(binding.立绘容器, binding.角色名字, binding.对话内容, roleName, contentEntry);
        对话界面服务.配置交互(已生成交互按钮, binding.继续按钮, binding.交互按钮容器, binding.交互按钮模板, binding.交互按钮槽位, contentEntry, ClearGeneratedInteractionButtons, HandleInteraction, 继续当前对话);
        显示屏幕火星特效();
    }

    private void ShowOnSecondaryBinding(副视角对话绑定 binding, string roleName, DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        对话界面服务.校验对话绑定(binding.对话预制体, binding.立绘容器, binding.角色名字, binding.对话内容, binding.继续按钮, "副视角对话绑定");
        当前副视角绑定 = binding;
        对话框持续显示 持续显示 = 对话界面服务.解析持续显示(binding.gameObject, "副视角对话绑定");
        持续显示.打开对话框();
        当前显示视角 = 对话显示视角.副视角;
        对话界面服务.应用对话内容(binding.立绘容器, binding.角色名字, binding.对话内容, roleName, contentEntry);
        对话界面服务.配置交互(已生成交互按钮, binding.继续按钮, binding.交互按钮容器, binding.交互按钮模板, binding.交互按钮槽位, contentEntry, ClearGeneratedInteractionButtons, HandleInteraction, 继续当前对话);
        显示屏幕火星特效();
    }

    private void HandleInteraction(DialogueContentDatabase.InteractionEntry interaction)
    {
        if (interaction == null)
        {
            return;
        }

        switch (interaction.interactionType)
        {
            case DialogueContentDatabase.InteractionType.Button:
                执行按钮交互(interaction);
                break;
            case DialogueContentDatabase.InteractionType.JumpToDialogueGroup:
                执行对话跳跃(interaction);
                break;
            case DialogueContentDatabase.InteractionType.ContinueDialogue:
                AdvanceDialogue();
                break;
            default:
                Debug.LogWarning($"对话运行时: 未处理的交互类型 '{interaction.interactionType}'。");
                break;
        }
    }

    private void 执行按钮交互(DialogueContentDatabase.InteractionEntry interaction)
    {
        string identifierId = interaction != null ? interaction.identifierId : string.Empty;
        if (string.IsNullOrWhiteSpace(identifierId))
        {
            Debug.LogError("对话运行时: 按钮交互缺少标识ID。");
            return;
        }

        DialogueInteractionIdentifierBinding binding = ResolveIdentifierBinding(identifierId);
        if (binding == null || binding.目标对象 == null)
        {
            Debug.LogError($"对话运行时: 找不到标识ID '{identifierId}' 对应的目标对象。");
            return;
        }

        if (binding.关闭按钮 == null)
        {
            Debug.LogError($"对话运行时: 标识ID '{identifierId}' 缺少关闭按钮绑定。");
            return;
        }

        Button closeButton = binding.关闭按钮.GetComponent<Button>();
        if (closeButton == null)
        {
            Debug.LogError($"对话运行时: 标识ID '{identifierId}' 的关闭按钮对象 '{binding.关闭按钮.name}' 缺少 Button 组件。");
            return;
        }

        关闭当前标识内容();

        binding.目标对象.SetActive(true);
        当前打开标识目标 = binding.目标对象;
        当前标识关闭按钮 = closeButton;
        当前标识关闭按钮.onClick.AddListener(关闭当前标识内容);
        SetCurrentDialogueControlsInteractable(false);
    }

    private void 执行对话跳跃(DialogueContentDatabase.InteractionEntry interaction)
    {
        string targetDialogueGroupId = interaction != null ? interaction.targetDialogueGroupId : string.Empty;
        if (string.IsNullOrWhiteSpace(targetDialogueGroupId))
        {
            Debug.LogError("对话运行时: 对话跳跃缺少目标对话组ID。");
            return;
        }

        DialogueGroupDatabase groupDatabase = DialogueGroupDatabase.LoadDefault();
        if (groupDatabase == null)
        {
            Debug.LogError("对话运行时: 缺少 DialogueGroupDatabase。");
            return;
        }

        DialogueGroupDatabase.DialogueGroupEntry targetGroup = groupDatabase.FindEntry(targetDialogueGroupId);
        if (targetGroup == null || targetGroup.contentIds == null || targetGroup.contentIds.Count == 0)
        {
            Debug.LogError($"对话运行时: 找不到目标对话组 '{targetDialogueGroupId}'。");
            return;
        }

        当前对话组 = targetGroup;
        当前对话索引 = 0;
        ShowCurrentDialogueEntry();
    }


    private void AdvanceDialogue()
    {
        if (当前对话组 == null)
        {
            Debug.LogError("对话运行时: 当前没有正在播放的对话组。");
            return;
        }

        StopDialogueVoice();
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
        StopDialogueVoice();
        HideCurrentViews();
        当前对话组 = null;
        当前对话索引 = -1;
        隐藏屏幕火星特效();
    }

    private void HideCurrentViews()
    {
        StopDialogueVoice();
        关闭当前标识内容();
        ClearGeneratedInteractionButtons();

        if (当前主视角绑定 != null)
        {
            对话界面服务.设置交互容器显隐(当前主视角绑定.交互按钮容器, false);
            对话界面服务.解析持续显示(当前主视角绑定.gameObject, "主视角对话绑定").关闭对话框();
        }

        if (当前副视角绑定 != null)
        {
            对话界面服务.设置交互容器显隐(当前副视角绑定.交互按钮容器, false);
            对话界面服务.解析持续显示(当前副视角绑定.gameObject, "副视角对话绑定").关闭对话框();
        }

        当前显示视角 = 对话显示视角.无;
    }

    private void HideAllViewsInScene()
    {
        StopDialogueVoice();
        关闭当前标识内容();
        ClearGeneratedInteractionButtons();

        主视角对话绑定[] mainBindings = FindObjectsOfType<主视角对话绑定>(true);
        for (int i = 0; i < mainBindings.Length; i++)
        {
            if (mainBindings[i] != null)
            {
                对话界面服务.设置交互容器显隐(mainBindings[i].交互按钮容器, false);
                对话界面服务.解析持续显示(mainBindings[i].gameObject, "主视角对话绑定").关闭对话框();
            }
        }

        副视角对话绑定[] secondaryBindings = FindObjectsOfType<副视角对话绑定>(true);
        for (int i = 0; i < secondaryBindings.Length; i++)
        {
            if (secondaryBindings[i] != null)
            {
                对话界面服务.设置交互容器显隐(secondaryBindings[i].交互按钮容器, false);
                对话界面服务.解析持续显示(secondaryBindings[i].gameObject, "副视角对话绑定").关闭对话框();
            }
        }

        当前主视角绑定 = null;
        当前副视角绑定 = null;
        当前显示视角 = 对话显示视角.无;
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

        DialogueContentDatabase.EnsureEntry(contentEntry);

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

        if (contentEntry.viewSide == DialogueContentDatabase.DialogueViewSide.Main)
        {
            主视角对话绑定 binding = FindObjectOfType<主视角对话绑定>(true);
            if (binding == null)
            {
                Debug.LogError("对话运行时: 场景中缺少 主视角对话绑定。");
                return;
            }

            if (当前显示视角 == 对话显示视角.副视角)
            {
                HideCurrentViews();
            }

            ShowOnMainBinding(binding, roleNameEntry.id, contentEntry);
            PlayDialogueVoice(contentEntry);
            return;
        }

        副视角对话绑定 secondaryBinding = FindObjectOfType<副视角对话绑定>(true);
        if (secondaryBinding == null)
        {
            Debug.LogError("对话运行时: 场景中缺少 副视角对话绑定。");
            return;
        }

        if (当前显示视角 == 对话显示视角.主视角)
        {
            HideCurrentViews();
        }

        ShowOnSecondaryBinding(secondaryBinding, roleNameEntry.id, contentEntry);
        PlayDialogueVoice(contentEntry);
    }

    private GameObject ResolveIdentifierTarget(string identifierId)
    {
        DialogueInteractionIdentifierBinding binding = ResolveIdentifierBinding(identifierId);
        return binding != null ? binding.目标对象 : null;
    }

    private DialogueInteractionIdentifierBinding ResolveIdentifierBinding(string identifierId)
    {
        if (string.IsNullOrWhiteSpace(identifierId))
        {
            return null;
        }

        DialogueInteractionIdentifierBinding binding = ResolveIdentifierBindingFromBinding(当前主视角绑定, identifierId);
        if (binding != null)
        {
            return binding;
        }

        binding = ResolveIdentifierBindingFromBinding(当前副视角绑定, identifierId);
        if (binding != null)
        {
            return binding;
        }

        主视角对话绑定[] mainBindings = FindObjectsOfType<主视角对话绑定>(true);
        for (int i = 0; i < mainBindings.Length; i++)
        {
            binding = ResolveIdentifierBindingFromBinding(mainBindings[i], identifierId);
            if (binding != null)
            {
                return binding;
            }
        }

        副视角对话绑定[] secondaryBindings = FindObjectsOfType<副视角对话绑定>(true);
        for (int i = 0; i < secondaryBindings.Length; i++)
        {
            binding = ResolveIdentifierBindingFromBinding(secondaryBindings[i], identifierId);
            if (binding != null)
            {
                return binding;
            }
        }

        return null;
    }

    private static DialogueInteractionIdentifierBinding ResolveIdentifierBindingFromBinding(主视角对话绑定 binding, string identifierId)
    {
        return ResolveIdentifierBindingFromEntries(binding != null ? binding.标识内容绑定 : null, identifierId);
    }

    private static DialogueInteractionIdentifierBinding ResolveIdentifierBindingFromBinding(副视角对话绑定 binding, string identifierId)
    {
        return ResolveIdentifierBindingFromEntries(binding != null ? binding.标识内容绑定 : null, identifierId);
    }

    private static DialogueInteractionIdentifierBinding ResolveIdentifierBindingFromEntries(List<DialogueInteractionIdentifierBinding> entries, string identifierId)
    {
        if (entries == null || string.IsNullOrWhiteSpace(identifierId))
        {
            return null;
        }

        string resolvedId = identifierId.Trim();
        for (int i = 0; i < entries.Count; i++)
        {
            DialogueInteractionIdentifierBinding entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.标识ID))
            {
                continue;
            }

            if (string.Equals(entry.标识ID.Trim(), resolvedId, StringComparison.Ordinal))
            {
                return entry;
            }
        }

        return null;
    }

    private void 关闭当前标识内容()
    {
        if (当前标识关闭按钮 != null)
        {
            当前标识关闭按钮.onClick.RemoveListener(关闭当前标识内容);
            当前标识关闭按钮 = null;
        }

        if (当前打开标识目标 != null)
        {
            当前打开标识目标.SetActive(false);
            当前打开标识目标 = null;
        }

        SetCurrentDialogueControlsInteractable(true);
    }

    private void SetCurrentDialogueControlsInteractable(bool interactable)
    {
        GameObject continueButtonObject = null;
        GameObject interactionContainerObject = null;

        switch (当前显示视角)
        {
            case 对话显示视角.主视角:
                continueButtonObject = 当前主视角绑定 != null ? 当前主视角绑定.继续按钮 : null;
                interactionContainerObject = 当前主视角绑定 != null ? 当前主视角绑定.交互按钮容器 : null;
                break;
            case 对话显示视角.副视角:
                continueButtonObject = 当前副视角绑定 != null ? 当前副视角绑定.继续按钮 : null;
                interactionContainerObject = 当前副视角绑定 != null ? 当前副视角绑定.交互按钮容器 : null;
                break;
        }

        对话界面服务.设置按钮可交互(continueButtonObject, interactable);
        对话界面服务.设置容器可交互(interactionContainerObject, interactable);
    }

    private void EnsureDialogueVoiceAudioSource()
    {
        if (对话语音播放器 != null)
        {
            return;
        }

        对话语音播放器 = GetComponent<AudioSource>();
        if (对话语音播放器 == null)
        {
            对话语音播放器 = gameObject.AddComponent<AudioSource>();
        }

        对话语音播放器.playOnAwake = false;
        对话语音播放器.loop = false;
        对话语音播放器.spatialBlend = 0f;
    }

    private void PlayDialogueVoice(DialogueContentDatabase.DialogueContentEntry contentEntry)
    {
        EnsureDialogueVoiceAudioSource();
        StopDialogueVoice();

        if (对话语音播放器 == null || contentEntry == null || contentEntry.voiceClip == null)
        {
            return;
        }

        对话语音播放器.clip = contentEntry.voiceClip;
        对话语音播放器.Play();
    }

    private void StopDialogueVoice()
    {
        if (对话语音播放器 == null)
        {
            return;
        }

        if (对话语音播放器.isPlaying)
        {
            对话语音播放器.Stop();
        }

        对话语音播放器.clip = null;
    }

    private void ClearGeneratedInteractionButtons()
    {
        for (int i = 已生成交互按钮.Count - 1; i >= 0; i--)
        {
            GameObject buttonObject = 已生成交互按钮[i];
            if (buttonObject != null)
            {
                Destroy(buttonObject);
            }
        }

        已生成交互按钮.Clear();
    }

    private void 绑定屏幕火星特效()
    {
        当前屏幕火星特效 = FindObjectOfType<屏幕火星特效>(true);
    }

    private void 显示屏幕火星特效()
    {
        if (当前屏幕火星特效 == null)
        {
            Debug.LogError("对话运行时: 当前场景缺少 屏幕火星特效。");
            throw new InvalidOperationException("屏幕火星特效");
        }

        当前屏幕火星特效.显示特效();
    }

    private void 隐藏屏幕火星特效()
    {
        if (当前屏幕火星特效 == null)
        {
            Debug.LogError("对话运行时: 当前场景缺少 屏幕火星特效。");
            throw new InvalidOperationException("屏幕火星特效");
        }

        当前屏幕火星特效.隐藏特效();
    }

}
