using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private float LifeTime = 5f;

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
            enemy.TakeDamage(50);
        }

        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}
