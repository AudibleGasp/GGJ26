using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Referanslar")]
    public TextMeshPro textMesh;

    [Header("Hareket Ayarları")]
    public float moveSpeed = 3f;
    public float swaySpeed = 5f;    
    public float swayAmount = 0.5f; 
    public float fadeDuration = 1f;

    private float _timer;
    private Color _originalColor;   
    private float _randomXOffset;
    
    // OPTİMİZASYON: Kamerayı önbelleğe alıyoruz
    private Transform _mainCamTransform;

    // GÜNCELLEME: İsteğe bağlı (nullable) renk parametresi eklendi
    public void Setup(string text, Color? customColor = null)
    {
        textMesh.text = text;
        
        // Eğer özel renk geldiyse onu kullan, yoksa prefab rengini al
        if (customColor.HasValue)
        {
            textMesh.color = customColor.Value;
        }

        _originalColor = textMesh.color; 
        
        _timer = 0;
        _randomXOffset = Random.Range(0f, 100f);

        if (Camera.main != null)
        {
            _mainCamTransform = Camera.main.transform;
            transform.rotation = _mainCamTransform.rotation;
        }
    }

    void Update()
    {
        transform.Translate(Vector3.up * moveSpeed * Time.deltaTime, Space.World);
        
        float xSway = Mathf.Sin((Time.time + _randomXOffset) * swaySpeed) * swayAmount;
        Vector3 rightDir = _mainCamTransform != null ? _mainCamTransform.right : transform.right;
        
        transform.position += rightDir * xSway * Time.deltaTime;

        _timer += Time.deltaTime;
        // Alpha değeriyle oynarken _originalColor kullanıyoruz, böylece renk bozulmuyor
        float alpha = Mathf.Lerp(1f, 0.9f, _timer / fadeDuration); // .75 demiştik
        textMesh.color = new Color(_originalColor.r, _originalColor.g, _originalColor.b, alpha);

        if (_timer >= fadeDuration)
        {
            Destroy(gameObject);
        }
        
        if (_mainCamTransform != null)
            transform.rotation = _mainCamTransform.rotation;
    }
}