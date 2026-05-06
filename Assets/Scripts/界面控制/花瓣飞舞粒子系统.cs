using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(ParticleSystem), typeof(ParticleSystemRenderer))]
public sealed class 花瓣飞舞粒子系统 : MonoBehaviour
{
    [Header("美术")]
    [SerializeField] private Sprite 花瓣Sprite;
    [SerializeField] private Material 粒子材质模板;

    [Header("相机前景")]
    [SerializeField] private bool 相机前景模式 = true;
    [SerializeField] private bool 自动移动到相机前方 = true;
    [SerializeField] private bool 自动适配相机画面 = true;
    [SerializeField, Min(0.31f)] private float 相机前方距离 = 5f;
    [SerializeField, Min(0.1f)] private float 屏幕覆盖倍率 = 1.2f;
    [SerializeField] private Vector2 屏幕中心偏移 = Vector2.zero;

    [Header("范围")]
    [SerializeField, Min(0f)] private float 发射宽度 = 4.5f;
    [SerializeField, Min(0f)] private float 发射高度 = 1.6f;
    [SerializeField, Min(0f)] private float 发射深度 = 0.6f;

    [Header("数量")]
    [SerializeField, Min(0f)] private float 每秒数量 = 12f;
    [SerializeField, Min(1)] private int 最大数量 = 160;

    [Header("飘动")]
    [SerializeField] private Vector2 生命周期 = new Vector2(4.5f, 8f);
    [SerializeField] private Vector2 初始速度 = new Vector2(0.08f, 0.38f);
    [SerializeField] private Vector2 花瓣尺寸 = new Vector2(0.18f, 0.36f);
    [SerializeField] private Vector2 横向风速 = new Vector2(-0.35f, 0.65f);
    [SerializeField] private Vector2 上下漂移 = new Vector2(-0.18f, 0.25f);
    [SerializeField] private Vector2 前后漂移 = new Vector2(-0.18f, 0.18f);
    [SerializeField, Min(0f)] private float 重力 = 0.035f;

    [Header("翻飞")]
    [SerializeField, Min(0f)] private float 噪声强度 = 0.45f;
    [SerializeField, Min(0.01f)] private float 噪声频率 = 0.32f;
    [SerializeField, Min(0f)] private float 翻面速度 = 145f;
    [SerializeField, Min(0f)] private float 旋转速度 = 190f;

    private ParticleSystem cachedParticleSystem;
    private ParticleSystemRenderer cachedRenderer;
    private Material runtimeMaterial;
    private Mesh runtimeMesh;

    private void Reset()
    {
        应用设置();
    }

    private void OnEnable()
    {
        应用设置();
    }

    private void OnValidate()
    {
        生命周期 = NormalizeRange(生命周期, 0.05f);
        初始速度 = NormalizeRange(初始速度, 0f);
        花瓣尺寸 = NormalizeRange(花瓣尺寸, 0.001f);
        横向风速 = NormalizeRange(横向风速, float.NegativeInfinity);
        上下漂移 = NormalizeRange(上下漂移, float.NegativeInfinity);
        前后漂移 = NormalizeRange(前后漂移, float.NegativeInfinity);

        应用设置();
    }

    [ContextMenu("重新应用花瓣粒子设置")]
    public void 应用设置()
    {
        ResolveComponents();
        if (cachedParticleSystem == null || cachedRenderer == null)
        {
            return;
        }

        ApplyForegroundCameraSettings();

        cachedParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ConfigureMain(cachedParticleSystem.main);
        ConfigureEmission(cachedParticleSystem.emission);
        ConfigureShape(cachedParticleSystem.shape);
        ConfigureVelocity(cachedParticleSystem.velocityOverLifetime);
        ConfigureNoise(cachedParticleSystem.noise);
        ConfigureRotation(cachedParticleSystem.rotationOverLifetime);
        ConfigureSize(cachedParticleSystem.sizeOverLifetime);
        ConfigureColor(cachedParticleSystem.colorOverLifetime);
        ConfigureTextureSheet(cachedParticleSystem.textureSheetAnimation);
        ConfigureRenderer();

        if (Application.isPlaying || gameObject.activeInHierarchy)
        {
            cachedParticleSystem.Play();
        }
    }

