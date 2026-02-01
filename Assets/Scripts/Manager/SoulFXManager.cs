using UnityEngine;
using System.Collections;

public class SoulFXManager : MonoBehaviour
{
    public static SoulFXManager Instance;

    [Header("Referanslar")]
    [Tooltip("Uçacak olan ruh/ikon görseli (UI Image Prefab)")]
    public GameObject soulPrefab; 
    
    [Tooltip("Hedefe vardığında çıkacak efekt (UI Particle veya Explosion Prefab)")]
    public GameObject impactPrefab;
    
    [Tooltip("Ruhun gideceği hedef nokta (Skor Text'i)")]
    public RectTransform targetUIElement;

    [Header("Hareket Ayarları")]
    public float flyDuration = 0.8f; // Ne kadar sürede gitsin?
    public float scalePunchAmount = 1.2f; // Hedef vurulunca ne kadar büyüsün?

    // Canvas referansı (Koordinat çevrimi için gerekli)
    private Canvas _parentCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Canvas'ı otomatik bul (Ebeveynlerde arar) veya manuel ata
        // Bu scriptin bir Canvas objesinin altında olmadığını varsayarak:
        if (targetUIElement != null)
            _parentCanvas = targetUIElement.GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// Bu fonksiyonu düşman öldüğünde çağıracaksın.
    /// </summary>
    /// <param name="worldPosition">Düşmanın öldüğü 3D pozisyon</param>
    public void SpawnSoul(Vector3 worldPosition)
    {
        if (soulPrefab == null || targetUIElement == null || _parentCanvas == null) return;

        // 1. Prefab'i Canvas içinde oluştur
        GameObject soulObj = Instantiate(soulPrefab, _parentCanvas.transform);
        
        // 2. Dünya pozisyonunu Ekran/Canvas pozisyonuna çevir
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        
        // Eğer Canvas "Screen Space - Overlay" değilse (örn. Camera ise) dönüşüm gerekebilir
        // Ancak çoğu UI için bu başlangıç yeterlidir.
        soulObj.transform.position = screenPos;

        // 3. Hareketi Başlat
        StartCoroutine(MoveSoulRoutine(soulObj));
    }

    private IEnumerator MoveSoulRoutine(GameObject soul)
    {
        float timer = 0f;
        Vector3 startPos = soul.transform.position;
        Vector3 endPos = targetUIElement.position;

        // Rastgele bir kavis noktası oluştur (Düz gitmesin, yay çizsin)
        // Başlangıç ile bitişin ortasında, rastgele sağa/sola sapmış bir nokta
        Vector3 midPoint = (startPos + endPos) / 2;
        midPoint += new Vector3(Random.Range(-200f, 200f), Random.Range(-100f, 100f), 0);

        while (timer < flyDuration)
        {
            if (soul == null) yield break;

            timer += Time.deltaTime;
            float t = timer / flyDuration;
            
            // Yumuşak hızlanma (Ease In-Out)
            float easeT = t * t * (3f - 2f * t); 

            // Bezier Curve (Kavisli Hareket) Formülü
            // P = (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
            Vector3 m1 = Vector3.Lerp(startPos, midPoint, easeT);
            Vector3 m2 = Vector3.Lerp(midPoint, endPos, easeT);
            soul.transform.position = Vector3.Lerp(m1, m2, easeT);

            yield return null;
        }

        // 4. Hedefe Vardı
        if (soul != null) Destroy(soul);

        // Patlama Efekti Çıkar
        if (impactPrefab != null)
        {
            GameObject impact = Instantiate(impactPrefab, _parentCanvas.transform);
            impact.transform.position = endPos; // Tam text'in üzerinde patlasın
            Destroy(impact, 1.5f); // 1.5 saniye sonra temizle
        }

        // Hedef Text'i hafifçe zıplat (ScoreManager'daki punch'tan bağımsız ekstra bir his)
        StartCoroutine(PunchEffect(targetUIElement));
    }

    private IEnumerator PunchEffect(RectTransform target)
    {
        Vector3 originalScale = Vector3.one; // UI genelde 1,1,1'dir
        target.localScale = originalScale * scalePunchAmount;

        float duration = 0.15f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            target.localScale = Vector3.Lerp(originalScale * scalePunchAmount, originalScale, timer / duration);
            yield return null;
        }
        target.localScale = originalScale;
    }
}