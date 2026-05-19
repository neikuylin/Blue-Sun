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
    private const string DefaultClipMaterialResourcePath = "黑色尖条流动裁切材质";
    private const string DefaultClipShaderName = "项目/特效/黑色尖条流动裁切";
    private static readonly Vector3 DefaultBattleCameraEuler = new Vector3(48.6f, 45f, 0f);
    private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
    private static readonly int ClipRectPropertyId = Shader.PropertyToID("_ClipRect");

    private struct 尖条粒子状态
    {
        public bool 激活;
        public float 存活时间;
        public float 生命周期;
        public float 速度;
        public Vector2 起点;
        public Vector2 方向;
        public float 距离;
        public float 长度;
        public float 宽度;
        public float 透明度;
        public float 旋转偏移;
    }

    public enum 流动起始点
    {
        东 = 0,
        南 = 1,
        西 = 2,
        北 = 3
    }

    [Header("预览")]
    [SerializeField] private bool 编辑模式预览 = true;

    [Header("朝向")]
    [SerializeField] private bool 平行于摄像头的旋转角度;

    [Header("材质")]
    [SerializeField] private Material 粒子材质模板;
    [SerializeField] private Color 粒子颜色 = new Color(0f, 0f, 0f, 0.85f);
    [SerializeField] private int 排序层级 = 0;

    [Header("范围")]
    [SerializeField] private float 起点X = -1f;
    [SerializeField] private float 终点X = 1f;
    [SerializeField] private Vector2 出生Y范围 = new Vector2(-1f, 1f);
    [SerializeField, Min(0f)] private float 越界余量 = 0.35f;

    [Header("流向")]
    [SerializeField] private 流动起始点 起始点 = 流动起始点.西;

    [Header("范围预览")]
    [SerializeField] private bool 显示范围平面 = true;
    [SerializeField] private bool 仅选中时显示范围 = false;
    [SerializeField] private Color 范围平面颜色 = new Color(0f, 0f, 0f, 0.12f);
    [SerializeField] private Color 范围边框颜色 = new Color(0f, 0f, 0f, 0.65f);

    [Header("粒子")]
    [SerializeField, Min(0f)] private float 每秒数量 = 10f;
    [SerializeField, Min(1)] private int 最大数量 = 80;
    [SerializeField] private Vector2 速度范围 = new Vector2(1.6f, 2.6f);
    [SerializeField] private Vector2 长度范围 = new Vector2(0.65f, 1.15f);
    [SerializeField] private Vector2 宽度范围 = new Vector2(0.06f, 0.13f);
    [SerializeField, Range(0f, 1f)] private float 淡入淡出比例 = 0.12f;
    [SerializeField, Min(0f)] private float 随机旋转角度 = 3f;

    [Header("形状")]
    [SerializeField, Range(0.05f, 0.45f)] private float 尖端长度比例 = 0.22f;

    private ParticleSystem cachedParticleSystem;
    private ParticleSystemRenderer cachedRenderer;
    private ParticleSystem.Particle[] particles;
    private 尖条粒子状态[] particleStates;
    private Material runtimeMaterial;
    private Material runtimeMaterialTemplateSource;
    private MaterialPropertyBlock materialPropertyBlock;
    private Mesh runtimeMesh;
    private float emissionAccumulator;
    private double lastEditorUpdateTime;

    public float 范围起点X => 起点X;
    public float 范围终点X => 终点X;
    public Vector2 范围Y => 出生Y范围;
    public bool Scene显示范围平面 => 显示范围平面;
    public Color Scene范围平面颜色 => 范围平面颜色;
    public Color Scene范围边框颜色 => 范围边框颜色;

#if UNITY_EDITOR
    public void Editor设置范围(float newStartX, float newEndX, Vector2 newYRange)
    {
        起点X = newStartX;
        终点X = newEndX;
        出生Y范围 = NormalizeRange(newYRange, -10000f);
        应用设置();
        EditorUtility.SetDirty(this);
    }
