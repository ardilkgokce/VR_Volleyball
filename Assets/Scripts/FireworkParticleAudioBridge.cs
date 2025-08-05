using UnityEngine;
using System.Collections.Generic;

public class FireworkParticleAudioBridge : MonoBehaviour
{
    [SerializeField] private ParticleSystem fireworkParticles;
    [SerializeField] private FireworkSoundManager soundManager;
    
    [Header("Ses Tetikleme Ayarları")]
    [SerializeField] private bool useParticleTracking = true;
    [SerializeField] private bool useBurstDetection = true;
    [SerializeField] private bool useEmissionRate = true;
    [SerializeField] private float soundCooldown = 0.05f;
    
    [Header("Yoğunluk Ayarları")]
    [SerializeField] private int soundsPerBurst = 3;
    [SerializeField] private float particleCountThreshold = 5;
    [SerializeField] private float emissionRateThreshold = 10f;
    
    private float lastSoundTime;
    private float lastBurstTime;
    private int lastParticleCount;
    private int currentParticleCount;
    private ParticleSystem.Particle[] particles;
    
    void Start()
    {
        if (fireworkParticles == null)
            fireworkParticles = GetComponent<ParticleSystem>();
            
        if (soundManager == null)
            soundManager = FindObjectOfType<FireworkSoundManager>();
        
        particles = new ParticleSystem.Particle[fireworkParticles.main.maxParticles];
        lastParticleCount = 0;
    }
    
    void Update()
    {
        currentParticleCount = fireworkParticles.particleCount;
        
        // Yöntem 1: Particle sayısı artışını tespit et
        if (useParticleTracking)
        {
            DetectNewParticles();
        }
        
        // Yöntem 2: Burst zamanlarını kontrol et
        if (useBurstDetection)
        {
            DetectAndPlayBurstSounds();
        }
        
        // Yöntem 3: Emission rate kontrolü
        if (useEmissionRate)
        {
            CheckEmissionRate();
        }
    }
    
    void DetectNewParticles()
    {
        // Particle sayısı artışını tespit et
        int particleIncrease = currentParticleCount - lastParticleCount;
        
        if (particleIncrease > 0 && Time.time - lastSoundTime > soundCooldown)
        {
            // Her X yeni particle için ses çal
            int soundsToPlay = Mathf.Min(particleIncrease / (int)particleCountThreshold, 3);
            
            if (soundsToPlay > 0)
            {
                for (int i = 0; i < soundsToPlay; i++)
                {
                    soundManager.PlayRandomFireworkSound();
                }
                lastSoundTime = Time.time;
            }
        }
        
        lastParticleCount = currentParticleCount;
    }
    
    void DetectAndPlayBurstSounds()
    {
        var emission = fireworkParticles.emission;
        
        // Burst kontrolü
        if (emission.burstCount > 0)
        {
            var bursts = new ParticleSystem.Burst[emission.burstCount];
            emission.GetBursts(bursts);
            
            float currentTime = fireworkParticles.time;
            float duration = fireworkParticles.main.duration;
            float normalizedTime = currentTime % duration;
            
            foreach (var burst in bursts)
            {
                // Burst zamanına yakınsa
                if (Mathf.Abs(normalizedTime - burst.time) < 0.1f && 
                    Time.time - lastBurstTime > 0.2f)
                {
                    // Burst count kadar ses çal
                    int burstCount = (int)burst.count.Evaluate(0);
                    int soundCount = Mathf.Min(burstCount, soundsPerBurst);
                    
                    for (int i = 0; i < soundCount; i++)
                    {
                        soundManager.PlayRandomFireworkSound();
                    }
                    
                    lastBurstTime = Time.time;
                }
            }
        }
    }
    
    void CheckEmissionRate()
    {
        var emission = fireworkParticles.emission;
        float currentRate = emission.rateOverTime.constant;
        
        // Emission rate yüksekse sürekli ses çal
        if (currentRate > emissionRateThreshold && 
            Time.time - lastSoundTime > soundCooldown)
        {
            soundManager.PlayRandomFireworkSound();
            lastSoundTime = Time.time;
        }
    }
    
    // Sub Emitter tetiklendiğinde (eğer varsa)
    void OnParticleSystemStopped()
    {
        if (soundManager != null)
        {
            soundManager.PlayFireworkBurst();
        }
    }
    
    // Manuel tetikleme
    public void TriggerFireworkSound()
    {
        soundManager.PlayFireworkBurst();
    }
    
    // Particle System oynatıldığında
    void OnEnable()
    {
        if (fireworkParticles != null && fireworkParticles.isPlaying)
        {
            soundManager.PlayRandomFireworkSound();
        }
    }
    
    // Yoğunluğu ayarla
    public void SetIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);
        soundsPerBurst = Mathf.RoundToInt(Mathf.Lerp(1, 5, intensity));
        particleCountThreshold = Mathf.Lerp(10, 2, intensity);
        soundCooldown = Mathf.Lerp(0.1f, 0.02f, intensity);
        emissionRateThreshold = Mathf.Lerp(20, 5, intensity);
        
        if (soundManager != null)
        {
            soundManager.SetIntensity(intensity);
        }
    }
    
    // Debug için
    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 100, 150, 30), "Manuel Havai Fişek"))
        {
            TriggerFireworkSound();
        }
        
        GUI.Label(new Rect(10, 140, 300, 20), $"Particle Sayısı: {currentParticleCount}");
    }
}