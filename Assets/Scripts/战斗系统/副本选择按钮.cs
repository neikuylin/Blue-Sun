using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("战斗/副本选择按钮")]
public sealed class 副本选择按钮 : MonoBehaviour
{
    [SerializeField] private string 地图模板ID = string.Empty;

    public void 选择副本()
    {
        副本选择状态.选择地图模板(地图模板ID);
    }
}
