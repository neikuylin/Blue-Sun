using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public sealed class CampMousePanController : MonoBehaviour
{
    private const string SceneName = "\u8425\u5730";
    private const string CanvasPath = "Canvas";

    [Serializable]
    public sealed class MovingImage
    {
        public string name = "Image";
        public RectTransform target;
        public Vector2 moveSpeed = Vector2.zero;

        [NonSerialized] public Vector2 initialAnchoredPosition;
    }

    [SerializeField] private RectTransform viewportRoot;
    [SerializeField] private float followSpeed = 6f;
    [SerializeField] private List<MovingImage> images = new List<MovingImage>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != SceneName)
        {
            return;
        }

        if (FindObjectOfType<CampMousePanController>() != null)
        {
            return;
        }

        Transform canvasTransform = SceneHierarchyPathUtility.Find(activeScene, CanvasPath);
        if (canvasTransform == null)
        {
            Debug.LogWarning("CampMousePanController: missing Canvas in camp scene.");
            return;
        }

        CampMousePanController controller = canvasTransform.gameObject.AddComponent<CampMousePanController>();
        controller.viewportRoot = canvasTransform as RectTransform;
        controller.RefreshBindings();
    }

    private void Awake()
    {
        RefreshBindings();
    }

    private void OnEnable()
    {
        RefreshBindings();
    }

    private void OnValidate()
    {
        followSpeed = Mathf.Max(0.01f, followSpeed);
        RefreshBindings();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!EnsureReferences())
        {
            return;
        }

        Vector2 mouseOffset = ResolveMouseOffset();
        float t = 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime);

        for (int i = 0; i < images.Count; i++)
        {
            MovingImage image = images[i];
            if (image == null || image.target == null)
            {
                continue;
            }

            Vector2 targetPosition = image.initialAnchoredPosition + new Vector2(
                mouseOffset.x * image.moveSpeed.x,
                mouseOffset.y * image.moveSpeed.y);

            image.target.anchoredPosition = Vector2.Lerp(image.target.anchoredPosition, targetPosition, t);
        }
    }

    [ContextMenu("Refresh Bindings")]
    public void RefreshBindings()
    {
        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        for (int i = images.Count - 1; i >= 0; i--)
        {
            MovingImage image = images[i];
            if (image == null)
            {
                images.RemoveAt(i);
                continue;
            }

            if (image.target == null)
            {
                continue;
            }

            image.initialAnchoredPosition = image.target.anchoredPosition;
            if (string.IsNullOrWhiteSpace(image.name))
            {
                image.name = image.target.name;
            }
        }
    }

    private bool EnsureReferences()
    {
        if (viewportRoot == null)
        {
            viewportRoot = SceneHierarchyPathUtility.FindInActiveScene(CanvasPath) as RectTransform;
        }

        return viewportRoot != null;
    }

    private Vector2 ResolveMouseOffset()
    {
        if (!Application.isFocused)
        {
            return Vector2.zero;
        }

        Vector3 mousePosition = Input.mousePosition;
        if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
        {
            return Vector2.zero;
        }

        float width = Mathf.Max(1f, Screen.width);
        float height = Mathf.Max(1f, Screen.height);

        return new Vector2(
            (mousePosition.x / width - 0.5f) * 2f,
            (mousePosition.y / height - 0.5f) * 2f);
    }
}
