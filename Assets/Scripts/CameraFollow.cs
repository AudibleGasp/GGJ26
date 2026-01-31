using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private Transform target;

    [Header("Look (Vertical)")]
    public float mouseSensitivity = 2f;
    public float lookXLimit = 85f;

    [Header("Movement Smoothing")]
    [Range(0, 1f)]
    [SerializeField] private float smoothTime = 0.03f;

    [Header("Sway & Tilt")]
    [SerializeField] private float tiltAngle = 2f;      // How much the camera rolls when strafing
    [SerializeField] private float tiltSpeed = 5f;      // How fast the camera reaches the tilt
    [SerializeField] private float bobAmount = 0.05f;   // Subtle vertical movement
    [SerializeField] private float bobFrequency = 10f;  // Speed of the bobbing

    private Vector3 _posOffset;
    private Vector3 _currentVelocity = Vector3.zero;
    private float _currentPitch = 0f;
    private float _tiltRoll = 0f;
    private float _bobTimer = 0f;
    
    private PlayerController playerController;

    void Start()
    {
        playerController = Main.Instance.PlayerController;
        
        if (target != null)
        {
            _posOffset = target.InverseTransformPoint(transform.position);
        }
        
        _currentPitch = transform.localEulerAngles.x;
    }

    void LateUpdate()
    {
        // 1. Get Inputs for Sway
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        // --- 2. Handle Rotation (Pitch + Yaw + Tilt) ---
        
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        _currentPitch -= mouseY;
        _currentPitch = Mathf.Clamp(_currentPitch, -lookXLimit, lookXLimit);

        float targetYaw = playerController.CurrentYaw;
        
        // Calculate Roll (Tilt) based on strafing
        float targetRoll = -moveX * tiltAngle;
        _tiltRoll = Mathf.Lerp(_tiltRoll, targetRoll, Time.deltaTime * tiltSpeed);
        
        transform.rotation = Quaternion.Euler(_currentPitch, targetYaw, _tiltRoll);

        // --- 3. Handle Position (Smoothing + Bob) ---
        
        Vector3 targetPosition = target.TransformPoint(_posOffset);

        // Calculate Head Bobbing when moving
        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f)
        {
            _bobTimer += Time.deltaTime * bobFrequency;
            float bobOffset = Mathf.Sin(_bobTimer) * bobAmount;
            targetPosition.y += bobOffset;
        }
        else
        {
            _bobTimer = 0; // Reset timer when standing still
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _currentVelocity,
            smoothTime
        );
    }
}