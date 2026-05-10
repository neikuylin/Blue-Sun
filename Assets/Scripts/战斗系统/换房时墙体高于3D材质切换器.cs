using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/换房时触发无视高低3D都挖空")]
public sealed class 换房时墙体高于3D材质切换器 : MonoBehaviour
{
    private void OnEnable()
    {
        BattleTurnSystem.换房移动开始 += On换房移动开始;
    }

    private void OnDisable()
    {
        BattleTurnSystem.换房移动开始 -= On换房移动开始;
    }

    [ContextMenu("触发无视高低3D都挖空")]
    public void 触发无视高低3D都挖空()
    {
        Sprite角色遮挡挖空控制器 revealController = GetComponent<Sprite角色遮挡挖空控制器>();
        if (revealController == null)
        {
            Debug.LogError("换房时触发无视高低3D都挖空：当前物体没有 Sprite角色遮挡挖空控制器。", this);
            return;
        }

        revealController.开启无视高低3D都挖空();
    }

    private void On换房移动开始(MapTemplateDatabase.ConnectionDirection direction)
    {
        触发无视高低3D都挖空();
    }
}
