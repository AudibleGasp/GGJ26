using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class ChaserEnemy : MonoBehaviour
{
    private static readonly int Attack = Animator.StringToHash("Attack");

    public enum EnemyState
    {
        Chasing, Attacking, Recovering
    }

    [Header("Stats")]
    [SerializeField] private float health = 30f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private MaskType mask;

    [Header("Attack (Lunge) Settings")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float windUpTime = 1f;
    [SerializeField] private float windUpSpeed = 3f;
    [SerializeField] private float lungeDuration = 0.3f;
    [SerializeField] private float lungeSpeed = 10f;
    [SerializeField] private float recoveryTime = 1f;
    [SerializeField] private float damage = 15f;
    
    [Header("Animations")]
    [SerializeField] 
    private Animator anim;
    [SerializeField] 
    private Mask maskPrefab;
    [SerializeField] 
    private Transform maskTransform;

    private Transform target;
    private Rigidbody rb;
    private Renderer enemyRenderer; // Renk değiştirmek için referans
    private EnemyState currentState = EnemyState.Chasing;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.freezeRotation = true;

        // Materyal rengini değiştirmek için Renderer'ı al
        enemyRenderer = GetComponent<Renderer>();
        // Başlangıç rengi: Yeşil (Kovalama)
        UpdateColor(Color.green);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) target = playerObj.transform;
    }

    void FixedUpdate()
    {
        if (isDead || target == null) return;

        // Sadece KOVALAMA modundaysak fiziksel hareket yap
        if (currentState == EnemyState.Chasing)
        {
            HandleMovement();
        }
    }

    private void HandleMovement()
    {
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackRange)
        {
            StartCoroutine(PerformLungeSequence());
        }
        else
        {
            // --- KOVALAMA (YEŞİL) ---
            Vector3 direction = (target.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * turnSpeed));
            }

            Vector3 moveVel = direction * moveSpeed;
            rb.linearVelocity = new Vector3(moveVel.x, rb.linearVelocity.y, moveVel.z);
        }
    }

    private IEnumerator PerformLungeSequence()
    {
        anim.SetTrigger(Attack);
        
        // 1. STATE: ATTACKING -> SARI YAP
        currentState = EnemyState.Attacking;
        UpdateColor(Color.yellow);

        rb.linearVelocity = Vector3.zero;
        Vector3 attackDir = (target.position - transform.position).normalized;
        attackDir.y = 0;
        
        // --- WIND UP ---
        float timer = 0;
        while (timer < windUpTime)
        {
            rb.linearVelocity = -attackDir * windUpSpeed; 
            timer += Time.deltaTime;
            yield return null;
        }

        // --- LUNGE ---
        rb.linearVelocity = attackDir * lungeSpeed + Vector3.up * 3;
        yield return new WaitForSeconds(lungeDuration);

        // 2. STATE: RECOVERY -> KIRMIZI YAP
        currentState = EnemyState.Recovering;
        UpdateColor(Color.red);
        
        rb.linearVelocity = Vector3.zero;
        yield return new WaitForSeconds(recoveryTime);

        // 3. STATE: CHASING -> TEKRAR YEŞİL YAP
        currentState = EnemyState.Chasing;
        UpdateColor(Color.green);
    }
    
    // Renk değiştirme yardımcı fonksiyonu
    private void UpdateColor(Color color)
    {
        if (enemyRenderer != null)
        {
            enemyRenderer.material.color = color;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // if (collision.gameObject.CompareTag("Enemy"))
        // {
        //     rb.linearVelocity -= (collision.transform.position - transform.position).normalized * 2;
        // }
        // else
        if (currentState == EnemyState.Attacking && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Lunge Hit!");
            // collision.gameObject.GetComponent<PlayerHealth>().TakeDamage(damage);
        }
    }

    public void Slap(Vector3 sourcePos)
    {
        if (mask == MaskType.None)
        {
            rb.linearVelocity = (rb.position - sourcePos).normalized * 3 + Vector3.up * 3;
            return;
        }
        
        DestroyMask();
    }

    public void TakeDamage(float amount)
    {
        if (mask != MaskType.None)
        {
            // Reduce damage ?
        }
        
        health -= amount;
        if (health <= 0)
        {
            DestroyMask();
            Destroy(gameObject);
        }
    }
    
    public void DestroyMask()
    {
        if (mask == MaskType.None)
        {
            return;
        }
        
        mask = MaskType.None;
        Mask spawnedMask = Instantiate(maskPrefab, maskTransform.position, maskTransform.rotation);
        spawnedMask.OnSlap(transform.right * 7 + Vector3.up * 5);
        maskTransform.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        // Gizmos rengi de mevcut duruma uysun
        switch (currentState)
        {
            case EnemyState.Chasing: Gizmos.color = Color.green; break;
            case EnemyState.Attacking: Gizmos.color = Color.yellow; break;
            case EnemyState.Recovering: Gizmos.color = Color.red; break;
        }
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}