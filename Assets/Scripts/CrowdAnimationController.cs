using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CrowdAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float minIdleTime = 1f;
    [SerializeField] private float maxIdleTime = 3f;
    
    [Header("Animation Weights")]
    [Tooltip("Animasyonların seçilme ağırlıkları")]
    [SerializeField] private AnimationWeight[] animationWeights = new AnimationWeight[]
    {
        new AnimationWeight { animationName = "Cheering", weight = 20f },
        new AnimationWeight { animationName = "Pointing", weight = 15f },
        new AnimationWeight { animationName = "Clap", weight = 25f },
        new AnimationWeight { animationName = "Victory", weight = 15f },
        new AnimationWeight { animationName = "Idle", weight = 25f }
    };
    
    [Header("Variation Settings")]
    [SerializeField] private float animationSpeedVariation = 0.2f;
    [SerializeField] private float startDelayMax = 5f;
    [SerializeField] private bool continuousMode = true;
    [SerializeField] private float continuousIdleChance = 0.3f;
    
    private Animator animator;
    private float totalWeight;
    private string currentAnimation = "Idle";
    private string lastAnimation = "";
    
    [System.Serializable]
    public class AnimationWeight
    {
        public string animationName;
        [Range(0, 100)] public float weight = 25f;
    }
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError("Animator component bulunamadı!");
            return;
        }
        
        // Toplam ağırlığı hesapla
        CalculateTotalWeight();
        
        // Animasyon hızına varyasyon ekle
        float speedMultiplier = 1f + Random.Range(-animationSpeedVariation, animationSpeedVariation);
        animator.speed = speedMultiplier;
        
        // Rastgele başlangıç gecikmesi
        float startDelay = Random.Range(0f, startDelayMax);
        
        // %50 şansla idle dışında bir animasyonla başla
        if (Random.value > 0.5f)
        {
            currentAnimation = SelectRandomAnimation();
            StartCoroutine(StartWithAnimation(startDelay));
        }
        else
        {
            StartCoroutine(StartAnimationCycle(startDelay));
        }
    }
    
    void CalculateTotalWeight()
    {
        totalWeight = 0f;
        foreach (var animWeight in animationWeights)
        {
            totalWeight += animWeight.weight;
        }
    }
    
    IEnumerator StartWithAnimation(float delay)
    {
        yield return new WaitForSeconds(delay);
        PlayAnimation(currentAnimation);
        yield return new WaitForSeconds(GetAnimationLength(currentAnimation));
        StartCoroutine(ContinuousAnimationCycle());
    }
    
    IEnumerator StartAnimationCycle(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);
        
        if (continuousMode)
        {
            StartCoroutine(ContinuousAnimationCycle());
        }
        else
        {
            StartCoroutine(ClassicAnimationCycle());
        }
    }
    
    IEnumerator ContinuousAnimationCycle()
    {
        while (true)
        {
            // Mevcut animasyon idle ise ve devam etme şansı varsa
            if (currentAnimation == "Idle" && Random.value < continuousIdleChance)
            {
                yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));
            }
            
            // Yeni animasyon seç
            string selectedAnimation = SelectDifferentAnimation();
            
            if (!string.IsNullOrEmpty(selectedAnimation))
            {
                PlayAnimation(selectedAnimation);
                
                float animLength = GetAnimationLength(selectedAnimation);
                
                if (selectedAnimation != "Idle")
                {
                    yield return new WaitForSeconds(animLength);
                }
                else
                {
                    yield return new WaitForSeconds(Random.Range(minIdleTime, maxIdleTime));
                }
            }
            
            // Kısa bir geçiş süresi
            yield return new WaitForSeconds(Random.Range(0.1f, 0.3f));
        }
    }
    
    IEnumerator ClassicAnimationCycle()
    {
        while (true)
        {
            float idleTime = Random.Range(minIdleTime, maxIdleTime);
            yield return new WaitForSeconds(idleTime);
            
            string selectedAnimation = SelectRandomAnimation();
            if (!string.IsNullOrEmpty(selectedAnimation) && selectedAnimation != "Idle")
            {
                PlayAnimation(selectedAnimation);
                yield return new WaitForSeconds(GetAnimationLength(selectedAnimation));
                PlayAnimation("Idle");
            }
        }
    }
    
    string SelectRandomAnimation()
    {
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var animWeight in animationWeights)
        {
            currentWeight += animWeight.weight;
            if (randomValue <= currentWeight)
            {
                return animWeight.animationName;
            }
        }
        
        return animationWeights[0].animationName;
    }
    
    string SelectDifferentAnimation()
    {
        int attempts = 0;
        string selected = "";
        
        do
        {
            selected = SelectRandomAnimation();
            attempts++;
        } 
        while (selected == lastAnimation && attempts < 10);
        
        return selected;
    }
    
    void PlayAnimation(string animationName)
    {
        lastAnimation = currentAnimation;
        currentAnimation = animationName;
        
        Debug.Log($"[{gameObject.name}] Playing: {animationName}");
        
        ResetAllTriggers();
        animator.SetTrigger(animationName);
    }
    
    void ResetAllTriggers()
    {
        animator.ResetTrigger("Idle");
        animator.ResetTrigger("Cheering");
        animator.ResetTrigger("Pointing");
        animator.ResetTrigger("Clap");
        animator.ResetTrigger("Victory");
    }
    
    float GetAnimationLength(string animationName)
    {
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        
        foreach (AnimationClip clip in clips)
        {
            if (clip.name == animationName || clip.name.Contains(animationName))
            {
                float length = clip.length / animator.speed;
                return length;
            }
        }
        
        return 2f;
    }
    
    [ContextMenu("Force Random Animation")]
    public void ForceRandomAnimation()
    {
        string selected = SelectDifferentAnimation();
        PlayAnimation(selected);
    }
    
    [ContextMenu("Test All Animations")]
    public void TestAllAnimations()
    {
        StartCoroutine(TestAnimationSequence());
    }
    
    IEnumerator TestAnimationSequence()
    {
        string[] testAnims = { "Idle", "Cheering", "Pointing", "Clap", "Victory" };
        
        foreach (string anim in testAnims)
        {
            Debug.Log($"Testing: {anim}");
            PlayAnimation(anim);
            yield return new WaitForSeconds(3f);
        }
    }
    
    public void SetAnimationActive(bool active)
    {
        enabled = active;
        if (!active)
        {
            StopAllCoroutines();
        }
        else
        {
            StartCoroutine(ContinuousAnimationCycle());
        }
    }
}