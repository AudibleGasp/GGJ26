using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int Mask = Animator.StringToHash("Mask");
    private static readonly int PickUp = Animator.StringToHash("PickUp");

    [Header("Movement")]
    public float movementSpeed = 10f;
    public float acceleration = 50f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public LayerMask groundLayer;
    public float groundCheckDistance = 1.1f;

    [Header("Attack")]
    public float attackDuration = 0.4f;
    public float attackCooldown = 0.45f;
    public float attackRange = 1f;
    public LayerMask hitLayer;

    [Header("Look (Horizontal)")]
    // Removed playerCamera reference; Camera now handles itself.
    public float mouseSensitivity = 2f; 

    [Header("Masks")]
    public int MaskLimit = 3;

    [Header("References")]
    public Rigidbody rb;
    public Animator anim;
    public Projectile basicProjectilePrefab;
    public Projectile penetratingProjectilePrefab;
    public ParticleSystem hitFX;
    public ParticleSystem slapFX;
    public Transform muzzleTransform;

    [Header("Health")]
    public int maxLives = 3;
    public int currentLives = 3; // Mevcut can

    [Header("Abilities")]
    public WindBlastAbility windAbility; // Inspector'dan buraya scripti sürükleyeceksin    

    // Internal State
    public float CurrentYaw { get; private set; }
    private Vector2 inputVector;
    private bool jumpRequested;

    public readonly List<MaskType> masks = new List<MaskType>();
    
    // Attack State
    private bool isAttacking;
    private float nextAttackTime;
    private float attackEndTime;
    private float hitTimer;

    public bool IsAirborne => !IsGrounded();

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        
        // Initialize Yaw to current rotation to prevent snapping on start
        CurrentYaw = transform.rotation.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // 1. Handle Body Rotation (Input Accumulation)
        HandleLookInput();

        // 2. Handle Attack Input
        HandleAttack();

        // 3. Read Movement Input
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        inputVector = new Vector2(moveX, moveZ);

        // 4. Check Jump Input
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded())
        {
            jumpRequested = true;
        }

        hitTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        HandleMovement();

        // Apply Rotation here for smooth physics interpolation
        Quaternion targetRotation = Quaternion.Euler(0, CurrentYaw, 0);
        rb.MoveRotation(targetRotation);

        if (jumpRequested)
        {
            PerformJump();
            jumpRequested = false;
        }
    }

    // ... [Rest of your Attack and Pickup methods remain unchanged] ...

    public bool TryPickUpMask(MaskType type)
    {
        if(masks.Count >= MaskLimit) return false;
        // Debug.Log($"<color=cyan>Picked-up mask {type}!</color>");
        anim.SetTrigger(PickUp);
        masks.Add(type);
        return true;
    }

    public void UseNextMask()
    {
        if (masks.Count <= 0) 
            return;

        // EĞER RÜZGAR MASKESİ İSE -> YETENEK KULLAN
        if (masks[0] == MaskType.Wind)
        {
            if (windAbility != null)
            {
                windAbility.PerformInstantBlast(muzzleTransform);
            }
        }
        // DEĞİLSE -> MERMİ FIRLAT (Orijinal kodun buraya taşındı)
        else
        {
            var projectileToSpawn = masks[0] switch
            {
                MaskType.None => basicProjectilePrefab,
                MaskType.Basic => basicProjectilePrefab,
                MaskType.Penetrating => penetratingProjectilePrefab,
                _ => basicProjectilePrefab
            };

            // TODO Switch for different masks
            
            if (projectileToSpawn != null)
            {
                Projectile p = Instantiate(projectileToSpawn, muzzleTransform.position + muzzleTransform.forward, muzzleTransform.rotation);
                p.Launch();
            }
        }
        
        // Maskeyi listeden sil
        masks.RemoveAt(0);
    }

    private void HandleAttack()
    {
        if (isAttacking && Time.time >= attackEndTime)
        {
            Debug.DrawLine(transform.position, transform.position + muzzleTransform.forward * attackRange, Color.red, 1f);

            slapFX.Play();
            
            isAttacking = false;
            if (Physics.Raycast(muzzleTransform.position, muzzleTransform.forward, out RaycastHit hit, attackRange, hitLayer))
            {
                // Assuming ChaserEnemy exists in your project
                EnemyBase enemy = hit.collider.GetComponent<EnemyBase>();
                enemy.Slap((muzzleTransform.forward * 2 + Vector3.up * 1.5f).normalized);
                hitFX.transform.position = hit.point;
                hitFX.Play();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            nextAttackTime = Time.time + .2f;
            PerformMaskAction();
        }
        else if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        anim.SetTrigger(Attack);
        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;
        attackEndTime = Time.time + attackDuration;
    }

    public void OnHit()
    {
        hitTimer = .4f;
    }

    private void PerformMaskAction()
    {
        anim.SetTrigger(Mask);
        UseNextMask();
    }

    private void HandleLookInput()
    {
        // We only calculate the value here. We apply it in FixedUpdate.
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        CurrentYaw += mouseX;
    }

    private void HandleMovement()
    {
        if(hitTimer > 0) return; // Hit animation is playing (or hit timer is active)
        
        Vector3 targetDirection = (transform.right * inputVector.x) + (transform.forward * inputVector.y);

        if (targetDirection.magnitude > 1)
            targetDirection.Normalize();

        Vector3 targetVelocity = targetDirection * movementSpeed;
        Vector3 currentVelocity = rb.linearVelocity;
        
        float newX = Mathf.MoveTowards(currentVelocity.x, targetVelocity.x, Time.fixedDeltaTime * acceleration);
        float newZ = Mathf.MoveTowards(currentVelocity.z, targetVelocity.z, Time.fixedDeltaTime * acceleration);

        rb.linearVelocity = new Vector3(newX, currentVelocity.y, newZ);
    }

    private void PerformJump()
    {
        Vector3 velocity = rb.linearVelocity;
        rb.linearVelocity = new Vector3(velocity.x, 0, velocity.z);
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, groundCheckDistance, groundLayer);
    }

    public void TakeDamage()
    {
        if (currentLives > 0)
        {
            currentLives--;
            
            // anim.SetTrigger("Hit"); 
            // if current lives less than 0 load the try again scene
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * groundCheckDistance);
    }
}