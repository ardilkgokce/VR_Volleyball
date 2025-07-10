using UnityEngine;
using System.Collections;

// SERVICE STATE - Animasyonlu servis atışı
public class ServiceState : BotState
{
    private GameObject ball;
    private Transform targetBot;
    private Rigidbody ballRb;
    private bool isTossing = false;
    private bool hasServed = false;
    private bool shouldPerformHit = false; // FixedUpdate'te vuruş yapılacağını belirtir
    private Vector3 servicePosition; // Servis pozisyonu
    
    // Inspector'dan ayarlanabilir servis parametreleri
    [System.Serializable]
    public class ServiceSettings
    {
        [Header("Toss Settings")]
        public float tossHeight = 2f; // Topun yukarı fırlatılma yüksekliği
        public float tossForce = 4f; // Yukarı fırlatma kuvveti
        
        [Header("Animation Timing")]
        public float animationStartDelay = 0.5f; // Topu yukarı attıktan sonra animasyon başlama süresi
        public float hitFrame = 18f; // Vuruş frame'i
        public float animationFPS = 24f; // Animasyon FPS'i
        
        [Header("Service Position")]
        public float serviceDistanceBack = 2f; // Servis için geriye gitme mesafesi
        
        [Header("Service Power")]
        public float serviceAngle = 30f; // Servis açısı (derece)
        public float servicePowerMultiplier = 1.2f; // Servis güç çarpanı
    }
    
    private ServiceSettings settings;
    private float hitTime; // Vuruş zamanı (saniye cinsinden)
    private Coroutine serviceCoroutine;
    
    public ServiceState(BotController bot, GameObject ballObj, Transform target) : base(bot) 
    {
        ball = ballObj;
        ballRb = ball.GetComponent<Rigidbody>();
        targetBot = target;
        
        // Bot'tan servis ayarlarını al (eğer varsa)
        settings = bot.serviceSettings ?? new ServiceSettings();
        hitTime = settings.hitFrame / settings.animationFPS;
    }
    
    public override void Enter()
    {
        if (ball == null || targetBot == null)
        {
            Debug.LogError("ServiceState: Missing ball or target!");
            bot.ChangeState(new IdleState(bot));
            return;
        }
        
        // Servise hazırlan
        bot.hasBall = true;
        bot.isPerformingVolley = true;
        
        // Önce topu bot'un MEVCUT pozisyonuna getir (parent yapmadan)
        ball.transform.position = bot.transform.position + Vector3.up * 1.2f;
        
        // Bot'u servis pozisyonuna taşı
        SetServicePosition();
        
        // ŞİMDİ topu bot'a parent yap ve local pozisyonu ayarla
        ball.transform.SetParent(bot.transform);
        ball.transform.localPosition = (Vector3.up * 1f) + Vector3.forward * 0.5f;
        
        // Transform senkronizasyonu
        Physics.SyncTransforms();
        
        // Hedefe doğru dön
        LookAtTarget(targetBot.position);
        
        // Servis sürecini başlat
        serviceCoroutine = bot.StartCoroutine(PerformService());
        
        Debug.Log($"{bot.gameObject.name} preparing to serve to {targetBot.name}");
    }
    
    private void SetServicePosition()
    {
        // Bot'un mevcut pozisyonunu kaydet
        Vector3 currentPosition = bot.transform.position;
        
        // Hedef servis pozisyonunu hesapla
        servicePosition = currentPosition;
        
        // Takıma göre geriye doğru hareket
        if (bot.team == Team.Red)
        {
            servicePosition.x -= settings.serviceDistanceBack; // Red takım için sola (geriye)
        }
        else
        {
            servicePosition.x += settings.serviceDistanceBack; // Blue takım için sağa (geriye)
        }
        
        // Bot'u servis pozisyonuna hareket ettir
        bot.transform.position = servicePosition;
        
        Debug.Log($"{bot.gameObject.name} moved to service position: {currentPosition} -> {servicePosition}");
    }
    
    private IEnumerator PerformService()
    {
        // 1. Aşama: Topu yukarı fırlat
        yield return new WaitForSeconds(0.5f); // Hazırlık süresi
        
        // Top pozisyonunu kontrol et (debug)
        Debug.Log($"{bot.gameObject.name} - Ball position before toss: {ball.transform.position}");
        
        TossBall();
        
        // 2. Aşama: Animasyon başlatma zamanını bekle
        yield return new WaitForSeconds(settings.animationStartDelay);
        
        // 3. Aşama: Spike animasyonunu başlat
        if (bot.animationController != null)
        {
            // Spike animasyonunu başlat
            bot.animationController.PlaySpikeAnimation();
            Debug.Log($"{bot.gameObject.name} started spike animation for service");
        }
        
        // 4. Aşama: Vuruş frame'ine kadar bekle
        float waitTime = hitTime + settings.animationStartDelay;
        yield return new WaitForSeconds(waitTime);
        
        // 5. Aşama: Vuruşu FixedUpdate'te yapmak için işaretle
        if (!hasServed)
        {
            shouldPerformHit = true;
        }
    }
    
    private void TossBall()
    {
        if (ball == null) return;
        
        isTossing = true;
        
        // Parent'tan çıkarmadan önce world pozisyonunu kaydet
        Vector3 ballWorldPos = ball.transform.position;
        
        // Topu serbest bırak
        ball.transform.SetParent(null);
        
        // World pozisyonunu geri yükle (parent değişikliği bozmasın)
        ball.transform.position = ballWorldPos;
        
        // Rigidbody'yi aktif et
        if (ballRb != null)
        {
            ballRb.useGravity = true;
            
            // Topu yukarı fırlat
            ballRb.velocity = Vector3.up * settings.tossForce;
            
            Debug.Log($"{bot.gameObject.name} tossed the ball up from position: {ballWorldPos}");
        }
    }
    
