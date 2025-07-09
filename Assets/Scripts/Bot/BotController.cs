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
    
    [Header("Default Position")]
    public Transform defaultPosition;
    public float returnToDefaultDelay = 2f;
    public bool autoReturnToDefault = true;
    
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
    public float rotationSpeed = 10f;
    
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
    
    // Court manager referansı
    private VolleyballCourtManager courtManager;
    
    // Movement
    private Vector3 targetMovePosition;
    private bool isMovingToBall = false;
    private bool isReturningToDefault = false;
    private static BotController activeCatcher = null;
    
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
    
    // Animation states
    private float currentMoveSpeed = 0f;
    private bool isPerformingVolley = false;
    
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
        
        // Animasyon güncellemeleri
        UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (!hasBall && canCatchBall && catchCooldown <= 0)
        {
            CheckForBall();
        }
        
        TrackBall();
        
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
    
    void UpdateAnimations()
    {
        if (animationController == null) return;
        
        // Hareket durumunu kontrol et
        bool isMoving = isMovingToBall || isReturningToDefault;
        
        if (isMoving)
        {
            Vector3 direction = targetMovePosition - myTransform.position;
            float distance = direction.magnitude;
            
            // Mesafeye göre hız ayarla
            if (distance > 3f)
            {
                currentMoveSpeed = runSpeed;
            }
            else
            {
                currentMoveSpeed = moveSpeed;
            }
        }
        else
        {
            // Yavaşça sıfıra düş
            currentMoveSpeed = Mathf.Lerp(currentMoveSpeed, 0f, Time.deltaTime * 5f);
        }
        
        // Animation controller'a güncellemeyi gönder
        animationController.UpdateMovementAnimation(currentMoveSpeed, isMoving);
    }

    void CheckForBall()
    {
        if (Time.frameCount % 60 == 0 && showTrajectory)
        {
            Debug.Log($"{gameObject.name} - CanCatch: {canCatchBall}, Cooldown: {catchCooldown:F2}");
        }
        
        int layerMask = 1 << LayerMask.NameToLayer("Default");
        colliderCount = Physics.OverlapSphereNonAlloc(myTransform.position, detectionRadius, nearbyColliders, layerMask);
        
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
                    float distance = Vector3.Distance(myTransform.position, ballPos);
                    
                    if (showTrajectory && Time.frameCount % 30 == 0)
                    {
                        Debug.Log($"{gameObject.name} - Top hızı: {ballSpeed:F2}, Mesafe: {distance:F2}");
                    }
                    
                    if (ballSpeed > 0.5f)
                    {
                        Vector3 ballVelocity = cachedBallRb.velocity;
                        Vector3 toBall = ballPos - myTransform.position;
                        
                        float dotProduct = Vector3.Dot(ballVelocity.normalized, -toBall.normalized);
                        
                        if (dotProduct > 0.3f && distance < catchRadius)
                        {
                            ball = cachedBall;
                            ballRb = cachedBallRb;
                            
                            VolleyballBall volleyballBall = ball.GetComponent<VolleyballBall>();
                            if (volleyballBall != null)
                            {
                                if (volleyballBall.OnHit(myTransform, team))
                                {
                                    if (isMovingToBall)
                                    {
                                        StopMoving();
                                    }
                                    
                                    // Voleybol animasyonunu tetikle ve hemen topu fırlat
                                    if (animationController != null)
                                    {
                                        animationController.PlayVolleyAnimation();
                                    }
                                    InstantThrow();
                                }
                                else
                                {
                                    Debug.LogWarning($"{gameObject.name} cannot hit the ball!");
                                }
                            }
                            else
                            {
                                if (isMovingToBall)
                                {
                                    StopMoving();
                                }
                                
                                if (animationController != null)
                                {
                                    animationController.PlayVolleyAnimation();
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
        if (hasBall && targetBot != null)
        {
            LookAtTarget(targetBot.position);
            return;
        }
        
        if (cachedBall != null && cachedBallRb != null && cachedBallRb.velocity.magnitude > 0.5f)
        {
            LookAtTarget(cachedBallTransform.position);
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
    
    void LookAtTarget(Vector3 targetPosition)
    {
        if (animationController != null)
        {
            animationController.LookAt(targetPosition, rotationSpeed);
        }
        else
        {
            // Fallback - animasyon controller yoksa eski yöntem
            Vector3 direction = targetPosition - myTransform.position;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
        }
    }
    
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
    
    void ThrowBallToTarget()
    {
        targetBot = GetRandomTarget();
        
        if (targetBot == null || ball == null)
        {
            Debug.LogError("Target bot veya ball null!");
            return;
        }
        
        bool isTargetVRPlayer = targetBot.CompareTag("Player");
        
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) topu VR Player ({vrProxy.playerTeam})'e fırlatıyor!");
            
            hasBall = false;
            catchCooldown = 0.5f;
            canCatchBall = false;
            
            for (int i = 0; i < allBots.Length; i++)
            {
                allBots[i].canCatchBall = false;
                allBots[i].lastThrower = myTransform;
            }
            
            vrProxy.EnableCatching();
        }
        else
        {
            BotController targetController = targetBot.GetComponent<BotController>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) topu {targetBot.name} ({targetController.team})'e fırlatıyor!");
            
            hasBall = false;
            catchCooldown = 0.5f;
            canCatchBall = false;
            
            for (int i = 0; i < allBots.Length; i++)
            {
                allBots[i].canCatchBall = false;
                allBots[i].lastThrower = myTransform;
            }
            
            targetController.EnableCatching();
        }
        
        ball.transform.SetParent(null);
        ballRb.useGravity = true;
        
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
        
        float gravity = Mathf.Abs(Physics.gravity.y);
        float heightDifference = targetPosition.y - ball.transform.position.y;
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        float angle = 45f * Mathf.Deg2Rad;
        
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        v0 *= dragCompensation;
        
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        ballRb.velocity = finalVelocity;
        
        CalculateLandingPoint(ball.transform.position, finalVelocity);
        
        UpdateBotColor();
        
        ball = null;
        ballRb = null;
        
        if (autoReturnToDefault)
        {
            StartCoroutine(ReturnToDefaultAfterDelay());
        }
    }
    
    void ThrowServiceToOpponent()
    {
        targetBot = GetOpponentTarget();
        
        if (targetBot == null || ball == null)
        {
            Debug.LogError("Service target or ball is null!");
            ThrowBallToTarget();
            return;
        }
        
        bool isTargetVRPlayer = targetBot.CompareTag("Player");
        
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) servis atıyor → VR Player ({vrProxy.playerTeam})!");
        }
        else
        {
            BotController targetController = targetBot.GetComponent<BotController>();
            if (showTrajectory)
                Debug.Log($"{gameObject.name} ({team}) servis atıyor → {targetBot.name} ({targetController.team})!");
        }
        
        hasBall = false;
        catchCooldown = 0.5f;
        canCatchBall = false;
        
        for (int i = 0; i < allBots.Length; i++)
        {
            allBots[i].canCatchBall = false;
            allBots[i].lastThrower = myTransform;
        }
        
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
        
        ball.transform.SetParent(null);
        ballRb.useGravity = true;
        
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
        
        float gravity = Mathf.Abs(Physics.gravity.y);
        float heightDifference = targetPosition.y - ball.transform.position.y;
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        float angle = 45f * Mathf.Deg2Rad;
        
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        v0 *= dragCompensation;
        
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        ballRb.velocity = finalVelocity;
        
        CalculateLandingPoint(ball.transform.position, finalVelocity);
        
        UpdateBotColor();
        
        ball = null;
        ballRb = null;
        
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
            activeCatcher.StopMoving();
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
        
        Debug.Log("=== BOT STATUS CHECK ===");
        for (int j = 0; j < allBots.Length; j++)
        {
            if (allBots[j] != null)
            {
                Debug.Log($"Bot[{j}] {allBots[j].gameObject.name} - Team: {allBots[j].team}, CanCatch: {allBots[j].canCatchBall}, HasBall: {allBots[j].hasBall}");
            }
        }
        Debug.Log("=======================");
        
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
    
    bool CanMoveToPosition(Vector3 targetPosition)
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
    
    void StartMovingToBall(Vector3 targetPos)
    {
        if (!CanMoveToPosition(targetPos))
        {
            Debug.Log($"{gameObject.name} cannot move to {targetPos.x:F2} - net boundary violation!");
            return;
        }
        
        targetMovePosition = targetPos;
        isMovingToBall = true;
        isReturningToDefault = false;
        Debug.Log($"{gameObject.name} started moving to position: {targetPos}");
    }
    
    void StopMoving()
    {
        isMovingToBall = false;
        if (activeCatcher == this)
        {
            activeCatcher = null;
            Debug.Log($"{gameObject.name} stopped moving");
        }
        
        // Animasyonu idle'a döndür
        if (animationController != null)
        {
            animationController.PlayIdleAnimation();
        }
    }
    
    void MoveToTarget()
    {
        Vector3 direction = targetMovePosition - myTransform.position;
        direction.y = 0;
        
        float distance = direction.magnitude;
        
        if (distance < stoppingDistance)
        {
            StopMoving();
            return;
        }
        
        float currentSpeed = distance > 3f ? runSpeed : moveSpeed;
        Vector3 movement = direction.normalized * currentSpeed * Time.deltaTime;
        
        Vector3 newPosition = myTransform.position + movement;
        
        if (!CanMoveToPosition(newPosition))
        {
            if (courtManager != null)
            {
                float halfLength = courtManager.courtLength / 2f;
                float halfWidth = courtManager.courtWidth / 2f;
                
                if (team == Team.Red)
                {
                    newPosition.x = Mathf.Min(newPosition.x, -netBoundary - 0.1f);
                    newPosition.x = Mathf.Max(newPosition.x, -halfLength - outOfBoundsLimit);
                }
                else if (team == Team.Blue)
                {
                    newPosition.x = Mathf.Max(newPosition.x, netBoundary + 0.1f);
                    newPosition.x = Mathf.Min(newPosition.x, halfLength + outOfBoundsLimit);
                }
                
                newPosition.z = Mathf.Clamp(newPosition.z, -halfWidth - outOfBoundsLimit, halfWidth + outOfBoundsLimit);
            }
            
            if (Vector3.Distance(myTransform.position, newPosition) < 0.01f)
            {
                StopMoving();
                Debug.Log($"{gameObject.name} reached boundary, stopping movement");
                return;
            }
        }
        
        myTransform.position = newPosition;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime * 2f);
        }
    }
    
    IEnumerator ReturnToDefaultAfterDelay()
    {
        yield return new WaitForSeconds(returnToDefaultDelay);
        
        if (!hasBall && !isMovingToBall)
        {
            isReturningToDefault = true;
            Debug.Log($"{gameObject.name} returning to default position");
        }
    }
    
    void ReturnToDefaultPosition()
    {
        if (defaultPosition == null)
        {
            isReturningToDefault = false;
            return;
        }
        
        Vector3 direction = defaultPosition.position - myTransform.position;
        direction.y = 0;
        
        float distance = direction.magnitude;
        
        if (distance < stoppingDistance)
        {
            isReturningToDefault = false;
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, defaultPosition.rotation, rotationSpeed * Time.deltaTime);
            Debug.Log($"{gameObject.name} reached default position");
            return;
        }
        
        Vector3 movement = direction.normalized * moveSpeed * Time.deltaTime;
        myTransform.position += movement;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            myTransform.rotation = Quaternion.Slerp(myTransform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    void InstantThrow()
    {
        if (activeCatcher == this)
        {
            StopMoving();
        }
        
        targetBot = GetRandomTarget();
        
        if (targetBot == null || ball == null) return;
        
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
        
        float gravity = Mathf.Abs(Physics.gravity.y);
        float heightDifference = targetPosition.y - currentBallPosition.y;
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        float angle = 45f * Mathf.Deg2Rad;
        
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        v0 *= dragCompensation;
        
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        ballRb.velocity = finalVelocity;
        
        for (int i = 0; i < allBots.Length; i++)
        {
            allBots[i].canCatchBall = false;
            allBots[i].lastThrower = myTransform;
        }
        
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
        
        CalculateLandingPoint(currentBallPosition, finalVelocity);
        
        hasBall = false;
        StartCoroutine(ResetColorAfterDelay());
        
        ball = null;
        ballRb = null;
        
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
    
    public void ForceReturnToDefault()
    {
        if (defaultPosition != null)
        {
            isReturningToDefault = true;
            isMovingToBall = false;
            Debug.Log($"{gameObject.name} forced to return to default position");
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
    
    void OnDrawGizmosSelected()
    {
        if (!showTrajectory) return;
        
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
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, catchRadius);
        
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
            Vector3 targetPos = targetBot.position + catchOffset;
            
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
            targetHeight = targetBot.position.y + 1.5f;
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
        
        UpdateBotColor();
        
        if (showTrajectory)
            Debug.Log($"{gameObject.name} topu aldı ve {(isServing ? "servis atacak" : "hemen atıyor")}...");
        
        if (isServing)
        {
            ThrowServiceToOpponent();
        }
        else
        {
            ThrowBallToTarget();
        }
    }
}