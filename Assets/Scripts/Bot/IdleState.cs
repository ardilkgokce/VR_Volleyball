using UnityEngine;
using System.Collections;

// IDLE STATE - Beklemede, topu takip ediyor
public class IdleState : BotState
{
    public IdleState(BotController bot) : base(bot) { }
    
    public override void Enter()
    {
        if (bot.animationController != null)
        {
            bot.animationController.PlayIdleAnimation();
        }
    }
    
    public override void Update()
    {
        // Animasyon güncellemesi
        UpdateAnimationSpeed(0f, false);
    }
    
    public override void FixedUpdate()
    {
        // Top takibi
        bot.TrackBallInternal();
        
        // Top kontrolü
        if (!bot.hasBall && bot.canCatchBall && bot.catchCooldown <= 0)
        {
            bot.CheckForBallInternal();
        }
    }
    
    public override void Exit()
    {
        // Idle'dan çıkarken yapılacaklar
    }
}

// MOVING TO BALL STATE - Topa doğru hareket
public class MovingToBallState : BotState
{
    private Vector3 targetPosition;
    private float currentSpeed;
    
    public MovingToBallState(BotController bot) : base(bot) { }
    
    public void SetTargetPosition(Vector3 position)
    {
        targetPosition = position;
    }
    
    public override void Enter()
    {
        bot.isMovingToBall = true;
        Debug.Log($"{bot.gameObject.name} started moving to ball");
    }
    
    public override void Update()
    {
        // Hareket animasyonu
        Vector3 direction = targetPosition - transform.position;
        float distance = direction.magnitude;
        
        currentSpeed = distance > 3f ? bot.runSpeed : bot.moveSpeed;
        UpdateAnimationSpeed(currentSpeed, true);
    }
    
    public override void FixedUpdate()
    {
        // Hareketi gerçekleştir
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        
        float distance = direction.magnitude;
        
        // Hedefe ulaştık mı?
        if (distance < bot.stoppingDistance)
        {
            bot.ChangeState(new IdleState(bot));
            return;
        }
        
        // Hareket et
        Vector3 movement = direction.normalized * currentSpeed * Time.deltaTime;
        Vector3 newPosition = transform.position + movement;
        
        // Sınır kontrolü
        if (!bot.CanMoveToPosition(newPosition))
        {
            bot.ChangeState(new IdleState(bot));
            return;
        }
        
        transform.position = newPosition;
        
        // Rotasyon
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bot.rotationSpeed * Time.deltaTime * 2f);
        }
        
        // Top kontrolü - hareket halindeyken de kontrol et
        if (!bot.hasBall && bot.canCatchBall && bot.catchCooldown <= 0)
        {
            bot.CheckForBallInternal();
        }
    }
    
    public override void Exit()
    {
        bot.isMovingToBall = false;
        BotController.activeCatcher = null;
        Debug.Log($"{bot.gameObject.name} stopped moving to ball");
    }
}

// PREPARING HIT STATE - Vuruş hazırlığı (voleybol animasyonu)
public class PreparingHitState : BotState
{
    private Transform targetBot;
    private GameObject ball;
    private float preparationTime = 0.05f; // Çok kısa hazırlık süresi
    private float timer;
    
    public PreparingHitState(BotController bot, Transform target, GameObject ballObj) : base(bot) 
    {
        targetBot = target;
        ball = ballObj;
    }
    
    public override void Enter()
    {
        timer = 0f;
        bot.isPerformingVolley = true;
        
        // Topu hemen hit point'e taşı
        PositionBallAtHitPoint();
        
        // Animasyonu başlat (eğer önceden başlatılmadıysa)
        if (bot.animationController != null && !bot.animationController.IsPerformingVolley())
        {
            bot.animationController.PlayVolleyAnimation();
        }
        
        // Hedefe bak
        if (targetBot != null)
        {
            LookAtTarget(targetBot.position);
        }
    }
    
    private void PositionBallAtHitPoint()
    {
        if (bot.hitPoint != null && ball != null)
        {
            // Topu hit point pozisyonuna taşı
            ball.transform.position = bot.hitPoint.position;
            
            // Parent yap ki hit point ile hareket etsin
            ball.transform.SetParent(bot.hitPoint);
            
            Debug.Log($"{bot.gameObject.name} positioned ball at hit point");
        }
        else if (ball != null)
        {
            // Hit point yoksa fallback olarak bot'un üstüne koy
            ball.transform.position = transform.position + Vector3.up * 1.5f;
            Debug.LogWarning($"{bot.gameObject.name} has no hit point assigned! Using default position.");
        }
    }
    
