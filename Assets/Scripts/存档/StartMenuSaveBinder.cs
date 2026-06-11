using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StartMenuSaveBinder : MonoBehaviour
{
    private const string StartSceneName = "开始界面";
    private const string ContinueButtonName = "继续按钮";

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

        continueButton = FindButtonByName(ContinueButtonName);
        if (continueButton == null)
        {
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
        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
        }

        continueButton = null;
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
