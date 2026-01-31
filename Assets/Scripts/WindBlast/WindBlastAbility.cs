using UnityEngine;

public class WindBlastAbility : MonoBehaviour
{
    [Header("Tuş ve Süre")]
    public KeyCode activationKey = KeyCode.G;
    public float cooldownTime = 1.0f;

    [Header("Pozisyon Ayarları")]
    public float heightOffset = .25f;   // Yerden ne kadar yukarıda? (Kamera hizası için 1.5 iyi)
    public float forwardOffset = 1.0f;  // Karakterin ne kadar önünde?
    public float leftRightOffset = .33f;

    [Header("Vuruş Alanı (Hitbox)")]
    // X: Genişlik, Y: Yükseklik, Z: İleri Uzunluk
    public Vector3 hitboxSize = new Vector3(4f, 2f, 6f); 
    public LayerMask enemyLayer; // Sadece "Enemy" seç!

    [Header("Fizik Gücü")]
    public float pushForce = 20f;   
    public float liftForce = 7f;    

    [Header("Görsel")]
    public GameObject windVFXPrefab; 

    [Header("Maske Fırlatma Ayarı")]
    public float maskLiftAmount = 2.0f; // Ne kadar dik yukarı fırlasın?
    public float maskPushAmount = 1.0f; // Ne kadar uzağa (geriye) gitsin?

    private float lastFireTime;

    void Update()
    {
        if (Input.GetKeyDown(activationKey) && Time.time >= lastFireTime + cooldownTime)
        {
            PerformInstantBlast();
            lastFireTime = Time.time;
        }
    }

    public void PerformInstantBlast()
    {
        // 1. MERKEZ NOKTAYI HESAPLA
        // Karakterin Merkezi + Yukarı + İleri
        Vector3 blastCenter = transform.position 
                              + (Vector3.up * heightOffset) 
                              + (transform.forward * forwardOffset)
                              + (transform.right * leftRightOffset);

        // 2. GÖRSELİ OLUŞTUR
        if (windVFXPrefab != null)
        {
            GameObject vfx = Instantiate(windVFXPrefab, blastCenter, transform.rotation);
            Destroy(vfx, 2.0f); // 2 saniye sonra sil
        }

        // 3. ANLIK ALAN TARAMASI (OverlapBox)
        // Hesaplanan 'blastCenter' noktasında kutuyu oluştur
        Collider[] hits = Physics.OverlapBox(blastCenter, hitboxSize / 2, transform.rotation, enemyLayer);

        if (hits.Length > 0)
        {
            foreach (Collider hit in hits)
            {
                // A. FİZİKSEL İTME (Mevcut Kod)
                Rigidbody enemyRb = hit.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    enemyRb.linearVelocity = Vector3.zero; // Hızı sıfırla
                    
                    Vector3 pushDir = transform.forward;
                    enemyRb.AddForce((pushDir * pushForce) + (Vector3.up * liftForce), ForceMode.Impulse);
                }

                // B. MASKE DÜŞÜRME (YENİ EKLENEN KISIM) ---------------------------
                // Çarptığımız objede EnemyBase (veya ChaserEnemy) var mı diye bakıyoruz
                EnemyBase enemyBase = hit.GetComponent<EnemyBase>();
                
                if (enemyBase != null)
                {
                    // YÖN HESABI: Hem yukarı (Up) hem de rüzgar yönünde (Forward) kuvvet uygula
                    Vector3 maskDir = (Vector3.up * maskLiftAmount) + (transform.forward * maskPushAmount);
                    
                    enemyBase.DestroyMask(maskDir);
                }
                // ------------------------------------------------------------------
            }
        }
    }

    // Editörde Kırmızı Kutuyu Görmek İçin
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.5f); // Yarı saydam kırmızı
        
        // Aynı hesaplamayı burada da yapıyoruz ki editörde görelim
        Vector3 center = transform.position 
                         + (Vector3.up * heightOffset) 
                         + (transform.forward * forwardOffset);

        // Kutuyu karakterin rotasyonuna göre döndürerek çiz
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}