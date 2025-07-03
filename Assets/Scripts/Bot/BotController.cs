using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum Team
{
    Red,
    Blue
   
    }

public class BotController : MonoBehaviour
{
    [Header("Team Settings")]
    public Team team = Team.Blue;
    
    [Header("Bot Settings")]
    public float detectionRadius = 5f;
    public float catchRadius = 1.5f;
    public float throwForce = 10f;
    public float throwHeight = 5f;
    
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float runSpeed = 6f;
    public float stoppingDistance = 0.5f;
    public bool enableMovement = true;
    public float maxMoveDistance = 20f; // Maksimum hareket mesafesi
    
    [Header("Default Position")]
    public Transform defaultPosition; // Inspector'dan atanacak
    public float returnToDefaultDelay = 2f; // InstantThrow'dan sonra kaç saniye bekleyeceği
    public bool autoReturnToDefault = true; // Otomatik dönüş aktif mi
    
    [Header("References")]
    private Transform targetBot;
    private GameObject ball;
    private Rigidbody ballRb;
    private bool hasBall = false;
    private float catchCooldown = 0f;
    [HideInInspector] 
    public bool canCatchBall = true;
    public Transform lastThrower;
    
    [Header("Visual")]
    public Color normalColor = Color.blue;
    public Color hasBallColor = Color.green;
    private Renderer botRenderer;
    public float rotationSpeed = 5f;
    
    [Header("Debug")]
    public bool showTrajectory = true;
    public int trajectoryPoints = 30;
    private Vector3 landingPoint;
    
    // Cache
    private static BotController[] allBots;
    private static List<Transform> validTargets = new List<Transform>();
    private Transform myTransform;
    private static readonly Vector3 catchOffset = Vector3.up * 1.5f;
    private static readonly Vector3 playerCatchOffset = Vector3.up * 1f;
    private WaitForSeconds colorResetDelay;
    
    // Movement
    private Vector3 targetMovePosition;
    private bool isMovingToBall = false;
    private bool isReturningToDefault = false;
    private static BotController activeCatcher = null; // Hangi bot topu yakalamaya gidiyor
    
    // Ball prediction
    private static Vector3 predictedLandingPoint;
    private static bool isPredictionValid = false;
    private static float lastPredictionTime;
    
    // Top tracking cache
    private static GameObject cachedBall;
    private static Rigidbody cachedBallRb;
    private static Transform cachedBallTransform;
    private Collider[] nearbyColliders = new Collider[10]; // Pre-allocated array
    private int colliderCount;
    
    void Awake()
    {
        myTransform = transform;
        botRenderer = GetComponent<Renderer>();
        colorResetDelay = new WaitForSeconds(0.1f);
        
        // Default pozisyon atanmamışsa mevcut pozisyonu kaydet
        if (defaultPosition == null)
        {
            GameObject defaultPosObj = new GameObject($"{gameObject.name}_DefaultPosition");
            defaultPosObj.transform.position = myTransform.position;
            defaultPosObj.transform.rotation = myTransform.rotation;
            defaultPosObj.transform.SetParent(transform.parent);
            defaultPosition = defaultPosObj.transform;
        }
    }
    
    void Start()
    {
        UpdateBotColor();
        
        // Takım rengini otomatik ayarla
        if (team == Team.Blue)
        {
            normalColor = new Color(0.2f, 0.5f, 1f);
            hasBallColor = new Color(0f, 0.8f, 1f);
        }
        else
        {
            normalColor = new Color(1f, 0.3f, 0.3f);
            hasBallColor = new Color(1f, 0f, 0f);
        }
        
        // Başlangıçta yakalama izni ver
        canCatchBall = true;
        
        Debug.Log($"{gameObject.name} başlatıldı. Team: {team}, CanCatch: {canCatchBall}");
        
        // Biraz gecikmeyle tüm botları bul (tüm botlar oluşturulduktan sonra)
        StartCoroutine(CacheAllBotsDelayed());
    }
    
