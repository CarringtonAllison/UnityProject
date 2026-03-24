using UnityEngine;

/// <summary>
/// Procedural limb animation — no Animator Controller required.
/// Reads speed state from PlayerController and drives arm/foot swing
/// plus a subtle head-bob entirely in code.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    [Header("Controller Reference")]
    [SerializeField] public PlayerController controller;

    [Header("Limb Transforms")]
    [SerializeField] public Transform leftArm;
    [SerializeField] public Transform rightArm;
    [SerializeField] public Transform leftFoot;
    [SerializeField] public Transform rightFoot;
    [SerializeField] public Transform head;

    [Header("Swing Settings")]
    [SerializeField] private float swingAngle        = 35f;   // max arm/leg swing in degrees
    [SerializeField] private float animSpeedMultiplier = 0.4f; // scales how fast limbs cycle

    [Header("Head Bob")]
    [SerializeField] private float bobAmplitude = 0.04f;
    [SerializeField] private float bobFrequency = 2f;         // multiplier on swing time

    // Arm/leg rest rotations captured at Start so we return to them cleanly
    private Quaternion _leftArmRest;
    private Quaternion _rightArmRest;
    private Quaternion _leftFootRest;
    private Quaternion _rightFootRest;
    private Vector3    _headRestLocal;

    private float _animTime;
    private float _currentAmplitude; // smoothed swing amplitude

    private void Start()
    {
        if (leftArm)   _leftArmRest   = leftArm.localRotation;
        if (rightArm)  _rightArmRest  = rightArm.localRotation;
        if (leftFoot)  _leftFootRest  = leftFoot.localRotation;
        if (rightFoot) _rightFootRest = rightFoot.localRotation;
        if (head)      _headRestLocal = head.localPosition;
    }

    private void LateUpdate()
    {
        if (controller == null) return;

        float speed = controller.CurrentSpeed;

        // Smoothly scale amplitude toward 0 when idle, 1 when moving at full speed
        float targetAmplitude = Mathf.Clamp01(speed / 5f); // 5 = walk speed reference
        _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, Time.deltaTime * 8f);

        // Advance animation clock only while moving
        _animTime += Time.deltaTime * speed * animSpeedMultiplier;

        SwingLimbs();
        BobHead();
    }

    private void SwingLimbs()
    {
        float swing = Mathf.Sin(_animTime) * swingAngle * _currentAmplitude;

        if (leftArm)
            leftArm.localRotation   = _leftArmRest   * Quaternion.Euler( swing, 0f, 0f);
        if (rightArm)
            rightArm.localRotation  = _rightArmRest  * Quaternion.Euler(-swing, 0f, 0f);
        if (leftFoot)
            leftFoot.localRotation  = _leftFootRest  * Quaternion.Euler(-swing, 0f, 0f);
        if (rightFoot)
            rightFoot.localRotation = _rightFootRest * Quaternion.Euler( swing, 0f, 0f);
    }

    private void BobHead()
    {
        if (head == null) return;

        float bobY = Mathf.Sin(_animTime * bobFrequency) * bobAmplitude * _currentAmplitude;
        float bobX = Mathf.Sin(_animTime * bobFrequency * 0.5f) * (bobAmplitude * 0.5f) * _currentAmplitude;

        head.localPosition = _headRestLocal + new Vector3(bobX, bobY, 0f);
    }
}
