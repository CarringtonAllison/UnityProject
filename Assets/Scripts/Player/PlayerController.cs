using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed   = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float jumpHeight  = 2f;
    [SerializeField] private float gravity     = -19.62f;
    [SerializeField] private float turnSpeed   = 15f;

    [Header("References")]
    [SerializeField] public Transform cameraTarget;

    // Read-only state for PlayerAnimator
    public float  CurrentSpeed      { get; private set; }
    public bool   IsGrounded        { get; private set; }
    public bool   IsSprinting       { get; private set; }
    public Vector3 HorizontalVelocity { get; private set; }

    private CharacterController _cc;
    private Vector3 _verticalVelocity;
    private float   _targetSpeed;
    private bool    _jumpRequested;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        GroundCheck();
        GatherInput();
        ApplyMovement();
        ApplyGravityAndJump();
    }

    private void GroundCheck()
    {
        IsGrounded = _cc.isGrounded;

        // Keep character pressed to ground; prevents slow drift off slopes
        if (IsGrounded && _verticalVelocity.y < 0f)
            _verticalVelocity.y = -2f;
    }

    private void GatherInput()
    {
        IsSprinting   = Input.GetKey(KeyCode.LeftShift);
        _jumpRequested = Input.GetKeyDown(KeyCode.Space) && IsGrounded;
        _targetSpeed  = IsSprinting ? sprintSpeed : walkSpeed;
    }

    private void ApplyMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 inputDir = new Vector3(h, 0f, v);

        if (inputDir.sqrMagnitude < 0.01f)
        {
            // Decelerate to a stop
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0f, Time.deltaTime * 10f);
            HorizontalVelocity = Vector3.zero;
            return;
        }

        // Normalise only when above threshold to avoid twitching at rest
        inputDir = inputDir.normalized;

        // Make movement relative to camera yaw
        float camYaw = cameraTarget != null ? cameraTarget.eulerAngles.y : 0f;
        Vector3 moveDir = Quaternion.Euler(0f, camYaw, 0f) * inputDir;

        // Smooth speed transition (acceleration feel)
        CurrentSpeed = Mathf.Lerp(CurrentSpeed, _targetSpeed, Time.deltaTime * 8f);

        // Rotate character to face movement direction
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
        }

        HorizontalVelocity = moveDir * CurrentSpeed;
        _cc.Move(HorizontalVelocity * Time.deltaTime);
    }

    private void ApplyGravityAndJump()
    {
        if (_jumpRequested)
            _verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        _verticalVelocity.y += gravity * Time.deltaTime;
        _cc.Move(_verticalVelocity * Time.deltaTime);
    }
}
