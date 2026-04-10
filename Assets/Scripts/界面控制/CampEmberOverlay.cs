using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-35)]
public sealed class CampEmberOverlay : MonoBehaviour
{
    private const string SceneName = "\u8425\u5730";
    private const string OverlayName = "CampEmberOverlay";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneName)
        {
            return;
        }

        if (FindObjectOfType<CampEmberOverlay>() != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("CampEmberOverlay: missing Main Camera in camp scene.");
            return;
        }

        GameObject overlayObject = new GameObject(OverlayName);
        overlayObject.transform.SetParent(mainCamera.transform, false);
        overlayObject.AddComponent<CampEmberOverlay>();
    }

    private void Awake()
    {
        transform.localPosition = new Vector3(0f, 0f, 8f);
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        ParticleSystem particleSystem = gameObject.AddComponent<ParticleSystem>();
        ParticleSystemRenderer renderer = gameObject.GetComponent<ParticleSystemRenderer>();

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ConfigureMainModule(particleSystem.main);
        ConfigureEmissionModule(particleSystem.emission);
        ConfigureShapeModule(particleSystem.shape);
        ConfigureVelocityModule(particleSystem.velocityOverLifetime);
        ConfigureNoiseModule(particleSystem.noise);
        ConfigureColorModule(particleSystem.colorOverLifetime);
        ConfigureSizeModule(particleSystem.sizeOverLifetime);
        ConfigureRenderer(renderer);

        particleSystem.Play();
    }

    private static void ConfigureMainModule(ParticleSystem.MainModule main)
    {
        main.duration = 6f;
        main.loop = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(3.6f, 6.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.55f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.maxParticles = 180;
        main.scalingMode = ParticleSystemScalingMode.Local;
    }

    private static void ConfigureEmissionModule(ParticleSystem.EmissionModule emission)
    {
        emission.enabled = true;
        emission.rateOverTime = 26f;
    }

    private static void ConfigureShapeModule(ParticleSystem.ShapeModule shape)
    {
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(6.5f, 2.4f, 0.2f);
        shape.position = new Vector3(4.5f, -3.1f, 0f);
    }

    private static void ConfigureVelocityModule(ParticleSystem.VelocityOverLifetimeModule velocity)
    {
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.x = new ParticleSystem.MinMaxCurve(-1.15f, -0.55f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.75f, 1.35f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.03f, 0.03f);
    }

    private static void ConfigureNoiseModule(ParticleSystem.NoiseModule noise)
    {
        noise.enabled = true;
        noise.separateAxes = true;
        noise.strengthX = 0.2f;
        noise.strengthY = 0.35f;
        noise.strengthZ = 0.08f;
        noise.frequency = 0.28f;
        noise.scrollSpeed = 0.2f;
        noise.damping = true;
        noise.quality = ParticleSystemNoiseQuality.High;
    }

    private static void ConfigureColorModule(ParticleSystem.ColorOverLifetimeModule colorOverLifetime)
    {
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.82f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.44f, 0.12f), 0.45f),
                new GradientColorKey(new Color(0.52f, 0.1f, 0.04f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.65f, 0.12f),
                new GradientAlphaKey(0.45f, 0.65f),
                new GradientAlphaKey(0f, 1f)
            });

        colorOverLifetime.color = gradient;
    }

    private static void ConfigureSizeModule(ParticleSystem.SizeOverLifetimeModule sizeOverLifetime)
    {
        sizeOverLifetime.enabled = true;

        AnimationCurve curve = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.2f, 1f),
            new Keyframe(0.75f, 0.85f),
            new Keyframe(1f, 0.15f));

        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);
    }

    private static void ConfigureRenderer(ParticleSystemRenderer renderer)
    {
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sortMode = ParticleSystemSortMode.Distance;
        renderer.minParticleSize = 0.001f;
        renderer.maxParticleSize = 0.08f;
        renderer.material = CreateParticleMaterial();
    }

    private static Material CreateParticleMaterial()
    {
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            Debug.LogError("CampEmberOverlay: missing shader 'Particles/Standard Unlit'.");
            return null;
        }

        Material material = new Material(shader);
        material.name = "CampEmberOverlayMaterial";
        material.mainTexture = CreateRadialTexture();
        material.SetFloat("_Mode", 2f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private static Texture2D CreateRadialTexture()
    {
        const int size = 64;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "CampEmberOverlayTexture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - half) / half;
                float dy = (y - half) / half;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return texture;
    }
}
