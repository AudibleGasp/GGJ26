using UnityEngine;

public class FloatingTextManager : MonoBehaviour
{
    public static FloatingTextManager Instance;

    [Header("Prefab")]
    public GameObject textPrefab;

    [Header("Spawn Randomness")]
    public float xRandomness = 1f;
    public float zRandomness = 1f;
    public float yRandomMin = -0.5f;
    public float yRandomMax = 0.5f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // GÜNCELLEME: Renk parametresi eklendi
    public void ShowText(Vector3 position, string text, Color? color = null)
    {
        if (textPrefab == null) return;

        float basePathY = position.y < -5f ? .5f : position.y;
        
        float randX = Random.Range(-xRandomness, xRandomness);
        float randZ = Random.Range( 0.25f, zRandomness);
        float randY = Random.Range(yRandomMin, yRandomMax);

        randX = position.y < -5f ? 0 : randX;
        randZ = position.y < -5f ? 0 : randZ;

        Vector3 finalSpawnPos = new Vector3(
            position.x + randX, 
            basePathY + randY, 
            position.z + randZ
        );

        GameObject go = Instantiate(textPrefab, finalSpawnPos, Quaternion.identity);
        
        FloatingText ft = go.GetComponent<FloatingText>();
        if (ft != null)
        {
            ft.Setup(text, color);
        }
    }
}