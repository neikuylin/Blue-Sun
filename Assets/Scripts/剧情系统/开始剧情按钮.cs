using UnityEngine;

[DisallowMultipleComponent]
public sealed class 开始剧情按钮 : MonoBehaviour
{
    [SerializeField] private string 剧情ID = string.Empty;

    public string 当前剧情ID => 剧情ID;

    public void 开始新游戏并播放剧情()
    {
        if (string.IsNullOrWhiteSpace(剧情ID))
        {
            Debug.LogError("开始剧情按钮：没有选择剧情。", this);
            return;
        }

        SaveGameService.ResetRuntimeToDefaults();
        if (!剧情运行时.播放(剧情ID.Trim()))
        {
            Debug.LogError($"开始剧情按钮：无法播放剧情“{剧情ID}”。", this);
        }
    }
}
