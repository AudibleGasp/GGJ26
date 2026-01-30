using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [Header("Settings")]
    public Transform playerCamera; // Drag your Camera here
    public float mouseSensitivity = 2f;
    public float lookXLimit = 85f;

    private float rotationX = 0;

    void Start()
    {
        // Hide and lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. Get Input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 2. Rotate the Player Body (Horizontal / Yaw)
        // We rotate 'transform' (the object this script is on) so movement directions update
        transform.Rotate(Vector3.up * mouseX);

        // 3. Rotate the Camera (Vertical / Pitch)
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        
        // Apply rotation to the camera specifically
        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
    }
}