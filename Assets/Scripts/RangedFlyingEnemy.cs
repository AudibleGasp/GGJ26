using UnityEngine;

public class RangedFlyingEnemy : EnemyBase
{
    [Header("Flight & Swarm Settings")]
    [SerializeField] private float flySpeed = 5f;
    [SerializeField] private float stopDistance = 2f;
    [SerializeField] private float maneuverPeriod = 3f;
    [SerializeField] private float offsetRadius = 5f;
    [SerializeField] private float heightOffset = 4f;

    [Header("Combat")]
    [SerializeField] private Projectile projectileToSpawn;
    [SerializeField] private Transform muzzleTransform;

    private Vector3 currentTargetOffset;
    private float maneuverTimer;

    private bool isCharging;

    protected override void Start()
    {
        base.Start(); // Sets target and rb
        PickNewOffset();
        UpdateColor(Color.green);
        maneuverTimer = Random.Range(0f, 1f);
    }

    void FixedUpdate()
    {
        if (isDead || target == null) return;

        HandleManeuverTimer();
        MoveTowardsOffset();
        LookAtPlayer();
    }

    private void HandleManeuverTimer()
    {
        maneuverTimer += Time.fixedDeltaTime;
        
        float remaining = maneuverPeriod - maneuverTimer;

        if (remaining <= 1f && !isCharging)
        {
            isCharging = true;
            UpdateColor(Color.red);
        }

        if (maneuverTimer >= maneuverPeriod)
        {
            FireProjectile();
            PickNewOffset();
            maneuverTimer = 0;

            isCharging = false;
            UpdateColor(Color.green);
        }
    }

    private void PickNewOffset()
    {
        // Generates a random point around the player
        Vector2 randomCircle = Random.insideUnitCircle * offsetRadius;
        currentTargetOffset = new Vector3(randomCircle.x, heightOffset + Random.Range(-1f, 3f), randomCircle.y);
    }

    private void MoveTowardsOffset()
    {
        Vector3 targetPosition = target.position + currentTargetOffset;
        Vector3 direction = (targetPosition - transform.position);
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            // Smoothly move towards the offset point
            Vector3 moveForce = direction.normalized * flySpeed;
            rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, moveForce, Time.fixedDeltaTime * 2f);
        }
        else
        {
            // Apply slight braking when near the point to avoid jitter
            rb.linearVelocity *= 0.95f;
        }
    }

    private void LookAtPlayer()
    {
        Vector3 lookDir = (target.position - transform.position).normalized;
        // lookDir.y = 0; // Keep the enemy upright
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * 5f));
        }
    }

    private void FireProjectile()
    {
        // Use the requested instantiation and launch logic
        Projectile p = Instantiate(projectileToSpawn, muzzleTransform.position + muzzleTransform.forward, muzzleTransform.rotation);
        p.Launch();
        
        AudioManager.Instance.PlayOneShotSound("mask-wind", 1f);
    }

    // Ensure the flyer reacts to slaps by breaking its flight path
    protected override void HandlePostMaskSlap(Vector3 direction)
    {
        base.HandlePostMaskSlap(direction); // Initial knockback
        maneuverTimer = -1f; // "Stun" the movement logic briefly by resetting timer
    }
}