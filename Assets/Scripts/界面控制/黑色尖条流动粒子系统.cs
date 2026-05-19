using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[AddComponentMenu("特效/黑色尖条流动粒子系统")]
public sealed class 黑色尖条流动粒子系统 : MonoBehaviour
{
    private static readonly Vector3 编辑模式战斗摄像机角度 = new Vector3(48.6f, 45f, 0f);

    public enum 流动方向
    {
        东 = 0,
        南 = 1,
        西 = 2,
        北 = 3
    }

    private struct 墨线状态
    {
        public bool 有效;
        public float 时间;
        public float 出生时间;
        public float 生命周期;
        public float 速度;
        public float 长度;
        public float 宽度;
        public float 起始主轴;
        public float 垂直位置;
        public Vector2 方向;
    }

    [Header("预览")]
    [SerializeField] private bool 编辑模式预览 = true;

    [Header("朝向")]
    [SerializeField] private bool 平行于摄像头的旋转角度;

    [Header("材质")]
    [SerializeField] private Material 粒子材质模板;
    [SerializeField] private Color 粒子颜色 = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private string 排序图层 = "Default";
    [SerializeField] private int 排序层级 = 0;

    [Header("流向")]
    [SerializeField] private 流动方向 方向 = 流动方向.东;

    [Header("辅助显示")]
    [SerializeField] private bool 显示Transform平面 = true;
    [SerializeField] private bool 仅选中时显示平面 = false;
    [SerializeField] private Color 平面颜色 = new Color(0f, 0f, 0f, 0.12f);
    [SerializeField] private Color 边框颜色 = new Color(0f, 0f, 0f, 0.65f);

    [Header("墨线")]
    [SerializeField, Min(0f)] private float 每秒数量 = 10f;
    [SerializeField, Min(1)] private int 最大数量 = 80;
    [SerializeField] private Vector2 速度范围 = new Vector2(1.6f, 2.6f);
    [SerializeField] private Vector2 长度范围 = new Vector2(0.65f, 1.15f);
    [SerializeField] private Vector2 宽度范围 = new Vector2(0.06f, 0.13f);
    [FormerlySerializedAs("每条短段数量")]
    [SerializeField, Min(2)] private int 轨迹点数量 = 24;
    [SerializeField, Min(0f)] private float 出生点左右浮动幅度 = 0.7f;
    [SerializeField, Min(0f)] private float 出生点左右浮动速度 = 0.8f;

    [Header("形状")]
    [SerializeField, Range(0.05f, 0.45f)] private float 尖端长度比例 = 0.22f;
    [SerializeField, Min(0f)] private float 蠕动强度 = 0.12f;
    [SerializeField, Min(0f)] private float 蠕动速度 = 1.2f;
    [SerializeField, Min(0.01f)] private float 蠕动密度 = 3.5f;
    [SerializeField, Range(0f, 1f)] private float 宽度鼓动强度 = 0.35f;
    [SerializeField, Range(0f, 1f)] private float 边缘不对称强度 = 0.45f;

    private ParticleSystem 粒子系统;
    private ParticleSystemRenderer 粒子渲染器;
    private MeshFilter 墨线网格过滤器;
    private MeshRenderer 墨线渲染器;
    private 墨线状态[] 状态数组;
    private Mesh 墨线网格;
    private readonly List<Vector3> 网格顶点 = new List<Vector3>();
    private readonly List<int> 网格三角形 = new List<int>();
    private readonly List<Color> 网格颜色 = new List<Color>();
    private float 发射累计;
    private double 上次编辑器时间;

    private void Reset()
    {
        应用设置();
    }

    private void OnEnable()
    {
        应用设置();
    }

    private void OnDisable()
    {
        清空墨线();
    }

    private void OnDestroy()
    {
        if (墨线网格 != null)
        {
            销毁对象(墨线网格);
            墨线网格 = null;
        }
    }

