using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
[AddComponentMenu("特效/水纹半月粒子系统")]
public sealed class 水纹半月粒子系统 : MonoBehaviour
{
    private struct 水纹粒子状态
    {
        public bool 激活;
        public float 存活时间;
        public float 生命周期;
        public float 角度;
        public float 扩散距离;
        public float 起始尺寸;
        public float 结束尺寸;
        public float 旋转偏移;
    }

    [SerializeField] private Material 粒子材质模板;
    [SerializeField] private Color 粒子颜色 = new Color(0f, 0f, 0f, 0.45f);
    [SerializeField, Min(0f)] private float 地面浮起 = 0.04f;
    [SerializeField] private bool 编辑模式预览 = true;

    [SerializeField, Min(0f)] private float 每秒数量 = 8f;
    [SerializeField, Min(1)] private int 最大数量 = 80;
    [SerializeField] private Vector2 生命周期 = new Vector2(1.1f, 1.7f);
    [SerializeField] private Vector2 扩散距离 = new Vector2(0.8f, 2.4f);
    [SerializeField] private Vector2 起始尺寸 = new Vector2(0.35f, 0.6f);
    [SerializeField] private Vector2 结束尺寸 = new Vector2(1.6f, 2.4f);

    [SerializeField] private float 起始角度 = 0f;
    [SerializeField] private float 结束角度 = 360f;
    [SerializeField] private bool 粒子朝向扩散方向 = true;
    [SerializeField, Min(0f)] private float 随机旋转偏移 = 8f;

    [SerializeField, Range(15f, 360f)] private float 半月弧度 = 180f;
    [SerializeField, Range(0.02f, 0.45f)] private float 弧宽比例 = 0.11f;
    [SerializeField, Range(6, 64)] private int 弧线段数 = 24;

    private ParticleSystem cachedParticleSystem;
    private ParticleSystemRenderer cachedRenderer;
    private ParticleSystem.Particle[] particles;
    private 水纹粒子状态[] particleStates;
    private Material runtimeMaterial;
    private Mesh runtimeMesh;
    private float emissionAccumulator;
    private double lastEditorUpdateTime;

    public float Scene起始角度 => 起始角度;
    public float Scene结束角度 => 结束角度;
    public float Scene角度跨度 => ResolvePositiveAngleSpan(起始角度, 结束角度);
    public float Scene最大扩散距离 => Mathf.Max(扩散距离.x, 扩散距离.y);
    public Color Scene参考颜色 => 粒子颜色;

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

    private void OnValidate()
    {
        生命周期 = NormalizeRange(生命周期, 0.05f);
        扩散距离 = NormalizeRange(扩散距离, 0f);
        起始尺寸 = NormalizeRange(起始尺寸, 0.001f);
        结束尺寸 = NormalizeRange(结束尺寸, 0.001f);
        最大数量 = Mathf.Max(1, 最大数量);
        每秒数量 = Mathf.Max(0f, 每秒数量);
        地面浮起 = Mathf.Max(0f, 地面浮起);
        随机旋转偏移 = Mathf.Max(0f, 随机旋转偏移);
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
        if (!Application.isPlaying && 编辑模式预览)
        {
            EditorApplication.QueuePlayerLoopUpdate();
            SceneView.RepaintAll();
        }
#endif
    }

    [ContextMenu("重新预览水纹")]
    public void 重新预览()
    {
        ClearRuntimeParticles();
        emissionAccumulator = 0f;
    }

    [ContextMenu("应用水纹设置")]
    public void 应用设置()
    {
        ResolveComponents();
        EnsureCapacity();
        ConfigureParticleSystem();
        ConfigureRenderer();
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
            particleStates = new 水纹粒子状态[capacity];
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
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = Mathf.Max(1, 最大数量);
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        main.startSize = 1f;
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

    private void ConfigureRenderer()
    {
        if (cachedRenderer == null)
        {
            return;
        }

        cachedRenderer.renderMode = ParticleSystemRenderMode.Mesh;
        cachedRenderer.mesh = ResolveCrescentMesh();
        cachedRenderer.sortMode = ParticleSystemSortMode.Distance;
        cachedRenderer.alignment = ParticleSystemRenderSpace.Local;
        cachedRenderer.minParticleSize = 0.001f;
        cachedRenderer.maxParticleSize = 20f;
        cachedRenderer.material = ResolveParticleMaterial();
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

        int index = FindReusableParticleIndex();
        if (index < 0)
        {
            return;
        }

        particleStates[index] = new 水纹粒子状态
        {
            激活 = true,
            存活时间 = 0f,
            生命周期 = RandomRange(生命周期),
            角度 = RandomAngle(),
            扩散距离 = RandomRange(扩散距离),
            起始尺寸 = RandomRange(起始尺寸),
            结束尺寸 = RandomRange(结束尺寸),
            旋转偏移 = Random.Range(-随机旋转偏移, 随机旋转偏移)
        };
    }

    private int FindReusableParticleIndex()
    {
        for (int i = 0; i < particleStates.Length; i++)
        {
            if (!particleStates[i].激活)
            {
                return i;
            }
        }

        int oldestIndex = 0;
        float oldestAge = float.MinValue;
        for (int i = 0; i < particleStates.Length; i++)
        {
            if (particleStates[i].存活时间 > oldestAge)
            {
                oldestAge = particleStates[i].存活时间;
                oldestIndex = i;
            }
        }

        return oldestIndex;
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
            水纹粒子状态 state = particleStates[i];
            if (!state.激活)
            {
                continue;
            }

            state.存活时间 += deltaTime;
            if (state.存活时间 >= state.生命周期)
            {
                state.激活 = false;
                particleStates[i] = state;
                continue;
            }

            particles[particleCount] = BuildParticle(state);
            particleCount++;
            particleStates[i] = state;
        }

        cachedParticleSystem.SetParticles(particles, particleCount);
    }

