using UnityEngine;

public class VRPlayerProxy : MonoBehaviour
{
    [Header("Player Settings")]
    public Team playerTeam = Team.Blue;
    public Transform targetAnchor; // Botların hedefleyeceği nokta
    public bool isActive = true;
    
    [Header("VR References")]
    public Transform headTransform; // XR Origin Camera
    public Transform leftHandTransform; // Left Hand Anchor
    public Transform rightHandTransform; // Right Hand Anchor
    
    [Header("Visual")]
    public Color teamColor;
    private Renderer[] handRenderers;
    
    // Bot sistemi entegrasyonu
    private Transform myTransform;
    private static VRPlayerProxy instance;
    
    // Fake bot controller değerleri
    [HideInInspector] public bool canCatchBall = true;
    [HideInInspector] public Transform lastThrower;
    
    void Awake()
    {
        myTransform = transform;
        instance = this;
        
        // Target anchor yoksa oluştur
        if (targetAnchor == null)
        {
            GameObject anchor = new GameObject("VR_TargetAnchor");
            anchor.transform.SetParent(transform);
            anchor.transform.localPosition = new Vector3(0, 1.2f, 0); // Göğüs hizası
            targetAnchor = anchor.transform;
        }
    }
    
    void Start()
    {
        // Takım rengini ayarla
        if (playerTeam == Team.Blue)
        {
            teamColor = new Color(0.2f, 0.5f, 1f);
        }
        else
        {
            teamColor = new Color(1f, 0.3f, 0.3f);
        }
        
        // El renderer'larını bul ve renklendir
        if (leftHandTransform != null)
        {
            Renderer leftRenderer = leftHandTransform.GetComponentInChildren<Renderer>();
            if (leftRenderer != null)
                leftRenderer.material.color = teamColor;
        }
        
        if (rightHandTransform != null)
        {
            Renderer rightRenderer = rightHandTransform.GetComponentInChildren<Renderer>();
            if (rightRenderer != null)
                rightRenderer.material.color = teamColor;
        }
        
        // Tag'i ayarla
        gameObject.tag = "Player";
    }
    
    void Update()
    {
        // Target anchor pozisyonunu güncelle (baş pozisyonuna göre)
        if (headTransform != null && targetAnchor != null)
        {
            Vector3 anchorPos = headTransform.position;
            anchorPos.y -= 0.3f; // Baştan 30cm aşağı (göğüs hizası)
            targetAnchor.position = anchorPos;
        }
    }
    
    // Bot sistemi için sahte metodlar
    public void EnableCatching()
    {
        canCatchBall = true;
        Debug.Log("VR Player can now catch the ball!");
    }
    
    // Botların VR player'ı hedeflemesi için
    public Transform GetTargetTransform()
    {
        return targetAnchor != null ? targetAnchor : myTransform;
    }
    
    // VR Player'ın topu vurduğunda çağrılacak
    public void OnBallHit(Vector3 ballVelocity)
    {
        // Tüm botlara yakalama izni ver
        BotController[] allBots = FindObjectsOfType<BotController>();
        foreach (BotController bot in allBots)
        {
            bot.canCatchBall = true;
        }
        
        Debug.Log($"VR Player ({playerTeam}) hit the ball with velocity: {ballVelocity.magnitude}");
    }
    
    // Singleton erişim
    public static VRPlayerProxy GetInstance()
    {
        return instance;
    }
    
    void OnDrawGizmos()
    {
        // Target anchor pozisyonunu göster
        if (targetAnchor != null)
        {
            Gizmos.color = teamColor;
            Gizmos.DrawWireSphere(targetAnchor.position, 0.3f);
            Gizmos.DrawLine(targetAnchor.position, targetAnchor.position + Vector3.up * 0.5f);
        }
    }
}