using UnityEngine;
using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(SphereCollider))]
public class VolleyballBall : MonoBehaviour
{
    [Header("Ball Properties")]
    [SerializeField] private float mass = 0.27f; // Gerçek voleybol topu ağırlığı (kg)
    [SerializeField] private float drag = 0.5f;
    [SerializeField] private float angularDrag = 0.3f;
    
    [Header("Hit Detection")]
    [SerializeField] private float minHitVelocity = 2f; // Minimum vuruş hızı
    [SerializeField] private LayerMask handLayer; // El layer'ı
    [SerializeField] private float velocityMultiplier = 1.5f; // El hızı çarpanı
    
    private Rigidbody rb;
    private SphereCollider sphereCollider;
    private Vector3 lastVelocity;
    private float lastHitTime;
    
    // El takibi için
    private Dictionary<Collider, Vector3> handVelocities = new Dictionary<Collider, Vector3>();
    private Dictionary<Collider, Vector3> lastHandPositions = new Dictionary<Collider, Vector3>();
    private GameObject[] handObjects;
    private bool handsAreTouching = false; // İki el temas halinde mi?
    private Transform leftHand;
    private Transform rightHand;
    
    // Events
    public System.Action<Vector3, float> OnBallHit; // Pozisyon, güç
    public System.Action<Vector3> OnBallLanded; // Düştüğü pozisyon
    
    void Start()
    {
        SetupBall();
        // Elleri bir kere bul ve kaydet
        CacheHandObjects();
    }
    
    void CacheHandObjects()
    {
        handObjects = GameObject.FindGameObjectsWithTag("Hand");
        Debug.Log($"Found {handObjects.Length} hand objects");
        
        // Sol ve sağ eli bul
        foreach (var hand in handObjects)
        {
            if (hand.name.ToLower().Contains("left"))
                leftHand = hand.transform;
            else if (hand.name.ToLower().Contains("right"))
                rightHand = hand.transform;
        }
    }
    
    void SetupBall()
    {
        // Rigidbody ayarları
        rb = GetComponent<Rigidbody>();
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Collider ayarları
        sphereCollider = GetComponent<SphereCollider>();
        transform.localScale = Vector3.one * 0.21f; // Gerçek voleybol topu boyutu
    }
    
