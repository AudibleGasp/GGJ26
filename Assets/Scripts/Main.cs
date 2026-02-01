using PrimeTween;
using UnityEngine;

public enum ParticleFX
{
    Basic,
    Penetrating,
    EnemyDespawn,
    Sparks
}

public class Main : MonoBehaviour
{
    public static Main Instance;
    public PlayerController PlayerController;
    public EnemySpawner EnemySpawner;

    [Header("Particles")]
    public ParticleSystem BasicHitFX;
    public ParticleSystem PenetratingHitFX;
    public ParticleSystem EnemyDespawnFX;
    public ParticleSystem Sparks;

    private AudioManager audioManager;
    private Tween timeTween;

    [Header("UI")] public Animation uiAnim;
    
    private void Awake()
    {
        Instance = this;
        audioManager = new AudioManager();
    }

    private void Start()
    {
        uiAnim.Play("Start");

        Tween.CompleteAll();
    }

    public void StartGame()
    {
        uiAnim.Play("Game");
        PlayerController.OnGameStart();
        EnemySpawner.OnGameStart();
        
        Time.timeScale = 1f;
    }

    public void EndGame()
    {
        PlayerController.KillPlayer();
        uiAnim.Play("End");

        timeTween = Tween.Custom(1, 0f, .5f, OnValueChange);
    }

    private void OnValueChange(float val)
    {
        Time.timeScale = val;
    }

    public void PlayParticle(ParticleFX fxType, Vector3 position, int count = 1)
    {
        ParticleSystem ps = fxType switch
        {
            ParticleFX.Basic => BasicHitFX,
            ParticleFX.Penetrating => PenetratingHitFX,
            ParticleFX.EnemyDespawn => EnemyDespawnFX,
            ParticleFX.Sparks => Sparks,
            _ => BasicHitFX
        };
        
        ps.transform.position = position;
        ps.Emit(count);
    }
}