    public override void Update()
    {
        timer += Time.deltaTime;
        
        // Hazırlık süresi doldu mu?
        if (timer >= preparationTime)
        {
            bot.ChangeState(new HittingState(bot, targetBot, ball));
        }
    }
    
    public override void FixedUpdate()
    {
        // Hedefe bakmaya devam et
        if (targetBot != null)
        {
            LookAtTarget(targetBot.position);
        }
    }
    
    public override void Exit()
    {
        // Hazırlık bitti
    }
}

// HITTING STATE - Topu fırlatma
public class HittingState : BotState
{
    private Transform targetBot;
    private GameObject ball;
    private Rigidbody ballRb;
    
    public HittingState(BotController bot, Transform target, GameObject ballObj) : base(bot) 
    {
        targetBot = target;
        ball = ballObj;
        if (ball != null)
        {
            ballRb = ball.GetComponent<Rigidbody>();
        }
    }
    
    public override void Enter()
    {
        if (targetBot == null || ball == null || ballRb == null)
        {
            Debug.LogError("HittingState: Missing references!");
            bot.ChangeState(new IdleState(bot));
            return;
        }
        
        // Topu el pozisyonuna taşı
        PositionBallAtHand();
        
        // Topu fırlat
        PerformHit();
    }
    
    private void PositionBallAtHand()
    {
        if (bot.hitPoint != null && ball != null)
        {
            // Topu hit point pozisyonuna taşı
            ball.transform.position = bot.hitPoint.position;
            
            // Opsiyonel: Hit point'in biraz önüne koy
            if (bot.hitPoint.forward != Vector3.zero)
            {
                ball.transform.position += bot.hitPoint.forward * 0.1f; // 10cm önde
            }
            
            Debug.Log($"{bot.gameObject.name} positioned ball at hit point for throwing");
        }
        else if (ball != null)
        {
            // Hit point yoksa fallback
            ball.transform.position = transform.position + Vector3.up * 1.5f;
            Debug.LogWarning($"{bot.gameObject.name} has no hit point assigned! Using default position.");
        }
    }
    
    private void PerformHit()
    {
        bool isTargetVRPlayer = targetBot.CompareTag("Player");
        
        // Debug log
        if (isTargetVRPlayer)
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            if (bot.showTrajectory)
                Debug.Log($"{bot.gameObject.name} ({bot.team}) topu VR Player ({vrProxy.playerTeam})'e fırlatıyor!");
        }
        else
        {
            BotController targetController = targetBot.GetComponent<BotController>();
            if (bot.showTrajectory)
                Debug.Log($"{bot.gameObject.name} ({bot.team}) topu {targetBot.name} ({targetController.team})'e fırlatıyor!");
        }
        
        // State güncellemeleri
        bot.hasBall = false;
        bot.catchCooldown = 0.5f;
        bot.canCatchBall = false;
        
        // Tüm botların yakalama iznini kapat
        BotController[] allBots = Object.FindObjectsOfType<BotController>();
        for (int i = 0; i < allBots.Length; i++)
        {
            allBots[i].canCatchBall = false;
            allBots[i].lastThrower = transform;
        }
        
        // Hedefin yakalama iznini aç
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
        
        // Topu serbest bırak
        ball.transform.SetParent(null);
        ballRb.useGravity = true;
        
        // Hedef pozisyonu hesapla - GetTargetPosition metodunu kullan
        Vector3 targetPosition = bot.GetTargetPosition(targetBot);
        
        // Fırlatma hesaplamaları - EL POZİSYONUNDAN BAŞLA
        Vector3 startPosition = ball.transform.position; // Artık el pozisyonunda
        Vector3 direction = targetPosition - startPosition;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        
        float gravity = Mathf.Abs(Physics.gravity.y);
        float heightDifference = targetPosition.y - startPosition.y;
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        float angle = 45f * Mathf.Deg2Rad;
        
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        v0 *= dragCompensation;
        
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        ballRb.velocity = finalVelocity;
        
        // Landing point hesapla
        bot.CalculateLandingPointInternal(startPosition, finalVelocity);
        
        // Renk güncelle
        bot.UpdateBotColorInternal();
        
