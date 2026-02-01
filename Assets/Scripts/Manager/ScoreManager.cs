using UnityEngine;
using TMPro;
using System.Collections; // Coroutine kullanımı için gerekli

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [Header("UI Referansları")]
    public TextMeshProUGUI carnageText;     // Skor (Carnage) Yazısı
    public TextMeshProUGUI multiplierText;  // Çarpan (Multiplier) Yazısı
    public TextMeshProUGUI timerText;       // Süre (Timer) Yazısı

    [Header("UI Animasyon (Game Feel)")]
    public float scoreCountDuration = 0.5f; // Sayıların dönme hızı (Rolling effect)
    public float punchScaleAmount = 1.2f;   // Skor artınca ne kadar büyüsün?
    public float punchDuration = 0.2f;      // Büyüme süresi

    [Header("Multiplier Ayarları")]
    public float multiplierIncrement = 0.1f; 

    [Header("Airborne Ayarları (Havada Vuruş)")]
    public float airborneBonus = 0.05f; 
    public Color airborneColor = Color.cyan; 

    [Header("Multi-Kill (Seri Katil) Ayarları")]
    public float multiKillBonus = 0.1f;          // 3'lü kombo ödülü
    public float multiKillDuration = 3.0f;       // Kaç saniye içinde yapılmalı?
    public Color multiKillColor = Color.magenta; // Yazı rengi

    [Header("Zaman Ayarları")]
    public float gameTimeInterval = 5f;   
    public float noDamageInterval = 5f;   

    [Header("Camera Shake")]
    public float shakeDuration = 0.15f;       // Sarsıntı ne kadar sürsün?
    [Range(0f, 1f)] 
    public float normalShakeIntensity = 0.1f; // Normal vuruş şiddeti
    [Range(0f, 2f)] 
    public float bigShakeIntensity = 0.4f;    // FATAL / Triple Kill şiddeti

    [Header("Slap Fury (Tokat Kombosu) Ayarları")]
    public int slapTargetCount = 5;          // Hedef: 5 Tokat
    public float slapComboDuration = 5.0f;   // Süre: 5 Saniye
    public float slapComboBonus = 0.5f;      // Ödül: +0.5 Multiplier
    public Color slapComboColor = Color.yellow; // Renk: Sarı/Altın

    // Slap Takibi için Private Değişkenler
    private int _currentSlapStreak = 0;
    private float _lastSlapTime;

    // Read-Only Değerler
    public float CurrentScore { get; private set; }
    public float GlobalMultiplier { get; private set; } = 1.0f;

    // Sayaçlar
    private float _gameTimeTimer;
    private float _noDamageTimer;
    private float _totalGameTime; // Oyunun toplam süresi

    // Multi-Kill Takibi
    private int _currentKillStreak = 0;
    private float _lastKillTime;

    // UI Animasyon Değişkenleri
    private float _displayedScore; // Ekranda o an görünen sayı
    private Coroutine _scoreAnimationRoutine;
    private Vector3 _originalScoreScale;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Orijinal boyutu hafızaya al (Punch efekti için)
        if (carnageText != null) _originalScoreScale = carnageText.transform.localScale;

        UpdateScoreUI(true); // True = Animasyonsuz ilk güncelleme
        UpdateMultiplierUI();
    }

    private void Update()
    {
        HandleTimers();

        // Toplam süreyi artır ve ekrana yaz
        _totalGameTime += Time.deltaTime;
        UpdateTimerUI();

        // Kill Streak Zaman Aşımı Kontrolü
        if (Time.time - _lastKillTime > multiKillDuration && _currentKillStreak > 0)
        {
            _currentKillStreak = 0;
        }
    }

    private void HandleTimers()
    {
        bool multiplierChanged = false;

        // 1. Oyun Süresi Bonusu
        _gameTimeTimer += Time.deltaTime;
        if (_gameTimeTimer >= gameTimeInterval)
        {
            GlobalMultiplier += multiplierIncrement;
            _gameTimeTimer = 0f;
            multiplierChanged = true;
        }

        // 2. Hasarsızlık Bonusu
        _noDamageTimer += Time.deltaTime;
        if (_noDamageTimer >= noDamageInterval)
        {
            GlobalMultiplier += multiplierIncrement;
            _noDamageTimer = 0f;
            multiplierChanged = true;
        }
        
        if (multiplierChanged) UpdateMultiplierUI();
    }

    private void UpdateTimerUI()
    {
        if (timerText == null) return;

        // Dakika:Saniye formatı (02:14 gibi)
        int minutes = Mathf.FloorToInt(_totalGameTime / 60F);
        int seconds = Mathf.FloorToInt(_totalGameTime % 60F);
        
        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void UpdateMultiplierUI()
    {
        if (multiplierText != null)
            multiplierText.text = $"x{GlobalMultiplier:F1}";
    }

    private void UpdateScoreUI(bool instant = false)
    {
        if (carnageText == null) return;

        // Ani güncelleme (Oyun başı için)
        if (instant)
        {
            _displayedScore = CurrentScore;
            carnageText.text = $"Souls: {Mathf.FloorToInt(_displayedScore)}";
            return;
        }

        // Eski animasyon varsa durdur, yenisini başlat
        if (_scoreAnimationRoutine != null) StopCoroutine(_scoreAnimationRoutine);
        _scoreAnimationRoutine = StartCoroutine(AnimateScoreChange());
    }

    // UI Animasyonu (Punch + Rolling Numbers)
    private IEnumerator AnimateScoreChange()
    {
        float startValue = _displayedScore;
        float endValue = CurrentScore;
        float timer = 0f;

        // 1. PUNCH (Büyüt)
        carnageText.transform.localScale = _originalScoreScale * punchScaleAmount;

        while (timer < scoreCountDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / scoreCountDuration;

            // 2. ROLLING NUMBERS (Sayıları yuvarla)
            _displayedScore = Mathf.Lerp(startValue, endValue, progress);
            carnageText.text = $"Souls: {Mathf.FloorToInt(_displayedScore)}";

            // 3. SCALE DAMPING (Yavaşça küçült)
            if (timer < punchDuration)
            {
                float punchProgress = timer / punchDuration;
                carnageText.transform.localScale = Vector3.Lerp(_originalScoreScale * punchScaleAmount, _originalScoreScale, punchProgress);
            }
            else
            {
                carnageText.transform.localScale = _originalScoreScale;
            }

            yield return null;
        }

        // Son değerleri sabitle
        _displayedScore = endValue;
        carnageText.text = $"Souls: {Mathf.FloorToInt(_displayedScore)}";
        carnageText.transform.localScale = _originalScoreScale;
    }

    public void ResetNoDamageTimer()
    {
        _noDamageTimer = 0f;
    }

    // --- PUAN EKLEME VE MANTIK MERKEZİ ---
    public void AddScore(int enemyBaseValue, Vector3 deathPosition, float bonusMultiplier = 1f, bool isPlayerAirborne = false)
    {
        // 1. KILL STREAK MANTIĞI
        bool isTripleKill = false;

        // Süre dolmadıysa artır, dolduysa sıfırdan başlat
        if (Time.time - _lastKillTime <= multiKillDuration) _currentKillStreak++;
        else _currentKillStreak = 1;

        _lastKillTime = Time.time;

        // 3. Kill'e ulaşıldı mı?
        if (_currentKillStreak >= 3)
        {
            isTripleKill = true;
            GlobalMultiplier += multiKillBonus; // Kalıcı ödül
            _currentKillStreak = 0; // Seriyi sıfırla
            UpdateMultiplierUI();
        }

        // 2. AIRBORNE MANTIĞI
        if (isPlayerAirborne)
        {
            GlobalMultiplier += airborneBonus;
            UpdateMultiplierUI();
        }

        // 3. PUAN HESAPLAMA
        float finalMultiplier = GlobalMultiplier * bonusMultiplier;
        float scoreGain = enemyBaseValue * finalMultiplier;

        CurrentScore += scoreGain;

        // UI Güncellemesini tetikle (Animasyonlu)
        UpdateScoreUI();

        // 4. FLOATING TEXT (Uçan Yazılar)
        if (FloatingTextManager.Instance != null)
        {
            string popupText = $"{Mathf.RoundToInt(scoreGain)}\n<size=60%>(x{finalMultiplier:F1})</size>";
            Color? textColor = null; 

            // Öncelik Sırası: Triple Kill > Airborne > Normal
            if (isPlayerAirborne)
            {
                popupText = "AIRBORNE!\n" + popupText;
                textColor = airborneColor;
            }

            if (isTripleKill)
            {
                popupText = "TRIPLE KILL!\n" + popupText;
                textColor = multiKillColor;
            }

            if (bonusMultiplier > 1.5f) // Düşerek öldüyse (FATAL)
            {
                popupText = "FATAL!\n" + popupText;
                textColor = multiKillColor;
            }

            FloatingTextManager.Instance.ShowText(deathPosition, popupText, textColor);
        }

        // --- YENİSİ ---
        if (SoulFXManager.Instance != null)
        {
            // deathPosition yerine 'scoreGain' gönderiyoruz (int'e çevirerek)
            SoulFXManager.Instance.SpawnFloatingScore((int)scoreGain);
        }

        // --- CAMERA PUNCH EFFECT ---
        if (CameraFollow.Instance != null)
        {
            // Büyük olay mı? (Fatal veya Triple Kill)
            bool isBigEvent = (bonusMultiplier > 1.5f) || (_currentKillStreak >= 3);
            
            // Inspector'dan gelen değerleri kullan
            float intensity = isBigEvent ? bigShakeIntensity : normalShakeIntensity;
            
            CameraFollow.Instance.Punch(shakeDuration, intensity);
        }
    }

    // --- YENİ FONKSİYON: Bunu PlayerCombat scriptinden çağıracaksın ---
    public void RegisterSlapHit(Vector3 hitPosition)
    {
        // 1. Süre Kontrolü: 5 saniye geçtiyse sayacı baştan başlat
        if (Time.time - _lastSlapTime > slapComboDuration)
        {
            _currentSlapStreak = 0;
        }

        // 2. Sayacı ve zamanı güncelle
        _currentSlapStreak++;
        _lastSlapTime = Time.time;

        // 3. Hedefe (5 Tokat) ulaşıldı mı?
        if (_currentSlapStreak >= slapTargetCount)
        {
            TriggerSlapCombo(hitPosition, _currentSlapStreak);
        }
    }

    private void TriggerSlapCombo(Vector3 pos, int slapCount=5)
    {
        // Ödül Ver
        GlobalMultiplier += slapComboBonus;
        UpdateMultiplierUI();

        // Game Feel (Kamera Sarsıntısı)
        if (CameraFollow.Instance != null)
        {
            CameraFollow.Instance.Punch(0.2f, normalShakeIntensity); // Güçlü sarsıntı
        }

        // Floating Text (Ekranda Yazı Çıksın)
        if (FloatingTextManager.Instance != null)
        {
            string comboText = $"{slapCount}x SLAP FURY!";
            FloatingTextManager.Instance.ShowText(pos, comboText, slapComboColor);
        }
    }
}