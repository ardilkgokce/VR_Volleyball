using UnityEngine;
using System.Collections;

public class RefereeController : MonoBehaviour
{
    [Header("Animation")]
    public Animator animator;
    public string headMoveParameterName = "HeadMove";
    public string headLayerName = "HeadLayer";
    private int headLayerIndex = -1;
    
    [Header("Ball Tracking")]
    private VolleyballBall currentBall;
    private Transform ballTransform;
    public float searchInterval = 0.5f; // Top arama sıklığı
    public float reSearchDelay = 0.2f; // Top bulunamazsa tekrar arama gecikmesi
    
    [Header("Head Movement Settings")]
    public float maxHorizontalAngle = 60f; // Maksimum bakış açısı
    public float trackingSpeed = 3f; // Kafa dönüş hızı
    public float smoothTime = 0.15f; // Smoothing için
    public bool enableTracking = true; // Takibi aç/kapa
    
    [Header("Look Behavior")]
    public float minBallDistance = 2f; // Minimum takip mesafesi
    public float maxBallDistance = 30f; // Maksimum takip mesafesi
    public AnimationCurve distanceInfluence = AnimationCurve.EaseInOut(0, 1, 1, 0); // Mesafeye göre takip yoğunluğu
    
    [Header("Advanced Settings")]
    public bool useLayerWeight = true; // Layer weight dinamik kontrol
    public float layerWeightSpeed = 2f; // Layer weight geçiş hızı
    public bool lookAtBallHeight = false; // Topun yüksekliğini de takip et
    public float maxVerticalAngle = 30f; // Maksimum dikey bakış açısı
    
    [Header("Debug")]
    public bool showDebugInfo = false;
    public bool showGizmos = true;
    
    // Private değişkenler
    private float currentHeadValue = 0f;
    private float headVelocity = 0f;
    private float targetHeadValue = 0f;
    private float currentLayerWeight = 0f;
    private Coroutine ballSearchCoroutine;
    private Vector3 lastKnownBallPosition;
    private float lastBallSeenTime;
    
    void Start()
    {
        InitializeReferee();
        StartBallSearch();
    }

    void InitializeReferee()
    {
        // Animator referansını al
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("Referee: Animator component bulunamadı!");
            enabled = false;
            return;
        }
        
