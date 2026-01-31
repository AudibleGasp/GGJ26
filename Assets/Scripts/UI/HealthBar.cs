using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // DOTween Kütüphanesi

public class HealthBar : MonoBehaviour
{
    [Header("Referanslar")]
    public RectTransform fillRect; // Yeşil/Kırmızı dolan kısım
    public PlayerController player; // Canı takip edilecek karakter

    [Header("Ayarlar")]
    public float widthPerLife = 170f; // Her bir canın piksel genişliği (510 / 3 = 170)
    public float animationDuration = 0.5f; // Barın düşme hızı

    private int lastLivesVal = -1; // Değişikliği algılamak için
    private float defaultHeight;

    void Start()
    {
        // Eğer Player atanmadıysa otomatik bul
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        // Yüksekliği kaydet (bozulmasın diye)
        defaultHeight = fillRect.sizeDelta.y;

        // Başlangıçta barı full (veya player canına eşit) yap
        if (player != null)
        {
            lastLivesVal = player.currentLives;
            UpdateWidth(player.currentLives, true); // true = Animasyonsuz ilk kurulum
        }
    }

    void Update()
    {
        if (player == null) return;

        // Player'ın canı değişti mi? (Polling yöntemi)
        if (player.currentLives != lastLivesVal)
        {
            // Yeni can değerini güncelle
            UpdateWidth(player.currentLives, false); 
            lastLivesVal = player.currentLives;
        }
    }

    void UpdateWidth(int lives, bool instant)
    {
        // Hedef Genişlik Hesabı: (Örn: 2 can * 170 = 340px)
        float targetWidth = lives * widthPerLife;

        if (instant)
        {
            // Animasyonsuz direkt ayarla (Start anı için)
            fillRect.sizeDelta = new Vector2(targetWidth, defaultHeight);
        }
        else
        {
            // --- DOTween Animasyonu ---
            // 1. Genişliği yumuşakça düşür
            fillRect.DOSizeDelta(new Vector2(targetWidth, defaultHeight), animationDuration)
                .SetEase(Ease.OutBounce); // Hafif sekerek düşsün

            // 2. Can azaldıysa barı salla (Shake Effect)
            if (lives < lastLivesVal) // Sadece hasar alınca salla
            {
                // Barı kırmızıya boyayıp geri döndür (Flash Effect)
                fillRect.GetComponent<Image>().DOColor(Color.red, 0.1f)
                    .SetLoops(2, LoopType.Yoyo);

                // Tüm barı salla
                transform.DOShakePosition(0.4f, 10f, 20, 90, false, true);
            }
        }
    }
}