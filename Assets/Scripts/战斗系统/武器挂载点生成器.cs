using UnityEngine;

public sealed class 武器挂载点生成器 : MonoBehaviour
{
    public const string 左手腕名称 = "Wrist_L";
    public const string 右手腕名称 = "Wrist_R";
    public const string 左手挂载点名称 = "武器挂载点（左）";
    public const string 右手挂载点名称 = "武器挂载点（右）";

    [SerializeField]
    private Vector3 左手本地位置 = new Vector3(0.05f, 0f, 0f);

    [SerializeField]
    private Vector3 左手本地欧拉角 = new Vector3(90f, 0f, 0f);

    [SerializeField]
    private Vector3 右手本地位置 = new Vector3(-0.06873833f, -0.005495171f, 0.0047287312f);

    [SerializeField]
    private Vector3 右手本地欧拉角 = new Vector3(-78.97751f, 37.49056f, -32.32306f);

    public Vector3 左手记录位置 => 左手本地位置;
    public Vector3 左手记录欧拉角 => 左手本地欧拉角;
    public Vector3 右手记录位置 => 右手本地位置;
    public Vector3 右手记录欧拉角 => 右手本地欧拉角;

    public bool 生成或更新武器挂载点(out string result)
    {
        Transform 左手腕 = 查找子物体(transform, 左手腕名称);
        Transform 右手腕 = 查找子物体(transform, 右手腕名称);

        if (左手腕 == null || 右手腕 == null)
        {
            result = "没有找到 Wrist_L 或 Wrist_R。";
            return false;
        }

        Transform 左手挂载点 = 取得或创建挂载点(左手挂载点名称);
        Transform 右手挂载点 = 取得或创建挂载点(右手挂载点名称);

        应用记录(左手挂载点, 左手腕, 左手本地位置, 左手本地欧拉角);
        应用记录(右手挂载点, 右手腕, 右手本地位置, 右手本地欧拉角);

        result = "已生成或更新左右武器挂载点。";
        return true;
    }

    private Transform 取得或创建挂载点(string mountName)
    {
        Transform found = 查找子物体(transform, mountName);
        if (found != null)
        {
            return found;
        }

        GameObject mount = new GameObject(mountName);
        return mount.transform;
    }

    private static void 应用记录(Transform mount, Transform wrist, Vector3 localPosition, Vector3 localEulerAngles)
    {
        mount.SetParent(wrist, false);
        mount.localPosition = localPosition;
        mount.localEulerAngles = localEulerAngles;
        mount.localScale = Vector3.one;
    }

    public static Transform 查找子物体(Transform root, string targetName)
    {
        if (root.name == targetName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = 查找子物体(root.GetChild(i), targetName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
