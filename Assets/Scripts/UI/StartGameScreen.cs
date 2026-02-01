using UnityEngine;

public class StartGameScreen : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Main.Instance.StartGame();
        }
    }
}
