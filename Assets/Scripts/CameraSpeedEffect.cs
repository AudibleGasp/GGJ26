using UnityEngine;

public class CameraSpeedEffect : MonoBehaviour
{
    [Header("Settings")]
    public Rigidbody targetRigidbody; // The car/character to track
    public Camera targetCamera;
    
    [Range(60f, 120f)]
    public float minFOV = 78f;
    [Range(60f, 120f)]
    public float maxFOV = 82f;
    
    public float speedThreshold = 9; // Speed at which maxFOV is reached
    public float smoothing = 10f;     // How quickly the camera reacts

    void Update()
    {
        // 1. Calculate current speed (magnitude of velocity)
        float currentSpeed = targetRigidbody.linearVelocity.magnitude;

        // 2. Map the speed to a value between 0 and 1
        float speedPercent = Mathf.Clamp01(currentSpeed / speedThreshold);

        // 3. Calculate the target FOV based on that percentage
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, speedPercent);

        // 4. Smoothly transition to the target FOV
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, Time.deltaTime * smoothing);
    }
}