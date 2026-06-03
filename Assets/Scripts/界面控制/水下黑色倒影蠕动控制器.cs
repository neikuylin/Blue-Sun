using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
[AddComponentMenu("特效/水下黑色倒影蠕动控制器")]
public sealed class 水下黑色倒影蠕动控制器 : MonoBehaviour
{
    private static readonly Vector3 战斗摄像机参考角度 = new Vector3(48.6f, 45f, 0f);
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int DirectionalFlowId = Shader.PropertyToID("_DirectionalFlow");
    private static readonly int FlowDirectionId = Shader.PropertyToID("_FlowDirection");
    private static readonly int DirectionalIntensityId = Shader.PropertyToID("_DirectionalIntensity");
    private static readonly int DirectionalWaveScaleId = Shader.PropertyToID("_DirectionalWaveScale");
    private static readonly int DirectionalSpeedId = Shader.PropertyToID("_DirectionalSpeed");
    private static readonly int DirectionalSidePullId = Shader.PropertyToID("_DirectionalSidePull");

    public enum 蠕动方向
    {
        无方向 = 0,
        东 = 1,
        南 = 2,
        西 = 3,
        北 = 4
    }

    public enum 方向基准
    {
        战斗摄像机投影 = 0,
        Sprite本地横纵 = 1
    }

    [SerializeField] private 蠕动方向 方向 = 蠕动方向.无方向;
    [SerializeField] private bool 保留原图颜色和透明度;
    [SerializeField] private 方向基准 东西南北基准 = 方向基准.战斗摄像机投影;
    [SerializeField, Range(0f, 4f)] private float 方向激烈程度 = 1.8f;
    [SerializeField, Range(0.1f, 20f)] private float 方向蠕动密度 = 8f;
    [SerializeField, Range(0f, 8f)] private float 方向推进速度 = 2.4f;
    [SerializeField, Range(0f, 3f)] private float 横切撕扯 = 1.1f;

    private SpriteRenderer spriteRenderer;
    private MaterialPropertyBlock propertyBlock;

    public 蠕动方向 当前方向 => 方向;
    public 方向基准 当前方向基准 => 东西南北基准;
    public Vector2 当前材质方向 => 取材质方向();

    private void Reset()
    {
        应用蠕动方向();
    }

    private void OnEnable()
    {
        应用蠕动方向();
    }

    private void OnDisable()
    {
        关闭方向蠕动();
    }

    private void OnValidate()
    {
        方向激烈程度 = Mathf.Clamp(方向激烈程度, 0f, 4f);
        方向蠕动密度 = Mathf.Clamp(方向蠕动密度, 0.1f, 20f);
        方向推进速度 = Mathf.Clamp(方向推进速度, 0f, 8f);
        横切撕扯 = Mathf.Clamp(横切撕扯, 0f, 3f);
        应用蠕动方向();
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying || 东西南北基准 == 方向基准.战斗摄像机投影)
        {
            应用蠕动方向();
        }
    }

    [ContextMenu("应用水下倒影蠕动方向")]
    public void 应用蠕动方向()
    {
        取组件和参数块();
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.GetPropertyBlock(propertyBlock);
        Vector2 direction = 取材质方向();
        bool hasDirection = direction.sqrMagnitude > 0.0001f;
        propertyBlock.SetFloat(DirectionalFlowId, hasDirection ? 1f : 0f);
        propertyBlock.SetVector(FlowDirectionId, new Vector4(direction.x, direction.y, 0f, 0f));
        propertyBlock.SetFloat(DirectionalIntensityId, 方向激烈程度);
        propertyBlock.SetFloat(DirectionalWaveScaleId, 方向蠕动密度);
        propertyBlock.SetFloat(DirectionalSpeedId, 方向推进速度);
        propertyBlock.SetFloat(DirectionalSidePullId, 横切撕扯);
        应用颜色模式(propertyBlock);
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void 关闭方向蠕动()
    {
        取组件和参数块();
        if (spriteRenderer == null)
        {
            return;
        }

        propertyBlock.Clear();
        spriteRenderer.SetPropertyBlock(propertyBlock);
    }

    private void 应用颜色模式(MaterialPropertyBlock block)
    {
        if (block == null)
        {
            return;
        }

        if (!保留原图颜色和透明度)
        {
            Material material = spriteRenderer != null ? spriteRenderer.sharedMaterial : null;
            if (material != null && material.HasProperty(ColorId))
            {
                block.SetColor(ColorId, material.GetColor(ColorId));
            }

            return;
        }

        block.SetColor(ColorId, Color.white);
    }

    private Vector2 取材质方向()
    {
        if (方向 == 蠕动方向.无方向)
        {
            return Vector2.zero;
        }

        Vector2 east = Vector2.right;
        Vector2 north = Vector2.up;
        if (东西南北基准 == 方向基准.战斗摄像机投影)
        {
            Quaternion cameraRotation = 取摄像机旋转();
            east = 投影到摄像机平面(Vector3.right, cameraRotation);
            north = 投影到摄像机平面(Vector3.forward, cameraRotation);
        }

        switch (方向)
        {
            case 蠕动方向.南:
                return -north;
            case 蠕动方向.西:
                return -east;
            case 蠕动方向.北:
                return north;
            default:
                return east;
        }
    }

    private void 取组件和参数块()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }

    private static Quaternion 取摄像机旋转()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            return Quaternion.Euler(战斗摄像机参考角度);
        }
#endif

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            return mainCamera.transform.rotation;
        }

        return Quaternion.Euler(战斗摄像机参考角度);
    }

    private static Vector2 投影到摄像机平面(Vector3 worldDirection, Quaternion cameraRotation)
    {
        Vector2 direction = new Vector2(
            Vector3.Dot(worldDirection, cameraRotation * Vector3.right),
            Vector3.Dot(worldDirection, cameraRotation * Vector3.up));

        if (direction.sqrMagnitude <= 0.000001f)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }
}