    void Update()
    {
        // Cached el objelerini kullan
        if (handObjects != null)
        {
            foreach (var hand in handObjects)
            {
                if (hand != null) // Null check
                {
                    var collider = hand.GetComponent<Collider>();
                    if (collider != null)
                    {
                        TrackHandMovement(collider);
                    }
                }
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // El ile temas kontrolü
        if (IsHandHit(collision))
        {
            ProcessHit(collision);
        }
        // Zemin teması kontrolü
        else if (collision.gameObject.CompareTag("Ground"))
        {
            OnBallLanded?.Invoke(transform.position);
        }
    }
    
    void OnCollisionStay(Collision collision)
    {
        // El pozisyonlarını takip et
        if (IsHandHit(collision))
        {
            TrackHandMovement(collision.collider);
        }
    }
    
    void TrackHandMovement(Collider handCollider)
    {
        if (!lastHandPositions.ContainsKey(handCollider))
        {
            lastHandPositions[handCollider] = handCollider.transform.position;
            return;
        }
        
        // El hızını hesapla
        Vector3 currentPos = handCollider.transform.position;
        Vector3 lastPos = lastHandPositions[handCollider];
        Vector3 velocity = (currentPos - lastPos) / Time.fixedDeltaTime;
        
        handVelocities[handCollider] = velocity;
        lastHandPositions[handCollider] = currentPos;
    }
    
    bool IsHandHit(Collision collision)
    {
        // Layer kontrolü veya tag kontrolü
        return collision.gameObject.layer == LayerMask.NameToLayer("Hand") ||
               collision.gameObject.CompareTag("Hand");
    }
    
    void ProcessHit(Collision collision)
    {
        // Çift vuruş engellemesi
        if (Time.time - lastHitTime < 0.1f) return;
        
        lastHitTime = Time.time;
        
        Vector3 handVelocity;
        Vector3 hitPoint;
        string hitSource;
        
        // İki el temas halindeyse merkez noktadan hesapla
        if (handsAreTouching && leftHand != null && rightHand != null)
        {
            Debug.Log("Dual hand block detected - hands are touching!");
            
            // Merkez nokta
            hitPoint = (leftHand.position + rightHand.position) / 2f;
            
            // İki elin ortalama hızı
            Vector3 leftVel = GetHandVelocity(leftHand.GetComponent<Collider>());
            Vector3 rightVel = GetHandVelocity(rightHand.GetComponent<Collider>());
            handVelocity = (leftVel + rightVel) / 2f;
            
            // Block bonusu
            handVelocity *= 1.4f;
            hitSource = "Dual Hand Block";
        }
        else
        {
            // Tek el vuruşu
            handVelocity = GetHandVelocity(collision.collider);
            hitPoint = collision.contacts[0].point;
            hitSource = collision.gameObject.name;
        }
        
        float handSpeed = handVelocity.magnitude;
        
        // Debug bilgileri
        Debug.Log($"=== HIT DEBUG ===");
        Debug.Log($"Hit Source: {hitSource}");
        Debug.Log($"Hand Velocity: {handVelocity}");
        Debug.Log($"Hand Speed: {handSpeed:F2}");
        
        // Minimum hız kontrolü
        if (handSpeed < 0.1f) 
        {
            Debug.Log("Using fallback velocity!");
            handVelocity = Vector3.up * 3f;
            handSpeed = handVelocity.magnitude;
        }
        
        // Event tetikle
        OnBallHit?.Invoke(hitPoint, handSpeed);
        
        // Vuruş tipini belirle ve kuvvet uygula
        HitType hitType = handsAreTouching ? HitType.Set : DetermineHitTypeByVelocity(handVelocity, collision.contacts[0]);
        Debug.Log($"Hit Type: {hitType}");
        
        ApplyHandVelocityHit(handVelocity, hitType);
    }
    
    Vector3 GetHandVelocity(Collider handCollider)
    {
        // Transform'un önceki pozisyonunu takip et
        if (!lastHandPositions.ContainsKey(handCollider))
        {
            lastHandPositions[handCollider] = handCollider.transform.position;
            handVelocities[handCollider] = Vector3.zero;
        }
        
        // Pozisyon farkından hız hesapla
        Vector3 currentPos = handCollider.transform.position;
        Vector3 lastPos = lastHandPositions[handCollider];
        Vector3 velocity = (currentPos - lastPos) / Time.deltaTime;
        
        // Güncelle
        lastHandPositions[handCollider] = currentPos;
        handVelocities[handCollider] = velocity;
        
        // Çok küçük hareketleri filtrele
        if (velocity.magnitude < 0.1f)
        {
            return Vector3.up * 2f; // Minimum yukarı hız
        }
        
        return velocity * velocityMultiplier;
    }
    
    void ApplyHandVelocityHit(Vector3 handVelocity, HitType hitType)
    {
        // Basit test için - direkt yukarı fırlat
        Vector3 force = Vector3.up * 8f + handVelocity * 2f;
        
        // Debug
        Debug.Log($"Applied Force: {force}");
        
        // Mevcut hızı sıfırla ve yeni hız uygula
        rb.velocity = force;
        
        // Hafif spin ekle
        rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
    }
    
    HitType DetermineHitTypeByVelocity(Vector3 handVelocity, ContactPoint contact)
    {
        float speed = handVelocity.magnitude;
        float upwardComponent = Vector3.Dot(handVelocity.normalized, Vector3.up);
        float downwardComponent = Vector3.Dot(handVelocity.normalized, Vector3.down);
        
        // Spike: Hızlı ve aşağı doğru hareket
        if (speed > 5f && downwardComponent > 0.5f)
            return HitType.Spike;
        
        // Set: Orta hız ve yukarı doğru
        if (speed < 4f && upwardComponent > 0.5f)
            return HitType.Set;
        
        // Default: Bump
        return HitType.Bump;
    }
    
    // Bu metodu HandController veya ayrı bir script'te çağırabilirsiniz
    public void SetHandsTouchingState(bool touching)
    {
        handsAreTouching = touching;
        if (touching)
            Debug.Log("Hands are now touching - block mode active");
    }
    
    // Top tahmin sistemi için yardımcı metod
    public Vector3 PredictLandingPoint()
    {
        float timeToGround = CalculateTimeToGround();
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 landingPoint = transform.position + horizontalVelocity * timeToGround;
        
        // Drag hesaba kat (basitleştirilmiş)
        landingPoint *= (1f - drag * timeToGround * 0.5f);
        
        return landingPoint;
    }
    
    float CalculateTimeToGround()
    {
        float h = transform.position.y;
        float v = rb.velocity.y;
        float g = Physics.gravity.y;
        
        // Quadratic formula: h = vt + 0.5gt²
        float discriminant = v * v - 2 * g * h;
        if (discriminant < 0) return 0;
        
        float t1 = (-v + Mathf.Sqrt(discriminant)) / g;
        float t2 = (-v - Mathf.Sqrt(discriminant)) / g;
        
        return Mathf.Max(t1, t2);
    }
    
    public enum HitType
    {
        Bump,   // Manşet pas
        Set,    // Parmak pas  
        Spike   // Smaç
    }
}