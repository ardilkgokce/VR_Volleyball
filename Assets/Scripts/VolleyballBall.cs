using UnityEngine;
using System.Collections.Generic;
using TMPro; // TextMeshPro için gerekli

public class VolleyballBall : MonoBehaviour
{
    [Header("Team Hit Tracking")]
    public Team currentTeam = Team.Blue;
    public int currentTeamHits = 0;
    public int maxHitsPerTeam = 3;
    
    [Header("Hit History")]
    public List<HitInfo> hitHistory = new List<HitInfo>();
    private Transform lastHitter;
    
    [Header("Visual Feedback")]
    public bool showDebugInfo = true;
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color criticalColor = Color.red;
    private Renderer ballRenderer;
    
    [Header("Audio")]
    public AudioClip hitSound;
    public AudioClip warningSound;
    private AudioSource audioSource;
    
    [Header("UI Settings")]
    public float uiDisplayDuration = 3f; // UI'ın ekranda kalma süresi
    private float uiTimer = 0f;
    
    // Court manager referansı
    private VolleyballCourtManager courtManager;
    
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
    
    // Düşme bölgesi enum'ı
    public enum DropZone
    {
        RedCourtInside,
        RedCourtOutside,
        BlueCourtInside,
        BlueCourtOutside
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
        
        // Court manager'ı bul
        courtManager = FindObjectOfType<VolleyballCourtManager>();
        if (courtManager == null)
        {
            Debug.LogWarning("VolleyballCourtManager not found! Drop zone detection may not work properly.");
        }
    }
    
    public bool OnHit(Transform hitter, Team hitterTeam)
    {
        if (lastHitter == hitter)
        {
            Debug.LogWarning($"{hitter.name} cannot hit the ball twice in a row!");
            return false;
        }
        
        if (hitterTeam != currentTeam)
        {
            currentTeam = hitterTeam;
            currentTeamHits = 1;
            Debug.Log($"Ball passed to {currentTeam} team. Hit count reset to 1.");
        }
        else
        {
            currentTeamHits++;
            
            if (currentTeamHits > maxHitsPerTeam)
            {
                Debug.LogError($"{currentTeam} team exceeded maximum hits ({maxHitsPerTeam})! FAULT!");
                OnFault(currentTeam);
                return false;
            }
            
            Debug.Log($"{currentTeam} team hit #{currentTeamHits}/{maxHitsPerTeam}");
        }
        
        hitHistory.Add(new HitInfo(hitter.name, hitterTeam, hitter.position));
        lastHitter = hitter;
        
        UpdateBallColor();
        PlayHitSound();
        
        return true;
    }
    
    public void OnBallDrop(Vector3 dropPosition)
    {
        // Düşme bölgesini belirle
        DropZone zone = DetermineDropZone(dropPosition);
        
        // Skor veren takımı belirle
        Team scoringTeam = GetScoringTeam(zone);
        
        // Skor sebebini oluştur
        string scoreReason = GetScoreReason(zone);
        
        Debug.Log($"Ball dropped in {zone}. {scoringTeam} team scores!");
        
        // GameManager'a skoru bildir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoringTeam, scoreReason);
        }
        else
        {
            Debug.LogError("GameManager instance not found! Cannot update score.");
        }
        
