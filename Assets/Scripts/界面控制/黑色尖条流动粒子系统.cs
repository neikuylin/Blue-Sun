using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
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

    private struct 粒子状态
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

    [Header("粒子")]
    [SerializeField, Min(0f)] private float 每秒数量 = 10f;
    [SerializeField, Min(1)] private int 最大数量 = 80;
    [SerializeField] private Vector2 速度范围 = new Vector2(1.6f, 2.6f);
    [SerializeField] private Vector2 长度范围 = new Vector2(0.65f, 1.15f);
    [SerializeField] private Vector2 宽度范围 = new Vector2(0.06f, 0.13f);
    [SerializeField, Min(1)] private int 每条短段数量 = 6;
    [SerializeField, Min(0f)] private float 短线长度 = 4f;
    [SerializeField, Min(0f)] private float 出生点左右浮动幅度 = 0.7f;
    [SerializeField, Min(0f)] private float 出生点左右浮动速度 = 0.8f;

    [Header("形状")]
    [SerializeField, Range(0.05f, 0.45f)] private float 尖端长度比例 = 0.22f;

    private ParticleSystem 粒子系统;
    private ParticleSystemRenderer 粒子渲染器;
    private ParticleSystem.Particle[] 粒子数组;
    private 粒子状态[] 状态数组;
    private Mesh 尖条网格;
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
        清空粒子();
    }

    private void OnDestroy()
    {
        if (尖条网格 != null)
        {
            销毁对象(尖条网格);
            尖条网格 = null;
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
        if (!粒子系统.isPlaying)
        {
            粒子系统.Play(false);
        }

        发射粒子(deltaTime);
        更新粒子(deltaTime);

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
        清空粒子();
        发射累计 = 0f;
    }

    [ContextMenu("应用黑色尖条流动设置")]
    public void 应用设置()
    {
        粒子系统 = GetComponent<ParticleSystem>();
        粒子渲染器 = GetComponent<ParticleSystemRenderer>();

        配置容量();
        配置粒子系统();
        配置渲染器();
    }

    private void 配置容量()
    {
        int capacity = 最大数量;
        粒子数组 = new ParticleSystem.Particle[capacity * 每条短段数量];
        状态数组 = new 粒子状态[capacity];
    }

    private void 配置粒子系统()
    {
        ParticleSystem.MainModule main = 粒子系统.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
        main.customSimulationSpace = transform;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = 最大数量 * 每条短段数量;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startSize3D = true;
        main.startColor = 粒子颜色;
        main.startRotation3D = true;

        ParticleSystem.EmissionModule emission = 粒子系统.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = 粒子系统.shape;
        shape.enabled = false;
        ParticleSystem.VelocityOverLifetimeModule velocity = 粒子系统.velocityOverLifetime;
        velocity.enabled = false;
        ParticleSystem.NoiseModule noise = 粒子系统.noise;
        noise.enabled = false;
        ParticleSystem.SizeOverLifetimeModule size = 粒子系统.sizeOverLifetime;
        size.enabled = false;
        ParticleSystem.ColorOverLifetimeModule color = 粒子系统.colorOverLifetime;
        color.enabled = false;
        ParticleSystem.TextureSheetAnimationModule textureSheet = 粒子系统.textureSheetAnimation;
        textureSheet.enabled = false;
    }

    private void 配置渲染器()
    {
        粒子渲染器.renderMode = ParticleSystemRenderMode.Mesh;
        粒子渲染器.mesh = 取尖条网格();
        粒子渲染器.sortMode = ParticleSystemSortMode.Distance;
        粒子渲染器.alignment = ParticleSystemRenderSpace.Local;
        粒子渲染器.minParticleSize = 0.001f;
        粒子渲染器.maxParticleSize = 100f;
        粒子渲染器.sortingLayerName = 排序图层;
        粒子渲染器.sortingOrder = 排序层级;
        粒子渲染器.sharedMaterial = 粒子材质模板;
    }

    private void 发射粒子(float deltaTime)
    {
        发射累计 += 每秒数量 * deltaTime;
        int count = Mathf.FloorToInt(发射累计);
        发射累计 -= count;

        for (int i = 0; i < count; i++)
        {
            生成一个粒子();
        }
    }

    private void 生成一个粒子()
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

        状态数组[index] = new 粒子状态
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

    private void 更新粒子(float deltaTime)
    {
        int particleCount = 0;
        for (int i = 0; i < 状态数组.Length; i++)
        {
            粒子状态 state = 状态数组[i];
            if (!state.有效)
            {
                continue;
            }

            state.时间 += deltaTime;
            if (state.时间 >= state.生命周期)
            {
                state.有效 = false;
                状态数组[i] = state;
                continue;
            }

            状态数组[i] = state;
            for (int segmentIndex = 0; segmentIndex < 每条短段数量; segmentIndex++)
            {
                粒子数组[particleCount] = 创建粒子(state, segmentIndex);
                particleCount++;
            }
        }

        粒子系统.SetParticles(粒子数组, particleCount);
    }

    private ParticleSystem.Particle 创建粒子(粒子状态 state, int segmentIndex)
    {
        float segmentLength = state.长度 / 每条短段数量;
        float distanceBehindHead = segmentLength * (segmentIndex + 0.5f);
        float main = state.起始主轴 + state.速度 * state.时间 - distanceBehindHead;
        Vector2 perpendicular = new Vector2(-state.方向.y, state.方向.x);
        float sampleTime = state.出生时间 - distanceBehindHead / state.速度;
        float cross = state.垂直位置 + 取出生点左右浮动(perpendicular, sampleTime);
        Vector2 center = state.方向 * main + perpendicular * cross;
        return new ParticleSystem.Particle
        {
            position = new Vector3(center.x, center.y, 0f),
            startLifetime = state.生命周期,
            remainingLifetime = state.生命周期 - state.时间,
            startColor = 粒子颜色,
            startSize3D = new Vector3(短线长度 / 取缩放长度(state.方向), state.宽度, 1f),
            rotation3D = new Vector3(0f, 0f, Mathf.Atan2(state.方向.y, state.方向.x) * Mathf.Rad2Deg)
        };
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

    private Mesh 取尖条网格()
    {
        if (尖条网格 == null)
        {
            尖条网格 = new Mesh
            {
                name = "运行时_黑色尖条粒子Mesh",
                hideFlags = HideFlags.DontSave
            };
        }

        float shoulderX = 0.5f - 尖端长度比例;
        Vector3[] vertices =
        {
            new Vector3(-0.5f, 0f, 0f),
            new Vector3(-shoulderX, 0.5f, 0f),
            new Vector3(shoulderX, 0.5f, 0f),
            new Vector3(0.5f, 0f, 0f),
            new Vector3(shoulderX, -0.5f, 0f),
            new Vector3(-shoulderX, -0.5f, 0f)
        };

        int[] triangles =
        {
            0, 1, 5,
            1, 2, 5,
            2, 4, 5,
            2, 3, 4
        };

        尖条网格.Clear();
        尖条网格.vertices = vertices;
        尖条网格.triangles = triangles;
        尖条网格.RecalculateBounds();
        return 尖条网格;
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

    private void 清空粒子()
    {
        for (int i = 0; i < 状态数组.Length; i++)
        {
            状态数组[i].有效 = false;
        }

        粒子系统.Clear(true);
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
