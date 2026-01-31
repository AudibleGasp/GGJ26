using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MaskUIManager : MonoBehaviour
{
    [Header("Referanslar")]
    public PlayerController playerController; // Inspector'dan Player'ı sürükle veya kod bulsun
    
    [Header("UI Objeleri")]
    public Transform maskContainer;    
    // Sıralama: Element 0: Slot 1 (EN ALT), Element 1: Ortadaki, Element 2: Slot 3 (EN ÜST)
    public Transform[] slots;          
    public GameObject maskPrefab;      
    public Transform movingMaskParent; 

    [Header("Animasyon Ayarları")]
    public float slideDuration = 0.5f; 
    public float startDistance = 500f; 
    public float overshootAmount = 1.5f; 
    public float containerPunchAmount = 0.2f; // Büyüme/Titreme şiddeti (0.1 = %10, 0.5 = %50)

    // Görsel maskeleri tutan liste
    private List<GameObject> activeVisualMasks = new List<GameObject>();

    private void Start()
    {
        // Eğer Inspector'dan atamadıysan otomatik bul
        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Update()
    {
        if (playerController == null) return;

        // --- SENKRONİZASYON MANTIĞI (POLLING) ---
        // Player'daki gerçek veri sayısı ile ekrandaki görsel sayısı tutmuyor mu?
        
        int dataCount = playerController.masks.Count; // Player'daki sayı
        int visualCount = activeVisualMasks.Count;    // Ekrandaki sayı

        // DURUM 1: Player yeni maske almış (Veri > Görsel)
        if (dataCount > visualCount)
        {
            // Aradaki fark kadar maske ekle
            for (int i = 0; i < dataCount - visualCount; i++)
            {
                // Eklenen maskenin tipini bul (Listenin sonundakidir)
                // (Mevcut görsel sayısının üzerine 'i' ekleyerek sıradaki veriyi alıyoruz)
                MaskType typeToAdd = playerController.masks[visualCount + i];
                AddMask(typeToAdd);
            }
        }
        // DURUM 2: Player maske harcamış (Veri < Görsel)
        else if (dataCount < visualCount)
        {
            // Aradaki fark kadar maske sil
            for (int i = 0; i < visualCount - dataCount; i++)
            {
                UseMask();
            }
        }
    }

    // Parametre olarak MaskType alıyoruz ki rengi ona göre ayarlayabilelim
    private void AddMask(MaskType type)
    {
        // Güvenlik: Slot sayısını aşarsa görsel oluşturma
        if (activeVisualMasks.Count >= slots.Length) return;

        GameObject newMask = Instantiate(maskPrefab, movingMaskParent);

        // --- MASKE TİPİNE GÖRE RENK AYARI ---
        // PlayerController'daki enum'a göre renk veriyoruz.
        Color maskColor = Color.white;
        switch (type)
        {
            case MaskType.Basic:
                maskColor = Color.white; 
                break;
            case MaskType.Penetrating:
                maskColor = Color.red; // Örnek: Delici maske kırmızı olsun
                break;
            case MaskType.Wind:
                maskColor = Color.yellow;
                break;
            // Diğer tipler buraya eklenebilir
        }
        newMask.GetComponent<Image>().color = maskColor;
        // -------------------------------------

        // DİKEY DOĞMA POZİSYONU (En üst slotun üzerinden)
        Vector3 startPos = slots[slots.Length - 1].position;
        startPos.y += startDistance; 
        newMask.transform.position = startPos;

        activeVisualMasks.Add(newMask);
        
        // Animasyonları başlat
        UpdatePositions(true);
        maskContainer.DOPunchScale(Vector3.one * containerPunchAmount, 0.3f, 10, 1).SetLink(maskContainer.gameObject);
    }

    private void UseMask()
    {
        if (activeVisualMasks.Count == 0) return;

        // PlayerController mantığına uygun olarak EN BAŞTAKİ (0. index = Slot 1) silinir.
        GameObject usedMask = activeVisualMasks[0];
        activeVisualMasks.RemoveAt(0);

        // Aşağı düşme ve yok olma efekti
        usedMask.transform.DOMoveY(usedMask.transform.position.y - 200f, 0.5f)
            .SetLink(usedMask);
            
        usedMask.GetComponent<Image>().DOFade(0, 0.5f)
            .SetLink(usedMask)
            .OnComplete(() => Destroy(usedMask));

        UpdatePositions(false);
    }

    private void UpdatePositions(bool isAddingNew)
    {
        for (int i = 0; i < activeVisualMasks.Count; i++)
        {
            GameObject maskObj = activeVisualMasks[i];
            
            // Yerçekimi sıralaması: i=0 -> Slot 1 (En Alt)
            Transform targetSlot = slots[i]; 

            if (isAddingNew && i == activeVisualMasks.Count - 1)
            {
                maskObj.transform.DOMove(targetSlot.position, slideDuration)
                    .SetEase(Ease.OutBack, overshootAmount)
                    .SetLink(maskObj);
            }
            else
            {
                maskObj.transform.DOMove(targetSlot.position, slideDuration)
                    .SetEase(Ease.OutQuad)
                    .SetLink(maskObj);
            }
        }
    }
}