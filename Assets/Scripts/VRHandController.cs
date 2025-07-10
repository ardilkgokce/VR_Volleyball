using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class VRHandController : MonoBehaviour
{
    [Header("Hand Settings")]
    public bool isLeftHand = true;
    public float hitForceMultiplier = 2f;
    public float minHitVelocity = 1f; // Minimum el hızı
    public float upwardForceBonus = 3f;
    
    [Header("Hit Detection")]
    public float hitRadius = 0.2f;
    public LayerMask ballLayer = -1;
    
    [Header("Haptic Feedback")]
    public float hapticIntensity = 0.7f;
    public float hapticDuration = 0.15f;
    
    [Header("Visual Feedback")]
    public Color normalColor = Color.white;
    public Color hitColor = Color.yellow;
    private Renderer handRenderer;
    private float colorResetTimer = 0f;
    
    [Header("Parabolic Hit Settings")]
    [Tooltip("Yavaş vuruş için yukarı açı (derece)")]
    public float slowHitAngle = 55f;
    [Tooltip("Orta hızlı vuruş için yukarı açı (derece)")]
    public float mediumHitAngle = 40f;
    [Tooltip("Hızlı vuruş için yukarı açı (derece)")]
    public float fastHitAngle = 25f;
    [Tooltip("Çok hızlı vuruş için yukarı açı (derece)")]
    public float veryFastHitAngle = 15f;
    
    [Tooltip("Hız eşikleri")]
    public float slowSpeedThreshold = 3f;
    public float mediumSpeedThreshold = 6f;
    public float fastSpeedThreshold = 10f;
    
    // VR Input
    private InputDevice targetDevice;
    private Vector3 previousPosition;
    private Vector3 currentVelocity;
    
    // Team integration
    public Team playerTeam = Team.Blue;
    private Transform myTransform;
    
    // Hit cooldown
    private float hitCooldown = 0f;
    private const float hitCooldownDuration = 0.2f;
    
    void Start()
    {
        myTransform = transform;
        
        // Get the correct controller
        if (isLeftHand)
            targetDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        else
            targetDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            
        // Get hand renderer if exists
        handRenderer = GetComponentInChildren<Renderer>();
        if (handRenderer != null)
            handRenderer.material.color = normalColor;
        
        // Set layer for physics
        gameObject.layer = LayerMask.NameToLayer("Default");
        
        // Make sure we have required components
        if (GetComponent<Rigidbody>() == null)
        {
            Rigidbody rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        
        SphereCollider col = GetComponent<SphereCollider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
        col.radius = hitRadius;
        col.isTrigger = false; // Solid collider for hitting
        
        previousPosition = myTransform.position;
    }
    
    void Update()
    {
        // Calculate hand velocity
        currentVelocity = (myTransform.position - previousPosition) / Time.deltaTime;
        previousPosition = myTransform.position;
        
        // Update cooldown
        if (hitCooldown > 0)
            hitCooldown -= Time.deltaTime;
            
        // Update color reset
        if (colorResetTimer > 0)
        {
            colorResetTimer -= Time.deltaTime;
            if (colorResetTimer <= 0 && handRenderer != null)
            {
                handRenderer.material.color = normalColor;
            }
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit the ball
        if (collision.gameObject.CompareTag("Ball") && hitCooldown <= 0)
        {
            HitBall(collision);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Alternative detection for trigger colliders
        if (other.CompareTag("Ball") && hitCooldown <= 0)
        {
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            if (ballRb != null)
            {
                HitBallDirect(other.gameObject, ballRb);
            }
        }
    }
    
    void HitBall(Collision collision)
    {
        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null) return;
        
        // Check if hand is moving fast enough
        float handSpeed = currentVelocity.magnitude;
        if (handSpeed < minHitVelocity) return;
        
        // Voleybol topu component'ini kontrol et
        VolleyballBall volleyballBall = collision.gameObject.GetComponent<VolleyballBall>();
        if (volleyballBall != null)
        {
            // VR oyuncunun takımını al
            VRPlayerProxy vrProxy = GetComponentInParent<VRPlayerProxy>();
            Team vrTeam = vrProxy != null ? vrProxy.playerTeam : playerTeam;
            
            // Vuruş kaydı
            if (!volleyballBall.OnHit(transform.root, vrTeam))
            {
                Debug.LogWarning("VR Player cannot hit the ball! (same player or too many hits)");
                return;
            }
        }
        
        // Calculate hit direction
        ContactPoint contact = collision.contacts[0];
        Vector3 hitDirection = (contact.point - myTransform.position).normalized;
        
        // Add hand velocity influence
        hitDirection = (hitDirection + currentVelocity.normalized).normalized;
        
        // Calculate hit force
        float hitForce = handSpeed * hitForceMultiplier;
        hitForce = Mathf.Clamp(hitForce, 5f, 30f);
        
        // Apply force to ball with parabolic trajectory
        ApplyParabolicHitForce(ballRb, hitDirection, hitForce, handSpeed);
        
        // Effects
        OnSuccessfulHit(contact.point);
    }
    
    void HitBallDirect(GameObject ball, Rigidbody ballRb)
    {
        // Check if hand is moving fast enough
        float handSpeed = currentVelocity.magnitude;
        if (handSpeed < minHitVelocity) return;
        
        // Voleybol topu component'ini kontrol et
        VolleyballBall volleyballBall = ball.GetComponent<VolleyballBall>();
        if (volleyballBall != null)
        {
            // VR oyuncunun takımını al
            VRPlayerProxy vrProxy = GetComponentInParent<VRPlayerProxy>();
            Team vrTeam = vrProxy != null ? vrProxy.playerTeam : playerTeam;
            
            // Vuruş kaydı
            if (!volleyballBall.OnHit(transform.root, vrTeam))
            {
                Debug.LogWarning("VR Player cannot hit the ball! (same player or too many hits)");
                return;
            }
        }
        
        // Calculate hit direction based on hand velocity
        Vector3 hitDirection = currentVelocity.normalized;
        
        // If hand is mostly moving sideways, add upward force
        if (Mathf.Abs(hitDirection.y) < 0.3f)
        {
            hitDirection.y = 0.3f;
            hitDirection.Normalize();
        }
        
        // Calculate hit force
        float hitForce = handSpeed * hitForceMultiplier;
        hitForce = Mathf.Clamp(hitForce, 5f, 30f);
        
        // Apply force to ball with parabolic trajectory
        ApplyParabolicHitForce(ballRb, hitDirection, hitForce, handSpeed);
        
        // Effects
        OnSuccessfulHit(ball.transform.position);
    }
    
    void ApplyParabolicHitForce(Rigidbody ballRb, Vector3 baseDirection, float force, float handSpeed)
    {
        // Reset ball velocity for clean hit
        ballRb.velocity = Vector3.zero;
        
        // Hıza göre açıyı belirle
        float angle;
        string hitType;
        
        if (handSpeed < slowSpeedThreshold)
        {
            angle = slowHitAngle;
            hitType = "Soft Set";
        }
        else if (handSpeed < mediumSpeedThreshold)
        {
            angle = mediumHitAngle;
            hitType = "Normal Hit";
        }
        else if (handSpeed < fastSpeedThreshold)
        {
            angle = fastHitAngle;
            hitType = "Power Hit";
        }
        else
        {
            angle = veryFastHitAngle;
            hitType = "Spike";
        }
        
        // Açıyı radyana çevir
        float angleRad = angle * Mathf.Deg2Rad;
        
        // Yatay yön (Y komponenti sıfırlanmış)
        Vector3 horizontalDirection = new Vector3(baseDirection.x, 0, baseDirection.z).normalized;
        
        // El aşağı hareket ediyorsa açıyı azalt (smaç efekti)
        if (currentVelocity.y < -2f)
        {
            angleRad *= 0.5f; // Açıyı yarıya düşür
            hitType = "Downward Spike";
            force *= 1.2f; // Biraz daha güç ekle
        }
        
        // Parabolik velocity hesapla
        Vector3 finalVelocity;
        
        // Yatay ve dikey hız bileşenleri
        float horizontalSpeed = force * Mathf.Cos(angleRad);
        float verticalSpeed = force * Mathf.Sin(angleRad);
        
        // Final velocity
        finalVelocity = horizontalDirection * horizontalSpeed;
        finalVelocity.y = verticalSpeed;
        
        // Velocity'yi uygula
        ballRb.velocity = finalVelocity;
        
        // Add some spin based on hit direction
        Vector3 spin = Vector3.Cross(Vector3.up, horizontalDirection) * 3f;
        ballRb.angularVelocity = spin;
        
        // ÖNCELİK SIRASI ÖNEMLİ:
        // 1. Önce bot sistemini bilgilendir
        NotifyBotSystem();
        
        // 2. Sonra tahmin sistemini çalıştır (velocity set edildikten sonra)
        StartCoroutine(NotifyPredictionDelayed(ballRb));
        
        Debug.Log($"VR Hand {hitType}! Speed: {handSpeed:F1}m/s, Angle: {angle}°, Force: {force:F1}");
    }
    
    // Velocity'nin oturması için küçük bir gecikme
    System.Collections.IEnumerator NotifyPredictionDelayed(Rigidbody ballRb)
    {
        yield return new WaitForFixedUpdate(); // Fizik güncellemesini bekle
        NotifyBallPrediction(ballRb);
    }
    
    void OnSuccessfulHit(Vector3 hitPoint)
    {
        // Set cooldown
        hitCooldown = hitCooldownDuration;
        
        // Visual feedback
        if (handRenderer != null)
        {
            handRenderer.material.color = hitColor;
            colorResetTimer = 0.2f;
        }
        
        // Haptic feedback
        SendHapticFeedback();
        
        // VR Player Proxy'ye bildir
        VRPlayerProxy vrProxy = GetComponentInParent<VRPlayerProxy>();
        if (vrProxy != null)
        {
            vrProxy.OnBallHit(currentVelocity);
        }
        
        // Particle effect at hit point (optional)
        // Instantiate(hitEffectPrefab, hitPoint, Quaternion.identity);
    }
    
    void NotifyBotSystem()
    {
        // Find all bots and enable their catching
        BotController[] allBots = FindObjectsOfType<BotController>();
        
        foreach (BotController bot in allBots)
        {
            bot.canCatchBall = true;
            bot.lastThrower = null; // VR oyuncu vurduğunda lastThrower'ı temizle
        }
        
        Debug.Log($"VR Player hit the ball - all bots can now catch!");
    }
    
    void NotifyBallPrediction(Rigidbody ballRb)
    {
        // Botlara topun düşeceği yeri haber ver
        if (ballRb != null)
        {
            BotController.OnVRPlayerHitBall(ballRb.position, ballRb.velocity);
        }
    }
    
    void SendHapticFeedback()
    {
        if (targetDevice.isValid)
        {
            HapticCapabilities capabilities;
            if (targetDevice.TryGetHapticCapabilities(out capabilities))
            {
                if (capabilities.supportsImpulse)
                {
                    targetDevice.SendHapticImpulse(0, hapticIntensity, hapticDuration);
                }
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Show hit radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, hitRadius);
        
        // Show velocity direction
        if (Application.isPlaying && currentVelocity.magnitude > minHitVelocity)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, currentVelocity.normalized * 1f);
            
            // Show velocity magnitude
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + currentVelocity.normalized * 0.5f, currentVelocity.magnitude * 0.1f);
            
            // Hıza göre açıyı göster
            float handSpeed = currentVelocity.magnitude;
            float angle = GetAngleForSpeed(handSpeed);
            Vector3 forward = currentVelocity.normalized;
            forward.y = 0;
            forward.Normalize();
            
            // Parabolik yörüngeyi göster
            Gizmos.color = Color.cyan;
            Vector3 parabolicDir = Quaternion.AngleAxis(-angle, Vector3.Cross(forward, Vector3.up)) * forward;
            Gizmos.DrawRay(transform.position, parabolicDir * 2f);
        }
    }
    
    // Hıza göre açı hesaplama (debug için)
    float GetAngleForSpeed(float speed)
    {
        if (speed < slowSpeedThreshold) return slowHitAngle;
        else if (speed < mediumSpeedThreshold) return mediumHitAngle;
        else if (speed < fastSpeedThreshold) return fastHitAngle;
        else return veryFastHitAngle;
    }
    
    // Public method for bot targeting
    public bool IsValidTarget()
    {
        // VR player is always a valid target
        return true;
    }
}