using UnityEngine;

[CreateAssetMenu(fileName = "SpriteOcclusionRevealSettings", menuName = "战斗/角色遮挡挖空设置")]
public sealed class SpriteOcclusionRevealSettings : ScriptableObject
{
    public const string DefaultResourcePath = "SpriteOcclusionRevealSettings";

    [SerializeField] private bool revealEnabled = true;
    [SerializeField, Min(0f)] private float radiusWorld = 1.2f;
    [SerializeField, Min(0f)] private float softnessWorld = 0.25f;
    [SerializeField, Range(1f, 32f)] private float dissolveNoiseScale = 6f;
    [SerializeField, Range(0f, 1f)] private float dissolveStrength = 0.85f;
    [SerializeField, Range(0f, 128f)] private float dissolveEdgeWidth = 18f;
    [SerializeField, Range(-256f, 256f)] private float dissolveScrollSpeed = 48f;
    [SerializeField] private bool dissolveSmoothEdges = true;

    public bool RevealEnabled => revealEnabled;
    public float RadiusWorld => Mathf.Max(0f, radiusWorld);
    public float SoftnessWorld => Mathf.Max(0f, softnessWorld);
    public float DissolveNoiseScale => Mathf.Max(1f, dissolveNoiseScale);
    public float DissolveStrength => Mathf.Clamp01(dissolveStrength);
    public float DissolveEdgeWidth => Mathf.Max(0f, dissolveEdgeWidth);
    public float DissolveScrollSpeed => dissolveScrollSpeed;
    public bool DissolveSmoothEdges => dissolveSmoothEdges;

    public static SpriteOcclusionRevealSettings LoadDefault()
    {
        return Resources.Load<SpriteOcclusionRevealSettings>(DefaultResourcePath);
    }
}
