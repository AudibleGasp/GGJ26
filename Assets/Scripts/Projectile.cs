using UnityEngine;

public class Projectile : MonoBehaviour
{
    public bool PlayerProjectile = true;
    
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private float speed = 10f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float LifeTime = 5f;
    [SerializeField] private ParticleFX hitFX;
    [SerializeField] private int damage = 30;

    private float lifeTime;

    public void Launch()
    {
        rb.linearVelocity = transform.forward * speed;
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;
        
        if(lifeTime > LifeTime)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(PlayerProjectile)
        {
            if (other.CompareTag("Enemy"))
            {
                EnemyBase enemy = other.GetComponent<EnemyBase>();
                enemy.TakeDamage(damage);
                Main.Instance.PlayParticle(hitFX, transform.position);
                AudioManager.Instance.PlayOneShotSound("playerHit");
            }
        }
        else
        {
            if (other.CompareTag("Player"))
            {
                PlayerController player = other.GetComponent<PlayerController>();
                PushTarget(other);
                player.OnHit();
                Main.Instance.PlayParticle(hitFX, transform.position);
                AudioManager.Instance.PlayOneShotSound("hit");
            }
        }

        if (destroyOnHit || transform.position.y < 0f)
        {
            Destroy(gameObject);
            Main.Instance.PlayParticle(hitFX, transform.position);
        }
    }

    private void PushTarget(Collider other)
    {
        Rigidbody targetRb = other.gameObject.GetComponent<Rigidbody>();
        var dir = other.transform.position - transform.position;
        dir.y = 0;
        targetRb.linearVelocity += (dir.normalized * 2 + Vector3.up).normalized * 10;
    }
}
