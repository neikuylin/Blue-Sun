using UnityEngine;

[DisallowMultipleComponent]
public sealed class 开始新游戏按钮 : MonoBehaviour
{
    public void 重置为新游戏()
    {
        SaveGameService.ResetRuntimeToDefaults();
    }
}
