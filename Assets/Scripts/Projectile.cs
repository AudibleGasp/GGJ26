using UnityEngine;

public class Projectile : MonoBehaviour
{
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
        if (other.CompareTag("Enemy"))
        {
            ChaserEnemy enemy = other.GetComponent<ChaserEnemy>();
            enemy.TakeDamage(damage);
            Main.Instance.PlayParticle(hitFX, transform.position);
        }

        if (destroyOnHit || other.CompareTag("Ground"))
        {
            Destroy(gameObject);
            Main.Instance.PlayParticle(hitFX, transform.position);
        }
    }
}
