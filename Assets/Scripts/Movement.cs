using System;
using UnityEngine;

public class PhysicsMovement : MonoBehaviour
{
    [Header("Movement")]
    public float MovementSpeed = 10;
    public float Acceleration = 50;
    
    [Header("Jump")]
    public float jumpForce = 7f;
    public LayerMask groundLayer;        // Which layers count as "Ground"
    public float checkDistance = 1.1f;   // How far down to check (Capsule height is usually 2, so half is 1)
    
    [Header("References")]
    public Rigidbody rb;

    private void Update()
    {
        // We check input in Update for responsiveness
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            PerformJump();
        }
    }
    
    void PerformJump()
    {
        // 1. Reset vertical velocity
        // This ensures the jump is always the same height, even if we were slightly falling.
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(velocity.x, 0, velocity.z);

        // 2. Apply immediate upward force
        rb.linearVelocity += Vector3.up * jumpForce;
    }

    private bool IsGrounded()
    {
        // Cast a ray from the center of the player downwards
        // transform.position is usually the center of the Capsule
        return Physics.Raycast(transform.position, Vector3.down, checkDistance, groundLayer);
    }

    // Visual debugging to see the ray in the Scene view
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * checkDistance);
    }

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