    private void OnValidate()
    {
        应用设置();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            上次编辑器时间 = EditorApplication.timeSinceStartup;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void Update()
    {
        if (!Application.isPlaying && !编辑模式预览)
        {
            return;
        }

        float deltaTime = 取时间间隔();
        发射墨线(deltaTime);
        更新墨线(deltaTime);
        重建墨线网格();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void OnDrawGizmos()
    {
        if (!仅选中时显示平面)
        {
            绘制范围();
        }
    }

    private void OnDrawGizmosSelected()
    {
        绘制范围();
    }

    [ContextMenu("重新预览黑色尖条流动")]
    public void 重新预览()
    {
        清空墨线();
        发射累计 = 0f;
    }

    [ContextMenu("应用黑色尖条流动设置")]
    public void 应用设置()
    {
        粒子系统 = 取组件<ParticleSystem>();
        粒子渲染器 = 取组件<ParticleSystemRenderer>();
        墨线网格过滤器 = 取组件<MeshFilter>();
        墨线渲染器 = 取组件<MeshRenderer>();

        配置容量();
        配置旧粒子系统();
        配置墨线渲染器();
    }

    private void 配置容量()
    {
        状态数组 = new 墨线状态[最大数量];
    }

    private void 配置旧粒子系统()
    {
        粒子系统.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        粒子系统.Clear(true);
        粒子渲染器.enabled = false;
    }

    private void 配置墨线渲染器()
    {
        墨线网格过滤器.sharedMesh = 取墨线网格();
        墨线渲染器.sharedMaterial = 粒子材质模板;
        墨线渲染器.sortingLayerName = 排序图层;
        墨线渲染器.sortingOrder = 排序层级;
    }

    private void 发射墨线(float deltaTime)
    {
        发射累计 += 每秒数量 * deltaTime;
        int count = Mathf.FloorToInt(发射累计);
        发射累计 -= count;

        for (int i = 0; i < count; i++)
        {
            生成一条墨线();
        }
    }

    private void 生成一条墨线()
    {
        int index = 取空槽位();
        if (index < 0)
        {
            return;
        }

        Vector2 direction = 取流动方向();
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float speed = Random.Range(速度范围.x, 速度范围.y);
        float length = Random.Range(长度范围.x, 长度范围.y) / 取缩放长度(direction);
        float width = Random.Range(宽度范围.x, 宽度范围.y) / 取缩放长度(perpendicular);
        float localSpeed = speed / 取缩放长度(direction);

        取流动轴范围(direction, perpendicular, out float startMain, out float endMain, out float crossMin, out float crossMax);
        float cross = Random.Range(crossMin, crossMax);
        float distance = endMain - startMain + length;

        状态数组[index] = new 墨线状态
        {
            有效 = true,
            时间 = 0f,
            出生时间 = 取浮动时间(),
            生命周期 = distance / localSpeed,
            速度 = localSpeed,
            长度 = length,
            宽度 = width,
            起始主轴 = startMain,
            垂直位置 = cross,
            方向 = direction
        };
    }

    private void 更新墨线(float deltaTime)
    {
        for (int i = 0; i < 状态数组.Length; i++)
        {
            墨线状态 state = 状态数组[i];
            if (!state.有效)
            {
                continue;
            }

            state.时间 += deltaTime;
            if (state.时间 >= state.生命周期)
            {
                state.有效 = false;
            }

            状态数组[i] = state;
        }
    }

    private void 重建墨线网格()
    {
        网格顶点.Clear();
        网格三角形.Clear();
        网格颜色.Clear();

        for (int i = 0; i < 状态数组.Length; i++)
        {
            墨线状态 state = 状态数组[i];
            if (state.有效)
            {
                添加墨线网格(state);
            }
        }

        墨线网格.Clear();
        墨线网格.SetVertices(网格顶点);
        墨线网格.SetTriangles(网格三角形, 0);
        墨线网格.SetColors(网格颜色);
        墨线网格.RecalculateBounds();
    }

    private void 添加墨线网格(墨线状态 state)
    {
        int pointCount = Mathf.Max(2, 轨迹点数量);
        int vertexStart = 网格顶点.Count;
        Vector2[] points = new Vector2[pointCount];

        for (int i = 0; i < pointCount; i++)
        {
            float t = (float)i / (pointCount - 1);
            float distanceBehindHead = state.长度 * (1f - t);
            float main = state.起始主轴 + state.速度 * state.时间 - distanceBehindHead;
            float distanceFromSource = state.长度 - distanceBehindHead;
            float sampleTime = state.出生时间 + state.时间 - distanceFromSource / state.速度;
            float cross = state.垂直位置 + 取出生点左右浮动(new Vector2(-state.方向.y, state.方向.x), sampleTime);
            points[i] = state.方向 * main + new Vector2(-state.方向.y, state.方向.x) * cross;
        }

        for (int i = 0; i < pointCount; i++)
        {
            Vector2 tangent = 取轨迹切线(points, i, state.方向);
            Vector2 side = new Vector2(-tangent.y, tangent.x);
            float t = (float)i / (pointCount - 1);
            float widthScale = 取宽度比例(t);
            float waveTime = 取浮动时间() * 蠕动速度;
            float wavePosition = t * 蠕动密度 + state.出生时间;
            float widthWave = Mathf.PerlinNoise(wavePosition, waveTime) * 2f - 1f;
            float leftEdgeWave = Mathf.PerlinNoise(wavePosition + 17.13f, waveTime + 3.71f) * 2f - 1f;
            float rightEdgeWave = Mathf.PerlinNoise(wavePosition + 41.29f, waveTime + 9.43f) * 2f - 1f;
            float baseHalfWidth = state.宽度 * 0.5f * widthScale;
            float pulsedHalfWidth = baseHalfWidth * (1f + widthWave * 宽度鼓动强度);
            float leftHalfWidth = pulsedHalfWidth * (1f + leftEdgeWave * 边缘不对称强度);
            float rightHalfWidth = pulsedHalfWidth * (1f + rightEdgeWave * 边缘不对称强度);
            Vector2 edgeMove = tangent * (Mathf.PerlinNoise(wavePosition + 83.7f, waveTime + 21.9f) * 2f - 1f) * 蠕动强度 * widthScale;

            Vector2 leftPoint = points[i] - side * leftHalfWidth - edgeMove;
            Vector2 rightPoint = points[i] + side * rightHalfWidth + edgeMove;
            网格顶点.Add(new Vector3(leftPoint.x, leftPoint.y, 0f));
            网格顶点.Add(new Vector3(rightPoint.x, rightPoint.y, 0f));
            网格颜色.Add(粒子颜色);
            网格颜色.Add(粒子颜色);
        }

        for (int i = 0; i < pointCount - 1; i++)
        {
            int a = vertexStart + i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;

            网格三角形.Add(a);
            网格三角形.Add(c);
            网格三角形.Add(b);
            网格三角形.Add(c);
            网格三角形.Add(d);
            网格三角形.Add(b);
        }
    }

    private Vector2 取流动方向()
    {
        Vector2 east = Vector2.right;
        Vector2 north = Vector2.up;

        if (平行于摄像头的旋转角度)
        {
            Quaternion cameraRotation = 取摄像机旋转();
            east = 投影到摄像机平面(Vector3.right, cameraRotation);
            north = 投影到摄像机平面(Vector3.forward, cameraRotation);
        }

        switch (方向)
        {
            case 流动方向.南:
                return -north;
            case 流动方向.西:
                return -east;
            case 流动方向.北:
                return north;
            default:
                return east;
        }
    }

    private static void 取流动轴范围(
        Vector2 direction,
        Vector2 perpendicular,
        out float startMain,
        out float endMain,
        out float crossMin,
        out float crossMax)
    {
        Vector2[] corners =
        {
            new Vector2(-0.5f, -0.5f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(0.5f, -0.5f),
            new Vector2(0.5f, 0.5f)
        };

        startMain = Vector2.Dot(corners[0], direction);
        endMain = startMain;
        crossMin = Vector2.Dot(corners[0], perpendicular);
        crossMax = crossMin;

        for (int i = 1; i < corners.Length; i++)
        {
            float main = Vector2.Dot(corners[i], direction);
            float cross = Vector2.Dot(corners[i], perpendicular);
            startMain = Mathf.Min(startMain, main);
            endMain = Mathf.Max(endMain, main);
            crossMin = Mathf.Min(crossMin, cross);
            crossMax = Mathf.Max(crossMax, cross);
        }
    }

    private float 取缩放长度(Vector2 direction)
    {
        Vector3 scale = transform.lossyScale;
        return new Vector2(direction.x * scale.x, direction.y * scale.y).magnitude;
    }

    private float 取出生点左右浮动(Vector2 perpendicular, float time)
    {
        float wave = Mathf.Sin(time * 出生点左右浮动速度 * Mathf.PI * 2f);
        return wave * 出生点左右浮动幅度 / 取缩放长度(perpendicular);
    }

    private static float 取浮动时间()
    {
        if (Application.isPlaying)
        {
            return Time.time;
        }

#if UNITY_EDITOR
        return (float)EditorApplication.timeSinceStartup;
#else
        return 0f;
#endif
    }

    private static Quaternion 取摄像机旋转()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return Quaternion.Euler(编辑模式战斗摄像机角度);
        }
#endif

        return Camera.main.transform.rotation;
    }

    private static Vector2 投影到摄像机平面(Vector3 worldDirection, Quaternion cameraRotation)
    {
        return new Vector2(
            Vector3.Dot(worldDirection, cameraRotation * Vector3.right),
            Vector3.Dot(worldDirection, cameraRotation * Vector3.up)).normalized;
    }

    private int 取空槽位()
    {
        for (int i = 0; i < 状态数组.Length; i++)
        {
            if (!状态数组[i].有效)
            {
                return i;
            }
        }

        return -1;
    }

    private Mesh 取墨线网格()
    {
        if (墨线网格 == null)
        {
            墨线网格 = new Mesh
            {
                name = "运行时_连续墨线Mesh",
                hideFlags = HideFlags.DontSave
            };
        }

        return 墨线网格;
    }

    private float 取时间间隔()
    {
        if (Application.isPlaying)
        {
            return Time.deltaTime;
        }

#if UNITY_EDITOR
        double now = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(now - 上次编辑器时间);
        上次编辑器时间 = now;
        return deltaTime;
#else
        return 0f;
#endif
    }

    private void 清空墨线()
    {
        if (状态数组 == null)
        {
            return;
        }

        for (int i = 0; i < 状态数组.Length; i++)
        {
            状态数组[i].有效 = false;
        }

        if (墨线网格 != null)
        {
            墨线网格.Clear();
        }

        if (粒子系统 != null)
        {
            粒子系统.Clear(true);
        }
    }

    private static Vector2 取轨迹切线(Vector2[] points, int index, Vector2 defaultDirection)
    {
        Vector2 tangent;
        if (index == 0)
        {
            tangent = points[1] - points[0];
        }
        else if (index == points.Length - 1)
        {
            tangent = points[index] - points[index - 1];
        }
        else
        {
            tangent = points[index + 1] - points[index - 1];
        }

        if (tangent.sqrMagnitude <= 0.000001f)
        {
            return defaultDirection;
        }

        return tangent.normalized;
    }

    private float 取宽度比例(float t)
    {
        float tail = t / 尖端长度比例;
        float head = (1f - t) / 尖端长度比例;
        return Mathf.Clamp01(Mathf.Min(tail, head));
    }

    private void 绘制范围()
    {
        if (!显示Transform平面)
        {
            return;
        }

        Vector3 bottomLeft = transform.TransformPoint(new Vector3(-0.5f, -0.5f, 0f));
        Vector3 topLeft = transform.TransformPoint(new Vector3(-0.5f, 0.5f, 0f));
        Vector3 topRight = transform.TransformPoint(new Vector3(0.5f, 0.5f, 0f));
        Vector3 bottomRight = transform.TransformPoint(new Vector3(0.5f, -0.5f, 0f));

#if UNITY_EDITOR
        Handles.DrawSolidRectangleWithOutline(
            new[] { bottomLeft, topLeft, topRight, bottomRight },
            平面颜色,
            边框颜色);
#endif
    }

    private T 取组件<T>() where T : Component
    {
        T component = GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    private static void 销毁对象(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
