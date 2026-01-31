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

    private Vector3 _posOffset;
    private Vector3 _currentVelocity = Vector3.zero;
    private float _currentPitch = 0f;
    
    private PlayerController playerController; // Reference the script, not just Transform

    void Start()
    {
        playerController = Main.Instance.PlayerController;
        
        // Calculate initial position offset in the TARGET'S local space
        if (target != null)
        {
            _posOffset = target.InverseTransformPoint(transform.position);
        }
        
        // Initialize pitch to current camera angle so it doesn't snap
        _currentPitch = transform.localEulerAngles.x;
    }

    void LateUpdate()
    {
        // --- 1. Handle Rotation (Pitch + Yaw) ---
        
        // Pitch (Up/Down): Calculated locally based on Mouse Y
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        _currentPitch -= mouseY;
        _currentPitch = Mathf.Clamp(_currentPitch, -lookXLimit, lookXLimit);

        // Yaw (Left/Right): We read this directly from the target (Player).
        // Since Player rotates in FixedUpdate (Interpolated), this value is smooth.
        float targetYaw = playerController.CurrentYaw;
        
        // Apply rotation
        transform.rotation = Quaternion.Euler(_currentPitch, targetYaw, 0);

        // --- 2. Handle Position (Smoothing) ---
        
        // Calculate where the camera SHOULD be based on the player's new rotation
        Vector3 targetPosition = target.TransformPoint(_posOffset);

        // Smoothly move there
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _currentVelocity,
            smoothTime
        );
    }
}