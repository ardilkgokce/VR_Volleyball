using UnityEngine;
using System.Collections.Generic;

public class VolleyballBall : MonoBehaviour
{
    [Header("Team Hit Tracking")]
    public Team currentTeam = Team.Blue; // Şu anda hangi takım oynuyor
    public int currentTeamHits = 0; // Mevcut takımın vuruş sayısı
    public int maxHitsPerTeam = 3; // Maksimum vuruş hakkı
    
    [Header("Hit History")]
    public List<HitInfo> hitHistory = new List<HitInfo>();
    private Transform lastHitter; // Son vuran
    
    [Header("Visual Feedback")]
    public bool showDebugInfo = true;
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow; // 2 vuruş
    public Color criticalColor = Color.red; // 3 vuruş
    private Renderer ballRenderer;
    
    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip warningSound;
    private AudioSource audioSource;
    
    [System.Serializable]
    public class HitInfo
    {
        public string hitterName;
        public Team team;
        public float time;
        public Vector3 position;
        
        public HitInfo(string name, Team t, Vector3 pos)
        {
            hitterName = name;
            team = t;
            time = Time.time;
            position = pos;
        }
    }
    
    void Start()
    {
        ballRenderer = GetComponent<Renderer>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        UpdateBallColor();
    }
    
    // Bot veya VR oyuncu topa vurduğunda çağrılacak
    public bool OnHit(Transform hitter, Team hitterTeam)
    {
        // Aynı kişi üst üste vuruyorsa (voleybol kuralı)
        if (lastHitter == hitter)
        {
            Debug.LogWarning($"{hitter.name} cannot hit the ball twice in a row!");
            return false;
        }
        
        // Takım değişti mi kontrol et
        if (hitterTeam != currentTeam)
        {
            // Yeni takım
            currentTeam = hitterTeam;
            currentTeamHits = 1;
            Debug.Log($"Ball passed to {currentTeam} team. Hit count reset to 1.");
        }
        else
        {
            // Aynı takım devam ediyor
            currentTeamHits++;
            
            if (currentTeamHits > maxHitsPerTeam)
            {
                Debug.LogError($"{currentTeam} team exceeded maximum hits ({maxHitsPerTeam})! FAULT!");
                OnFault(currentTeam);
                return false;
            }
            
            Debug.Log($"{currentTeam} team hit #{currentTeamHits}/{maxHitsPerTeam}");
        }
        
        // Vuruş geçmişine ekle
        hitHistory.Add(new HitInfo(hitter.name, hitterTeam, hitter.position));
        lastHitter = hitter;
        
        // Görsel ve ses efektleri
        UpdateBallColor();
        PlayHitSound();
        
        // 3. vuruşsa uyarı
        if (currentTeamHits == maxHitsPerTeam)
        {
            Debug.LogWarning($"{currentTeam} team must pass to other side! Last hit!");
            if (warningSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(warningSound);
            }
        }
        
        return true;
    }
    
    // Top yere düştüğünde veya dışarı çıktığında
    public void OnBallDrop(Vector3 dropPosition)
    {
        // Hangi tarafta düştü?
        Team scoringTeam = dropPosition.x > 0 ? Team.Red : Team.Blue;
        Debug.Log($"Ball dropped at {dropPosition}. {scoringTeam} team scores!");
        
        ResetBall();
    }
    
    // Kural ihlali olduğunda
    void OnFault(Team faultTeam)
    {
        Team scoringTeam = faultTeam == Team.Blue ? Team.Red : Team.Blue;
        Debug.LogError($"{faultTeam} committed a fault! {scoringTeam} scores!");
        
        // TODO: Skor sistemi entegrasyonu
        
        ResetBall();
    }
    
    // Topu sıfırla
    public void ResetBall()
    {
        currentTeamHits = 0;
        lastHitter = null;
        hitHistory.Clear();
        UpdateBallColor();
        
        Debug.Log("Ball reset for new rally");
    }
    
    // Renk güncelleme
    void UpdateBallColor()
    {
        if (ballRenderer == null) return;
        
        if (currentTeamHits == 0)
        {
            ballRenderer.material.color = normalColor;
        }
        else if (currentTeamHits == maxHitsPerTeam - 1) // 2 vuruş
        {
            ballRenderer.material.color = warningColor;
        }
        else if (currentTeamHits == maxHitsPerTeam) // 3 vuruş
        {
            ballRenderer.material.color = criticalColor;
        }
        else
        {
            ballRenderer.material.color = normalColor;
        }
    }
    
    void PlayHitSound()
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.pitch = 1f + (currentTeamHits - 1) * 0.1f; // Her vuruşta pitch artar
            audioSource.PlayOneShot(hitSound);
        }
    }
    
    // Ground collision
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            OnBallDrop(transform.position);
        }
    }
    
    // Hangi takımın kaç vuruş hakkı kaldı
    public int GetRemainingHits()
    {
        return maxHitsPerTeam - currentTeamHits;
    }
    
    // Bot için helper method - karşı takıma mı atmalı?
    public bool MustPassToOpponent()
    {
        return currentTeamHits >= maxHitsPerTeam;
    }
    
    void OnGUI()
    {
        if (!showDebugInfo || !Application.isPlaying) return;
        
        GUIStyle style = new GUIStyle();
        style.fontSize = 20;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 300, 30), $"Current Team: {currentTeam}", style);
        GUI.Label(new Rect(10, 40, 300, 30), $"Hits: {currentTeamHits}/{maxHitsPerTeam}", style);
        
        if (currentTeamHits == maxHitsPerTeam)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 70, 300, 30), "MUST PASS TO OTHER SIDE!", style);
        }
    }
}