    private void PerformServiceHit()
    {
        if (ball == null || targetBot == null || hasServed || ballRb == null) return;
        
        hasServed = true;
        shouldPerformHit = false;
        
        // State güncellemeleri
        bot.hasBall = false;
        bot.catchCooldown = 0.5f;
        bot.canCatchBall = false;
        
        // Vuruş anındaki topun gerçek pozisyonunu al (yukarıda olacak)
        Vector3 currentBallPosition = ball.transform.position;
        
        // Eğer hit point varsa ve top ona yakınsa, hit point pozisyonunu kullan
        Vector3 hitPosition;
        if (bot.hitPoint != null && Vector3.Distance(currentBallPosition, bot.hitPoint.position) < 1f)
        {
            hitPosition = bot.hitPoint.position;
            ball.transform.position = hitPosition;
        }
        else
        {
            // Hit point yoksa veya uzaksa, topun mevcut pozisyonunu kullan
            hitPosition = currentBallPosition;
        }
        
        // Tüm botların yakalama iznini kapat
        BotController[] allBots = Object.FindObjectsOfType<BotController>();
        foreach (BotController b in allBots)
        {
            b.canCatchBall = false;
            b.lastThrower = bot.transform;
        }
        
        // Hedefin yakalama iznini aç
        if (targetBot.CompareTag("Player"))
        {
            VRPlayerProxy vrProxy = targetBot.GetComponent<VRPlayerProxy>();
            vrProxy.EnableCatching();
        }
        else
        {
            BotController targetController = targetBot.GetComponent<BotController>();
            targetController.EnableCatching();
        }
        
        // VolleyballBall component'ine vuruşu bildir
        VolleyballBall vbBall = ball.GetComponent<VolleyballBall>();
        if (vbBall != null)
        {
            vbBall.OnHit(bot.transform, bot.team);
        }
        
        // Hedef pozisyonu hesapla - mevcut sistemi kullan
        Vector3 targetPosition = bot.GetTargetPosition(targetBot);
        
        // VURUŞ ANINDAKİ GERÇEK POZİSYONDAN HESAPLA
        Vector3 direction = targetPosition - hitPosition;
        float horizontalDistance = new Vector3(direction.x, 0, direction.z).magnitude;
        float heightDifference = targetPosition.y - hitPosition.y;
        
        // Servis için daha düşük açı ve daha güçlü vuruş
        float angle = settings.serviceAngle * Mathf.Deg2Rad; // Inspector'dan ayarlanabilir açı
        float gravity = Mathf.Abs(Physics.gravity.y);
        
        // Hız hesaplama - yükseklik farkını da hesaba kat
        float v0 = Mathf.Sqrt((gravity * horizontalDistance * horizontalDistance) / 
                             (2 * Mathf.Cos(angle) * Mathf.Cos(angle) * 
                             (horizontalDistance * Mathf.Tan(angle) - heightDifference)));
        
        // Servis için ekstra güç ve drag kompanzasyonu
        float dragCompensation = 1f + (ballRb.drag * 0.2f * horizontalDistance / 10f);
        v0 *= dragCompensation * settings.servicePowerMultiplier;
        
        // Velocity vektörünü oluştur
        Vector3 finalVelocity = new Vector3(direction.x, 0, direction.z).normalized * v0 * Mathf.Cos(angle);
        finalVelocity.y = v0 * Mathf.Sin(angle);
        
        // Topu fırlat - VELOCITY ATAMASINI FIXEDUPDATE'TE YAP
        ballRb.velocity = finalVelocity;
        
        // Landing point hesapla - gerçek vuruş pozisyonundan
        bot.CalculateLandingPointInternal(hitPosition, finalVelocity);
        
        // Renk güncelle
        bot.UpdateBotColorInternal();
        
        // Referansları temizle
        bot.ball = null;
        bot.ballRb = null;
        
        Debug.Log($"{bot.gameObject.name} served from height {hitPosition.y:F2} to {targetBot.name}!");
    }
    
    public override void Update()
    {
        // Servis sırasında hedefe bakmaya devam et
        if (!hasServed && targetBot != null)
        {
            LookAtTarget(targetBot.position);
        }
    }
    
    public override void FixedUpdate()
    {
        // Vuruş zamanı geldiyse ve henüz yapılmadıysa
        if (shouldPerformHit && !hasServed)
        {
            PerformServiceHit();
        }
        
        // Servis tamamlandıysa state'i değiştir
        if (hasServed)
        {
            // Servisten sonra pozisyona dönüş
            if (bot.autoReturnToDefault)
            {
                bot.StartCoroutine(DelayedReturnToDefault());
            }
            else
            {
                bot.ChangeState(new IdleState(bot));
            }
        }
    }
    
    private IEnumerator DelayedReturnToDefault()
    {
        // Kısa bir süre bekle
        yield return new WaitForSeconds(0.5f);
        
        // Idle state'e geç
        bot.ChangeState(new IdleState(bot));
        
        // Sonra default pozisyona dön
        yield return new WaitForSeconds(bot.returnToDefaultDelay);
        
        if (bot.defaultPosition != null)
        {
            bot.ChangeState(new ReturningToPositionState(bot));
        }
    }
    
    public override void Exit()
    {
        // Coroutine'i durdur
        if (serviceCoroutine != null)
        {
            bot.StopCoroutine(serviceCoroutine);
        }
        
        bot.isPerformingVolley = false;
        hasServed = false;
        isTossing = false;
        shouldPerformHit = false;
        Debug.Log($"{bot.gameObject.name} finished serving");
    }
}