    System.Collections.IEnumerator CacheAllBotsDelayed()
    {
        // Tüm botların oluşturulmasını bekle
        yield return new WaitForSeconds(0.1f);
        
        // Tüm botları cache'le
        allBots = FindObjectsOfType<BotController>();
        Debug.Log($"Cached {allBots.Length} bots in total");
        
        // Debug - tüm botları listele
        for (int i = 0; i < allBots.Length; i++)
        {
            if (allBots[i] != null)
                Debug.Log($"Bot {i}: {allBots[i].gameObject.name}");
        }
    }
    
    void Update()
    {
        // Yakalama bekleme süresi varsa azalt
        if (catchCooldown > 0)
        {
            catchCooldown -= Time.deltaTime;
        }
        
        // Top yoksa, yakalama izni varsa ve cooldown yoksa topu kontrol et
        if (!hasBall && canCatchBall && catchCooldown <= 0)
        {
            CheckForBall();
        }
        
        // Topu takip et
        TrackBall();
        
        // Hareket sistemi
        if (enableMovement)
        {
            if (isReturningToDefault)
            {
                ReturnToDefaultPosition();
            }
            else if (isMovingToBall && !hasBall && activeCatcher == this)
            {
                MoveToTarget();
            }
        }
    }
    
    void CheckForBall()
    {
        // Debug için kontrol
        if (Time.frameCount % 60 == 0 && showTrajectory)
        {
            Debug.Log($"{gameObject.name} - CanCatch: {canCatchBall}, Cooldown: {catchCooldown:F2}");
        }
        
        // Pre-allocated array kullan
        int layerMask = 1 << LayerMask.NameToLayer("Default");
        colliderCount = Physics.OverlapSphereNonAlloc(myTransform.position, detectionRadius, nearbyColliders, layerMask);
        
        for (int i = 0; i < colliderCount; i++)
        {
            if (nearbyColliders[i] != null && nearbyColliders[i].CompareTag("Ball"))
            {
                // Cached ball referansını kullan
                if (cachedBall == null || cachedBall != nearbyColliders[i].gameObject)
                {
                    cachedBall = nearbyColliders[i].gameObject;
                    cachedBallTransform = cachedBall.transform;
                    cachedBallRb = cachedBall.GetComponent<Rigidbody>();
                }
                
                if (cachedBallRb != null)
                {
                    float ballSpeed = cachedBallRb.velocity.magnitude;
                    Vector3 ballPos = cachedBallTransform.position;
                    float distance = Vector3.Distance(myTransform.position, ballPos);
                    
                    // Debug - top hızını göster
                    if (showTrajectory && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"{gameObject.name} - Top hızı: {ballSpeed:F2}, Mesafe: {distance:F2}");
                    }
                    
                    // Hız kontrolünü gevşet (2f'den 0.5f'e düşür)
                    if (ballSpeed > 0.5f)
                    {
                        Vector3 ballVelocity = cachedBallRb.velocity;
                        Vector3 toBall = ballPos - myTransform.position;
                        
                        float dotProduct = Vector3.Dot(ballVelocity.normalized, -toBall.normalized);
                        
                        if (dotProduct > 0.3f && distance < catchRadius) // dotProduct eşiğini de düşür
                        {
                            ball = cachedBall;
                            ballRb = cachedBallRb;
                            
                            // Voleybol topu component'ini al
                            VolleyballBall volleyballBall = ball.GetComponent<VolleyballBall>();
                            if (volleyballBall != null)
                            {
                                // Vuruş kaydı
                                if (volleyballBall.OnHit(myTransform, team))
                                {
                                    // Hareket ediyorsa durdur
                                    if (isMovingToBall)
                                    {
                                        StopMoving();
                                    }
                                    
                                    InstantThrow();
                                }
                                else
                                {
                                    // Vuruş geçersiz (aynı oyuncu veya fazla vuruş)
                                    Debug.LogWarning($"{gameObject.name} cannot hit the ball!");
                                }
                            }
                            else
                            {
                                // Eski sistem - component yoksa
                                if (isMovingToBall)
                                {
                                    StopMoving();
                                }
                                InstantThrow();
                            }
                            
                            break;
                        }
                    }
                }
            }
        }
    }
    
    void TrackBall()
    {
        // Eğer elimizde top varsa, hedef bota bak
        if (hasBall && targetBot != null)
        {
            LookAtTarget(targetBot.position);
            return;
        }
        
        // Cached ball varsa ve hala hareket ediyorsa, ona bak
        if (cachedBall != null && cachedBallRb != null && cachedBallRb.velocity.magnitude > 0.5f)
        {
            LookAtTarget(cachedBallTransform.position);
            return;
        }
        
        // Her 10 frame'de bir top ara (performans için)
        if (Time.frameCount % 10 == 0)
        {
            // OverlapSphereNonAlloc kullan - garbage oluşturmaz
            int layerMask = 1 << LayerMask.NameToLayer("Default");
            colliderCount = Physics.OverlapSphereNonAlloc(myTransform.position, detectionRadius * 2f, nearbyColliders, layerMask);
            
            for (int i = 0; i < colliderCount; i++)
            {
                if (nearbyColliders[i].CompareTag("Ball"))
                {
                    // Top bulundu, cache'le
                    cachedBall = nearbyColliders[i].gameObject;
                    cachedBallTransform = cachedBall.transform;
                    cachedBallRb = cachedBall.GetComponent<Rigidbody>();
                    break;
                }
            }
        }
    }
    
    void LookAtTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - myTransform.position;
        direction.y = 0; // Y eksenini sıfırla
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    Transform GetRandomTarget()
    {
        validTargets.Clear();
        
        // Voleybol topu component'ini kontrol et
        VolleyballBall volleyballBall = null;
        if (ball != null)
        {
            volleyballBall = ball.GetComponent<VolleyballBall>();
        }
        
        // 3. vuruşsa karşı takıma atmak ZORUNDA
        bool mustPassToOpponent = volleyballBall != null && volleyballBall.MustPassToOpponent();
        
        if (mustPassToOpponent)
        {
            Debug.Log($"{gameObject.name} MUST pass to opponent team! (3rd hit)");
        }
        
        // Cache'lenmiş bot listesini kullan
        for (int i = 0; i < allBots.Length; i++)
        {
            if (allBots[i] != null && allBots[i].transform != myTransform)
            {
                // 3. vuruşsa sadece karşı takım botları hedef olabilir
                if (mustPassToOpponent)
                {
                    if (allBots[i].team != team) // Karşı takım
                    {
                        validTargets.Add(allBots[i].transform);
                    }
                }
                else
                {
                    // Normal durumda herkes hedef olabilir
                    validTargets.Add(allBots[i].transform);
                }
            }
        }
        
        // VR oyuncuyu da hedef olarak ekle (eğer varsa)
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        if (vrPlayer != null)
        {
            VRPlayerProxy vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
            if (vrProxy != null)
            {
                // 3. vuruşsa ve VR oyuncu karşı takımdaysa ekle
                if (mustPassToOpponent)
                {
                    if (vrProxy.playerTeam != team)
                    {
                        validTargets.Add(vrPlayer.transform);
                        
                    }
                }
                else
                {
                    // Normal durumda ekle
                    validTargets.Add(vrPlayer.transform);
                }
            }
        }
        
        if (validTargets.Count > 0)
        {
            // Stratejik seçim: 3. vuruşsa karşı takımın en uzak oyuncusunu hedefle
            if (mustPassToOpponent && validTargets.Count > 1)
            {
                Transform farthestTarget = null;
                float maxDistance = 0f;
                
                foreach (Transform target in validTargets)
                {
                    float dist = Vector3.Distance(myTransform.position, target.position);
                    if (dist > maxDistance)
                    {
                        maxDistance = dist;
                        farthestTarget = target;
                    }
                }
                
                Debug.Log($"{gameObject.name} targeting farthest opponent: {farthestTarget.name}");
                return farthestTarget;
            }
            else
            {
                // Normal rastgele seçim
                int randomIndex = Random.Range(0, validTargets.Count);
                Transform selected = validTargets[randomIndex];
                
                Debug.Log($"{gameObject.name} hedef seçti: {selected.name}");
                
                return selected;
            }
        }
        
        Debug.LogError($"{gameObject.name} couldn't find valid target! MustPassToOpponent: {mustPassToOpponent}");
        return null;
    }
    
    void ThrowBallToTarget()
    {
        // Rastgele hedef seç
        targetBot = GetRandomTarget();
        
        if (targetBot == null || ball == null)
        {
            Debug.LogError("Target bot veya ball null!");
            return;
        }
        
        // VR Player mı yoksa Bot mu kontrol et
        bool isTargetVRPlayer = targetBot.CompareTag("Player");
        
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) topu VR Player ({vrProxy.playerTeam})'e fırlatıyor!");
            
            hasBall = false;
            catchCooldown = 0.5f;
            canCatchBall = false;
            
            // TÜM botların yakalama iznini kapat
            for (int i = 0; i < allBots.Length; i++)
            {
                allBots[i].canCatchBall = false;
                allBots[i].lastThrower = myTransform;
            }
            
            // VR Player'a yakalama izni ver
            vrProxy.EnableCatching();
        }
        else
        {
            // Normal bot hedefi - mevcut kod
            BotController targetController = targetBot.GetComponent<BotController>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) topu {targetBot.name} ({targetController.team})'e fırlatıyor!");
            
            hasBall = false;
            catchCooldown = 0.5f;
            canCatchBall = false;
            
            // TÜM botların yakalama iznini kapat
            for (int i = 0; i < allBots.Length; i++)
            {
                allBots[i].canCatchBall = false;
                allBots[i].lastThrower = myTransform;
            }
            
            // Sadece hedef botun yakalama iznini aç
            targetController.EnableCatching();
        }
        
        // Topu serbest bırak ve fırlat - ortak kod
        ball.transform.SetParent(null);
        ballRb.useGravity = true;
        
        // Hedef pozisyonu - VR için VRPlayerProxy'den al
        Vector3 targetPosition;
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            targetPosition = vrProxy.GetTargetTransform().position + playerCatchOffset;
        }
        else
        {
            targetPosition = targetBot.position + catchOffset;
        }
        
        Vector3 direction = targetPosition - ball.transform.position;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        
        // Parabolik atış hesaplaması
        float gravity = Mathf.Abs(Physics.gravity.y);
        float heightDifference = targetPosition.y - ball.transform.position.y;
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        float angle = 45f * Mathf.Deg2Rad;
        
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        v0 *= dragCompensation;
        
        // Hız vektörünü oluştur
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        // Topu fırlat
        ballRb.velocity = finalVelocity;
        
        // Düşüş noktasını hesapla
        CalculateLandingPoint(ball.transform.position, finalVelocity);
        
        UpdateBotColor();
        
        // Referansları temizle
        ball = null;
        ballRb = null;
        
        // Default pozisyona dönmeyi başlat
        if (autoReturnToDefault)
        {
            StartCoroutine(ReturnToDefaultAfterDelay());
        }
    }
    
    void UpdateBotColor()
    {
        if (botRenderer != null)
        {
            botRenderer.material.color = hasBall ? hasBallColor : normalColor;
        }
    }
    
    // Diğer bot tarafından çağrılacak - yakalama iznini aç
    public void EnableCatching()
    {
        canCatchBall = true;
        if (showTrajectory)
            Debug.Log($"{gameObject.name} artık topu yakalayabilir!");
    }
    
    // VR oyuncu topa vurduğunda çağrılacak (static method)
    public static void OnVRPlayerHitBall(Vector3 ballPosition, Vector3 ballVelocity)
    {
        // Önceki aktif yakalayıcıyı durdur
        if (activeCatcher != null)
        {
            activeCatcher.StopMoving();
        }
        activeCatcher = null;
        
        // Topun düşeceği noktayı hesapla
        predictedLandingPoint = PredictBallLandingPoint(ballPosition, ballVelocity);
        isPredictionValid = true;
        lastPredictionTime = Time.time;
        
        Debug.Log($"VR Player hit ball! Predicted landing: {predictedLandingPoint}");
        
        // En yakın botu bul ve harekete geçir
        FindAndActivateClosestBot();
    }
    
    // En yakın botu bul ve aktif et
    static void FindAndActivateClosestBot()
    {
        // Eğer bot listesi boşsa tekrar dene
        if (allBots == null || allBots.Length == 0)
        {
            allBots = FindObjectsOfType<BotController>();
            Debug.LogWarning($"Bot list was empty, refetched: {allBots.Length} bots found");
        }
        
        if (!isPredictionValid || allBots == null || allBots.Length == 0)
        {
            Debug.LogError("Cannot find closest bot - no valid bots or prediction!");
            return;
        }
        
        Debug.Log($"Finding closest bot to landing point: {predictedLandingPoint}");
        Debug.Log($"Total bots available: {allBots.Length}");
        
        BotController closestBot = null;
        float closestDistance = float.MaxValue;
        int validBotCount = 0;
        
        // Tüm botları kontrol et
        for (int i = 0; i < allBots.Length; i++)
        {
            BotController bot = allBots[i];
            if (bot != null)
            {
                float distance = Vector3.Distance(bot.transform.position, predictedLandingPoint);
                
                // Debug - her botu logla
                Debug.Log($"Bot[{i}] {bot.gameObject.name} - CanCatch: {bot.canCatchBall}, HasBall: {bot.hasBall}, Distance: {distance:F2}, Position: {bot.transform.position}");
                
                if (bot.canCatchBall && !bot.hasBall)
                {
                    validBotCount++;
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestBot = bot;
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Bot[{i}] is null!");
            }
        }
        
        Debug.Log($"Valid bots for catching: {validBotCount}");
        
        // En yakın botu aktif et
        if (closestBot != null)
        {
            // Mesafe kontrolünü kaldır veya artır
            if (closestDistance < 20f) // Detection radius kontrolünü 20f'e çıkardım
            {
                closestBot.StartMovingToBall(predictedLandingPoint);
                activeCatcher = closestBot;
                Debug.Log($"✓ ACTIVATED: {closestBot.gameObject.name} is moving! Distance: {closestDistance:F2}");
            }
            else
            {
                Debug.LogWarning($"Closest bot {closestBot.gameObject.name} is too far: {closestDistance:F2}");
            }
        }
        else
        {
            Debug.LogError($"No valid bot found! Total: {allBots.Length}, Valid: {validBotCount}");
        }
    }
    
    // Topun düşeceği noktayı hesapla
    static Vector3 PredictBallLandingPoint(Vector3 startPos, Vector3 velocity)
    {
        float groundHeight = 0.5f; // Yere düşme yüksekliği
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // Yere düşme zamanını hesapla (quadratic formula)
        float a = -0.5f * gravity;
        float b = velocity.y;
        float c = startPos.y - groundHeight;
        
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return startPos; // Hesaplanamıyor
        
        float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float timeToGround = Mathf.Max(t1, t2); // Pozitif olanı al
        
        if (timeToGround < 0) return startPos;
        
        // Düşme noktasını hesapla
        Vector3 landingPoint = new Vector3(
            startPos.x + velocity.x * timeToGround,
            groundHeight,
            startPos.z + velocity.z * timeToGround
        );
        
        return landingPoint;
    }
    
    // Bu bot topu yakalamaya gitmeli mi kontrol et - ARTIK KULLANILMIYOR
    void CheckIfShouldMoveToball()
    {
        // Bu metod artık kullanılmıyor, FindAndActivateClosestBot() kullanılıyor
    }
    
    // Hedefe doğru hareket başlat
    void StartMovingToBall(Vector3 targetPos)
    {
        targetMovePosition = targetPos;
        isMovingToBall = true;
        isReturningToDefault = false; // Default pozisyona dönmeyi iptal et
        Debug.Log($"{gameObject.name} started moving to position: {targetPos}");
    }
    
    // Hareketi durdur
    void StopMoving()
    {
        isMovingToBall = false;
        if (activeCatcher == this)
        {
            activeCatcher = null;
            Debug.Log($"{gameObject.name} stopped moving");
        }
    }
    
    // Hedefe doğru hareket et
    void MoveToTarget()
    {
        Vector3 direction = targetMovePosition - myTransform.position;
        direction.y = 0; // Y eksenini sıfırla
        
        float distance = direction.magnitude;
        
        // Hedefe ulaştıysa dur
        if (distance < stoppingDistance)
        {
            StopMoving();
            return;
        }
        
        // Hareket hızını belirle (uzaksa koş, yakınsa yürü)
        float currentSpeed = distance > 3f ? runSpeed : moveSpeed;
        
        // Hareket et
        Vector3 movement = direction.normalized * currentSpeed * Time.deltaTime;
        myTransform.position += movement;
        
        // Hedefe bak
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime * 2f);
        }
    }
    
    // Default pozisyona dönmeyi başlat (gecikmeyle)
    IEnumerator ReturnToDefaultAfterDelay()
    {
        yield return new WaitForSeconds(returnToDefaultDelay);
        
        // Hala top yok ve hareket etmiyorsa default pozisyona dön
        if (!hasBall && !isMovingToBall)
        {
            isReturningToDefault = true;
            Debug.Log($"{gameObject.name} returning to default position");
        }
    }
    
    // Default pozisyona dönüş hareketi
    void ReturnToDefaultPosition()
    {
        if (defaultPosition == null)
        {
            isReturningToDefault = false;
            return;
        }
        
        Vector3 direction = defaultPosition.position - myTransform.position;
        direction.y = 0; // Y eksenini sıfırla
        
        float distance = direction.magnitude;
        
        // Default pozisyona ulaştıysa dur
        if (distance < stoppingDistance)
        {
            isReturningToDefault = false;
            // Rotation'ı da default'a döndür
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, defaultPosition.rotation, rotationSpeed * Time.deltaTime);
            Debug.Log($"{gameObject.name} reached default position");
            return;
        }
        
        // Hareket et
        Vector3 movement = direction.normalized * moveSpeed * Time.deltaTime;
        myTransform.position += movement;
        
        // Default pozisyona bak
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    // Topu bulunduğu yerden anında fırlat
    void InstantThrow()
    {
        // Aktif yakalayıcıyı temizle
        if (activeCatcher == this)
        {
            StopMoving();
        }
        
        // Rastgele hedef seç
        targetBot = GetRandomTarget();
        
        if (targetBot == null || ball == null) return;
        
        // VR Player mı yoksa Bot mu kontrol et
        bool isTargetVRPlayer = targetBot.CompareTag("Player");
        
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) topu havada yakaladı ve VR Player ({vrProxy.playerTeam})'e fırlatıyor!");
        }
        else
        {
            BotController targetController = targetBot.GetComponent<BotController>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) topu havada yakaladı ve {targetBot.name} ({targetController.team})'e fırlatıyor!");
        }
        
        hasBall = true;
        UpdateBotColor();
        
        // Mevcut pozisyondan fırlat
        Vector3 currentBallPosition = ball.transform.position;
        Vector3 targetPosition;
        
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            targetPosition = vrProxy.GetTargetTransform().position + playerCatchOffset;
        }
        else
        {
            targetPosition = targetBot.position + catchOffset;
        }
        
        Vector3 direction = targetPosition - currentBallPosition;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        
        // Parabolik atış hesaplaması
        float gravity = Mathf.Abs(Physics.gravity.y);
        float heightDifference = targetPosition.y - currentBallPosition.y;
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        float angle = 45f * Mathf.Deg2Rad;
        
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        v0 *= dragCompensation;
        
        // Hız vektörünü oluştur
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        // Topu fırlat
        ballRb.velocity = finalVelocity;
        
        // TÜM botların yakalama iznini kapat
        for (int i = 0; i < allBots.Length; i++)
        {
            allBots[i].canCatchBall = false;
            allBots[i].lastThrower = myTransform;
        }
        
        // Hedef'e yakalama izni ver
        canCatchBall = false;
        catchCooldown = 0.5f;
        
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            vrProxy.EnableCatching();
        }
        else
        {
            BotController targetController = targetBot.GetComponent<BotController>();
            targetController.EnableCatching();
        }
        
        // Düşüş noktasını hesapla
        CalculateLandingPoint(currentBallPosition, finalVelocity);
        
        // Rengi normale döndür
        hasBall = false;
        StartCoroutine(ResetColorAfterDelay());
        
        // Referansları temizle
        ball = null;
        ballRb = null;
        
        // Default pozisyona dönmeyi başlat
        if (autoReturnToDefault)
        {
            StartCoroutine(ReturnToDefaultAfterDelay());
        }
    }
    
    IEnumerator ResetColorAfterDelay()
    {
        yield return colorResetDelay;
        UpdateBotColor();
    }
    
    // Public method - dışarıdan default pozisyona dönmeyi başlatmak için
    public void ForceReturnToDefault()
    {
        if (defaultPosition != null)
        {
            isReturningToDefault = true;
            isMovingToBall = false;
            Debug.Log($"{gameObject.name} forced to return to default position");
        }
    }
    
    // Default pozisyonu set etmek için
    public void SetDefaultPosition(Vector3 position, Quaternion rotation)
    {
        if (defaultPosition == null)
        {
            GameObject defaultPosObj = new GameObject($"{gameObject.name}_DefaultPosition");
            defaultPosObj.transform.SetParent(transform.parent);
            defaultPosition = defaultPosObj.transform;
        }
        
        defaultPosition.position = position;
        defaultPosition.rotation = rotation;
        
        Debug.Log($"{gameObject.name} default position set to: {position}");
    }
    
    // Gizmos ile algılama alanını göster
    void OnDrawGizmosSelected()
    {
        if (!showTrajectory) return;
        
        // Algılama alanı
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Yakalama alanı
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
        
        // Hareket hedefi
        if (isMovingToBall)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, targetMovePosition);
            Gizmos.DrawWireSphere(targetMovePosition, 0.5f);
        }
        
        // Default pozisyon
        if (defaultPosition != null)
        {
            Gizmos.color = isReturningToDefault ? Color.cyan : new Color(0.5f, 0.5f, 1f, 0.5f);
            Gizmos.DrawWireSphere(defaultPosition.position, 0.8f);
            Gizmos.DrawLine(defaultPosition.position, defaultPosition.position + defaultPosition.forward * 1.5f);
            
            // Default pozisyona dönüş yolu
            if (isReturningToDefault)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, defaultPosition.position);
            }
        }
    }
    
    // Play modda trajectory'i göster
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showTrajectory) return;
        
        // Tahmin edilen düşme noktasını göster
        if (isPredictionValid && Time.time - lastPredictionTime < 2f)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.5f); // Mor
            Gizmos.DrawWireSphere(predictedLandingPoint, 0.5f);
            Gizmos.DrawLine(predictedLandingPoint + Vector3.up * 3f, predictedLandingPoint);
            
            // Aktif yakalayıcıyı göster
            if (activeCatcher == this)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, predictedLandingPoint);
            }
        }
        
        // Mevcut gizmo kodları...
        
        // Eğer top havadaysa ve bu bot atmışsa trajectory göster
        if (ball == null && landingPoint != Vector3.zero)
        {
            // Düşüş noktasını göster
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(landingPoint, 0.3f);
            Gizmos.DrawLine(landingPoint + Vector3.up * 0.1f, landingPoint - Vector3.up * 0.1f);
            Gizmos.DrawLine(landingPoint + Vector3.right * 0.3f, landingPoint - Vector3.right * 0.3f);
            Gizmos.DrawLine(landingPoint + Vector3.forward * 0.3f, landingPoint - Vector3.forward * 0.3f);
        }
        
        // Eğer top elimizdeyse ve hedef varsa, tahmini trajectory'i göster
        if (hasBall && ball != null && targetBot != null)
        {
            Vector3 startPos = ball.transform.position;
            Vector3 targetPos = targetBot.position + catchOffset;
            
            // Atış parametrelerini hesapla
            Vector3 direction = targetPos - startPos;
            float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
            float gravity = Mathf.Abs(Physics.gravity.y);
            float heightDifference = targetPos.y - startPos.y;
            float angle = 45f * Mathf.Deg2Rad;
            
            float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                                 (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                                 (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
            
            Vector3 velocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
            velocity.y = v0 * Mathf.Sin(angle);
            
            // Trajectory noktalarını çiz
            DrawTrajectory(startPos, velocity);
        }
    }
    
    void DrawTrajectory(Vector3 startPosition, Vector3 velocity)
    {
        float timeStep = 0.1f;
        Vector3 previousPoint = startPosition;
        
        Gizmos.color = Color.cyan;
        
        for (int i = 0; i < trajectoryPoints; i++)
        {
            float time = i * timeStep;
            Vector3 point = CalculatePositionAtTime(startPosition, velocity, time);
            
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
            
            // Hedef yüksekliğe ulaştıysa dur
            if (targetBot != null && point.y <= targetBot.position.y + 1.5f && time > 0)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(point, 0.2f);
                break;
            }
        }
    }
    
    Vector3 CalculatePositionAtTime(Vector3 startPosition, Vector3 velocity, float time)
    {
        return new Vector3(
            startPosition.x + velocity.x * time,
            startPosition.y + velocity.y * time + 0.5f * Physics.gravity.y * time * time,
            startPosition.z + velocity.z * time
        );
    }
    
    void CalculateLandingPoint(Vector3 startPosition, Vector3 velocity)
    {
        if (targetBot == null) return;
        
        // Hedef yüksekliğini tag'e göre belirle
        float targetHeight;
        
        if (targetBot.CompareTag("Player"))
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            if (vrProxy != null && vrProxy.GetTargetTransform() != null)
            {
                targetHeight = vrProxy.GetTargetTransform().position.y;
            }
            else
            {
                targetHeight = targetBot.position.y + 1.2f; // Varsayılan göğüs hizası
            }
        }
        else
        {
            targetHeight = targetBot.position.y + 1.5f; // Bot için normal yükseklik
        }
        
        float a = 0.5f * Physics.gravity.y;
        float b = velocity.y;
        float c = startPosition.y - targetHeight;
        
        // Kuadratik formül ile zamanı hesapla
        float discriminant = b * b - 4 * a * c;
        if (discriminant >= 0)
        {
            float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
            float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
            float time = Mathf.Max(t1, t2); // Pozitif ve büyük olanı al
            
            if (time > 0)
            {
                landingPoint = CalculatePositionAtTime(startPosition, velocity, time);
            }
        }
    }
    
    // GameManager'dan çağrılacak - başlangıçta topu ver
    public void StartWithBall(GameObject startBall)
    {
        ball = startBall;
        ballRb = ball.GetComponent<Rigidbody>();
        hasBall = true;
        canCatchBall = true;
        
        // Global cache'i güncelle
        cachedBall = ball;
        cachedBallTransform = ball.transform;
        cachedBallRb = ballRb;
        
        // Topu bota yapıştır
        ball.transform.position = myTransform.position + catchOffset;
        ball.transform.SetParent(myTransform);
        
        UpdateBotColor();
        
        if (showTrajectory)
            Debug.Log($"{gameObject.name} topu aldı ve hemen atıyor...");
        
        // Hemen fırlat
        ThrowBallToTarget();
    }
}