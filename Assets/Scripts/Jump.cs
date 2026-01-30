using UnityEngine;

public class Jump : MonoBehaviour
{
    [Header("Settings")]
    public float jumpForce = 7f;
    
    [Header("Ground Detection")]
    public LayerMask groundLayer;        // Which layers count as "Ground"
    public float checkDistance = 1.1f;   // How far down to check (Capsule height is usually 2, so half is 1)

    [Header("References")]
    public Rigidbody rb;

    void Update()
    {
        // We check input in Update for responsiveness
        if (Input.GetButtonDown("Jump") && IsGrounded())
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
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
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
}