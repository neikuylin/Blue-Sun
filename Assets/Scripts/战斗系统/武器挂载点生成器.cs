using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class 武器挂载点生成器 : MonoBehaviour
{
    public const string 左手腕名称 = "Wrist_L";
    public const string 右手腕名称 = "Wrist_R";
    public const string 左手挂载点名称 = "武器挂载点（左）";
    public const string 右手挂载点名称 = "武器挂载点（右）";

    [SerializeField]
    private int 当前模板索引;

    [SerializeField]
    private List<武器挂载点模板> 模板列表 = new List<武器挂载点模板>
    {
        new 武器挂载点模板
        {
            模型名称 = "索拉娜",
            左手本地位置 = new Vector3(0.05f, 0f, 0f),
            左手本地欧拉角 = new Vector3(90f, 0f, 0f),
            右手本地位置 = new Vector3(-0.06873833f, -0.005495171f, 0.0047287312f),
            右手本地欧拉角 = new Vector3(-78.97751f, 37.49056f, -32.32306f),
        }
    };

    public int 当前模板 => 当前模板索引;
    public IReadOnlyList<武器挂载点模板> 已保存模板 => 模板列表;

    public bool 生成或更新武器挂载点(out string result)
    {
        if (!取得当前模板(out 武器挂载点模板 template, out result))
        {
            return false;
        }

        return 生成或更新武器挂载点(template, out result);
    }

    public bool 保存当前模型挂载点(out string result)
    {
        Transform 左手腕 = 查找子物体(transform, 左手腕名称);
        Transform 右手腕 = 查找子物体(transform, 右手腕名称);
        Transform 左手挂载点 = 查找子物体(transform, 左手挂载点名称);
        Transform 右手挂载点 = 查找子物体(transform, 右手挂载点名称);

        if (左手腕 == null || 右手腕 == null)
        {
            result = "没有找到 Wrist_L 或 Wrist_R。";
            return false;
        }

        if (左手挂载点 == null || 右手挂载点 == null)
        {
            result = "没有找到武器挂载点（左）或武器挂载点（右）。";
            return false;
        }

        if (左手挂载点.parent != 左手腕)
        {
            左手挂载点.SetParent(左手腕, true);
        }

        if (右手挂载点.parent != 右手腕)
        {
            右手挂载点.SetParent(右手腕, true);
        }

        string modelName = string.IsNullOrWhiteSpace(gameObject.name) ? "未命名模型" : gameObject.name;
        int index = 查找模板索引(modelName);
        if (index < 0)
        {
            模板列表.Add(new 武器挂载点模板());
            index = 模板列表.Count - 1;
        }

        武器挂载点模板 template = 模板列表[index];
        template.模型名称 = modelName;
        template.左手本地位置 = 左手挂载点.localPosition;
        template.左手本地欧拉角 = 规整角度(左手挂载点.localEulerAngles);
        template.右手本地位置 = 右手挂载点.localPosition;
        template.右手本地欧拉角 = 规整角度(右手挂载点.localEulerAngles);

        当前模板索引 = index;
        result = $"已保存模型“{modelName}”的武器挂载点模板。";
        return true;
    }

    public bool 用右手镜像当前挂载点到左手(out string result)
    {
        Transform 左手腕 = 查找子物体(transform, 左手腕名称);
        Transform 右手挂载点 = 查找子物体(transform, 右手挂载点名称);

        if (左手腕 == null)
        {
            result = "没有找到 Wrist_L。";
            return false;
        }

        if (右手挂载点 == null)
        {
            result = "没有找到武器挂载点（右）。";
            return false;
        }

        Transform 左手挂载点 = 取得或创建挂载点(左手挂载点名称);
        镜像右手到左手(右手挂载点, 左手挂载点, 左手腕);

        result = "已按右手挂载点水平镜像生成左手挂载点。";
        return true;
    }

    public bool 用右手镜像当前模板到左手(out string result)
    {
        if (!取得当前模板(out 武器挂载点模板 template, out result))
        {
            return false;
        }

        Transform 左手腕 = 查找子物体(transform, 左手腕名称);
        Transform 右手腕 = 查找子物体(transform, 右手腕名称);

        if (左手腕 == null || 右手腕 == null)
        {
            result = "没有找到 Wrist_L 或 Wrist_R。";
            return false;
        }

        GameObject temporaryRightMount = new GameObject("__临时右手武器挂载点__");
        GameObject temporaryLeftMount = new GameObject("__临时左手武器挂载点__");
        try
        {
            Transform rightMount = temporaryRightMount.transform;
            Transform leftMount = temporaryLeftMount.transform;
            应用记录(rightMount, 右手腕, template.右手本地位置, template.右手本地欧拉角);
            镜像右手到左手(rightMount, leftMount, 左手腕);

            template.左手本地位置 = leftMount.localPosition;
            template.左手本地欧拉角 = 规整角度(leftMount.localEulerAngles);
        }
        finally
        {
            if (Application.isPlaying)
            {
                Destroy(temporaryRightMount);
                Destroy(temporaryLeftMount);
            }
            else
            {
                DestroyImmediate(temporaryRightMount);
                DestroyImmediate(temporaryLeftMount);
            }
        }

        result = $"已按“{template.模型名称}”模板的右手参数镜像更新左手参数。";
        return true;
    }

    public bool 用左手镜像当前挂载点到右手(out string result)
    {
        Transform 右手腕 = 查找子物体(transform, 右手腕名称);
        Transform 左手挂载点 = 查找子物体(transform, 左手挂载点名称);

        if (右手腕 == null)
        {
            result = "没有找到 Wrist_R。";
            return false;
        }

        if (左手挂载点 == null)
        {
            result = "没有找到武器挂载点（左）。";
            return false;
        }

        Transform 右手挂载点 = 取得或创建挂载点(右手挂载点名称);
        镜像挂载点(左手挂载点, 右手挂载点, 右手腕);

        result = "已按左手挂载点水平镜像生成右手挂载点。";
        return true;
    }

    public bool 用左手镜像当前模板到右手(out string result)
    {
        if (!取得当前模板(out 武器挂载点模板 template, out result))
        {
            return false;
        }

        Transform 左手腕 = 查找子物体(transform, 左手腕名称);
        Transform 右手腕 = 查找子物体(transform, 右手腕名称);

        if (左手腕 == null || 右手腕 == null)
        {
            result = "没有找到 Wrist_L 或 Wrist_R。";
            return false;
        }

        GameObject temporaryLeftMount = new GameObject("__临时左手武器挂载点__");
        GameObject temporaryRightMount = new GameObject("__临时右手武器挂载点__");
        try
        {
            Transform leftMount = temporaryLeftMount.transform;
            Transform rightMount = temporaryRightMount.transform;
            应用记录(leftMount, 左手腕, template.左手本地位置, template.左手本地欧拉角);
            镜像挂载点(leftMount, rightMount, 右手腕);

            template.右手本地位置 = rightMount.localPosition;
            template.右手本地欧拉角 = 规整角度(rightMount.localEulerAngles);
        }
        finally
        {
            if (Application.isPlaying)
            {
                Destroy(temporaryLeftMount);
                Destroy(temporaryRightMount);
            }
            else
            {
                DestroyImmediate(temporaryLeftMount);
                DestroyImmediate(temporaryRightMount);
            }
        }

        result = $"已按“{template.模型名称}”模板的左手参数镜像更新右手参数。";
        return true;
    }

    private bool 生成或更新武器挂载点(武器挂载点模板 template, out string result)
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

        应用记录(左手挂载点, 左手腕, template.左手本地位置, template.左手本地欧拉角);
        应用记录(右手挂载点, 右手腕, template.右手本地位置, template.右手本地欧拉角);

        result = $"已按“{template.模型名称}”模板生成或更新左右武器挂载点。";
        return true;
    }

    private bool 取得当前模板(out 武器挂载点模板 template, out string result)
    {
        template = null;

        if (模板列表 == null || 模板列表.Count == 0)
        {
            result = "没有可用的武器挂载点模板。";
            return false;
        }

        当前模板索引 = Mathf.Clamp(当前模板索引, 0, 模板列表.Count - 1);
        template = 模板列表[当前模板索引];

        if (template == null)
        {
            result = "当前模板为空。";
            return false;
        }

        if (string.IsNullOrWhiteSpace(template.模型名称))
        {
            template.模型名称 = "未命名模型";
        }

        result = string.Empty;
        return true;
    }

    private int 查找模板索引(string modelName)
    {
        for (int i = 0; i < 模板列表.Count; i++)
        {
            武器挂载点模板 template = 模板列表[i];
            if (template != null && template.模型名称 == modelName)
            {
                return i;
            }
        }

        return -1;
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

    private void 镜像右手到左手(Transform rightMount, Transform leftMount, Transform leftWrist)
    {
        镜像挂载点(rightMount, leftMount, leftWrist);
    }

    private void 镜像挂载点(Transform sourceMount, Transform targetMount, Transform targetWrist)
    {
        Vector3 localPositionInRoot = transform.InverseTransformPoint(sourceMount.position);
        localPositionInRoot.x = -localPositionInRoot.x;

        Vector3 mirroredWorldPosition = transform.TransformPoint(localPositionInRoot);
        Vector3 mirroredForward = 镜像世界方向(sourceMount.forward);
        Vector3 mirroredUp = 镜像世界方向(sourceMount.up);

        targetMount.SetParent(targetWrist, false);
        targetMount.position = mirroredWorldPosition;
        targetMount.rotation = Quaternion.LookRotation(mirroredForward, mirroredUp);
        targetMount.localScale = Vector3.one;
    }

    private Vector3 镜像世界方向(Vector3 worldDirection)
    {
        Vector3 localDirection = transform.InverseTransformDirection(worldDirection);
        localDirection.x = -localDirection.x;
        return transform.TransformDirection(localDirection);
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

    private static Vector3 规整角度(Vector3 value)
    {
        return new Vector3(规整角度(value.x), 规整角度(value.y), 规整角度(value.z));
    }

    private static float 规整角度(float value)
    {
        if (value > 180f)
        {
            value -= 360f;
        }

        return value;
    }
}

[Serializable]
public sealed class 武器挂载点模板
{
    public string 模型名称;
    public Vector3 左手本地位置;
    public Vector3 左手本地欧拉角;
    public Vector3 右手本地位置;
    public Vector3 右手本地欧拉角;
}
