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
        
        // Calculate hit direction
        ContactPoint contact = collision.contacts[0];
        Vector3 hitDirection = (contact.point - myTransform.position).normalized;
        
        // Add hand velocity influence
        hitDirection = (hitDirection + currentVelocity.normalized).normalized;
        
        // Calculate hit force
        float hitForce = handSpeed * hitForceMultiplier;
        hitForce = Mathf.Clamp(hitForce, 5f, 30f);
        
        // Apply force to ball
        ApplyHitForce(ballRb, hitDirection, hitForce);
        
        // Effects
        OnSuccessfulHit(contact.point);
    }
    
    void HitBallDirect(GameObject ball, Rigidbody ballRb)
    {
        // Check if hand is moving fast enough
        float handSpeed = currentVelocity.magnitude;
        if (handSpeed < minHitVelocity) return;
        
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
        
        // Apply force to ball
        ApplyHitForce(ballRb, hitDirection, hitForce);
        
        // Effects
        OnSuccessfulHit(ball.transform.position);
    }
    
    void ApplyHitForce(Rigidbody ballRb, Vector3 direction, float force)
    {
        // Reset ball velocity for clean hit
        ballRb.velocity = Vector3.zero;
        
        // Apply the hit force
        Vector3 finalVelocity = direction * force;
        
        // Add upward bonus if hitting forward
        if (direction.y < 0.5f)
        {
            finalVelocity.y += upwardForceBonus;
        }
        
        ballRb.velocity = finalVelocity;
        
        // Add some spin
        Vector3 spin = Vector3.Cross(Vector3.up, direction) * 5f;
        ballRb.angularVelocity = spin;
        
        // Notify bot system
        NotifyBotSystem();
        
        Debug.Log($"VR Hand hit ball! Speed: {force:F1}, Direction: {direction}");
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
        }
    }
    
    // Public method for bot targeting
    public bool IsValidTarget()
    {
        // VR player is always a valid target
        return true;
    }
}