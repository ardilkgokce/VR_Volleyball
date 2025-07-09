using UnityEngine;
using System.Collections;

public class BotAnimationController : MonoBehaviour
{
    [Header("Animation References")]
    [SerializeField] private Animator animator;
    
    [Header("Animation States")]
    [SerializeField] private string idleStateName = "Idle";
    [SerializeField] private string runStateName = "Run_F";
    [SerializeField] private string volleyStateName = "BX_Volleying_01";
    
    [Header("Animation Parameters")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string isRunningParam = "IsRunning";
    [SerializeField] private string volleyTrigger = "Volley";
    
    [Header("Animation Sync Settings")]
    [Tooltip("Animasyonun başlangıç frame'i (vuruş anından başlasın)")]
    [SerializeField] private float volleyStartTime = 0.467f; // 14 frame / 30 fps
    [Tooltip("Animasyonun toplam süresi")]
    [SerializeField] private float volleyAnimationDuration = 1f;
    [Tooltip("Animasyon geçiş süresi")]
    [SerializeField] private float transitionTime = 0.1f;
    
    // Animation states
    private bool isPerformingVolley = false;
    private float currentSpeed = 0f;
    private Coroutine volleyCoroutine;
    
    // Events
    public System.Action OnVolleyAnimationComplete;
    
    void Awake()
    {
        // Animator'ı bul
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }
        
        if (animator == null)
        {
            Debug.LogError($"{gameObject.name}: Animator component not found! Animations will not work.");
        }
    }
    
    /// <summary>
    /// Hareket hızını günceller ve uygun animasyonu oynatır
    /// </summary>
    public void UpdateMovementAnimation(float moveSpeed, bool isMoving)
    {
        if (animator == null || isPerformingVolley) return;
        
        currentSpeed = moveSpeed;
        
        // Animator parametrelerini güncelle
        animator.SetFloat(speedParam, currentSpeed);
        animator.SetBool(isRunningParam, isMoving && currentSpeed > 0.1f);
    }
    
    /// <summary>
    /// Idle animasyonuna geçer
    /// </summary>
    public void PlayIdleAnimation()
    {
        if (animator == null || isPerformingVolley) return;
        
        animator.SetFloat(speedParam, 0f);
        animator.SetBool(isRunningParam, false);
    }
    
    /// <summary>
    /// Koşma animasyonunu oynatır
    /// </summary>
    public void PlayRunAnimation(float speed)
    {
        if (animator == null || isPerformingVolley) return;
        
        animator.SetFloat(speedParam, speed);
        animator.SetBool(isRunningParam, true);
    }
    
    /// <summary>
    /// Voleybol vuruş animasyonunu başlatır
    /// </summary>
    public void PlayVolleyAnimation()
    {
        if (animator == null) return;
        
        // Önceki volley animasyonunu iptal et
        if (volleyCoroutine != null)
        {
            StopCoroutine(volleyCoroutine);
        }
        
        // Yeni animasyonu başlat
        volleyCoroutine = StartCoroutine(VolleyAnimationSequence());
    }
    
    /// <summary>
    /// Voleybol animasyon dizisi
    /// </summary>
    private IEnumerator VolleyAnimationSequence()
    {
        isPerformingVolley = true;
        
        // Animasyonu trigger et
        animator.SetTrigger(volleyTrigger);
        
        // Bir frame bekle ki state geçişi tamamlansın
        yield return null;
        
        // Animasyonu belirli bir zamandan başlat
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // State geçişinin tamamlanmasını bekle
        float timeoutCounter = 0f;
        while (!stateInfo.IsName(volleyStateName) && timeoutCounter < 0.5f)
        {
            yield return null;
            timeoutCounter += Time.deltaTime;
            stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        }
        
        // Eğer volley state'ine geçtiyse
        if (stateInfo.IsName(volleyStateName))
        {
            // Animasyonu belirtilen zamandan başlat (normalizedTime 0-1 arası)
            float normalizedStartTime = volleyStartTime / volleyAnimationDuration;
            animator.Play(volleyStateName, 0, normalizedStartTime);
        }
        
        // Animasyonun kalan süresini bekle
        float remainingTime = volleyAnimationDuration - volleyStartTime;
        yield return new WaitForSeconds(remainingTime);
        
        isPerformingVolley = false;
        volleyCoroutine = null;
        
        // Event'i tetikle
        OnVolleyAnimationComplete?.Invoke();
    }
    
    /// <summary>
    /// Karakteri belirtilen hedefe doğru döndürür
    /// </summary>
    public void LookAt(Vector3 targetPosition, float rotationSpeed)
    {
        
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
    
    /// <summary>
    /// Animasyon durumunu kontrol eder
    /// </summary>
    public bool IsPerformingVolley()
    {
        return isPerformingVolley;
    }
    
    /// <summary>
    /// Mevcut animasyon state'ini döndürür
    /// </summary>
    public string GetCurrentAnimationState()
    {
        if (animator == null) return "None";
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        if (stateInfo.IsName(idleStateName)) return "Idle";
        if (stateInfo.IsName(runStateName)) return "Running";
        if (stateInfo.IsName(volleyStateName)) return "Volleying";
        
        return "Unknown";
    }
    
    /// <summary>
    /// Animasyon hızını ayarlar (slow motion efekti için)
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }
    
    /// <summary>
    /// Tüm animasyonları durdurur ve idle'a döner
    /// </summary>
    public void ResetAnimations()
    {
        if (volleyCoroutine != null)
        {
            StopCoroutine(volleyCoroutine);
            volleyCoroutine = null;
        }
        
        isPerformingVolley = false;
        PlayIdleAnimation();
        SetAnimationSpeed(1f);
    }
    
    /// <summary>
    /// Animasyon parametrelerini debug için yazdırır
    /// </summary>
    public void DebugAnimationState()
    {
        if (animator == null) return;
        
        Debug.Log($"[{gameObject.name}] Animation State: {GetCurrentAnimationState()}, " +
                  $"Speed: {animator.GetFloat(speedParam):F2}, " +
                  $"IsRunning: {animator.GetBool(isRunningParam)}, " +
                  $"IsPerformingVolley: {isPerformingVolley}");
    }
    
    void OnValidate()
    {
        // Inspector'da değerler değiştiğinde kontrol et
        if (volleyStartTime < 0) volleyStartTime = 0;
        if (volleyAnimationDuration <= 0) volleyAnimationDuration = 1f;
        if (transitionTime < 0) transitionTime = 0;
    }
}