        ResetBall();
    }
    
    // Topun düştüğü bölgeyi belirle
    private DropZone DetermineDropZone(Vector3 dropPosition)
    {
        if (courtManager == null)
        {
            // Court manager yoksa basit X koordinatına göre belirle
            return dropPosition.x > 0 ? DropZone.BlueCourtInside : DropZone.RedCourtInside;
        }
        
        float halfLength = courtManager.courtLength / 2f;
        float halfWidth = courtManager.courtWidth / 2f;
        
        // X koordinatına göre hangi takım sahası
        bool isRedSide = dropPosition.x < 0;
        
        // Saha içinde mi dışında mı kontrol et
        bool isInsideCourt = Mathf.Abs(dropPosition.x) <= halfLength && 
                            Mathf.Abs(dropPosition.z) <= halfWidth;
        
        if (isRedSide)
        {
            return isInsideCourt ? DropZone.RedCourtInside : DropZone.RedCourtOutside;
        }
        else
        {
            return isInsideCourt ? DropZone.BlueCourtInside : DropZone.BlueCourtOutside;
        }
    }
    
    // Hangi takımın skor aldığını belirle
    private Team GetScoringTeam(DropZone zone)
    {
        switch (zone)
        {
            case DropZone.RedCourtInside:
                return Team.Blue; // Red sahada düştü, Blue skor alır
            case DropZone.BlueCourtInside:
                return Team.Red; // Blue sahada düştü, Red skor alır
            case DropZone.RedCourtOutside:
            case DropZone.BlueCourtOutside:
                // Dışarı düştüyse, son vuran takımın rakibi skor alır
                return currentTeam == Team.Blue ? Team.Red : Team.Blue;
            default:
                return Team.Blue;
        }
    }
    
    // UI'ı güncelle
    
    
    // Bölge adını döndür
    private string GetZoneDisplayText(DropZone zone)
    {
        switch (zone)
        {
            case DropZone.RedCourtInside:
                return "Kırmızı Saha İçi";
            case DropZone.RedCourtOutside:
                return "Kırmızı Saha Dışı";
            case DropZone.BlueCourtInside:
                return "Mavi Saha İçi";
            case DropZone.BlueCourtOutside:
                return "Mavi Saha Dışı";
            default:
                return "Bilinmeyen Bölge";
        }
    }
 
    
    // Skor sebebini döndür
    private string GetScoreReason(DropZone zone)
    {
        switch (zone)
        {
            case DropZone.RedCourtInside:
                return "Ball dropped in Red court";
            case DropZone.BlueCourtInside:
                return "Ball dropped in Blue court";
            case DropZone.RedCourtOutside:
            case DropZone.BlueCourtOutside:
                return "Ball out of bounds";
            default:
                return "Unknown reason";
        }
    }
    
    void OnFault(Team faultTeam)
    {
        Team scoringTeam = faultTeam == Team.Blue ? Team.Red : Team.Blue;
        Debug.LogError($"{faultTeam} committed a fault! {scoringTeam} scores!");
        
        // GameManager'a fault skorunu bildir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoringTeam, $"{faultTeam} team fault - too many hits");
        }
        
        ResetBall();
    }
    
    public void ResetBall()
    {
        currentTeamHits = 0;
        lastHitter = null;
        hitHistory.Clear();
        UpdateBallColor();
        
        Debug.Log("Ball reset for new rally");
    }
    
    void UpdateBallColor()
    {
        if (ballRenderer == null) return;
        
        if (currentTeamHits == 0)
        {
            ballRenderer.material.color = normalColor;
        }
        else if (currentTeamHits == maxHitsPerTeam - 1)
        {
            ballRenderer.material.color = warningColor;
        }
        else if (currentTeamHits == maxHitsPerTeam)
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
            audioSource.PlayOneShot(hitSound);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") || collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            OnBallDrop(transform.position);
        }
    }
    
    public int GetRemainingHits()
    {
        return maxHitsPerTeam - currentTeamHits;
    }
    
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
    
    // Debug için bölge görselleştirme
    void OnDrawGizmos()
    {
        if (courtManager == null || !Application.isPlaying) return;
        
        float halfLength = courtManager.courtLength / 2f;
        float halfWidth = courtManager.courtWidth / 2f;
        
        // Red saha içi
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        Gizmos.DrawCube(new Vector3(-halfLength/2, 0.1f, 0), new Vector3(halfLength, 0.1f, courtManager.courtWidth));
        
        // Blue saha içi
        Gizmos.color = new Color(0, 0, 1, 0.1f);
        Gizmos.DrawCube(new Vector3(halfLength/2, 0.1f, 0), new Vector3(halfLength, 0.1f, courtManager.courtWidth));
    }
}