    [ContextMenu("适配当前相机前景")]
    public void 适配当前相机前景()
    {
        相机前景模式 = true;
        自动移动到相机前方 = true;
        自动适配相机画面 = true;
        相机前方距离 = Mathf.Max(0.31f, 相机前方距离);
        屏幕覆盖倍率 = Mathf.Max(1f, 屏幕覆盖倍率);
        ApplyForegroundCameraSettings();
        应用设置();
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

    private void ConfigureMain(ParticleSystem.MainModule main)
    {
        main.duration = 8f;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.scalingMode = ParticleSystemScalingMode.Local;
        main.maxParticles = Mathf.Max(1, 最大数量);
        main.gravityModifier = 重力;
        main.startLifetime = new ParticleSystem.MinMaxCurve(生命周期.x, 生命周期.y);
        main.startSpeed = new ParticleSystem.MinMaxCurve(初始速度.x, 初始速度.y);
        main.startSize = new ParticleSystem.MinMaxCurve(花瓣尺寸.x, 花瓣尺寸.y);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
    }

    private void ConfigureEmission(ParticleSystem.EmissionModule emission)
    {
        emission.enabled = true;
        emission.rateOverTime = 每秒数量;
    }

    private void ConfigureShape(ParticleSystem.ShapeModule shape)
    {
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(发射宽度, 发射高度, 发射深度);
        shape.randomDirectionAmount = 0.35f;
    }

    private void ApplyForegroundCameraSettings()
    {
        if (!相机前景模式)
        {
            return;
        }

        Camera targetCamera = ResolveTargetCamera();
        if (targetCamera == null)
        {
            return;
        }

        float visibleHeight = ResolveCameraVisibleHeight(targetCamera);
        if (visibleHeight <= 0f)
        {
            return;
        }

        if (自动移动到相机前方 && transform.parent == targetCamera.transform)
        {
            transform.localPosition = new Vector3(屏幕中心偏移.x, 屏幕中心偏移.y, 相机前方距离);
            transform.localRotation = Quaternion.identity;
        }

        if (自动适配相机画面)
        {
            float visibleWidth = visibleHeight * Mathf.Max(0.01f, targetCamera.aspect);
            发射宽度 = visibleWidth * 屏幕覆盖倍率;
            发射高度 = visibleHeight * 屏幕覆盖倍率;
            发射深度 = Mathf.Max(0.2f, 相机前方距离 * 0.25f);
        }
    }

    private Camera ResolveTargetCamera()
    {
        Camera parentCamera = GetComponentInParent<Camera>();
        if (parentCamera != null)
        {
            return parentCamera;
        }

        return Camera.main;
    }

    private float ResolveCameraVisibleHeight(Camera targetCamera)
    {
        if (targetCamera.orthographic)
        {
            return targetCamera.orthographicSize * 2f;
        }

        return 2f * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Max(0.31f, 相机前方距离);
    }

    private void ConfigureVelocity(ParticleSystem.VelocityOverLifetimeModule velocity)
    {
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(横向风速.x, 横向风速.y);
        velocity.y = new ParticleSystem.MinMaxCurve(上下漂移.x, 上下漂移.y);
        velocity.z = new ParticleSystem.MinMaxCurve(前后漂移.x, 前后漂移.y);
    }

    private void ConfigureNoise(ParticleSystem.NoiseModule noise)
    {
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = 噪声强度;
        noise.strengthY = 噪声强度 * 0.7f;
        noise.strengthZ = 噪声强度 * 0.45f;
        noise.frequency = 噪声频率;
        noise.scrollSpeed = 0.35f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;
    }

    private void ConfigureRotation(ParticleSystem.RotationOverLifetimeModule rotation)
    {
        float flipRadians = 翻面速度 * Mathf.Deg2Rad;
        float spinRadians = 旋转速度 * Mathf.Deg2Rad;

        rotation.enabled = true;
        rotation.separateAxes = true;
        rotation.x = new ParticleSystem.MinMaxCurve(-flipRadians, flipRadians);
        rotation.y = new ParticleSystem.MinMaxCurve(-flipRadians * 0.75f, flipRadians * 0.75f);
        rotation.z = new ParticleSystem.MinMaxCurve(-spinRadians, spinRadians);
    }

    private static void ConfigureSize(ParticleSystem.SizeOverLifetimeModule sizeOverLifetime)
    {
        sizeOverLifetime.enabled = true;

        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.35f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.72f, 0.9f),
            new Keyframe(1f, 0.18f));

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void ConfigureColor(ParticleSystem.ColorOverLifetimeModule colorOverLifetime)
    {
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.85f, 0.78f),
                new GradientAlphaKey(0f, 1f)
            });

        colorOverLifetime.color = gradient;
    }

    private void ConfigureTextureSheet(ParticleSystem.TextureSheetAnimationModule textureSheet)
    {
        textureSheet.enabled = 花瓣Sprite != null;
        if (花瓣Sprite == null)
        {
            return;
        }

        textureSheet.mode = ParticleSystemAnimationMode.Sprites;
        while (textureSheet.spriteCount > 0)
        {
            textureSheet.RemoveSprite(0);
        }

        textureSheet.AddSprite(花瓣Sprite);
        textureSheet.frameOverTime = 0f;
    }

    private void ConfigureRenderer()
    {
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

        Texture texture = 花瓣Sprite != null ? 花瓣Sprite.texture : Texture2D.whiteTexture;
        runtimeMaterial.mainTexture = texture;
        SetTextureIfExists(runtimeMaterial, "_BaseMap", texture);
        SetTextureIfExists(runtimeMaterial, "_MainTex", texture);
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
