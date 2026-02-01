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
        if (!isDead && transform.position.y < -8f)
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
            AudioManager.Instance.PlayOneShotSound("slap");
            return;
        }
        
        TutorialManager.Instance.TryProgress(TutorialStep.Slap);
        
        DestroyMask();
        AudioManager.Instance.PlayOneShotSound("punch");
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
        AudioManager.Instance.PlayOneShotSound("kill");
        
        Main.Instance.PlayParticle(ParticleFX.EnemyDespawn, transform.position);
        isDead = true;

        // --- EKLENEN KISIM: SKOR GÖNDERİMİ ---
        if (ScoreManager.Instance != null)
        {
            // Oyuncu o sırada havada mı?
            bool playerInAir = Main.Instance.PlayerController.IsAirborne;
            
            // --- HESAPLAMA KISMI ---
            // Varsayılan olarak pozisyonu al, ama Collider varsa tepesini bul
            Vector3 scorePopupPos = transform.position; 
            Collider col = GetComponent<Collider>();
            
            if (col != null)
            {
                // Collider'ın en tepe noktası (Kafa üstü)
                scorePopupPos = new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z);
            }

            // Hesapladığımız 'scorePopupPos' noktasını gönderiyoruz
            ScoreManager.Instance.AddScore(scoreValue, scorePopupPos, 1f, playerInAir);
        }
        // -------------------------------------

        DestroyMask();
        Destroy(gameObject);
        EnemySpawner.Instance.EnemyCount--;
    }

    // --- YENİ EKLENEN FONKSİYON ---
    protected virtual void HandleFallDeath()
    {
        if (isDead) return;
        isDead = true;

        if (ScoreManager.Instance != null)
        {
            bool playerInAir = Main.Instance.PlayerController.IsAirborne;

            // --- HESAPLAMA KISMI ---
            Vector3 scorePopupPos = transform.position;
            Collider col = GetComponent<Collider>();

            if (col != null)
            {
                scorePopupPos = new Vector3(col.bounds.center.x, col.bounds.max.y, col.bounds.center.z);
            }

            // Düşerek ölüm (x2 Bonus)
            ScoreManager.Instance.AddScore(scoreValue, scorePopupPos, 2f, playerInAir);
        }
        AudioManager.Instance.PlayOneShotSound("kill");

        DestroyMask(); // Maskesi varsa onu da düşür
        Destroy(gameObject);
        EnemySpawner.Instance.EnemyCount--;
    }
    

    protected void UpdateColor(Color color)
    {
        if (enemyRenderer != null) enemyRenderer.material.color = color;
    }
}