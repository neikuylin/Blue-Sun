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

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        bool isFollowingTarget = followTarget != null;

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

            transform.position += move * moveSpeed * Time.deltaTime;
        }

        if (attachedCamera != null && attachedCamera.orthographic && Mathf.Abs(scroll) > 0.001f)
        {
            attachedCamera.orthographicSize = Mathf.Clamp(
                attachedCamera.orthographicSize - scroll * zoomSpeed,
                minZoom,
                maxZoom);
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
