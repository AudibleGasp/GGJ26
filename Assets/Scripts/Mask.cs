using System;
using PrimeTween;
using UnityEngine;
using Random = UnityEngine.Random;

public enum MaskType
{
    None,
    Basic,
    Penetrating,
    Wind
}

public class Mask : MonoBehaviour
{
    public MaskType Type;
    public Rigidbody rb;

    public float LifeTime = 3f;
    public float CanPickUpAfter = .5f;
    
    private float currentLife = 0;

    private bool triggeredDespawnAnim;

    private void OnEnable()
    {
        currentLife = 0f;
    }
    
    private void Update()
    {
        if (currentLife > LifeTime)
        {
            Tween.Scale(transform, 0f, .1f).OnComplete(gameObject, _ =>
            {
                Destroy(gameObject);
            }, false);
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
            TutorialManager.Instance.TryProgress(TutorialStep.PickUp);
            
            AudioManager.Instance.PlayOneShotSound("pickup");
            
            Main.Instance.PlayParticle(ParticleFX.Sparks, transform.position, 2);
            Destroy(gameObject);
        }
    }

    public void OnSlap(Vector3 velocity)
    {
        rb.linearVelocity = velocity;
        rb.AddTorque(Random.insideUnitSphere.normalized, ForceMode.Impulse);
    }
}