        // HeadLayer index'ini bul
        if (!string.IsNullOrEmpty(headLayerName))
        {
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) == headLayerName)
                {
                    headLayerIndex = i;
                    Debug.Log($"Referee: HeadLayer bulundu. Index: {headLayerIndex}");
                    break;
                }
            }
            
            if (headLayerIndex == -1)
            {
                Debug.LogWarning($"Referee: '{headLayerName}' adında layer bulunamadı. Sadece parameter kontrolü kullanılacak.");
            }
        }
        
        // Animation curve'ü kontrol et
        if (distanceInfluence == null || distanceInfluence.keys.Length == 0)
        {
            distanceInfluence = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
    }
    
    void StartBallSearch()
    {
        if (ballSearchCoroutine != null)
            StopCoroutine(ballSearchCoroutine);
        
        ballSearchCoroutine = StartCoroutine(BallSearchRoutine());
    }
    
    IEnumerator BallSearchRoutine()
    {
        while (true)
        {
            if (currentBall == null || ballTransform == null)
            {
                SearchForBall();
                
                // Top bulunamazsa kısa bir süre bekle ve tekrar dene
                if (currentBall == null)
                {
                    yield return new WaitForSeconds(reSearchDelay);
                    
                    // İkinci deneme
                    SearchForBall();
                    
                    if (currentBall == null)
                    {
                        // Hala bulunamadıysa normal interval'e geç
                        yield return new WaitForSeconds(searchInterval);
                    }
                }
            }
            else
            {
                // Top var, normal interval ile kontrol et
                yield return new WaitForSeconds(searchInterval);
            }
        }
    }
    
    void SearchForBall()
    {
        // VolleyballBall component'ini ara
        currentBall = FindObjectOfType<VolleyballBall>();
        
        if (currentBall != null)
        {
            ballTransform = currentBall.transform;
            lastKnownBallPosition = ballTransform.position;
            lastBallSeenTime = Time.time;
            
            if (showDebugInfo)
                Debug.Log($"Referee: Top bulundu! {currentBall.name}");
        }
        else
        {
            ballTransform = null;
            
            if (showDebugInfo && Time.frameCount % 60 == 0) // Spam önlemek için
                Debug.Log("Referee: Top aranıyor...");
        }
    }
    
    void Update()
    {
        if (!enableTracking || animator == null)
            return;
        
        UpdateHeadTracking();
        UpdateLayerWeight();
        
        if (showDebugInfo)
            ShowDebugInfo();
    }
    
    void UpdateHeadTracking()
    {
        // Hedef değeri hesapla
        if (ballTransform != null && ballTransform.gameObject.activeInHierarchy)
        {
            // Top pozisyonunu güncelle
            lastKnownBallPosition = ballTransform.position;
            lastBallSeenTime = Time.time;
            
            // Yönü hesapla
            Vector3 directionToBall = lastKnownBallPosition - transform.position;
            float distance = directionToBall.magnitude;
            
            // Mesafe kontrolü
            if (distance >= minBallDistance && distance <= maxBallDistance)
            {
                // Yatay açıyı hesapla
                directionToBall.y = lookAtBallHeight ? directionToBall.y : 0;
                float horizontalAngle = Vector3.SignedAngle(transform.forward, directionToBall, Vector3.up);
                
                // Mesafe etkisini uygula
                float distanceNormalized = Mathf.InverseLerp(minBallDistance, maxBallDistance, distance);
                float distanceMultiplier = distanceInfluence.Evaluate(distanceNormalized);
                
                // Normalize et ve mesafe etkisini uygula
                targetHeadValue = Mathf.Clamp(horizontalAngle / maxHorizontalAngle, -1f, 1f);
                targetHeadValue *= distanceMultiplier;
                
                // Dikey bakış (opsiyonel)
                if (lookAtBallHeight && headLayerIndex >= 0)
                {
                    float verticalAngle = Mathf.Atan2(directionToBall.y, 
                        new Vector2(directionToBall.x, directionToBall.z).magnitude) * Mathf.Rad2Deg;
                    verticalAngle = Mathf.Clamp(verticalAngle, -maxVerticalAngle, maxVerticalAngle);
                    
                    // Dikey açıyı ayrı bir parameter olarak gönderebilirsiniz
                    // animator.SetFloat("HeadVertical", verticalAngle / maxVerticalAngle);
                }
            }
            else
            {
                // Top çok yakın veya çok uzak, merkeze dön
                targetHeadValue = Mathf.Lerp(targetHeadValue, 0f, Time.deltaTime * trackingSpeed);
            }
        }
        else
        {
            // Top yoksa
            if (Time.time - lastBallSeenTime > 2f) // 2 saniye top görülmediyse
            {
                // Yavaşça merkeze dön
                targetHeadValue = Mathf.Lerp(targetHeadValue, 0f, Time.deltaTime * trackingSpeed * 0.5f);
            }
            // Else: Son bilinen pozisyona bakmaya devam et
        }
        
        // Smooth geçiş
        currentHeadValue = Mathf.SmoothDamp(currentHeadValue, targetHeadValue, ref headVelocity, smoothTime);
        
        // Animator'a değeri gönder
        animator.SetFloat(headMoveParameterName, currentHeadValue);
    }
    
    void UpdateLayerWeight()
    {
        if (!useLayerWeight || headLayerIndex < 0)
            return;
        
        // Layer weight'i hesapla
        float targetWeight = 0f;
        
        if (ballTransform != null && enableTracking)
        {
            // Top varsa ve takip açıksa
            float ballDistance = Vector3.Distance(transform.position, ballTransform.position);
            
            if (ballDistance >= minBallDistance && ballDistance <= maxBallDistance)
            {
                // Mesafe uygunsa tam weight
                targetWeight = 1f;
                
                // Açıya göre azalt (top arkadaysa takip etme)
                Vector3 dirToBall = (ballTransform.position - transform.position).normalized;
                float dotProduct = Vector3.Dot(transform.forward, dirToBall);
                if (dotProduct < -0.5f) // Top arkada
                {
                    targetWeight = 0f;
                }
            }
        }
        
        // Smooth weight geçişi
        currentLayerWeight = Mathf.Lerp(currentLayerWeight, targetWeight, layerWeightSpeed * Time.deltaTime);
        animator.SetLayerWeight(headLayerIndex, currentLayerWeight);
    }
    
    void ShowDebugInfo()
    {
        if (Time.frameCount % 30 == 0) // Her 30 frame'de bir göster
        {
            string status = currentBall != null ? "Takip Ediliyor" : "Top Aranıyor";
            float distance = ballTransform != null ? 
                Vector3.Distance(transform.position, ballTransform.position) : 0f;
            
            Debug.Log($"[Referee] Status: {status} | " +
                     $"HeadMove: {currentHeadValue:F2} | " +
                     $"Distance: {distance:F1}m | " +
                     $"LayerWeight: {currentLayerWeight:F2}");
        }
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmos || !Application.isPlaying)
            return;
        
        // Görüş alanını göster
        Gizmos.color = Color.green;
        Vector3 origin = transform.position + Vector3.up * 1.5f; // Göz hizası
        
        // Sol ve sağ görüş sınırları
        Vector3 leftBoundary = Quaternion.Euler(0, -maxHorizontalAngle, 0) * transform.forward;
        Vector3 rightBoundary = Quaternion.Euler(0, maxHorizontalAngle, 0) * transform.forward;
        
        Gizmos.DrawRay(origin, leftBoundary * maxBallDistance);
        Gizmos.DrawRay(origin, rightBoundary * maxBallDistance);
        
        // Görüş konisi
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        int segments = 20;
        Vector3 prevPoint = origin + leftBoundary * maxBallDistance;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = Mathf.Lerp(-maxHorizontalAngle, maxHorizontalAngle, i / (float)segments);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;
            Vector3 point = origin + dir * maxBallDistance;
            Gizmos.DrawLine(prevPoint, point);
            Gizmos.DrawLine(origin, point);
            prevPoint = point;
        }
        
        // Top çizgisi
        if (ballTransform != null)
        {
            float distance = Vector3.Distance(transform.position, ballTransform.position);
            
            if (distance >= minBallDistance && distance <= maxBallDistance)
            {
                Gizmos.color = Color.yellow;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            
            Gizmos.DrawLine(origin, ballTransform.position);
            Gizmos.DrawWireSphere(ballTransform.position, 0.5f);
        }
        
        // Min/Max mesafe çemberleri
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(transform.position, minBallDistance, 20);
        DrawCircle(transform.position, maxBallDistance, 40);
    }
    
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + Vector3.forward * radius;
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 point = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
            Gizmos.DrawLine(prevPoint, point);
            prevPoint = point;
        }
    }
    
    // Public metodlar
    public void SetTrackingEnabled(bool enabled)
    {
        enableTracking = enabled;
        if (!enabled)
        {
            targetHeadValue = 0f;
        }
    }
    
    public void ForceSearchBall()
    {
        SearchForBall();
    }
    
    public bool HasBall()
    {
        return currentBall != null && ballTransform != null;
    }
    
    public float GetBallDistance()
    {
        if (ballTransform == null) return -1f;
        return Vector3.Distance(transform.position, ballTransform.position);
    }
}