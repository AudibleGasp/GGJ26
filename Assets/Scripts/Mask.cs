using System;
using UnityEngine;
using Random = UnityEngine.Random;

public enum MaskType
{
    None,
    Basic,
}

public class Mask : MonoBehaviour
{
    public MaskType Type;
    public Rigidbody rb;

    public float LifeTime = 3f;
    public float CanPickUpAfter = .5f;
    
    private float currentLife = 0;

    private void OnEnable()
    {
        currentLife = 0f;
    }

    private void Update()
    {
        if (currentLife > LifeTime)
        {
            Destroy(gameObject);
        }
        
        currentLife += Time.deltaTime;
    }

    private void OnTriggerStay(Collider other)
    {
        if (currentLife < CanPickUpAfter)
        {
            return;
        }
        
        PlayerController player = Main.Instance.PlayerController;
        if(player.TryPickUpMask(Type))
        {
            Destroy(gameObject);
        }
    }

    public void OnSlap(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        rb.AddTorque(Random.insideUnitSphere.normalized, ForceMode.Impulse);
    }
}
