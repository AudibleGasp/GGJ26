using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ChaserEnemy : EnemyBase
{
    private static readonly int Attack = Animator.StringToHash("Attack");
    
    public enum EnemyState { Chasing, WindUp, Lunge, Recovering }

    [Header("Chaser Specifics")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float acceleration = 20f;
    
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float windUpTime = 0.5f;
    [SerializeField] private float windUpAcceleration = 5f;
    [SerializeField] private float lungeDuration = 0.3f;
    [SerializeField] private float lungeSpeed = 15f;
    [SerializeField] private float lungeAcceleration = 50f; // High burst speed
    [SerializeField] private float recoveryTime = 1f;
    [SerializeField] private float stopAcceleration = 10f; // How fast it brakes

    [Space(10)] [SerializeField] private GameObject attackingFX;

    private EnemyState currentState = EnemyState.Chasing;
    private float stateTimer;
    private Vector3 attackDirection;

    protected override void Start()
    {
        base.Start(); // Sets the target and Rigidbody
        ChangeState(EnemyState.Chasing);
    }

    void FixedUpdate()
    {
        if (isDead || target == null) return;
        UpdateStateLogic();
        // Fall check
        //if (transform.position.y < -10) Destroy(gameObject);
    }

    // Overriding Slap so Chaser enters Recovery when hit
    protected override void HandlePostMaskSlap(Vector3 direction)
    {
        base.HandlePostMaskSlap(direction);
        ChangeState(EnemyState.Recovering);
    }

    private void UpdateStateLogic()
    {
        stateTimer += Time.fixedDeltaTime;
        Vector3 targetVelocity = Vector3.zero;
        float currentAccel = acceleration;

        switch (currentState)
        {
            case EnemyState.Chasing:
                HandleChasingState(out targetVelocity, out currentAccel);
                break;

            case EnemyState.WindUp:
                HandleWindUpState(out targetVelocity, out currentAccel);
                break;

            case EnemyState.Lunge:
                HandleLungeState(out targetVelocity, out currentAccel);
                break;

            case EnemyState.Recovering:
                HandleRecoveryState(out targetVelocity, out currentAccel);
                break;
        }

        // Apply Velocity with Acceleration
        Vector3 currentHorizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        Vector3 newVel = Vector3.MoveTowards(currentHorizontalVel, targetVelocity, currentAccel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(newVel.x, rb.linearVelocity.y, newVel.z);
    }

    #region State Handlers

    private void HandleChasingState(out Vector3 targetVel, out float accel)
    {
        float distance = Vector3.Distance(transform.position, target.position);
        accel = acceleration;

        if (distance <= attackRange)
        {
            targetVel = Vector3.zero;
            ChangeState(EnemyState.WindUp);
        }
        else
        {
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;
            RotateTowards(direction);
            targetVel = direction * moveSpeed;
        }
    }

    private void HandleWindUpState(out Vector3 targetVel, out float accel)
    {
        accel = windUpAcceleration;
        // Move backward slowly during windup
        targetVel = -attackDirection * 1.5f; 

        if (stateTimer >= windUpTime)
        {
            ChangeState(EnemyState.Lunge);
        }
    }

    private void HandleLungeState(out Vector3 targetVel, out float accel)
    {
        accel = lungeAcceleration;
        targetVel = attackDirection * lungeSpeed;

        if (stateTimer >= lungeDuration)
        {
            ChangeState(EnemyState.Recovering);
        }
    }

    private void HandleRecoveryState(out Vector3 targetVel, out float accel)
    {
        accel = stopAcceleration;
        targetVel = Vector3.zero;

        if (stateTimer >= recoveryTime)
        {
            ChangeState(EnemyState.Chasing);
        }
    }

    #endregion

    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        stateTimer = 0;
        
        attackingFX.SetActive(newState == EnemyState.Lunge);

        switch (newState)
        {
            case EnemyState.Chasing:
                UpdateColor(Color.green);
                break;

            case EnemyState.WindUp:
                UpdateColor(Color.yellow);
                anim.SetTrigger(Attack);
                // Lock in the direction at the start of the attack
                attackDirection = (target.position - transform.position).normalized;
                attackDirection.y = 0;
                RotateTowards(attackDirection);
                break;

            case EnemyState.Lunge:
                // Add a small upward hop at the start of the lunge
                rb.linearVelocity += Vector3.up * 5f;
                break;

            case EnemyState.Recovering:
                UpdateColor(Color.red);
                break;
        }
    }

    private void RotateTowards(Vector3 direction)
    {
        if (direction != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(direction);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * turnSpeed));
        }
    }

    private void UpdateColor(Color color)
    {
        if (enemyRenderer != null) enemyRenderer.material.color = color;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (currentState == EnemyState.Lunge)
        {
            rb.linearVelocity = -rb.linearVelocity / 2f;

            if (collision.gameObject.CompareTag("Player"))
            {
                PushTarget(collision);

                // --- DEĞİŞİKLİK BURADA ---
                // Oyuncu scriptine eriş
                PlayerController player = collision.gameObject.GetComponent<PlayerController>();
                
                if (player != null)
                {
                    // 1. Can azalt (Bu, HealthBar'ı tetikler)
                    player.TakeDamage();

                    // 2. Hit efektini/timer'ı çalıştır (Eski kodun)
                    player.OnHit();
                }
                // -------------------------
            }
            
            if (collision.gameObject.CompareTag("Enemy"))
            {
                PushTarget(collision);
            }
            
            // collision.gameObject.GetComponent<Rigidbody>().linearVelocity += Vector3.up * 5;
        }
    }

    private void PushTarget(Collision collision)
    {
        Rigidbody targetRb = collision.gameObject.GetComponent<Rigidbody>();
        targetRb.linearVelocity += (collision.transform.position - transform.position + Vector3.up * 2).normalized * 4;
    }

    private void OnDrawGizmos()
    {
        // Gizmos rengi de mevcut duruma uysun
        switch (currentState)
        {
            case EnemyState.Chasing: Gizmos.color = Color.green; break;
            case EnemyState.Lunge: Gizmos.color = Color.red; break;
            case EnemyState.Recovering: Gizmos.color = Color.yellow; break;
        }
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}