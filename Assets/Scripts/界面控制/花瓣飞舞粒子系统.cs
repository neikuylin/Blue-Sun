using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
public sealed class 花瓣飞舞粒子系统 : MonoBehaviour
{
    private enum 风向模板
    {
        东风 = 0,
        西风 = 1,
        南风 = 2,
        北风 = 3
    }

    private struct 花瓣状态
    {
        public bool 激活;
        public Vector3 天空位置;
        public Vector3 地面位置;
        public float 已存活时间;
        public float 下落时间;
        public float 停留时间;
        public float 消失时间;
        public float 尺寸;
        public float 旋转角度;
        public float 旋转速度;
        public Vector3 旋转轴;
        public float 风倍率;
        public float 摆动相位;
        public float 摆动频率;
        public Vector3 摆动方向;
    }

    [System.Serializable]
    private struct 曝光格子区域
    {
        public Vector2Int 起点格子;
        public Vector2Int 尺寸;
    }

    [Header("美术")]
    [SerializeField] private Sprite 花瓣Sprite;
    [SerializeField] private Material 粒子材质模板;
    [SerializeField] private Color 花瓣颜色 = new Color(1f, 0.62f, 0.78f, 1f);
    [SerializeField] private Color 材质颜色 = Color.white;

    [Header("房间沙盘")]
    [SerializeField] private 战斗格子沙盘辅助 地板沙盘;
    [SerializeField] private bool 无地板时自动读取运行地板 = true;
    [SerializeField, Min(0f)] private float 天空高度 = 5f;
    [SerializeField, Min(0f)] private float 地面浮起 = 0.04f;

    [Header("无地板预览")]
    [SerializeField] private bool 无地板时启用本地预览 = true;
    [SerializeField] private Vector2 本地预览范围 = new Vector2(10f, 10f);

    [Header("数量")]
    [SerializeField, Min(0f)] private float 每秒数量 = 18f;
    [SerializeField, Min(1)] private int 最大数量 = 180;

    [Header("生命周期")]
    [SerializeField, Min(0f)] private float 预热时间 = 0f;
    [SerializeField] private Vector2 下落时间 = new Vector2(2.8f, 4.2f);
    [SerializeField] private Vector2 地面停留时间 = new Vector2(0.8f, 1.4f);
    [SerializeField] private Vector2 透明消失时间 = new Vector2(0.7f, 1.1f);
    [SerializeField] private Vector2 花瓣尺寸 = new Vector2(0.14f, 0.28f);

    [Header("翻飞")]
    [SerializeField, Min(0f)] private float 最小旋转速度 = 100f;
    [SerializeField, Min(0f)] private float 最大旋转速度 = 260f;

    [Header("风")]
    [SerializeField] private bool 启用风 = false;
    [SerializeField] private 风向模板 风 = 风向模板.东风;
    [SerializeField, Min(0f)] private float 风速 = 0.35f;
    [SerializeField, Min(0f)] private float 风随机强度 = 0.35f;
    [SerializeField, Min(0f)] private float 摆动强度 = 0.18f;
    [SerializeField] private Vector2 摆动频率范围 = new Vector2(0.7f, 1.4f);
    [SerializeField, Min(0f)] private float 阵风强度 = 0.25f;
    [SerializeField, Min(0.01f)] private float 阵风频率 = 0.45f;

    [Header("天空方向偏移")]
    [SerializeField, Min(0f)] private float 东风天空偏移 = 0f;
    [SerializeField, Min(0f)] private float 西风天空偏移 = 0f;
    [SerializeField, Min(0f)] private float 南风天空偏移 = 0f;
    [SerializeField, Min(0f)] private float 北风天空偏移 = 0f;

    [Header("格子空间曝光")]
    [SerializeField] private bool 启用格子空间曝光 = true;
    [SerializeField] private List<曝光格子区域> 曝光格子区域列表 = new List<曝光格子区域>();
    [SerializeField, Min(0f)] private float 曝光强度 = 4f;
    [SerializeField] private Color 曝光颜色 = new Color(1f, 0.86f, 0.55f, 1f);

