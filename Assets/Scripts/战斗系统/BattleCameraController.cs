using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    public float moveSpeed = 18f;
    public float zoomSpeed = 8f;
    public float minZoom = 4f;
    public float maxZoom = 16f;

    private Camera attachedCamera;
    private Transform followTarget;
    private Vector3 lastFollowTargetPosition;
    private float focusPlaneY;
    private bool hasBoundary;
    private float westBoundary;
    private float eastBoundary;
    private float southBoundary;
    private float northBoundary;
    private float boundaryMaxOrthographicSize;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        bool isFollowingTarget = followTarget != null;

        bool positionChanged = false;
        bool zoomChanged = false;

        if (!isFollowingTarget)
        {
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");

            Vector3 right = transform.right;
            Vector3 up = transform.up;

            right.Normalize();
            up.Normalize();

            Vector3 move = (up * vertical + right * horizontal);
            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            if (move.sqrMagnitude > 0f)
            {
                transform.position += move * moveSpeed * Time.deltaTime;
                positionChanged = true;
            }
        }

        if (attachedCamera != null && attachedCamera.orthographic && Mathf.Abs(scroll) > 0.001f)
        {
            float previousSize = attachedCamera.orthographicSize;
            attachedCamera.orthographicSize = Mathf.Clamp(
                attachedCamera.orthographicSize - scroll * zoomSpeed,
                ResolveEffectiveMinZoom(),
                ResolveEffectiveMaxZoom());
            zoomChanged = !Mathf.Approximately(previousSize, attachedCamera.orthographicSize);
        }

        if (positionChanged || zoomChanged)
        {
            ApplyBoundaryConstraints();
        }
    }

    private void LateUpdate()
    {
        if (followTarget == null)
        {
            return;
        }

        Vector3 currentTargetPosition = followTarget.position;
        Vector3 delta = currentTargetPosition - lastFollowTargetPosition;
        delta = Vector3.Project(delta, transform.right) + Vector3.Project(delta, transform.up);

        if (delta.sqrMagnitude > 0f)
        {
            transform.position += delta;
            ApplyBoundaryConstraints();
        }

        lastFollowTargetPosition = currentTargetPosition;
    }

    public void SnapToTarget(Transform target, float planeY = 0f)
    {
        if (target == null)
        {
            return;
        }

        focusPlaneY = planeY;
        Vector3 focusPoint = GetCameraFocusPointOnPlane(focusPlaneY);
        Vector3 targetPoint = target.position;
        Vector3 delta = targetPoint - focusPoint;
        delta = Vector3.Project(delta, transform.right) + Vector3.Project(delta, transform.up);
        transform.position += delta;
        ApplyBoundaryConstraints();
    }

    public void StartFollowing(Transform target, float planeY = 0f, bool snapImmediately = true)
    {
        if (target == null)
        {
            StopFollowing();
            return;
        }

        if (snapImmediately)
        {
            SnapToTarget(target, planeY);
        }
        else
        {
            focusPlaneY = planeY;
        }

        followTarget = target;
        lastFollowTargetPosition = target.position;
    }

    public void StopFollowing()
    {
        followTarget = null;
    }

    public void RefreshBoundaryReferences()
    {
        if (attachedCamera == null)
        {
            attachedCamera = GetComponent<Camera>();
        }

        摄像机边界参考图[] references = FindObjectsOfType<摄像机边界参考图>(false);
        if (references == null || references.Length == 0)
        {
            hasBoundary = false;
            return;
        }

        bool hasWest = false;
        bool hasEast = false;
        bool hasSouth = false;
        bool hasNorth = false;
        westBoundary = float.PositiveInfinity;
        eastBoundary = float.NegativeInfinity;
        southBoundary = float.PositiveInfinity;
        northBoundary = float.NegativeInfinity;

        for (int i = 0; i < references.Length; i++)
        {
            摄像机边界参考图 reference = references[i];
            if (reference == null)
            {
                continue;
            }

            float value;
            if (!reference.TryCalculateBoundaryValue(attachedCamera, out value))
            {
                hasBoundary = false;
                return;
            }

            switch (reference.Direction)
            {
                case 摄像机边界参考图.边界方向.西:
                    westBoundary = Mathf.Min(westBoundary, value);
                    hasWest = true;
                    break;
                case 摄像机边界参考图.边界方向.东:
                    eastBoundary = Mathf.Max(eastBoundary, value);
                    hasEast = true;
                    break;
                case 摄像机边界参考图.边界方向.南:
                    southBoundary = Mathf.Min(southBoundary, value);
                    hasSouth = true;
                    break;
                case 摄像机边界参考图.边界方向.北:
                    northBoundary = Mathf.Max(northBoundary, value);
                    hasNorth = true;
                    break;
            }
        }

        if (!hasWest || !hasEast || !hasSouth || !hasNorth)
        {
            hasBoundary = false;
            Debug.LogError("BattleCameraController：摄像机边界参考图必须同时配置东、南、西、北四个方向。", this);
            return;
        }

        float boundaryWidth = eastBoundary - westBoundary;
        float boundaryHeight = northBoundary - southBoundary;
        if (boundaryWidth <= 0f || boundaryHeight <= 0f)
        {
            hasBoundary = false;
            Debug.LogError("BattleCameraController：摄像机边界范围无效，东必须大于西，北必须大于南。", this);
            return;
        }

        if (attachedCamera == null || !attachedCamera.orthographic || attachedCamera.aspect <= 0f)
        {
            hasBoundary = false;
            Debug.LogError("BattleCameraController：摄像机边界只支持有效的正交摄像机。", this);
            return;
        }

        boundaryMaxOrthographicSize = Mathf.Min(
            boundaryWidth / (2f * attachedCamera.aspect),
            boundaryHeight * 0.5f);
        if (boundaryMaxOrthographicSize <= 0f)
        {
            hasBoundary = false;
            Debug.LogError("BattleCameraController：摄像机边界无法得到有效缩放上限。", this);
            return;
        }

        hasBoundary = true;
        attachedCamera.orthographicSize = Mathf.Clamp(
            attachedCamera.orthographicSize,
            ResolveEffectiveMinZoom(),
            ResolveEffectiveMaxZoom());
        ApplyBoundaryConstraints();
    }

    private void ApplyBoundaryConstraints()
    {
        if (!hasBoundary || attachedCamera == null || !attachedCamera.orthographic)
        {
            return;
        }

        float halfHeight = attachedCamera.orthographicSize;
        float halfWidth = halfHeight * attachedCamera.aspect;
        float minCenterX = westBoundary + halfWidth;
        float maxCenterX = eastBoundary - halfWidth;
        float minCenterY = southBoundary + halfHeight;
        float maxCenterY = northBoundary - halfHeight;
        if (minCenterX > maxCenterX || minCenterY > maxCenterY)
        {
            Debug.LogError("BattleCameraController：摄像机可视范围大于边界范围，无法限制摄像机。", this);
            return;
        }

        Vector3 right = transform.right.normalized;
        Vector3 up = transform.up.normalized;
        float currentX = Vector3.Dot(transform.position, right);
        float currentY = Vector3.Dot(transform.position, up);
        float clampedX = Mathf.Clamp(currentX, minCenterX, maxCenterX);
        float clampedY = Mathf.Clamp(currentY, minCenterY, maxCenterY);
        transform.position += right * (clampedX - currentX) + up * (clampedY - currentY);
    }

    private float ResolveEffectiveMaxZoom()
    {
        return hasBoundary
            ? Mathf.Min(maxZoom, boundaryMaxOrthographicSize)
            : maxZoom;
    }

    private float ResolveEffectiveMinZoom()
    {
        return hasBoundary
            ? Mathf.Min(minZoom, ResolveEffectiveMaxZoom())
            : minZoom;
    }

    private Vector3 GetCameraFocusPointOnPlane(float planeY)
    {
        if (attachedCamera == null)
        {
            return transform.position;
        }

        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
        Ray centerRay = attachedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        float enter;
        if (plane.Raycast(centerRay, out enter))
        {
            return centerRay.GetPoint(enter);
        }

        Vector3 fallback = transform.position;
        fallback.y = planeY;
        return fallback;
    }
}
