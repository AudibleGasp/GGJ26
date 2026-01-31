using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Base Enemy Stats")]
    [SerializeField] protected float health = 30f;
    [SerializeField] protected MaskType mask = MaskType.None;
    
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
        
        DestroyMask(direction);
    }

    protected virtual void HandlePostMaskSlap(Vector3 direction)
    {
        // Default behavior: get knocked back
        rb.linearVelocity = direction * 10 + -transform.forward * 4;
    }

    public void DestroyMask(Vector3 direction)
    {
        if (mask == MaskType.None) return;
        
        mask = MaskType.None;
        if (maskPrefab != null && maskTransform != null)
        {
            Mask spawnedMask = Instantiate(maskPrefab, maskTransform.position, maskTransform.rotation);
            spawnedMask.OnSlap(direction * 10);
            maskTransform.gameObject.SetActive(false);
        }
    }

    protected virtual void Die()
    {
        isDead = true;
        DestroyMask(Vector3.up);
        Destroy(gameObject);
    }

    protected void UpdateColor(Color color)
    {
        if (enemyRenderer != null) enemyRenderer.material.color = color;
    }
}