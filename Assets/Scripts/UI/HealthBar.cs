using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public RectTransform fillRect; // Inspector'dan Fill objesini sürükle

    private float maxBarWidth; // Parent'tan otomatik alacak, gizli kalsın

    [Header("Test")]
    [Range(0, 100)]
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    void Start()
    {
        if (fillRect != null && fillRect.parent != null)
        {
            // Parent'ın (Progress Bar arka planı) RectTransform'unu al
            RectTransform parentRect = fillRect.parent.GetComponent<RectTransform>();

            // 1. Max genişliği parent'ın genişliği olarak ayarla
            maxBarWidth = parentRect.rect.width;

            // 2. Başlangıçta Fill objesini Parent ile birebir aynı boyuta getir (Full can)
            // X: Parent genişliği, Y: Parent yüksekliği
            fillRect.sizeDelta = new Vector2(maxBarWidth, parentRect.rect.height);
            
            // Eğer pozisyon kaymışsa merkeze oturt (Opsiyonel ama garantidir)
            fillRect.anchoredPosition = Vector2.zero;
        }
    }

    void Update()
    {
        UpdateVisual();
    }

    void UpdateVisual()
    {
        if (fillRect == null) return;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        float ratio = currentHealth / maxHealth;

        // Genişliği (x) orana göre değiştir, yüksekliği (y) olduğu gibi bırak (Start'ta ayarlandı)
        fillRect.sizeDelta = new Vector2(maxBarWidth * ratio, fillRect.sizeDelta.y);
    }
}