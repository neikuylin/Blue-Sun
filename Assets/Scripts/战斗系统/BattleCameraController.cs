using UnityEngine;

public class BattleCameraController : MonoBehaviour
{
    public float moveSpeed = 18f;
    public float zoomSpeed = 8f;
    public float minZoom = 4f;
    public float maxZoom = 16f;

    private Camera attachedCamera;

    private void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        forward.y = 0f;
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        Vector3 move = (forward * vertical + right * horizontal);
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        transform.position += move * moveSpeed * Time.deltaTime;

        if (attachedCamera != null && attachedCamera.orthographic && Mathf.Abs(scroll) > 0.001f)
        {
            attachedCamera.orthographicSize = Mathf.Clamp(
                attachedCamera.orthographicSize - scroll * zoomSpeed,
                minZoom,
                maxZoom);
        }
    }
}
