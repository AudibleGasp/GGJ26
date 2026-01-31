using UnityEngine;

public enum ParticleFX
{
    Basic,
    Penetrating,
    EnemyDespawn
}

public class Main : MonoBehaviour
{
    public static Main Instance;
    public PlayerController PlayerController;

    [Header("Particles")]
    public ParticleSystem BasicHitFX;
    public ParticleSystem PenetratingHitFX;
    public ParticleSystem EnemyDespawnFX;

    private void Awake()
    {
        Instance = this;
    }
    
    public void PlayParticle(ParticleFX fxType, Vector3 position)
    {
        ParticleSystem ps = fxType switch
        {
            ParticleFX.Basic => BasicHitFX,
            ParticleFX.Penetrating => PenetratingHitFX,
            ParticleFX.EnemyDespawn => EnemyDespawnFX,
            _ => BasicHitFX
        };
        
        ps.transform.position = position;
        ps.Emit(1);
    }
}