    private ParticleSystem.Particle BuildParticle(水纹粒子状态 state)
    {
        float progress = Mathf.Clamp01(state.存活时间 / Mathf.Max(0.001f, state.生命周期));
        float easedProgress = 1f - Mathf.Pow(1f - progress, 2f);
        Vector3 direction = AngleToWorldDirection(state.角度);
        Vector3 position = transform.position + transform.up * 地面浮起 + direction * (state.扩散距离 * easedProgress);

        Color color = 粒子颜色;
        color.a *= 1f - SmoothFade(progress);

        float size = Mathf.Lerp(state.起始尺寸, state.结束尺寸, easedProgress);
        float rotationY = 粒子朝向扩散方向 ? 90f - state.角度 + state.旋转偏移 : state.旋转偏移;

        return new ParticleSystem.Particle
        {
            position = position,
            startSize = size,
            startColor = color,
            startLifetime = 1f,
            remainingLifetime = 1f,
            rotation3D = new Vector3(0f, rotationY, 0f)
        };
    }

    private Vector3 AngleToWorldDirection(float angle)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector3 localDirection = new Vector3(Mathf.Cos(radians), 0f, Mathf.Sin(radians));
        return transform.TransformDirection(localDirection).normalized;
    }

    private float RandomAngle()
    {
        float span = ResolvePositiveAngleSpan(起始角度, 结束角度);
        if (span <= 0.001f)
        {
            return 起始角度;
        }

        return 起始角度 + Random.Range(0f, span);
    }

    private float ResolveDeltaTime()
    {
        if (Application.isPlaying)
        {
            return Time.deltaTime;
        }

#if UNITY_EDITOR
        double now = EditorApplication.timeSinceStartup;
        if (lastEditorUpdateTime <= 0d)
        {
            lastEditorUpdateTime = now;
        }

        float deltaTime = Mathf.Clamp((float)(now - lastEditorUpdateTime), 0f, 0.05f);
        lastEditorUpdateTime = now;
        return deltaTime;
#else
        return 0f;
#endif
    }

    private Mesh ResolveCrescentMesh()
    {
        if (runtimeMesh != null)
        {
            runtimeMesh.Clear();
        }
        else
        {
            runtimeMesh = new Mesh
            {
                name = "RuntimeCrescentRippleParticleMesh",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        int segments = Mathf.Clamp(弧线段数, 6, 64);
        int vertexCount = (segments + 1) * 2;
        Vector3[] vertices = new Vector3[vertexCount];
        Vector2[] uvs = new Vector2[vertexCount];
        int[] triangles = new int[segments * 6 * 2];

        float halfArc = 半月弧度 * 0.5f;
        float startAngle = 90f - halfArc;
        float outerRadius = 0.5f;
        float innerRadius = Mathf.Max(0.01f, outerRadius - 弧宽比例);
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = (startAngle + 半月弧度 * t) * Mathf.Deg2Rad;
            Vector3 outer = new Vector3(Mathf.Cos(angle) * outerRadius, 0f, Mathf.Sin(angle) * outerRadius);
            Vector3 inner = new Vector3(Mathf.Cos(angle) * innerRadius, 0f, Mathf.Sin(angle) * innerRadius);
            int vertexIndex = i * 2;
            vertices[vertexIndex] = outer;
            vertices[vertexIndex + 1] = inner;
            uvs[vertexIndex] = new Vector2(t, 1f);
            uvs[vertexIndex + 1] = new Vector2(t, 0f);
        }

        int triangleIndex = 0;
        for (int i = 0; i < segments; i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = d;
            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = a;
            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = c;
            triangles[triangleIndex++] = b;
            triangles[triangleIndex++] = d;
        }

        runtimeMesh.vertices = vertices;
        runtimeMesh.uv = uvs;
        runtimeMesh.triangles = triangles;
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
            runtimeMaterial.name = "RuntimeCrescentRippleParticleMaterial";
            runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
        }

        ConfigureTransparentMaterial(runtimeMaterial);
        SetColorIfExists(runtimeMaterial, "_Color", Color.white);
        SetColorIfExists(runtimeMaterial, "_BaseColor", Color.white);
        return runtimeMaterial;
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

    private static float SmoothFade(float progress)
    {
        float fade = Mathf.Clamp01((progress - 0.55f) / 0.45f);
        return fade * fade * (3f - 2f * fade);
    }

    private static float ResolvePositiveAngleSpan(float startAngle, float endAngle)
    {
        float rawSpan = endAngle - startAngle;
        if (Mathf.Approximately(rawSpan, 0f))
        {
            return 0f;
        }

        if (Mathf.Abs(rawSpan) >= 360f)
        {
            return 360f;
        }

        return Mathf.Repeat(rawSpan, 360f);
    }

    private static float RandomRange(Vector2 range)
    {
        return Random.Range(range.x, range.y);
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