        // Referansları temizle
        bot.ball = null;
        bot.ballRb = null;
    }
    
    public override void Update()
    {
        // Hit anında yapılacak bir şey yok
    }
    
    public override void FixedUpdate()
    {
        // Hit sonrası - duruma göre Idle veya Returning state'ine geç
        if (bot.autoReturnToDefault)
        {
            bot.StartCoroutine(DelayedReturnToDefault());
        }
        else
        {
            bot.ChangeState(new IdleState(bot));
        }
    }
    
    private IEnumerator DelayedReturnToDefault()
    {
        // Idle'a geç
        bot.ChangeState(new IdleState(bot));
        
        // Bekle
        yield return new WaitForSeconds(bot.returnToDefaultDelay);
        
        // Eğer hala idle'daysak ve top yoksa, default pozisyona dön
        if (!bot.hasBall && !bot.isMovingToBall && bot.defaultPosition != null)
        {
            // Default pozisyona olan mesafeyi kontrol et
            float distanceToDefault = Vector3.Distance(bot.transform.position, bot.defaultPosition.position);
            
            // Minimum mesafe kontrolü - çok yakınsa dönmeye gerek yok
            if (distanceToDefault > 0.5f) // 50cm'den uzaksa
            {
                bot.ChangeState(new ReturningToPositionState(bot));
            }
            
        }
    }
    
    public override void Exit()
    {
        bot.isPerformingVolley = false;
    }
}

// RETURNING TO POSITION STATE - Default pozisyona dönüş
public class ReturningToPositionState : BotState
{
    private bool isRotationOnly = false;
    
    public ReturningToPositionState(BotController bot) : base(bot) { }
    
    public override void Enter()
    {
        if (bot.defaultPosition == null)
        {
            bot.ChangeState(new IdleState(bot));
            return;
        }
        
        // Mesafe kontrolü
        float distance = Vector3.Distance(transform.position, bot.defaultPosition.position);
        
        if (distance <= bot.minReturnDistance)
        {
            // Sadece rotasyon düzeltmesi yap
            isRotationOnly = true;
            Debug.Log($"{bot.gameObject.name} only fixing rotation (distance: {distance:F2}m)");
        }
        else
        {
            // Normal dönüş
            bot.isReturningToDefault = true;
            Debug.Log($"{bot.gameObject.name} returning to default position (distance: {distance:F2}m)");
        }
    }
    
    public override void Update()
    {
        if (!isRotationOnly && bot.defaultPosition != null)
        {
            // Hareket animasyonu
            Vector3 direction = bot.defaultPosition.position - transform.position;
            float distance = direction.magnitude;
            
            float speed = distance > 1f ? bot.moveSpeed : bot.moveSpeed * 0.5f;
            UpdateAnimationSpeed(speed, true);
        }
        else
        {
            // Rotasyon düzeltmesi sırasında idle animasyon
            UpdateAnimationSpeed(0f, false);
        }
    }
    
    public override void FixedUpdate()
    {
        if (bot.defaultPosition == null)
        {
            bot.ChangeState(new IdleState(bot));
            return;
        }
        
        if (isRotationOnly)
        {
            // Sadece rotasyonu düzelt
            float angleDiff = Quaternion.Angle(transform.rotation, bot.defaultPosition.rotation);
            
            if (angleDiff < 5f)
            {
                transform.rotation = bot.defaultPosition.rotation;
                Debug.Log($"{bot.gameObject.name} rotation fixed");
                bot.ChangeState(new IdleState(bot));
            }
            else
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, bot.defaultPosition.rotation, bot.rotationSpeed * Time.deltaTime * 2f);
            }
            return;
        }
        
        // Normal hareket
        Vector3 direction = bot.defaultPosition.position - transform.position;
        direction.y = 0;
        
        float distance = direction.magnitude;
        
        // Hedefe ulaştık mı?
        if (distance < bot.stoppingDistance)
        {
            // Pozisyona ulaştık, rotasyonu da düzelt
            transform.rotation = Quaternion.Slerp(transform.rotation, bot.defaultPosition.rotation, bot.rotationSpeed * Time.deltaTime);
            
            // Rotasyon da tamamsa idle'a geç
            float angleDiff = Quaternion.Angle(transform.rotation, bot.defaultPosition.rotation);
            if (angleDiff < 5f)
            {
                transform.rotation = bot.defaultPosition.rotation;
                Debug.Log($"{bot.gameObject.name} reached default position");
                bot.ChangeState(new IdleState(bot));
            }
            return;
        }
        
        // Hareket et
        Vector3 movement = direction.normalized * bot.moveSpeed * Time.deltaTime;
        transform.position += movement;
        
        // Rotasyon
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, bot.rotationSpeed * Time.deltaTime);
        }
    }
    
    public override void Exit()
    {
        bot.isReturningToDefault = false;
        if (isRotationOnly)
        {
            Debug.Log($"{bot.gameObject.name} finished rotation adjustment");
        }
        else
        {
            Debug.Log($"{bot.gameObject.name} stopped returning to default");
        }
    }
}