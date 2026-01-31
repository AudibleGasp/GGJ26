using UnityEngine;
using System.Collections; // <-- YENİ: Coroutine kullanmak için gerekli

public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance; // <-- YENİ: ScoreManager'dan ulaşmak için Singleton

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
    
    private Vector3 _shakeOffset = Vector3.zero; // <-- YENİ: Sarsıntı değerini tutacak değişken
    
    private PlayerController playerController;

    private void Awake() // <-- YENİ: Singleton ataması
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (Main.Instance != null)
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

        // PlayerController güvenliği
        float targetYaw = playerController != null ? playerController.CurrentYaw : transform.eulerAngles.y;
        
        // Calculate Roll (Tilt) based on strafing
        float targetRoll = -moveX * tiltAngle;
        _tiltRoll = Mathf.Lerp(_tiltRoll, targetRoll, Time.deltaTime * tiltSpeed);
        
        transform.rotation = Quaternion.Euler(_currentPitch, targetYaw, _tiltRoll);

        // --- 3. Handle Position (Smoothing + Bob + SHAKE) ---
        
        if (target == null) return;

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

        // SmoothDamp ile yumuşatılmış ana pozisyonu hesapla
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref _currentVelocity,
            smoothTime
        );

        // YENİ: Sarsıntıyı (Shake) en son ekle. Bu sayede SmoothDamp sarsıntıyı yutmaz.
        transform.position = smoothedPosition + _shakeOffset;
    }

    // --- YENİ EKLENEN FONKSİYONLAR ---

    public void Punch(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Rastgele X ve Y yönünde titret
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            _shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Sarsıntı bitince ofseti sıfırla
        _shakeOffset = Vector3.zero;
    }
}