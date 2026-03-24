using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] public Transform target;

    [Header("Orbit")]
    [SerializeField] private float mouseSensitivity = 3f;
    [SerializeField] private float verticalMin      = -30f;
    [SerializeField] private float verticalMax      =  60f;

    [Header("Distance")]
    [SerializeField] private float distance     = 6f;
    [SerializeField] private float heightOffset = 1.6f;
    [SerializeField] private float minDistance  = 1.5f;

    [Header("Collision")]
    [SerializeField] private float collisionRadius = 0.2f;
    [SerializeField] private LayerMask collisionMask = ~0; // everything by default

    private float _yaw;
    private float _pitch;
    private Camera _cam;

    private void Awake()
    {
        _cam = GetComponentInChildren<Camera>();
        // Initialise yaw to current world rotation so camera doesn't snap on start
        _yaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Read mouse input
        _yaw   += Input.GetAxis("Mouse X") * mouseSensitivity;
        _pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
        _pitch  = Mathf.Clamp(_pitch, verticalMin, verticalMax);

        // Move pivot to player position + height offset
        transform.position = target.position + Vector3.up * heightOffset;
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Camera collision: shorten arm if something is in the way
        float desiredDist = distance;
        Vector3 pivotPos  = transform.position;
        Vector3 camDir    = transform.rotation * Vector3.back; // direction from pivot to camera

        if (Physics.SphereCast(pivotPos, collisionRadius, camDir, out RaycastHit hit,
                               desiredDist, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredDist = Mathf.Clamp(hit.distance - 0.1f, minDistance, distance);
        }

        // Position the Camera child
        if (_cam != null)
            _cam.transform.localPosition = new Vector3(0f, 0f, -desiredDist);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
    }
}
