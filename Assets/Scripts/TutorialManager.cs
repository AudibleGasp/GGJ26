using UnityEngine;

public enum TutorialStep
{
    Wait,
    Slap,
    PickUp,
    UsePower,
    End
}

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    [SerializeField] 
    private Animation anim; 

    // Slap their mask off [Mouse 1]
    // Pick up that mask
    // Use the power to free their souls

    private PlayerController playerController;
    
    private TutorialStep currentStep;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        switch (currentStep)
        {
            case TutorialStep.Wait:
                if (EnemySpawner.Instance.EnemyCount > 0)
                {
                    TryProgress(TutorialStep.Wait);
                }
                break;
        }
    }

    public void TryProgress(TutorialStep step)
    {
        if (step != currentStep)
        {
            return;
        }
        
        currentStep++;
        switch (currentStep)
        {
            case TutorialStep.Wait:
                break;
            case TutorialStep.Slap:
                anim.Play("Slap");
                break;
            case TutorialStep.PickUp:
                anim.Play("PickUp");
                break;
            case TutorialStep.UsePower:
                anim.Play("UsePower");
                break;
            case TutorialStep.End:
                anim.Play("End");
                break;
            default:
                break;
        }
        
        if(currentStep != TutorialStep.End)
        {
            AudioManager.Instance.PlayOneShotSound("spawn");
            transform.rotation = Quaternion.AngleAxis(Camera.main.transform.rotation.eulerAngles.y, Vector3.up);
        }    
    }
}
