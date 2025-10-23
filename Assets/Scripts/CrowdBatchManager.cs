using UnityEngine;
using System.Collections.Generic;

public class CrowdBatchManager : MonoBehaviour
{
    [Header("Crowd Members")]
    [Tooltip("Sahnedeki izleyici karakterlerini buraya sürükleyin")]
    [SerializeField] private List<GameObject> spectators = new List<GameObject>();
    
    [Header("Global Events")]
    [SerializeField] private bool enableGlobalEvents = true;
    [SerializeField] private float globalEventInterval = 30f; // Her 30 saniyede bir
    
    private List<CrowdAnimationController> animationControllers = new List<CrowdAnimationController>();
    
    void Start()
    {
        InitializeSpectators();
        
        if (enableGlobalEvents)
        {
            InvokeRepeating(nameof(TriggerGlobalEvent), globalEventInterval, globalEventInterval);
        }
    }
    
    void InitializeSpectators()
    {
        foreach (GameObject spectator in spectators)
        {
            if (spectator == null) continue;
            
            // CrowdAnimationController ekle (yoksa)
            CrowdAnimationController controller = spectator.GetComponent<CrowdAnimationController>();
            if (controller == null)
            {
                controller = spectator.AddComponent<CrowdAnimationController>();
            }
            
            animationControllers.Add(controller);
            
            // Modüler karakter için rastgele görünüm
            //RandomizeAppearance(spectator);
        }
        
        Debug.Log($"Toplam {animationControllers.Count} izleyici başlatıldı.");
    }
    
    //void RandomizeAppearance(GameObject spectator)
    //{
    //    // Modüler parçaları rastgele aç/kapa
    //    Transform[] allChildren = spectator.GetComponentsInChildren<Transform>(true);
        
    //    // Saç stilleri
    //    List<GameObject> hairOptions = new List<GameObject>();
    //    foreach (Transform child in allChildren)
    //    {
    //        if (child.name.Contains("hair") || child.name.Contains("Hair"))
    //        {
    //            hairOptions.Add(child.gameObject);
    //        }
    //    }
        
    //    // Rastgele bir saç stili seç
    //    if (hairOptions.Count > 0)
    //    {
    //        foreach (var hair in hairOptions)
    //            hair.SetActive(false);
            
    //        int randomHair = Random.Range(0, hairOptions.Count);
    //        hairOptions[randomHair].SetActive(true);
    //    }
        
    //    // Renk varyasyonları
    //    SkinnedMeshRenderer[] renderers = spectator.GetComponentsInChildren<SkinnedMeshRenderer>();
    //    foreach (var renderer in renderers)
    //    {
    //        if (renderer.material.HasProperty("_ColorCustomization1"))
    //        {
    //            // Kıyafet rengi
    //            Color clothColor = Random.ColorHSV(0f, 1f, 0.3f, 0.8f, 0.4f, 1f);
    //            renderer.material.SetColor("_ColorCustomization1", clothColor);
    //        }
            
    //        if (renderer.material.HasProperty("_ColorCustomization2"))
    //        {
    //            // Aksesuar rengi
    //            Color accessoryColor = Random.ColorHSV(0f, 1f, 0.3f, 0.8f, 0.4f, 1f);
    //            renderer.material.SetColor("_ColorCustomization2", accessoryColor);
    //        }
    //    }
    //}
    
    void TriggerGlobalEvent()
    {
        // Tüm izleyicilerin aynı anda tepki vermesi (örn: gol sonrası)
        string globalAnimation = "Cheering";
        
        Debug.Log($"Global Event: {globalAnimation}");
        
        foreach (var controller in animationControllers)
        {
            if (controller != null && controller.enabled)
            {
                controller.ForceRandomAnimation();
            }
        }
    }
    
    // Inspector'da manuel kontrol için
    [ContextMenu("Add Selected Objects")]
    void AddSelectedObjects()
    {
        #if UNITY_EDITOR
        GameObject[] selected = UnityEditor.Selection.gameObjects;
        foreach (GameObject obj in selected)
        {
            if (!spectators.Contains(obj))
            {
                spectators.Add(obj);
            }
        }
        Debug.Log($"{selected.Length} obje eklendi.");
        #endif
    }
    
    [ContextMenu("Clear Empty Entries")]
    void ClearEmptyEntries()
    {
        spectators.RemoveAll(item => item == null);
        Debug.Log("Boş girişler temizlendi.");
    }
    
    [ContextMenu("Force All Cheer")]
    public void ForceAllCheer()
    {
        foreach (var controller in animationControllers)
        {
            if (controller != null)
            {
                controller.GetComponent<Animator>().SetTrigger("Cheering");
            }
        }
    }
    
    // Belirli bir gruba animasyon oynatma
    public void PlayAnimationForGroup(int startIndex, int count, string animationName)
    {
        int endIndex = Mathf.Min(startIndex + count, animationControllers.Count);
        
        for (int i = startIndex; i < endIndex; i++)
        {
            if (animationControllers[i] != null)
            {
                animationControllers[i].GetComponent<Animator>().SetTrigger(animationName);
            }
        }
    }
}