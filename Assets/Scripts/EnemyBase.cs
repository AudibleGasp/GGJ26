using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Enemy Stats")]
    [SerializeField] protected float health = 30f;
    [SerializeField] protected MaskType mask = MaskType.None;
    [SerializeField] protected int scoreValue = 100;
    
    [Header("Base References")]
    [SerializeField] protected Renderer enemyRenderer;
    [SerializeField] protected Mask maskPrefab;
    [SerializeField] protected Transform maskTransform;
    [SerializeField] protected Animator anim;

    protected Rigidbody rb;
    protected Transform target;
    protected bool isDead = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    protected virtual void Start()
    {
        target = Main.Instance.PlayerController.transform;
    }

    protected virtual void Update()
    {
        // Eğer haritadan aşağı düştüyse (FATAL DEATH)
        if (!isDead && transform.position.y < -15f)
        {
            HandleFallDeath();
        }
    }

    public virtual void TakeDamage(float amount)
    {
        if (isDead) return;

        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    public virtual void Slap(Vector3 direction)
    {
        if (mask == MaskType.None)
        {
            HandlePostMaskSlap(direction);
            return;
        }
        
        DestroyMask();
    }

    protected virtual void HandlePostMaskSlap(Vector3 direction)
    {
        // Default behavior: get knocked back
        rb.linearVelocity = direction * 13;
    }

    public void DestroyMask()
    {
        if (mask == MaskType.None) return;
        
        mask = MaskType.None;
        if (maskPrefab != null && maskTransform != null)
        {
            Main.Instance.PlayParticle(ParticleFX.Sparks, maskTransform.position);
            Mask spawnedMask = Instantiate(maskPrefab, maskTransform.position, maskTransform.rotation);
            float side = Random.value > .5f ? 1 : -1;
            spawnedMask.OnSlap((transform.right * side + Vector3.up * .75f) * 8);
            maskTransform.gameObject.SetActive(false);
        }
    }

protected virtual void Die()
    {
        Main.Instance.PlayParticle(ParticleFX.EnemyDespawn, transform.position);
        isDead = true;

        // --- EKLENEN KISIM: SKOR GÖNDERİMİ ---
        if (ScoreManager.Instance != null)
        {
            // Oyuncu o sırada havada mı?
            bool playerInAir = Main.Instance.PlayerController.IsAirborne;
            
            // Puanı gönder (Normal Çarpan: 1f)
            ScoreManager.Instance.AddScore(scoreValue, transform.position, 1f, playerInAir);
        }
        // -------------------------------------

        DestroyMask();
        Destroy(gameObject);
    }

    // --- YENİ EKLENEN FONKSİYON ---
    protected virtual void HandleFallDeath()
    {
        if (isDead) return;
        isDead = true;

        if (ScoreManager.Instance != null)
        {
            bool playerInAir = Main.Instance.PlayerController.IsAirborne;

            // Düşerek ölüm: Bonus Çarpanı 2 (FATAL)
            ScoreManager.Instance.AddScore(scoreValue, transform.position, 2f, playerInAir);
        }

        DestroyMask(); // Maskesi varsa onu da düşür
        Destroy(gameObject);
    }
    

    protected void UpdateColor(Color color)
    {
        if (enemyRenderer != null) enemyRenderer.material.color = color;
    }
}