#endif

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
        ClearRuntimeParticles();
    }

    private void OnDestroy()
    {
        if (runtimeMesh != null)
        {
            DestroyRuntimeObject(runtimeMesh);
            runtimeMesh = null;
        }

        if (runtimeMaterial != null)
        {
            DestroyRuntimeObject(runtimeMaterial);
            runtimeMaterial = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (!仅选中时显示范围)
        {
            DrawRangePreview();
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawRangePreview();
    }

    private void OnValidate()
    {
        最大数量 = Mathf.Max(1, 最大数量);
        每秒数量 = Mathf.Max(0f, 每秒数量);
        速度范围 = NormalizeRange(速度范围, 0.01f);
        长度范围 = NormalizeRange(长度范围, 0.01f);
        宽度范围 = NormalizeRange(宽度范围, 0.001f);
        出生Y范围 = NormalizeRange(出生Y范围, -10000f);
        越界余量 = Mathf.Max(0f, 越界余量);
        随机旋转角度 = Mathf.Max(0f, 随机旋转角度);
        淡入淡出比例 = Mathf.Clamp01(淡入淡出比例);
        尖端长度比例 = Mathf.Clamp(尖端长度比例, 0.05f, 0.45f);
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
        if (!Application.isPlaying && !编辑模式预览)
        {
            return;
        }

        ResolveComponents();
        EnsureCapacity();
        RefreshRendererMaterialAndClipRect();

        if (cachedParticleSystem == null)
        {
            return;
        }

        float deltaTime = ResolveDeltaTime();
        if (!cachedParticleSystem.isPlaying)
        {
            cachedParticleSystem.Play(false);
        }

        SpawnByDeltaTime(deltaTime);
        UpdateParticles(deltaTime);

#if UNITY_EDITOR
        if (!Application.isPlaying && 编辑模式预览)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    [ContextMenu("重新预览黑色尖条流动")]
    public void 重新预览()
    {
        ClearRuntimeParticles();
        emissionAccumulator = 0f;
    }

    [ContextMenu("应用黑色尖条流动设置")]
    public void 应用设置()
    {
        ResolveComponents();
        EnsureCapacity();
        ConfigureParticleSystem();
        ConfigureRenderer();
    }

    [ContextMenu("设为正方形范围")]
    public void 设为正方形范围()
    {
        起点X = -1f;
        终点X = 1f;
        出生Y范围 = new Vector2(-1f, 1f);
        应用设置();

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        SceneView.RepaintAll();
#endif
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

        if (particleStates == null || particleStates.Length != capacity)
        {
            particleStates = new 尖条粒子状态[capacity];
        }
    }

    private void ConfigureParticleSystem()
    {
        if (cachedParticleSystem == null)
        {
            return;
        }

        ParticleSystem.MainModule main = cachedParticleSystem.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
        main.customSimulationSpace = transform;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = Mathf.Max(1, 最大数量);
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 1f;
        main.startSize3D = true;
        main.startColor = 粒子颜色;
        main.startRotation3D = true;

        ParticleSystem.EmissionModule emission = cachedParticleSystem.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = cachedParticleSystem.shape;
        shape.enabled = false;
        ParticleSystem.VelocityOverLifetimeModule velocity = cachedParticleSystem.velocityOverLifetime;
        velocity.enabled = false;
        ParticleSystem.NoiseModule noise = cachedParticleSystem.noise;
        noise.enabled = false;
        ParticleSystem.SizeOverLifetimeModule size = cachedParticleSystem.sizeOverLifetime;
        size.enabled = false;
        ParticleSystem.ColorOverLifetimeModule color = cachedParticleSystem.colorOverLifetime;
        color.enabled = false;
        ParticleSystem.TextureSheetAnimationModule textureSheet = cachedParticleSystem.textureSheetAnimation;
        textureSheet.enabled = false;
    }

    private static Quaternion ResolveBattleCameraRotation()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return Quaternion.Euler(DefaultBattleCameraEuler);
        }
#endif

        BattleCameraController battleCameraController = FindObjectOfType<BattleCameraController>();
        if (battleCameraController != null)
        {
            return battleCameraController.transform.rotation;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null && mainCamera.GetComponent<BattleCameraController>() != null)
        {
            return mainCamera.transform.rotation;
        }

        return Quaternion.Euler(DefaultBattleCameraEuler);
    }

    private void ConfigureRenderer()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        cachedRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        cachedRenderer.mesh = ResolveStripMesh();
        cachedRenderer.sortMode = ParticleSystemSortMode.Distance;
        cachedRenderer.alignment = ParticleSystemRenderSpace.Local;
        cachedRenderer.minParticleSize = 0.001f;
        cachedRenderer.maxParticleSize = 20f;
        cachedRenderer.sortingOrder = 排序层级;
        cachedRenderer.sharedMaterial = ResolveParticleMaterial();
        ApplyRendererProperties();
    }

    private void RefreshRendererMaterialAndClipRect()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        if (cachedRenderer.sharedMaterial == null || cachedRenderer.sharedMaterial != runtimeMaterial)
        {
            cachedRenderer.sharedMaterial = ResolveParticleMaterial();
        }

        ApplyRendererProperties();
    }

    private void SpawnByDeltaTime(float deltaTime)
    {
        if (每秒数量 <= 0f || deltaTime <= 0f)
        {
            return;
        }

        emissionAccumulator += 每秒数量 * deltaTime;
        int spawnCount = Mathf.FloorToInt(emissionAccumulator);
        if (spawnCount <= 0)
        {
            return;
        }

        emissionAccumulator -= spawnCount;
        for (int i = 0; i < spawnCount; i++)
        {
            SpawnOneParticle();
        }
    }

    private void SpawnOneParticle()
    {
        if (particleStates == null)
        {
            return;
        }

        int index = FindReusableIndex();
        if (index < 0)
        {
            return;
        }

        float speed = Random.Range(速度范围.x, 速度范围.y);
        ResolveFlowAxes(
            out Vector2 direction,
            out Vector2 perpendicular,
            out float startMain,
            out float endMain,
            out float crossMin,
            out float crossMax);

        float travelDistance = Mathf.Abs(endMain - startMain) + 越界余量 * 2f;
        Vector2 startPoint = direction * (startMain - 越界余量) +
            perpendicular * Random.Range(crossMin, crossMax);

        particleStates[index] = new 尖条粒子状态
        {
            激活 = true,
            存活时间 = 0f,
            生命周期 = travelDistance / Mathf.Max(0.001f, speed),
            速度 = speed,
            起点 = startPoint,
            方向 = direction,
            距离 = travelDistance,
            长度 = Random.Range(长度范围.x, 长度范围.y),
            宽度 = Random.Range(宽度范围.x, 宽度范围.y),
            透明度 = 粒子颜色.a,
            旋转偏移 = Random.Range(-随机旋转角度, 随机旋转角度)
        };
    }

    private void UpdateParticles(float deltaTime)
    {
        if (particleStates == null || particles == null || cachedParticleSystem == null)
        {
            return;
        }

        int particleCount = 0;
        for (int i = 0; i < particleStates.Length; i++)
        {
            尖条粒子状态 state = particleStates[i];
            if (!state.激活)
            {
                continue;
            }

            state.存活时间 += deltaTime;
            float traveledDistance = state.速度 * state.存活时间;
            if (state.存活时间 >= state.生命周期 || traveledDistance > state.距离)
            {
                state.激活 = false;
                particleStates[i] = state;
                continue;
            }

            particleStates[i] = state;
            Vector2 center = state.起点 + state.方向 * traveledDistance;
            if (TryBuildParticle(state, center, out ParticleSystem.Particle particle))
            {
                particles[particleCount] = particle;
                particleCount++;
            }
        }

        cachedParticleSystem.SetParticles(particles, particleCount);
    }

    private bool TryBuildParticle(尖条粒子状态 state, Vector2 center, out ParticleSystem.Particle particle)
    {
        float clipMinX = Mathf.Min(起点X, 终点X);
        float clipMaxX = Mathf.Max(起点X, 终点X);
        float clipMinY = Mathf.Min(出生Y范围.x, 出生Y范围.y);
        float clipMaxY = Mathf.Max(出生Y范围.x, 出生Y范围.y);
        bool axisAligned = Mathf.Abs(state.方向.x) > 0.999f || Mathf.Abs(state.方向.y) > 0.999f;

        if (!axisAligned)
        {
            if (!StripIntersectsClipRect(center, state.方向, state.长度, state.宽度, clipMinX, clipMaxX, clipMinY, clipMaxY))
            {
                particle = default;
                return false;
            }

            particle = CreateParticle(state, center, state.长度, state.宽度);
            return true;
        }

        bool horizontal = Mathf.Abs(state.方向.x) > 0.999f;
        float halfLength = state.长度 * 0.5f;
        float halfWidth = state.宽度 * 0.5f;

        float stripMinX = center.x - (horizontal ? halfLength : halfWidth);
        float stripMaxX = center.x + (horizontal ? halfLength : halfWidth);
        float visibleMinX = Mathf.Max(stripMinX, clipMinX);
        float visibleMaxX = Mathf.Min(stripMaxX, clipMaxX);
        float visibleLength = visibleMaxX - visibleMinX;

        float stripMinY = center.y - (horizontal ? halfWidth : halfLength);
        float stripMaxY = center.y + (horizontal ? halfWidth : halfLength);
        float visibleMinY = Mathf.Max(stripMinY, clipMinY);
        float visibleMaxY = Mathf.Min(stripMaxY, clipMaxY);
        float visibleWidth = visibleMaxY - visibleMinY;

        if (visibleLength <= 0.001f || visibleWidth <= 0.001f)
        {
            particle = default;
            return false;
        }

        Vector2 particleCenter = new Vector2((visibleMinX + visibleMaxX) * 0.5f, (visibleMinY + visibleMaxY) * 0.5f);
        float particleLength = horizontal ? visibleLength : visibleWidth;
        float particleWidth = horizontal ? visibleWidth : visibleLength;
        particle = CreateParticle(state, particleCenter, particleLength, particleWidth);
        return true;
    }

    private ParticleSystem.Particle CreateParticle(尖条粒子状态 state, Vector2 center, float length, float width)
    {
        float progress = Mathf.Clamp01(state.存活时间 / Mathf.Max(0.001f, state.生命周期));
        float fade = ResolveFade(progress);
        Color color = 粒子颜色;
        color.a = state.透明度 * fade;

        return new ParticleSystem.Particle
        {
            position = new Vector3(center.x, center.y, 0f),
            startLifetime = state.生命周期,
            remainingLifetime = Mathf.Max(0.001f, state.生命周期 - state.存活时间),
            startColor = color,
            startSize3D = new Vector3(length, width, 1f),
            rotation3D = new Vector3(0f, 0f, ResolveDirectionAngle(state.方向) + state.旋转偏移)
        };
    }

    private void ResolveFlowAxes(
        out Vector2 direction,
        out Vector2 perpendicular,
        out float startMain,
        out float endMain,
        out float crossMin,
        out float crossMax)
    {
        float minX = Mathf.Min(起点X, 终点X);
        float maxX = Mathf.Max(起点X, 终点X);
        float minY = Mathf.Min(出生Y范围.x, 出生Y范围.y);
        float maxY = Mathf.Max(出生Y范围.x, 出生Y范围.y);

        direction = ResolveFlowDirection();
        perpendicular = new Vector2(-direction.y, direction.x);

        Vector2[] corners =
        {
            new Vector2(minX, minY),
            new Vector2(minX, maxY),
            new Vector2(maxX, minY),
            new Vector2(maxX, maxY)
        };

        startMain = float.PositiveInfinity;
        endMain = float.NegativeInfinity;
        crossMin = float.PositiveInfinity;
        crossMax = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            float main = Vector2.Dot(corners[i], direction);
            float cross = Vector2.Dot(corners[i], perpendicular);
            startMain = Mathf.Min(startMain, main);
            endMain = Mathf.Max(endMain, main);
            crossMin = Mathf.Min(crossMin, cross);
            crossMax = Mathf.Max(crossMax, cross);
        }
    }

    private Vector2 ResolveFlowDirection()
    {
        Vector2 east = Vector2.right;
        Vector2 north = Vector2.up;

        if (平行于摄像头的旋转角度)
        {
            Quaternion battleCameraRotation = ResolveBattleCameraRotation();
            east = ProjectWorldDirectionToCameraPlane(Vector3.right, battleCameraRotation, east);
            north = ProjectWorldDirectionToCameraPlane(Vector3.forward, battleCameraRotation, north);
        }

        switch (起始点)
        {
            case 流动起始点.东:
                return -east;
            case 流动起始点.南:
                return north;
            case 流动起始点.北:
                return -north;
            default:
                return east;
        }
    }

    private static Vector2 ProjectWorldDirectionToCameraPlane(Vector3 worldDirection, Quaternion cameraRotation, Vector2 fallback)
    {
        Vector2 projected = new Vector2(
            Vector3.Dot(worldDirection, cameraRotation * Vector3.right),
            Vector3.Dot(worldDirection, cameraRotation * Vector3.up));

        if (projected.sqrMagnitude <= 0.0001f)
        {
            return fallback.normalized;
        }

        return projected.normalized;
    }

    private static bool StripIntersectsClipRect(
        Vector2 center,
        Vector2 direction,
        float length,
        float width,
        float minX,
        float maxX,
        float minY,
        float maxY)
    {
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float halfLength = length * 0.5f;
        float halfWidth = width * 0.5f;

        Vector2[] corners =
        {
            center + direction * halfLength + perpendicular * halfWidth,
            center + direction * halfLength - perpendicular * halfWidth,
            center - direction * halfLength + perpendicular * halfWidth,
            center - direction * halfLength - perpendicular * halfWidth
        };

        float stripMinX = float.PositiveInfinity;
        float stripMaxX = float.NegativeInfinity;
        float stripMinY = float.PositiveInfinity;
        float stripMaxY = float.NegativeInfinity;

        for (int i = 0; i < corners.Length; i++)
        {
            stripMinX = Mathf.Min(stripMinX, corners[i].x);
            stripMaxX = Mathf.Max(stripMaxX, corners[i].x);
            stripMinY = Mathf.Min(stripMinY, corners[i].y);
            stripMaxY = Mathf.Max(stripMaxY, corners[i].y);
        }

        return stripMaxX >= minX && stripMinX <= maxX && stripMaxY >= minY && stripMinY <= maxY;
    }

    private static float ResolveDirectionAngle(Vector2 direction)
    {
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private float ResolveFade(float progress)
    {
        if (淡入淡出比例 <= 0f)
        {
            return 1f;
        }

        float fadeIn = Mathf.Clamp01(progress / 淡入淡出比例);
        float fadeOut = Mathf.Clamp01((1f - progress) / 淡入淡出比例);
        return Mathf.Min(fadeIn, fadeOut);
    }

    private int FindReusableIndex()
    {
        for (int i = 0; i < particleStates.Length; i++)
        {
            if (!particleStates[i].激活)
            {
                return i;
            }
        }

        return -1;
    }

    private Mesh ResolveStripMesh()
    {
        if (runtimeMesh == null)
        {
            runtimeMesh = new Mesh
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

        Vector2[] uvs =
        {
            new Vector2(0f, 0.5f),
            new Vector2(尖端长度比例, 1f),
            new Vector2(1f - 尖端长度比例, 1f),
            new Vector2(1f, 0.5f),
            new Vector2(1f - 尖端长度比例, 0f),
            new Vector2(尖端长度比例, 0f)
        };

        runtimeMesh.Clear();
        runtimeMesh.vertices = vertices;
        runtimeMesh.triangles = triangles;
        runtimeMesh.uv = uvs;
        runtimeMesh.RecalculateBounds();
        runtimeMesh.RecalculateNormals();
        return runtimeMesh;
    }

    private Material ResolveParticleMaterial()
    {
        Material materialTemplate = 粒子材质模板 != null
            ? 粒子材质模板
            : Resources.Load<Material>(DefaultClipMaterialResourcePath);

        if (runtimeMaterial != null && runtimeMaterialTemplateSource == materialTemplate)
        {
            return runtimeMaterial;
        }

        if (runtimeMaterial != null)
        {
            DestroyRuntimeObject(runtimeMaterial);
        }

        runtimeMaterialTemplateSource = materialTemplate;
        runtimeMaterial = materialTemplate != null
            ? new Material(materialTemplate)
            : new Material(ResolveDefaultShader());
        runtimeMaterial.name = "运行时_黑色尖条粒子材质";
        runtimeMaterial.hideFlags = HideFlags.DontSave;
        return runtimeMaterial;
    }

    private void ApplyRendererProperties()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        if (materialPropertyBlock == null)
        {
            materialPropertyBlock = new MaterialPropertyBlock();
        }

        float minX = Mathf.Min(起点X, 终点X);
        float maxX = Mathf.Max(起点X, 终点X);
        float minY = Mathf.Min(出生Y范围.x, 出生Y范围.y);
        float maxY = Mathf.Max(出生Y范围.x, 出生Y范围.y);
        Vector4 clipRect = new Vector4(minX, minY, maxX, maxY);

        Material currentMaterial = cachedRenderer.sharedMaterial;
        if (currentMaterial != null)
        {
            currentMaterial.SetColor(ColorPropertyId, Color.white);
            currentMaterial.SetVector(ClipRectPropertyId, clipRect);
        }

        cachedRenderer.GetPropertyBlock(materialPropertyBlock);
        materialPropertyBlock.SetColor(ColorPropertyId, Color.white);
        materialPropertyBlock.SetVector(ClipRectPropertyId, clipRect);
        cachedRenderer.SetPropertyBlock(materialPropertyBlock);
    }

    private static Shader ResolveDefaultShader()
    {
        Shader shader = Shader.Find(DefaultClipShaderName);
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            return shader;
        }

        shader = Shader.Find("Unlit/Color");
        if (shader != null)
        {
            return shader;
        }

        return Shader.Find("Standard");
    }

    private float ResolveDeltaTime()
    {
        if (Application.isPlaying)
        {
            return Time.deltaTime;
        }

#if UNITY_EDITOR
        double now = EditorApplication.timeSinceStartup;
        if (lastEditorUpdateTime <= 0)
        {
            lastEditorUpdateTime = now;
            return 0f;
        }

        float deltaTime = Mathf.Clamp((float)(now - lastEditorUpdateTime), 0f, 0.05f);
        lastEditorUpdateTime = now;
        return deltaTime;
#else
        return 0f;
#endif
    }

    private void ClearRuntimeParticles()
    {
        if (particleStates != null)
        {
            for (int i = 0; i < particleStates.Length; i++)
            {
                particleStates[i].激活 = false;
            }
        }

        if (cachedParticleSystem != null)
        {
            cachedParticleSystem.Clear(true);
        }
    }

    private void DrawRangePreview()
    {
        if (!显示范围平面)
        {
            return;
        }

        float minX = Mathf.Min(起点X, 终点X);
        float maxX = Mathf.Max(起点X, 终点X);
        float minY = Mathf.Min(出生Y范围.x, 出生Y范围.y);
        float maxY = Mathf.Max(出生Y范围.x, 出生Y范围.y);

        Vector3 bottomLeft = transform.TransformPoint(new Vector3(minX, minY, 0f));
        Vector3 topLeft = transform.TransformPoint(new Vector3(minX, maxY, 0f));
        Vector3 topRight = transform.TransformPoint(new Vector3(maxX, maxY, 0f));
        Vector3 bottomRight = transform.TransformPoint(new Vector3(maxX, minY, 0f));

#if UNITY_EDITOR
        Handles.DrawSolidRectangleWithOutline(
            new[] { bottomLeft, topLeft, topRight, bottomRight },
            范围平面颜色,
            范围边框颜色);
#else
        Gizmos.color = 范围边框颜色;
        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);
#endif
    }

    private static Vector2 NormalizeRange(Vector2 range, float minimum)
    {
        float min = Mathf.Max(Mathf.Min(range.x, range.y), minimum);
        float max = Mathf.Max(Mathf.Max(range.x, range.y), minimum);
        return new Vector2(min, max);
    }

    private static void DestroyRuntimeObject(Object target)
    {
        if (target == null)
        {
            return;
        }

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