    private ParticleSystem cachedParticleSystem;
    private ParticleSystemRenderer cachedRenderer;
    private ParticleSystem.Particle[] particles;
    private 花瓣状态[] petalStates;
    private Material runtimeMaterial;
    private Mesh runtimeMesh;
    private float emissionAccumulator;
    private double lastEditorUpdateTime;

    public void 绑定地板沙盘(战斗格子沙盘辅助 沙盘)
    {
        地板沙盘 = 沙盘;
        应用设置();
    }

    public void 设置模板曝光区域(IReadOnlyList<格子模板数据库.花瓣曝光区域Entry> 区域列表)
    {
        if (曝光格子区域列表 == null)
        {
            曝光格子区域列表 = new List<曝光格子区域>();
        }

        曝光格子区域列表.Clear();
        if (区域列表 != null)
        {
            for (int i = 0; i < 区域列表.Count; i++)
            {
                格子模板数据库.花瓣曝光区域Entry source = 区域列表[i];
                if (source == null)
                {
                    continue;
                }

                曝光格子区域列表.Add(new 曝光格子区域
                {
                    起点格子 = source.startCell.ToVector2Int(),
                    尺寸 = source.size
                });
            }
        }

        NormalizeExposureAreas();
        应用设置();
    }

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
        if (cachedParticleSystem != null)
        {
            cachedParticleSystem.Clear(true);
        }
    }

    private void OnValidate()
    {
        下落时间 = NormalizeRange(下落时间, 0.05f);
        地面停留时间 = NormalizeRange(地面停留时间, 0f);
        透明消失时间 = NormalizeRange(透明消失时间, 0.05f);
        花瓣尺寸 = NormalizeRange(花瓣尺寸, 0.001f);
        摆动频率范围 = NormalizeRange(摆动频率范围, 0.01f);
        本地预览范围.x = Mathf.Max(0.01f, 本地预览范围.x);
        本地预览范围.y = Mathf.Max(0.01f, 本地预览范围.y);
        NormalizeExposureAreas();
        if (最大旋转速度 < 最小旋转速度)
        {
            最大旋转速度 = 最小旋转速度;
        }

        应用设置();

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            lastEditorUpdateTime = EditorApplication.timeSinceStartup;
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    private void Update()
    {
        TryAutoBindRuntimeFloor();

        if (!CanUpdateParticles())
        {
            return;
        }

        ResolveComponents();
        if (cachedParticleSystem == null)
        {
            return;
        }

        EnsureCapacity();
        float deltaTime = ResolveDeltaTime();
        if (!cachedParticleSystem.isPlaying)
        {
            cachedParticleSystem.Play(false);
        }

        SpawnByDeltaTime(deltaTime);
        UpdateParticles(deltaTime);

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    [ContextMenu("重新应用房间花瓣设置")]
    public void 应用设置()
    {
        ResolveComponents();
        EnsureCapacity();
        ConfigureParticleSystem();
        ConfigureRenderer();
        预热粒子();
    }

    private void ResolveComponents()
    {
        if (cachedParticleSystem == null)
        {
            cachedParticleSystem = GetComponent<ParticleSystem>();
        }

        if (cachedRenderer == null)
        {
            cachedRenderer = GetComponent<ParticleSystemRenderer>();
        }
    }

    private void EnsureCapacity()
    {
        int capacity = Mathf.Max(1, 最大数量);
        if (particles == null || particles.Length != capacity)
        {
            particles = new ParticleSystem.Particle[capacity];
        }

        if (petalStates == null || petalStates.Length != capacity)
        {
            petalStates = new 花瓣状态[capacity];
        }
    }

    private void ConfigureParticleSystem()
    {
        if (cachedParticleSystem == null)
        {
            return;
        }

        cachedParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ClearRuntimeParticles();

        ParticleSystem.MainModule main = cachedParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = Mathf.Max(1, 最大数量);
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startColor = 花瓣颜色;
        main.startRotation3D = true;

        ParticleSystem.EmissionModule emission = cachedParticleSystem.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = cachedParticleSystem.shape;
        shape.enabled = false;
        ParticleSystem.VelocityOverLifetimeModule velocity = cachedParticleSystem.velocityOverLifetime;
        velocity.enabled = false;
        ParticleSystem.NoiseModule noise = cachedParticleSystem.noise;
        noise.enabled = false;
        ParticleSystem.RotationOverLifetimeModule rotation = cachedParticleSystem.rotationOverLifetime;
        rotation.enabled = false;
        ParticleSystem.SizeOverLifetimeModule size = cachedParticleSystem.sizeOverLifetime;
        size.enabled = false;
        ParticleSystem.ColorOverLifetimeModule color = cachedParticleSystem.colorOverLifetime;
        color.enabled = false;
        ParticleSystem.TextureSheetAnimationModule textureSheet = cachedParticleSystem.textureSheetAnimation;
        textureSheet.enabled = false;
    }

    private bool CanUpdateParticles()
    {
        return 地板沙盘 != null || 无地板时启用本地预览;
    }

    private void TryAutoBindRuntimeFloor()
    {
        if (!无地板时自动读取运行地板 || 地板沙盘 != null)
        {
            return;
        }

        地板沙盘 = FindRuntimeFloorSandbox();
        if (地板沙盘 != null)
        {
            应用设置();
        }
    }

    private static 战斗格子沙盘辅助 FindRuntimeFloorSandbox()
    {
        GameObject runtimeRoot = GameObject.Find("BattleRuntime");
        if (runtimeRoot == null)
        {
            return null;
        }

        Transform floorRoot = runtimeRoot.transform.Find("RoomContent/Floor");
        if (floorRoot == null)
        {
            return null;
        }

        return floorRoot.GetComponentInChildren<战斗格子沙盘辅助>(true);
    }

    private float ResolveDeltaTime()
    {
        if (Application.isPlaying)
        {
            return Time.deltaTime;
        }

#if UNITY_EDITOR
        double currentTime = EditorApplication.timeSinceStartup;
        if (lastEditorUpdateTime <= 0d)
        {
            lastEditorUpdateTime = currentTime;
            return 0f;
        }

        float deltaTime = Mathf.Clamp((float)(currentTime - lastEditorUpdateTime), 0f, 0.05f);
        lastEditorUpdateTime = currentTime;
        return deltaTime;
#else
        return 0f;
#endif
    }

    private void ClearRuntimeParticles()
    {
        emissionAccumulator = 0f;
        if (petalStates != null)
        {
            for (int i = 0; i < petalStates.Length; i++)
            {
                petalStates[i] = new 花瓣状态();
            }
        }

        if (cachedParticleSystem != null)
        {
            cachedParticleSystem.Clear(true);
        }
    }

    private void SpawnByDeltaTime(float deltaTime)
    {
        emissionAccumulator += Mathf.Max(0f, 每秒数量) * deltaTime;
        int spawnCount = Mathf.FloorToInt(emissionAccumulator);
        if (spawnCount <= 0)
        {
            return;
        }

        emissionAccumulator -= spawnCount;
        for (int i = 0; i < spawnCount; i++)
        {
            if (!SpawnOnePetal())
            {
                return;
            }
        }
    }

    private void 预热粒子()
    {
        if (!Application.isPlaying || 预热时间 <= 0f || !CanUpdateParticles())
        {
            return;
        }

        float step = Mathf.Max(0.02f, 1f / Mathf.Max(1f, 每秒数量));
        float elapsed = 0f;
        while (elapsed < 预热时间)
        {
            float deltaTime = Mathf.Min(step, 预热时间 - elapsed);
            SpawnByDeltaTime(deltaTime);
            UpdateParticles(deltaTime);
            elapsed += deltaTime;
        }
    }

    private bool SpawnOnePetal()
    {
        int index = FindInactiveStateIndex();
        if (index < 0)
        {
            return false;
        }

        Vector3 skyAndGroundOffset = ResolveSkyDirectionOffset();
        Vector3 ground = ResolveGroundPosition() + skyAndGroundOffset;
        Vector3 sky = ground + Vector3.up * 天空高度;

        petalStates[index] = new 花瓣状态
        {
            激活 = true,
            天空位置 = sky,
            地面位置 = ground,
            已存活时间 = 0f,
            下落时间 = Random.Range(下落时间.x, 下落时间.y),
            停留时间 = Random.Range(地面停留时间.x, 地面停留时间.y),
            消失时间 = Random.Range(透明消失时间.x, 透明消失时间.y),
            尺寸 = Random.Range(花瓣尺寸.x, 花瓣尺寸.y),
            旋转角度 = Random.Range(0f, 360f),
            旋转速度 = Random.Range(最小旋转速度, 最大旋转速度),
            旋转轴 = Random.onUnitSphere,
            风倍率 = Random.Range(Mathf.Max(0f, 1f - 风随机强度), 1f + 风随机强度),
            摆动相位 = Random.Range(0f, Mathf.PI * 2f),
            摆动频率 = Random.Range(摆动频率范围.x, 摆动频率范围.y),
            摆动方向 = ResolveSwayDirection()
        };

        return true;
    }

    private Vector3 ResolveGroundPosition()
    {
        if (地板沙盘 != null)
        {
            float gridX = Random.Range(-0.5f, 地板沙盘.GridWidth - 0.5f);
            float gridY = Random.Range(-0.5f, 地板沙盘.GridHeight - 0.5f);
            return 地板沙盘.GetSandboxGridPointWorld(gridX, gridY) + Vector3.up * 地面浮起;
        }

        float halfWidth = 本地预览范围.x * 0.5f;
        float halfDepth = 本地预览范围.y * 0.5f;
        Vector3 localPosition = new Vector3(
            Random.Range(-halfWidth, halfWidth),
            地面浮起,
            Random.Range(-halfDepth, halfDepth));
        return transform.TransformPoint(localPosition);
    }

    private int FindInactiveStateIndex()
    {
        for (int i = 0; i < petalStates.Length; i++)
        {
            if (!petalStates[i].激活)
            {
                return i;
            }
        }

        return -1;
    }

    private void UpdateParticles(float deltaTime)
    {
        int particleCount = 0;
        for (int i = 0; i < petalStates.Length; i++)
        {
            花瓣状态 state = petalStates[i];
            if (!state.激活)
            {
                continue;
            }

            state.已存活时间 += deltaTime;
            float totalLifetime = state.下落时间 + state.停留时间 + state.消失时间;
            if (state.已存活时间 >= totalLifetime)
            {
                state.激活 = false;
                petalStates[i] = state;
                continue;
            }

            particles[particleCount] = BuildParticle(state);
            particleCount++;
            petalStates[i] = state;
        }

        cachedParticleSystem.SetParticles(particles, particleCount);
    }

    private ParticleSystem.Particle BuildParticle(花瓣状态 state)
    {
        float fallProgress = Mathf.Clamp01(state.已存活时间 / state.下落时间);
        Vector3 position = fallProgress < 1f
            ? Vector3.Lerp(state.天空位置, state.地面位置, SmoothFall(fallProgress))
            : state.地面位置;
        position += ResolveWindOffset(state);

        float fadeStart = state.下落时间 + state.停留时间;
        float alpha = state.已存活时间 <= fadeStart
            ? 1f
            : 1f - Mathf.Clamp01((state.已存活时间 - fadeStart) / state.消失时间);

        Color color = 花瓣颜色;
        ApplyExposureColor(ref color, position);
        color.a *= alpha;

        ParticleSystem.Particle particle = new ParticleSystem.Particle
        {
            position = position,
            startSize = state.尺寸,
            startColor = color,
            startLifetime = 1f,
            remainingLifetime = 1f,
            rotation3D = state.旋转轴 * (state.旋转角度 + state.旋转速度 * Mathf.Min(state.已存活时间, state.下落时间))
        };

        return particle;
    }

    private void ApplyExposureColor(ref Color color, Vector3 position)
    {
        if (!启用格子空间曝光 || 曝光强度 <= 0f || !IsInsideExposureArea(position))
        {
            return;
        }

        color.r += 曝光颜色.r * 曝光强度;
        color.g += 曝光颜色.g * 曝光强度;
        color.b += 曝光颜色.b * 曝光强度;
    }

    private bool IsInsideExposureArea(Vector3 position)
    {
        if (地板沙盘 == null || 曝光格子区域列表 == null || 曝光格子区域列表.Count == 0)
        {
            return false;
        }

        if (!TryResolveVerticalGridLine(position, out Vector2 gridAtGround, out Vector2 gridPerHeight))
        {
            return false;
        }

        for (int i = 0; i < 曝光格子区域列表.Count; i++)
        {
            曝光格子区域 area = 曝光格子区域列表[i];
            Vector2 min = new Vector2(area.起点格子.x - 0.5f, area.起点格子.y - 0.5f);
            Vector2 max = new Vector2(area.起点格子.x + area.尺寸.x - 0.5f, area.起点格子.y + area.尺寸.y - 0.5f);
            if (VerticalGridLineIntersectsArea(gridAtGround, gridPerHeight, min, max, Mathf.Max(0f, 天空高度)))
            {
                return true;
            }
        }

        return false;
    }

    private bool TryResolveVerticalGridLine(Vector3 position, out Vector2 gridAtGround, out Vector2 gridPerHeight)
    {
        gridAtGround = Vector2.zero;
        gridPerHeight = Vector2.zero;

        Vector3 origin = 地板沙盘.GetSandboxGridPointWorld(0f, 0f);
        Vector3 east = 地板沙盘.GetSandboxGridPointWorld(1f, 0f) - origin;
        Vector3 north = 地板沙盘.GetSandboxGridPointWorld(0f, 1f) - origin;

        Vector2 east2D = new Vector2(east.x, east.y);
        Vector2 north2D = new Vector2(north.x, north.y);
        float determinant = east2D.x * north2D.y - east2D.y * north2D.x;
        if (Mathf.Abs(determinant) <= 0.0001f)
        {
            return false;
        }

        Vector2 relative = new Vector2(position.x - origin.x, position.y - origin.y);
        Vector2 verticalStep = Vector2.up;
        gridAtGround = InverseBasisMultiply(relative, east2D, north2D, determinant);
        gridPerHeight = -InverseBasisMultiply(verticalStep, east2D, north2D, determinant);
        return true;
    }

    private static Vector2 InverseBasisMultiply(Vector2 value, Vector2 basisX, Vector2 basisY, float determinant)
    {
        return new Vector2(
            (value.x * basisY.y - value.y * basisY.x) / determinant,
            (basisX.x * value.y - basisX.y * value.x) / determinant);
    }

    private static bool VerticalGridLineIntersectsArea(Vector2 gridAtGround, Vector2 gridPerHeight, Vector2 min, Vector2 max, float height)
    {
        float intervalMin = 0f;
        float intervalMax = height;
        return ApplyAxisInterval(gridAtGround.x, gridPerHeight.x, min.x, max.x, ref intervalMin, ref intervalMax)
            && ApplyAxisInterval(gridAtGround.y, gridPerHeight.y, min.y, max.y, ref intervalMin, ref intervalMax);
    }

    private static bool ApplyAxisInterval(float origin, float slope, float min, float max, ref float intervalMin, ref float intervalMax)
    {
        if (Mathf.Abs(slope) <= 0.0001f)
        {
            return origin >= min && origin <= max;
        }

        float enter = (min - origin) / slope;
        float exit = (max - origin) / slope;
        if (enter > exit)
        {
            float temp = enter;
            enter = exit;
            exit = temp;
        }

        intervalMin = Mathf.Max(intervalMin, enter);
        intervalMax = Mathf.Min(intervalMax, exit);
        return intervalMin <= intervalMax;
    }

    private void NormalizeExposureAreas()
    {
        if (曝光格子区域列表 == null)
        {
            曝光格子区域列表 = new List<曝光格子区域>();
            return;
        }

        for (int i = 0; i < 曝光格子区域列表.Count; i++)
        {
            曝光格子区域 area = 曝光格子区域列表[i];
            area.尺寸.x = Mathf.Max(1, area.尺寸.x);
            area.尺寸.y = Mathf.Max(1, area.尺寸.y);
            曝光格子区域列表[i] = area;
        }
    }

    private static float SmoothFall(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private Vector3 ResolveWindOffset(花瓣状态 state)
    {
        if (!启用风)
        {
            return Vector3.zero;
        }

        Vector3 windDirection = ResolveTemplateWindDirection();
        if (windDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        windDirection.Normalize();
        float windTime = Mathf.Min(state.已存活时间, state.下落时间);
        float gust = 1f + Mathf.Sin((windTime + state.摆动相位) * 阵风频率) * 阵风强度;
        float windDistance = 风速 * state.风倍率 * Mathf.Max(0f, gust) * windTime;
        float swayDistance = Mathf.Sin(windTime * state.摆动频率 + state.摆动相位) * 摆动强度;

        return windDirection * windDistance + state.摆动方向 * swayDistance;
    }

    private Vector3 ResolveSkyDirectionOffset()
    {
        Vector3 offsetDirection = -ResolveTemplateWindDirection();
        if (offsetDirection.sqrMagnitude <= 0.0001f)
        {
            return Vector3.zero;
        }

        return offsetDirection.normalized * ResolveCurrentSkyOffsetDistance();
    }

    private float ResolveCurrentSkyOffsetDistance()
    {
        switch (风)
        {
            case 风向模板.东风:
                return 东风天空偏移;
            case 风向模板.西风:
                return 西风天空偏移;
            case 风向模板.南风:
                return 南风天空偏移;
            case 风向模板.北风:
                return 北风天空偏移;
            default:
                return 0f;
        }
    }

    private Vector3 ResolveSwayDirection()
    {
        Vector3 swayDirection = ResolveTemplateSwayDirection();
        if (swayDirection.sqrMagnitude > 0.0001f)
        {
            return swayDirection.normalized;
        }

        float angle = Random.Range(0f, Mathf.PI * 2f);
        return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
    }

    private Vector3 ResolveTemplateWindDirection()
    {
        Vector3 east = ResolveTemplateAxis(1f, 0f);
        Vector3 north = ResolveTemplateAxis(0f, 1f);

        switch (风)
        {
            case 风向模板.东风:
                return -east;
            case 风向模板.西风:
                return east;
            case 风向模板.南风:
                return north;
            case 风向模板.北风:
                return -north;
            default:
                return -east;
        }
    }

    private Vector3 ResolveTemplateSwayDirection()
    {
        switch (风)
        {
            case 风向模板.东风:
            case 风向模板.西风:
                return ResolveTemplateAxis(0f, 1f);
            case 风向模板.南风:
            case 风向模板.北风:
                return ResolveTemplateAxis(1f, 0f);
            default:
                return ResolveTemplateAxis(0f, 1f);
        }
    }

    private Vector3 ResolveTemplateAxis(float deltaGridX, float deltaGridY)
    {
        if (地板沙盘 != null)
        {
            Vector3 origin = 地板沙盘.GetSandboxGridPointWorld(0f, 0f);
            Vector3 target = 地板沙盘.GetSandboxGridPointWorld(deltaGridX, deltaGridY);
            return target - origin;
        }

        Vector3 localAxis = new Vector3(deltaGridX, 0f, deltaGridY);
        return transform.TransformDirection(localAxis);
    }

    private void ConfigureRenderer()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        cachedRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        cachedRenderer.mesh = ResolvePetalMesh();
        cachedRenderer.sortMode = ParticleSystemSortMode.Distance;
        cachedRenderer.minParticleSize = 0.001f;
        cachedRenderer.maxParticleSize = 0.5f;
        cachedRenderer.material = ResolveParticleMaterial();
    }

    private Mesh ResolvePetalMesh()
    {
        float aspect = ResolveSpriteAspect();

        if (runtimeMesh != null)
        {
            runtimeMesh.Clear();
        }
        else
        {
            runtimeMesh = new Mesh
            {
                name = "RuntimePetalParticleMesh",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        float halfWidth = 0.5f * aspect;
        const float halfHeight = 0.5f;
        runtimeMesh.vertices = new[]
        {
            new Vector3(-halfWidth, -halfHeight, 0f),
            new Vector3(halfWidth, -halfHeight, 0f),
            new Vector3(-halfWidth, halfHeight, 0f),
            new Vector3(halfWidth, halfHeight, 0f)
        };
        runtimeMesh.uv = new[]
        {
            new Vector2(0f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f)
        };
        runtimeMesh.triangles = new[]
        {
            0, 2, 1,
            2, 3, 1,
            0, 1, 2,
            2, 1, 3
        };
        runtimeMesh.RecalculateBounds();
        runtimeMesh.RecalculateNormals();
        return runtimeMesh;
    }

    private Material ResolveParticleMaterial()
    {
        Shader shader = 粒子材质模板 != null ? 粒子材质模板.shader : Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (runtimeMaterial == null || runtimeMaterial.shader != shader)
        {
            runtimeMaterial = 粒子材质模板 != null ? new Material(粒子材质模板) : new Material(shader);
            runtimeMaterial.name = "RuntimePetalParticleMaterial";
            runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        if (花瓣Sprite != null)
        {
            Texture texture = 花瓣Sprite.texture;
            runtimeMaterial.mainTexture = texture;
            SetTextureIfExists(runtimeMaterial, "_BaseMap", texture);
            SetTextureIfExists(runtimeMaterial, "_MainTex", texture);
        }

        SetColorIfExists(runtimeMaterial, "_Color", 材质颜色);
        SetColorIfExists(runtimeMaterial, "_BaseColor", 材质颜色);
        ConfigureTransparentMaterial(runtimeMaterial);
        return runtimeMaterial;
    }

    private float ResolveSpriteAspect()
    {
        if (花瓣Sprite == null || 花瓣Sprite.rect.height <= 0f)
        {
            return 0.55f;
        }

        return Mathf.Clamp(花瓣Sprite.rect.width / 花瓣Sprite.rect.height, 0.15f, 4f);
    }

    private static void SetTextureIfExists(Material material, string propertyName, Texture texture)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, texture);
        }
    }

    private static void SetColorIfExists(Material material, string propertyName, Color color)
    {
        if (material != null && material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, color);
        }
    }

    private static void ConfigureTransparentMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
        }

        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        if (material.HasProperty("_Cull"))
        {
            material.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        }

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
    }

    private static Vector2 NormalizeRange(Vector2 range, float minValue)
    {
        range.x = Mathf.Max(minValue, range.x);
        range.y = Mathf.Max(minValue, range.y);
        if (range.y < range.x)
        {
            range.y = range.x;
        }

        return range;
    }
}
