using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class FireworkSoundManager : MonoBehaviour
{
    [Header("Ses Ayarları")]
    [SerializeField] private AudioClip[] fireworkSounds;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Çalma Ayarları")]
    [SerializeField] private float minVolume = 0.6f;
    [SerializeField] private float maxVolume = 1.0f;
    [SerializeField] private float minPitch = 0.85f;
    [SerializeField] private float maxPitch = 1.15f;
    
    [Header("Sıklık Ayarları")]
    [SerializeField] private bool enableContinuousMode = true;
    [SerializeField] private float minInterval = 0.05f;  // Minimum ses aralığı
    [SerializeField] private float maxInterval = 0.3f;   // Maximum ses aralığı
    [SerializeField] private int burstSize = 3;          // Aynı anda kaç ses
    [SerializeField] private float burstSpread = 0.1f;   // Burst içi gecikme
    
    [Header("Ses Limitleri")]
    [SerializeField] private int maxSimultaneousSounds = 20;
    [SerializeField] private float volumeReductionPerSound = 0.05f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    private List<float> activeClipEndTimes = new List<float>();
    private int currentActiveSounds = 0;
    
    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Sürekli mod aktifse otomatik ses çalmaya başla
        if (enableContinuousMode)
        {
            StartCoroutine(ContinuousFireworkMode());
        }
    }
    
    void Update()
    {
        if (showDebugInfo)
        {
            activeClipEndTimes.RemoveAll(endTime => Time.time > endTime);
            currentActiveSounds = activeClipEndTimes.Count;
        }
    }
    
    // Ana ses çalma metodu
    public void PlayRandomFireworkSound()
    {
        if (fireworkSounds.Length == 0) return;
        
        // Ses limiti kontrolü
        if (currentActiveSounds >= maxSimultaneousSounds)
        {
            if (showDebugInfo)
                Debug.LogWarning($"Maksimum ses limitine ulaşıldı: {maxSimultaneousSounds}");
            return;
        }
        
        AudioClip selectedClip = fireworkSounds[Random.Range(0, fireworkSounds.Length)];
        
        // Volume hesaplama - çok ses varsa azalt
        float volumeMultiplier = 1f - (currentActiveSounds * volumeReductionPerSound);
        volumeMultiplier = Mathf.Clamp(volumeMultiplier, 0.3f, 1f);
        float volume = Random.Range(minVolume, maxVolume) * volumeMultiplier;
        
        float pitch = Random.Range(minPitch, maxPitch);
        
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(selectedClip, volume);
        
        if (showDebugInfo)
        {
            float clipDuration = selectedClip.length / pitch;
            activeClipEndTimes.Add(Time.time + clipDuration);
            Debug.Log($"Ses: {selectedClip.name} | Vol: {volume:F2} | Pitch: {pitch:F2} | Aktif: {currentActiveSounds + 1}");
        }
    }
    
    // Burst modunda ses çalma
    public void PlayFireworkBurst()
    {
        StartCoroutine(BurstCoroutine());
    }
    
    IEnumerator BurstCoroutine()
    {
        int soundsToPlay = Random.Range(1, burstSize + 1);
        
        for (int i = 0; i < soundsToPlay; i++)
        {
            PlayRandomFireworkSound();
            
            if (i < soundsToPlay - 1)
            {
                float delay = Random.Range(0, burstSpread);
                yield return new WaitForSeconds(delay);
            }
        }
    }
    
    // Sürekli mod
    IEnumerator ContinuousFireworkMode()
    {
        while (enableContinuousMode)
        {
            // Burst veya tekli ses
            if (Random.value > 0.5f && burstSize > 1)
            {
                PlayFireworkBurst();
            }
            else
            {
                PlayRandomFireworkSound();
            }
            
            // Sonraki ses için bekle
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    // Particle System için çoklu ses desteği
    public void OnParticleBurst(int particleCount = 1)
    {
        StartCoroutine(PlayMultipleSounds(particleCount));
    }
    
    IEnumerator PlayMultipleSounds(int count)
    {
        count = Mathf.Min(count, 5); // Maksimum 5 ses aynı anda
        
        for (int i = 0; i < count; i++)
        {
            PlayRandomFireworkSound();
            yield return new WaitForSeconds(0.02f); // 20ms aralık
        }
    }
    
    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 10, 300, 20), $"Aktif Havai Fişek Sesleri: {currentActiveSounds}");
            GUI.Label(new Rect(10, 30, 300, 20), $"Sürekli Mod: {(enableContinuousMode ? "Açık" : "Kapalı")}");
        }
    }
    
    // Runtime ayarlar için
    public void SetContinuousMode(bool enabled)
    {
        enableContinuousMode = enabled;
        if (enabled)
        {
            StartCoroutine(ContinuousFireworkMode());
        }
        else
        {
            StopAllCoroutines();
        }
    }
    
    public void SetIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        minInterval = Mathf.Lerp(0.2f, 0.05f, intensity);
        maxInterval = Mathf.Lerp(0.5f, 0.1f, intensity);
        burstSize = Mathf.RoundToInt(Mathf.Lerp(1, 5, intensity));
    }
}