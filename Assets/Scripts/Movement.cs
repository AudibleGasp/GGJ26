using UnityEngine;

public class PhysicsMovement : MonoBehaviour
{
    [Header("Settings")]
    public float MovementSpeed = 10;
    public float Acceleration = 50;
    
    [Header("References")]
    public Rigidbody rb;

    public void FixedUpdate()
    {
        // 1. Get Input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // 2. Calculate Direction
        // Because the Look script rotates the transform, .right and .forward 
        // will automatically be pointing in the correct visual direction.
        Vector3 targetDirection = (transform.right * moveX) + (transform.forward * moveZ);

        // Normalize to prevent fast diagonal movement
        if (targetDirection.magnitude > 1) 
            targetDirection.Normalize();

        Vector3 targetVelocity = targetDirection * MovementSpeed;

        // 3. Apply Physics (Preserving Gravity)
        ApplyVelocity(targetVelocity);
    }

    private void ApplyVelocity(Vector3 targetVel)
    {
        Vector3 currentVelocity = rb.linearVelocity;

        // We only smooth out X and Z. We leave Y (gravity) untouched.
        float newX = Mathf.MoveTowards(currentVelocity.x, targetVel.x, Time.fixedDeltaTime * Acceleration);
        float newZ = Mathf.MoveTowards(currentVelocity.z, targetVel.z, Time.fixedDeltaTime * Acceleration);

        rb.linearVelocity = new Vector3(newX, currentVelocity.y, newZ);
    }
}