using UnityEngine;
using TMPro; 
using System.Collections;

public class SoulFXManager : MonoBehaviour
{
    public static SoulFXManager Instance;

    [Header("Referanslar")]
    public GameObject floatingScorePrefab; 
    public GameObject impactPrefab;
    public RectTransform targetUIElement;

    [Header("Hedef İnce Ayar")]
    public Vector3 targetOffset; 

    [Header("Spawn (Doğuş) Ayarları")]
    public float spawnDistanceY = 150f; 
    [Tooltip("Doğarken sağa sola ne kadar dağılsın?")]
    public float spawnRandomRangeX = 100f; // Artırdım (Geniş dağılım)

    [Header("Impact (Vuruş) Ayarları")]
    [Tooltip("Hedefe vururken tam ortaya değil, sağa sola ne kadar sapsın?")]
    public float targetRandomRangeX = 80f; // YENİ: Hedefteki sapma miktarı

    [Header("Hareket Ayarları")]
    public float flyDuration = 0.5f;     
    public float scalePunchAmount = 1.3f; 

    private Canvas _parentCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (targetUIElement != null)
            _parentCanvas = targetUIElement.GetComponentInParent<Canvas>();
    }

    public void SpawnFloatingScore(int amount)
    {
        if (floatingScorePrefab == null || targetUIElement == null || _parentCanvas == null) return;

        GameObject scoreObj = Instantiate(floatingScorePrefab, targetUIElement.parent);
        
        TextMeshProUGUI txt = scoreObj.GetComponent<TextMeshProUGUI>();
        if (txt == null) txt = scoreObj.GetComponentInChildren<TextMeshProUGUI>();
        if (txt != null) txt.text = "+" + amount.ToString();

        // 1. HEDEF SAPMASINI BELİRLE (Rastgele bir X noktası seç)
        // Her skor objesi, hedefin farklı bir noktasına (sağına, soluna) kilitlenir.
        float randomTargetX = Random.Range(-targetRandomRangeX, targetRandomRangeX);

        // 2. BAŞLANGIÇ POZİSYONU
        Vector3 baseTargetPos = GetTargetPosition();
        
        Vector3 startPos = baseTargetPos;
        startPos.y -= spawnDistanceY; 
        // Aşağıda doğarken de rastgele bir yerde doğsun
        startPos.x += Random.Range(-spawnRandomRangeX, spawnRandomRangeX); 
        // Not: startPos.x hesabında baseTargetPos.x'i baz aldık ama üzerine random ekledik.

        scoreObj.transform.position = startPos;
        scoreObj.transform.localScale = Vector3.one;

        // 3. Hareketi Başlat (Hedef sapmasını da gönderiyoruz)
        StartCoroutine(MoveScoreRoutine(scoreObj, randomTargetX));
    }

    private IEnumerator MoveScoreRoutine(GameObject scoreObj, float targetOffsetX)
    {
        float timer = 0f;
        Vector3 startPos = scoreObj.transform.position;
        
        while (timer < flyDuration)
        {
            if (scoreObj == null) yield break;

            timer += Time.deltaTime;
            float t = timer / flyDuration;
            float easeT = t * t; // Exponential Ease-In (Hızlanarak çarpma)

            // Hedef pozisyonu her karede güncelle (UI titrerse veya kayarsa diye)
            // AMA hesapladığımız random 'targetOffsetX' değerini hep koru.
            Vector3 currentTarget = GetTargetPosition();
            currentTarget.x += targetOffsetX; // Rastgele seçilen o noktaya git

            scoreObj.transform.position = Vector3.Lerp(startPos, currentTarget, easeT);
            yield return null;
        }

        // --- Çarpma Anı ---
        // Patlama efektini tam çarptığı (offsetli) yerde çıkar
        Vector3 finalHitPos = scoreObj.transform.position;

        if (scoreObj != null) Destroy(scoreObj);

        if (impactPrefab != null)
        {
            GameObject impact = Instantiate(impactPrefab, targetUIElement.parent);
            impact.transform.position = finalHitPos; 
            Destroy(impact, 1.0f);
        }

        StartCoroutine(PunchEffect(targetUIElement));
    }

    private IEnumerator PunchEffect(RectTransform target)
    {
        Vector3 originalScale = Vector3.one;
        target.localScale = originalScale * scalePunchAmount;

        float duration = 0.1f;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            target.localScale = Vector3.Lerp(originalScale * scalePunchAmount, originalScale, timer / duration);
            yield return null;
        }
        target.localScale = originalScale;
    }

    private Vector3 GetTargetPosition()
    {
        if (targetUIElement == null) return Vector3.zero;
        return targetUIElement.position + targetOffset;
    }

    private void OnDrawGizmosSelected()
    {
        if (targetUIElement != null)
        {
            Gizmos.color = Color.green;
            Vector3 centerPos = targetUIElement.position + targetOffset;
            
            // Merkez Nokta
            Gizmos.DrawWireSphere(centerPos, 10f); 
            
            // Vuruş Alanı Genişliği (Hedefin nereye kadar sapabileceğini gösterir)
            Gizmos.color = Color.yellow;
            Vector3 leftLimit = centerPos + Vector3.left * targetRandomRangeX;
            Vector3 rightLimit = centerPos + Vector3.right * targetRandomRangeX;
            Gizmos.DrawLine(leftLimit, rightLimit);
            Gizmos.DrawWireSphere(leftLimit, 5f);
            Gizmos.DrawWireSphere(rightLimit, 5f);
        }
    }
}