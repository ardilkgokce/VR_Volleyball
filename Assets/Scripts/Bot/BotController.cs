using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

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
    public float catchHeight = 1.5f; // Yakalama alanının yerden yüksekliği
    public float catchVerticalRange = 1f; // Yakalama alanının dikey genişliği
    public float throwForce = 10f;
    public float throwHeight = 5f;
    
    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float runSpeed = 6f;
    public float stoppingDistance = 0.5f;
    public bool enableMovement = true;
    public float maxMoveDistance = 20f;
    public float netBoundary = 0.5f;
    public float outOfBoundsLimit = 2f;
    
    [Header("Animation Settings")]
    public BotAnimationController animationController;
    
    [Header("Hit Point")]
    public Transform hitPoint; // Topun vuruş anında konumlanacağı nokta
    
    [Header("Default Position")]
    public Transform defaultPosition;
    public float returnToDefaultDelay = 2f;
    public bool autoReturnToDefault = true;
    
    [Header("References")]
    [HideInInspector] public Transform targetBot;
    [HideInInspector] public GameObject ball;
    [HideInInspector] public Rigidbody ballRb;
    [HideInInspector] public bool hasBall = false;
    [HideInInspector] public float catchCooldown = 0f;
    [HideInInspector] public bool canCatchBall = true;
    public Transform lastThrower;
    
    [Header("Visual")]
    public Color normalColor = Color.blue;
    public Color hasBallColor = Color.green;
    private Renderer botRenderer;
    public float rotationSpeed = 10f;
    
    [Header("Debug")]
    public bool showTrajectory = true;
    public int trajectoryPoints = 30;
    private Vector3 landingPoint;
    
    // State Machine
    private BotState currentState;
    private string currentStateName = "None";
    
    // State flags (public için)
    [HideInInspector] public bool isMovingToBall = false;
    [HideInInspector] public bool isReturningToDefault = false;
    [HideInInspector] public bool isPerformingVolley = false;
    
    // Cache
    private static BotController[] allBots;
    private static List<Transform> validTargets = new List<Transform>();
    private Transform myTransform;
    public static readonly Vector3 catchOffset = Vector3.up * 1.5f;
    public static readonly Vector3 playerCatchOffset = Vector3.up * 1f;
    private WaitForSeconds colorResetDelay;
    
    // Court manager referansı
    private VolleyballCourtManager courtManager;
    
    // Movement
    private Vector3 targetMovePosition;
    public static BotController activeCatcher = null;
    
    // Ball prediction
    private static Vector3 predictedLandingPoint;
    private static bool isPredictionValid = false;
    private static float lastPredictionTime;
    
    // Top tracking cache
    private static GameObject cachedBall;
    private static Rigidbody cachedBallRb;
    private static Transform cachedBallTransform;
    private Collider[] nearbyColliders = new Collider[10];
    private int colliderCount;
    
    void Awake()
    {
        myTransform = transform;
        botRenderer = GetComponent<Renderer>();
        colorResetDelay = new WaitForSeconds(0.1f);
        
        // Animation Controller'ı bul
        if (animationController == null)
        {
            animationController = GetComponent<BotAnimationController>();
            if (animationController == null)
            {
                animationController = GetComponentInChildren<BotAnimationController>();
            }
        }
        
        if (animationController == null)
        {
            Debug.LogWarning($"{gameObject.name}: BotAnimationController component not found! Animations will not work.");
        }
        
        // Hit point kontrolü
        if (hitPoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: Hit Point not assigned! Ball positioning may not work correctly.");
        }
        
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
        UpdateBotColorInternal();
        
        // Court manager'ı bul
        courtManager = FindObjectOfType<VolleyballCourtManager>();
        if (courtManager == null)
        {
            Debug.LogWarning("VolleyballCourtManager not found! Court boundary checks may not work properly.");
        }
        
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
        
        canCatchBall = true;
        
        Debug.Log($"{gameObject.name} başlatıldı. Team: {team}, CanCatch: {canCatchBall}");
        
        StartCoroutine(CacheAllBotsDelayed());
        
        // Başlangıç state'i
        ChangeState(new IdleState(this));
    }
    
    System.Collections.IEnumerator CacheAllBotsDelayed()
    {
        yield return new WaitForSeconds(0.1f);
        
        allBots = FindObjectsOfType<BotController>();
        Debug.Log($"Cached {allBots.Length} bots in total");
        
        for (int i = 0; i < allBots.Length; i++)
        {
            if (allBots[i] != null)
                Debug.Log($"Bot {i}: {allBots[i].gameObject.name}");
        }
    }
    
    void Update()
    {
        if (catchCooldown > 0)
        {
            catchCooldown -= Time.deltaTime;
        }
        
        // State Update
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    void FixedUpdate()
    {
        // State FixedUpdate
        if (currentState != null)
        {
            currentState.FixedUpdate();
        }
    }
    
    // State değiştirme metodu
    public void ChangeState(BotState newState)
    {
        if (currentState != null)
        {
            currentState.Exit();
        }
        
        currentState = newState;
        currentStateName = newState.GetType().Name;
        
        if (currentState != null)
        {
            currentState.Enter();
        }
        
        if (showTrajectory)
        {
            Debug.Log($"{gameObject.name} changed state to: {currentStateName}");
        }
    }
    
    // Internal metodlar - State'ler tarafından kullanılacak
    internal void CheckForBallInternal()
    {
        if (Time.frameCount % 60 == 0 && showTrajectory)
        {
            Debug.Log($"{gameObject.name} - CanCatch: {canCatchBall}, Cooldown: {catchCooldown:F2}");
        }
        
        // Yakalama merkezi pozisyonu (yerden catchHeight kadar yukarıda)
        Vector3 catchCenter = myTransform.position + Vector3.up * catchHeight;
        
        int layerMask = 1 << LayerMask.NameToLayer("Default");
        colliderCount = Physics.OverlapSphereNonAlloc(catchCenter, detectionRadius, nearbyColliders, layerMask);
        
        for (int i = 0; i < colliderCount; i++)
        {
            if (nearbyColliders[i] != null && nearbyColliders[i].CompareTag("Ball"))
            {
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
                    
                    // Yatay mesafe kontrolü
                    Vector3 horizontalDiff = ballPos - myTransform.position;
                    horizontalDiff.y = 0;
                    float horizontalDistance = horizontalDiff.magnitude;
                    
                    // Dikey mesafe kontrolü
                    float ballHeight = ballPos.y;
                    float minCatchHeight = catchHeight - catchVerticalRange;
                    float maxCatchHeight = catchHeight + catchVerticalRange;
                    
                    bool isInVerticalRange = ballHeight >= minCatchHeight && ballHeight <= maxCatchHeight;
                    bool isInHorizontalRange = horizontalDistance < catchRadius;
                    
                    if (showTrajectory && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"{gameObject.name} - Ball: Speed={ballSpeed:F2}, HorizDist={horizontalDistance:F2}, Height={ballHeight:F2}, InRange={isInHorizontalRange && isInVerticalRange}");
                    }
                    
                    if (ballSpeed > 0.5f && isInHorizontalRange && isInVerticalRange)
                    {
                        Vector3 ballVelocity = cachedBallRb.velocity;
                        Vector3 toBall = ballPos - myTransform.position;
                        
                        float dotProduct = Vector3.Dot(ballVelocity.normalized, -toBall.normalized);
                        
                        if (dotProduct > 0.3f)
                        {
                            ball = cachedBall;
                            ballRb = cachedBallRb;
                            
                            VolleyballBall volleyballBall = ball.GetComponent<VolleyballBall>();
                            if (volleyballBall != null)
                            {
                                if (volleyballBall.OnHit(myTransform, team))
                                {
                                    // Hedef seç ve vuruş state'ine geç
                                    targetBot = GetRandomTarget();
                                    if (targetBot != null && ball != null)
                                    {
                                        ChangeState(new PreparingHitState(this, targetBot, ball));
                                    }
                                }
                                else
                                {
                                    Debug.LogWarning($"{gameObject.name} cannot hit the ball!");
                                }
                            }
                            else
                            {
                                // Eski sistem için de aynı işlem
                                targetBot = GetRandomTarget();
                                if (targetBot != null && ball != null)
                                {
                                    ChangeState(new PreparingHitState(this, targetBot, ball));
                                }
                            }
                            
                            break;
                        }
                    }
                }
            }
        }
    }
    
    internal void TrackBallInternal()
    {
        if (hasBall && targetBot != null)
        {
            if (animationController != null)
            {
                animationController.LookAt(targetBot.position, rotationSpeed);
            }
            return;
        }
        
        if (cachedBall != null && cachedBallRb != null && cachedBallRb.velocity.magnitude > 0.5f)
        {
            if (animationController != null)
            {
                animationController.LookAt(cachedBallTransform.position, rotationSpeed);
            }
            return;
        }
        
        if (Time.frameCount % 10 == 0)
        {
            int layerMask = 1 << LayerMask.NameToLayer("Default");
            colliderCount = Physics.OverlapSphereNonAlloc(myTransform.position, detectionRadius * 2f, nearbyColliders, layerMask);
            
            for (int i = 0; i < colliderCount; i++)
            {
                if (nearbyColliders[i].CompareTag("Ball"))
                {
                    cachedBall = nearbyColliders[i].gameObject;
                    cachedBallTransform = cachedBall.transform;
                    cachedBallRb = cachedBall.GetComponent<Rigidbody>();
                    break;
                }
            }
        }
    }
    
    internal void UpdateBotColorInternal()
    {
        if (botRenderer != null)
        {
            botRenderer.material.color = hasBall ? hasBallColor : normalColor;
        }
    }
    
    internal void CalculateLandingPointInternal(Vector3 startPosition, Vector3 velocity)
    {
        if (targetBot == null) return;
        
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
                targetHeight = targetBot.position.y + 1.2f;
            }
        }
        else
        {
            // Bot için catch height kullan
            BotController targetBotController = targetBot.GetComponent<BotController>();
            if (targetBotController != null)
            {
                targetHeight = targetBot.position.y + targetBotController.catchHeight + 0.1f;
            }
            else
            {
                targetHeight = targetBot.position.y + 1.5f;
            }
        }
        
        float a = 0.5f * Physics.gravity.y;
        float b = velocity.y;
        float c = startPosition.y - targetHeight;
        
        float discriminant = b * b - 4 * a * c;
        if (discriminant >= 0)
        {
            float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
            float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
            float time = Mathf.Max(t1, t2);
            
            if (time > 0)
            {
                landingPoint = CalculatePositionAtTime(startPosition, velocity, time);
            }
        }
    }
    
    // Public metodlar - dışarıdan erişim için
    Transform GetRandomTarget()
    {
        validTargets.Clear();
        
        VolleyballBall volleyballBall = null;
        if (ball != null)
        {
            volleyballBall = ball.GetComponent<VolleyballBall>();
        }
        
        bool mustPassToOpponent = volleyballBall != null && volleyballBall.MustPassToOpponent();
        
        if (mustPassToOpponent)
        {
            Debug.Log($"{gameObject.name} MUST pass to opponent team! (3rd hit)");
        }
        
        for (int i = 0; i < allBots.Length; i++)
        {
            if (allBots[i] != null && allBots[i].transform != myTransform)
            {
                if (mustPassToOpponent)
                {
                    if (allBots[i].team != team)
                    {
                        validTargets.Add(allBots[i].transform);
                    }
                }
                else
                {
                    validTargets.Add(allBots[i].transform);
                }
            }
        }
        
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        if (vrPlayer != null)
        {
            VRPlayerProxy vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
            if (vrProxy != null)
            {
                if (mustPassToOpponent)
                {
                    if (vrProxy.playerTeam != team)
                    {
                        validTargets.Add(vrPlayer.transform);
                    }
                }
                else
                {
                    validTargets.Add(vrPlayer.transform);
                }
            }
        }
        
        if (validTargets.Count > 0)
        {
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
                int randomIndex = Random.Range(0, validTargets.Count);
                Transform selected = validTargets[randomIndex];
                
                Debug.Log($"{gameObject.name} hedef seçti: {selected.name}");
                
                return selected;
            }
        }
        
        Debug.LogError($"{gameObject.name} couldn't find valid target! MustPassToOpponent: {mustPassToOpponent}");
        return null;
    }
    
    Transform GetOpponentTarget()
    {
        validTargets.Clear();
        
        Team opponentTeam = team == Team.Red ? Team.Blue : Team.Red;
        
        for (int i = 0; i < allBots.Length; i++)
        {
            if (allBots[i] != null && allBots[i].team == opponentTeam)
            {
                validTargets.Add(allBots[i].transform);
            }
        }
        
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        if (vrPlayer != null)
        {
            VRPlayerProxy vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
            if (vrProxy != null && vrProxy.playerTeam == opponentTeam)
            {
                validTargets.Add(vrPlayer.transform);
            }
        }
        
        if (validTargets.Count > 0)
        {
            Transform centerTarget = null;
            float minDistanceToCenter = float.MaxValue;
            
            foreach (Transform target in validTargets)
            {
                float distToCenter = Mathf.Abs(target.position.z);
                if (distToCenter < minDistanceToCenter)
                {
                    minDistanceToCenter = distToCenter;
                    centerTarget = target;
                }
            }
            
            if (centerTarget != null)
            {
                Debug.Log($"{gameObject.name} servisi {centerTarget.name}'e atıyor (orta saha oyuncusu)");
                return centerTarget;
            }
            
            return validTargets[Random.Range(0, validTargets.Count)];
        }
        
        Debug.LogError($"{gameObject.name} couldn't find opponent target for service!");
        return null;
    }
    
    // Hedef pozisyonunu hesaplayan yardımcı metod
    public Vector3 GetTargetPosition(Transform target)
    {
        if (target == null) return Vector3.zero;
        
        if (target.CompareTag("Player"))
        {
            VRPlayerProxy vrProxy = target.GetComponent<VRPlayerProxy>();
            if (vrProxy != null && vrProxy.GetTargetTransform() != null)
            {
                return vrProxy.GetTargetTransform().position + playerCatchOffset;
            }
            else
            {
                return target.position + playerCatchOffset;
            }
        }
        else
        {
            // Bot için catch height kullan
            BotController targetBotController = target.GetComponent<BotController>();
            if (targetBotController != null)
            {
                return target.position + Vector3.up * targetBotController.catchHeight + new Vector3(0,0.1f,0);
            }
            else
            {
                return target.position + catchOffset;
            }
        }
    }
    
    public void EnableCatching()
    {
        canCatchBall = true;
        if (showTrajectory)
            Debug.Log($"{gameObject.name} artık topu yakalayabilir!");
    }
    
    public static void OnVRPlayerHitBall(Vector3 ballPosition, Vector3 ballVelocity)
    {
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        Team vrTeam = Team.Blue;
    
        if (vrPlayer != null)
        {
            VRPlayerProxy vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
            if (vrProxy != null)
            {
                vrTeam = vrProxy.playerTeam;
            }
        }
    
        if (activeCatcher != null)
        {
            activeCatcher.ChangeState(new IdleState(activeCatcher));
        }
        activeCatcher = null;
    
        predictedLandingPoint = PredictBallLandingPoint(ballPosition, ballVelocity);
        isPredictionValid = true;
        lastPredictionTime = Time.time;
    
        Debug.Log($"VR Player ({vrTeam}) hit ball! Predicted landing: {predictedLandingPoint}");
    
        FindAndActivateClosestBot();
    }
    
    static void FindAndActivateClosestBot()
    {
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
        int boundaryViolationCount = 0;
        int wrongCourtCount = 0;
        int cannotCatchCount = 0;
        
        for (int i = 0; i < allBots.Length; i++)
        {
            BotController bot = allBots[i];
            if (bot != null)
            {
                if (!bot.canCatchBall)
                {
                    cannotCatchCount++;
                    Debug.Log($"Bot[{i}] {bot.gameObject.name} - CanCatchBall is FALSE!");
                    continue;
                }
                
                if (!bot.CanMoveToPosition(predictedLandingPoint))
                {
                    boundaryViolationCount++;
                    Debug.Log($"Bot[{i}] {bot.gameObject.name} cannot reach landing point due to boundary violation");
                    continue;
                }
                
                if (bot.team == Team.Red)
                {
                    if (!bot.IsBallInOurCourt(predictedLandingPoint))
                    {
                        wrongCourtCount++;
                        Debug.Log($"Bot[{i}] {bot.gameObject.name} - ball will land in opponent court");
                        continue;
                    }
                    
                    if (!bot.IsBallInBounds(predictedLandingPoint))
                    {
                        bool isFromOpponent = false;
                        
                        GameObject vrPlayer = GameObject.FindWithTag("Player");
                        if (vrPlayer != null)
                        {
                            VRPlayerProxy vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
                            if (vrProxy != null && vrProxy.playerTeam != bot.team)
                            {
                                isFromOpponent = true;
                            }
                        }
                        
                        if (!isFromOpponent && cachedBall != null)
                        {
                            VolleyballBall vbBall = cachedBall.GetComponent<VolleyballBall>();
                            if (vbBall != null && vbBall.currentTeam != bot.team)
                            {
                                isFromOpponent = true;
                            }
                        }
                        
                        if (isFromOpponent)
                        {
                            Debug.Log($"Bot[{i}] {bot.gameObject.name} - ball from opponent going out, not chasing");
                            continue;
                        }
                    }
                }
                else if (bot.team == Team.Blue)
                {
                    if (predictedLandingPoint.x < 0)
                    {
                        wrongCourtCount++;
                        Debug.Log($"Blue bot {bot.gameObject.name} - ball will land in Red court");
                        continue;
                    }
                }
                
                float distance = Vector3.Distance(bot.transform.position, predictedLandingPoint);
                
                Debug.Log($"Bot[{i}] {bot.gameObject.name} - Team: {bot.team}, CanCatch: {bot.canCatchBall}, HasBall: {bot.hasBall}, Distance: {distance:F2}");
                
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
        
        Debug.Log($"Summary - Valid: {validBotCount}, Cannot catch: {cannotCatchCount}, Boundary violations: {boundaryViolationCount}, Wrong court: {wrongCourtCount}");
        
        if (closestBot != null)
        {
            if (closestDistance < 20f)
            {
                // MovingToBallState'e geç
                MovingToBallState moveState = new MovingToBallState(closestBot);
                moveState.SetTargetPosition(predictedLandingPoint);
                closestBot.ChangeState(moveState);
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
    
    static Vector3 PredictBallLandingPoint(Vector3 startPos, Vector3 velocity)
    {
        float groundHeight = 0.5f;
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        float a = -0.5f * gravity;
        float b = velocity.y;
        float c = startPos.y - groundHeight;
        
        float discriminant = b * b - 4 * a * c;
        if (discriminant < 0) return startPos;
        
        float t1 = (-b - Mathf.Sqrt(discriminant)) / (2 * a);
        float t2 = (-b + Mathf.Sqrt(discriminant)) / (2 * a);
        float timeToGround = Mathf.Max(t1, t2);
        
        if (timeToGround < 0) return startPos;
        
        Vector3 landingPoint = new Vector3(
            startPos.x + velocity.x * timeToGround,
            groundHeight,
            startPos.z + velocity.z * timeToGround
        );
        
        return landingPoint;
    }
    
    public bool CanMoveToPosition(Vector3 targetPosition)
    {
        if (team == Team.Red)
        {
            if (targetPosition.x > -netBoundary)
            {
                Debug.LogWarning($"Red team bot {gameObject.name} cannot cross net boundary! Target X: {targetPosition.x:F2}");
                return false;
            }
        }
    
        if (team == Team.Red && courtManager != null)
        {
            float halfLength = courtManager.courtLength / 2f;
            float halfWidth = courtManager.courtWidth / 2f;
        
            if (Mathf.Abs(targetPosition.z) > halfWidth + outOfBoundsLimit)
            {
                Debug.LogWarning($"Red team bot {gameObject.name} cannot move too far out of bounds! Target Z: {targetPosition.z:F2}");
                return false;
            }
        
            if (targetPosition.x < -halfLength - outOfBoundsLimit)
            {
                Debug.LogWarning($"Red team bot {gameObject.name} cannot move too far behind court! Target X: {targetPosition.x:F2}");
                return false;
            }
        }
    
        return true;
    }
    
    bool IsBallInOurCourt(Vector3 ballPosition)
    {
        if (team == Team.Red)
        {
            return ballPosition.x < 0;
        }
        else
        {
            return ballPosition.x > 0;
        }
    }
    
    bool IsBallInBounds(Vector3 ballPosition)
    {
        if (courtManager == null) return true;
        
        float halfLength = courtManager.courtLength / 2f;
        float halfWidth = courtManager.courtWidth / 2f;
        
        return Mathf.Abs(ballPosition.x) <= halfLength && 
               Mathf.Abs(ballPosition.z) <= halfWidth;
    }
    
    public void ForceReturnToDefault()
    {
        if (defaultPosition != null)
        {
            ChangeState(new ReturningToPositionState(this));
        }
    }
    
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
    
    Vector3 CalculatePositionAtTime(Vector3 startPosition, Vector3 velocity, float time)
    {
        return new Vector3(
            startPosition.x + velocity.x * time,
            startPosition.y + velocity.y * time + 0.5f * Physics.gravity.y * time * time,
            startPosition.z + velocity.z * time
        );
    }
    
    public void StartWithBall(GameObject startBall, bool isServing = false)
    {
        ball = startBall;
        ballRb = ball.GetComponent<Rigidbody>();
        hasBall = true;
        canCatchBall = true;
        
        cachedBall = ball;
        cachedBallTransform = ball.transform;
        cachedBallRb = ballRb;
        
        ball.transform.position = myTransform.position + catchOffset;
        ball.transform.SetParent(myTransform);
        
        UpdateBotColorInternal();
        
        if (showTrajectory)
            Debug.Log($"{gameObject.name} topu aldı ve {(isServing ? "servis atacak" : "hemen atıyor")}...");
        
        // Servis için özel hedefleme
        if (isServing)
        {
            targetBot = GetOpponentTarget();
        }
        else
        {
            targetBot = GetRandomTarget();
        }
        
        if (targetBot != null && ball != null)
        {
            // Direkt vuruş state'ine geç (servis için hazırlık yok)
            ChangeState(new HittingState(this, targetBot, ball));
        }
    }
    
    // Debug için state bilgisi
    public string GetCurrentStateName()
    {
        return currentStateName;
    }
    
    void OnDrawGizmosSelected()
    {
        if (!showTrajectory) return;
        
        // State bilgisini göster
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, $"State: {currentStateName}");
            #endif
        }
        
        // Yakalama alanını göster (silindir şeklinde)
        Vector3 catchCenter = transform.position + Vector3.up * catchHeight;
        
        // Yatay yakalama alanı (daire)
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        DrawCircle(catchCenter, catchRadius, 32);
        
        // Dikey yakalama alanı (silindir kenarları)
        Gizmos.color = new Color(0, 1, 0, 0.5f);
        Vector3 topCenter = catchCenter + Vector3.up * catchVerticalRange;
        Vector3 bottomCenter = catchCenter - Vector3.up * catchVerticalRange;
        
        // Üst ve alt daireler
        DrawCircle(topCenter, catchRadius, 32);
        DrawCircle(bottomCenter, catchRadius, 32);
        
        // Dikey çizgiler
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * catchRadius;
            Gizmos.DrawLine(topCenter + offset, bottomCenter + offset);
        }
        
        // Merkez noktası
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(catchCenter, 0.1f);
        
        // Detection radius (eskisi gibi)
        Gizmos.color = new Color(1, 1, 0, 0.2f);
        Gizmos.DrawWireSphere(catchCenter, detectionRadius);
        
        // Diğer debug gösterimleri aynı kalacak...
        if (team == Team.Red)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            Gizmos.DrawLine(new Vector3(-netBoundary, 0, -10), new Vector3(-netBoundary, 3, -10));
            Gizmos.DrawLine(new Vector3(-netBoundary, 0, 10), new Vector3(-netBoundary, 3, 10));
            Gizmos.DrawLine(new Vector3(-netBoundary, 3, -10), new Vector3(-netBoundary, 3, 10));
        }
        else if (team == Team.Blue)
        {
            Gizmos.color = new Color(0f, 0f, 1f, 0.5f);
            Gizmos.DrawLine(new Vector3(netBoundary, 0, -10), new Vector3(netBoundary, 3, -10));
            Gizmos.DrawLine(new Vector3(netBoundary, 0, 10), new Vector3(netBoundary, 3, 10));
            Gizmos.DrawLine(new Vector3(netBoundary, 3, -10), new Vector3(netBoundary, 3, 10));
        }
        
        if (courtManager != null)
        {
            float halfLength = courtManager.courtLength / 2f;
            float halfWidth = courtManager.courtWidth / 2f;
            
            Gizmos.color = new Color(0.5f, 1f, 0.5f, 0.3f);
            
            if (team == Team.Red)
            {
                Vector3 center = new Vector3((-halfLength - netBoundary) / 2f - 0.5f, 0.1f, 0);
                Vector3 size = new Vector3(halfLength - netBoundary + outOfBoundsLimit, 0.1f, 
                                         (halfWidth + outOfBoundsLimit) * 2f);
                Gizmos.DrawCube(center, size);
                
                Gizmos.color = Color.red;
                Gizmos.DrawLine(new Vector3(-halfLength - outOfBoundsLimit, 0, -halfWidth - outOfBoundsLimit), 
                              new Vector3(-halfLength - outOfBoundsLimit, 0, halfWidth + outOfBoundsLimit));
            }
            else
            {
                Vector3 center = new Vector3((halfLength + netBoundary) / 2f + 0.5f, 0.1f, 0);
                Vector3 size = new Vector3(halfLength - netBoundary + outOfBoundsLimit, 0.1f, 
                                         (halfWidth + outOfBoundsLimit) * 2f);
                Gizmos.DrawCube(center, size);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(new Vector3(halfLength + outOfBoundsLimit, 0, -halfWidth - outOfBoundsLimit), 
                              new Vector3(halfLength + outOfBoundsLimit, 0, halfWidth + outOfBoundsLimit));
            }
        }
        
        if (isMovingToBall)
        {
            bool canReach = CanMoveToPosition(targetMovePosition);
            Gizmos.color = canReach ? Color.magenta : Color.red;
            Gizmos.DrawLine(transform.position, targetMovePosition);
            Gizmos.DrawWireSphere(targetMovePosition, 0.5f);
            
            if (!canReach)
            {
                Vector3 crossPos = targetMovePosition + Vector3.up * 2f;
                float crossSize = 0.5f;
                Gizmos.DrawLine(crossPos + new Vector3(-crossSize, crossSize, 0), 
                              crossPos + new Vector3(crossSize, -crossSize, 0));
                Gizmos.DrawLine(crossPos + new Vector3(-crossSize, -crossSize, 0), 
                              crossPos + new Vector3(crossSize, crossSize, 0));
            }
        }
        
        if (defaultPosition != null)
        {
            Gizmos.color = isReturningToDefault ? Color.cyan : new Color(0.5f, 0.5f, 1f, 0.5f);
            Gizmos.DrawWireSphere(defaultPosition.position, 0.8f);
            Gizmos.DrawLine(defaultPosition.position, defaultPosition.position + defaultPosition.forward * 1.5f);
            
            if (isReturningToDefault)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, defaultPosition.position);
            }
        }
    }
    
    // Yardımcı metod - daire çizme
    void DrawCircle(Vector3 center, float radius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);
        
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showTrajectory) return;
        
        if (isPredictionValid && Time.time - lastPredictionTime < 2f)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
            Gizmos.DrawWireSphere(predictedLandingPoint, 0.5f);
            Gizmos.DrawLine(predictedLandingPoint + Vector3.up * 3f, predictedLandingPoint);
            
            if (activeCatcher == this)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, predictedLandingPoint);
            }
        }
        
        if (ball == null && landingPoint != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(landingPoint, 0.3f);
            Gizmos.DrawLine(landingPoint + Vector3.up * 0.1f, landingPoint - Vector3.up * 0.1f);
            Gizmos.DrawLine(landingPoint + Vector3.right * 0.3f, landingPoint - Vector3.right * 0.3f);
            Gizmos.DrawLine(landingPoint + Vector3.forward * 0.3f, landingPoint - Vector3.forward * 0.3f);
        }
        
        if (hasBall && ball != null && targetBot != null)
        {
            Vector3 startPos = ball.transform.position;
            Vector3 targetPos = GetTargetPosition(targetBot);
            
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
            
            DrawTrajectory(startPos, velocity);
            
            // Hedef noktayı göster
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(targetPos, 0.3f);
            Gizmos.DrawWireCube(targetPos, Vector3.one * 0.2f);
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
            
            if (targetBot != null)
            {
                float targetHeight;
                
                // VR Player mı yoksa Bot mu kontrol et
                if (targetBot.CompareTag("Player"))
                {
                    VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
                    if (vrProxy != null && vrProxy.GetTargetTransform() != null)
                    {
                        targetHeight = vrProxy.GetTargetTransform().position.y;
                    }
                    else
                    {
                        targetHeight = targetBot.position.y + 1.2f;
                    }
                }
                else
                {
                    // Bot için catch height kullan
                    BotController targetBotController = targetBot.GetComponent<BotController>();
                    if (targetBotController != null)
                    {
                        targetHeight = targetBot.position.y + targetBotController.catchHeight;
                    }
                    else
                    {
                        targetHeight = targetBot.position.y + 1.5f;
                    }
                }
                
                if (point.y <= targetHeight && time > 0)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(point, 0.2f);
                    break;
                }
            }
        }
    }
}