using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StartMenuSaveBinder : MonoBehaviour
{
    private const string StartSceneName = "开始界面";
    private const string StartButtonName = "开始按钮";
    private const string ContinueButtonName = "继续按钮";

    private Button startButton;
    private Button continueButton;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (FindObjectOfType<StartMenuSaveBinder>() != null)
        {
            return;
        }

        GameObject go = new GameObject(nameof(StartMenuSaveBinder));
        DontDestroyOnLoad(go);
        go.AddComponent<StartMenuSaveBinder>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        BindScene();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Unbind();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindScene();
    }

    private void BindScene()
    {
        Unbind();

        if (SceneManager.GetActiveScene().name != StartSceneName)
        {
            return;
        }

        startButton = FindButtonByName(StartButtonName);
        if (startButton == null)
        {
            Debug.LogError($"开始界面存档绑定：找不到按钮 '{StartButtonName}'。");
            return;
        }

        SetButtonText(startButton, "开始游戏");
        startButton.onClick = new Button.ButtonClickedEvent();
        startButton.onClick.AddListener(开始游戏按钮点击);

        continueButton = FindButtonByName(ContinueButtonName);
        if (continueButton == null)
        {
            continueButton = CreateContinueButton(startButton);
        }

        if (continueButton == null)
        {
            Debug.LogError("开始界面存档绑定：继续按钮创建失败。");
            return;
        }

        SetButtonText(continueButton, "继续游戏");
        continueButton.onClick = new Button.ButtonClickedEvent();
        continueButton.onClick.AddListener(() => SaveGameService.LoadDefaultSlot());
        continueButton.interactable = SaveGameService.HasDefaultSaveFile();
        continueButton.gameObject.SetActive(true);
    }

    private void Unbind()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(开始游戏按钮点击);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
        }

        startButton = null;
        continueButton = null;
    }

    private static void 开始游戏按钮点击()
    {
        SaveGameService.ResetRuntimeToDefaults();
        if (!事件剧情硬编码规则.尝试从开始按钮播放出生剧情())
        {
            Debug.LogWarning("开始界面：开始游戏按钮没有触发出生剧情。请检查出生剧情事件是否勾选，以及是否绑定了剧情。");
        }
    }

    private static Button FindButtonByName(string buttonName)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.name == buttonName)
            {
                return button;
            }
        }

        return null;
    }

    private static Button CreateContinueButton(Button source)
    {
        if (source == null || source.transform.parent == null)
        {
            return null;
        }

        GameObject instance = Instantiate(source.gameObject, source.transform.parent, false);
        instance.name = ContinueButtonName;

        RectTransform sourceRect = source.transform as RectTransform;
        RectTransform targetRect = instance.transform as RectTransform;
        if (sourceRect != null && targetRect != null)
        {
            targetRect.anchorMin = sourceRect.anchorMin;
            targetRect.anchorMax = sourceRect.anchorMax;
            targetRect.pivot = sourceRect.pivot;
            targetRect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, -Mathf.Max(72f, sourceRect.rect.height + 20f));
            targetRect.sizeDelta = sourceRect.sizeDelta;
            targetRect.localScale = sourceRect.localScale;
        }

        return instance.GetComponent<Button>();
    }

    private static void SetButtonText(Button button, string text)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text tmpText = button.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
        {
            tmpText.text = text;
            return;
        }

        Text legacyText = button.GetComponentInChildren<Text>(true);
        if (legacyText != null)
        {
            legacyText.text = text;
        }
    }
}
