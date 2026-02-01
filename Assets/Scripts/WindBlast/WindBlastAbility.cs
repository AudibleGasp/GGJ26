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
            PerformInstantBlast(transform);
            lastFireTime = Time.time;
        }
    }

    public void PerformInstantBlast(Transform performer)
    {
        // --- YENİ EKLENEN SATIR ---
        // Kutunun merkezini, boyunun yarısı kadar (z * 0.5) ileri kaydırıyoruz.
        // Böylece Z boyutu arttıkça kutu geriye değil, ileriye doğru büyür.
        float centerZ = forwardOffset + (hitboxSize.z * 0.5f);

        // 1. MERKEZ NOKTAYI HESAPLA
        // Karakterin Merkezi + Yukarı + İleri
        Vector3 blastCenter = performer.position 
                              + (Vector3.up * heightOffset) 
                              + (performer.forward * centerZ)
                              - (transform.forward * 3f)
                              + (performer.right * leftRightOffset);

        // 2. GÖRSELİ OLUŞTUR
        if (windVFXPrefab != null)
        {
            GameObject vfx = Instantiate(windVFXPrefab, blastCenter, performer.rotation);
            Destroy(vfx, 2.0f); // 2 saniye sonra sil
        }

        // 3. ANLIK ALAN TARAMASI (OverlapBox)
        // Hesaplanan 'blastCenter' noktasında kutuyu oluştur
        Collider[] hits = Physics.OverlapBox(blastCenter, hitboxSize / 2, performer.rotation, enemyLayer);

        if (hits.Length > 0)
        {
            foreach (Collider hit in hits)
            {
                // A. FİZİKSEL İTME (Mevcut Kod)
                Rigidbody enemyRb = hit.GetComponent<Rigidbody>();
                if (enemyRb != null)
                {
                    enemyRb.linearVelocity = Vector3.zero; // Hızı sıfırla
                    
                    Vector3 pushDir = performer.forward;
                    enemyRb.AddForce((pushDir * pushForce) + (Vector3.up * liftForce), ForceMode.Impulse);
                }

                // B. MASKE DÜŞÜRME (YENİ EKLENEN KISIM) ---------------------------
                // Çarptığımız objede EnemyBase (veya ChaserEnemy) var mı diye bakıyoruz
                EnemyBase enemyBase = hit.GetComponent<EnemyBase>();
                
                if (enemyBase != null)
                {
                    // YÖN HESABI: Hem yukarı (Up) hem de rüzgar yönünde (Forward) kuvvet uygula
                    // Vector3 maskDir = (Vector3.up * maskLiftAmount) + (transform.forward * maskPushAmount);
                    
                    enemyBase.DestroyMask();
                }
                // ------------------------------------------------------------------
            }
        }
    }

    // ARTIK OBJE SEÇİLİ OLMASA BİLE ÇİZER
    private void OnDrawGizmos()
    {
        // 1. RENGİ BELİRLE
        // İçini yarı saydam kırmızı yapalım
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); 

        float centerZ = forwardOffset + (hitboxSize.z * 0.5f);

        // 2. MERKEZİ HESAPLA (PerformInstantBlast ile birebir aynı olmalı)
        // DİKKAT: Orijinal kodda 'leftRightOffset' gizmos'ta unutulmuştu, ekledim.
        Vector3 center = transform.position 
                         + (Vector3.up * heightOffset) 
                         + (transform.forward * centerZ)
                         - (transform.forward * 3f)
                         + (transform.right * leftRightOffset); 

        // 3. ROTASYONU AYARLA
        // Kutunun karakterle birlikte dönmesi için Matrix kullanıyoruz
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);

        // 4. ÇİZİM
        // İçi dolu kutu (Hacmi görmek için)
        Gizmos.DrawCube(Vector3.zero, hitboxSize);

        // Kenar çizgileri (Net sınırları görmek için)
        Gizmos.color = Color.red; // Tam kırmızı
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}