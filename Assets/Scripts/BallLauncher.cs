using UnityEngine;

public class BallLauncher : MonoBehaviour
{
    [Header("Ball Settings")]
    [SerializeField] private GameObject volleyballPrefab;
    [SerializeField] private float launchInterval = 3f; // Kaç saniyede bir top fırlatılacak
    
    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 10f;
    [SerializeField] private Vector3 launchDirection = new Vector3(0.2f, 0.8f, 0.5f);
    [SerializeField] private bool randomizeDirection = true;
    [SerializeField] private float randomAngleRange = 30f; // Derece cinsinden
    
    [Header("Auto Launch")]
    [SerializeField] private bool autoLaunch = true;
    [SerializeField] private int maxBallsInScene = 3; // Sahnede max top sayısı
    
    [Header("Debug")]
    [SerializeField] private bool showLaunchDirection = true;
    [SerializeField] private float debugLineLength = 3f;
    
    private float nextLaunchTime;
    private int currentBallCount;
    
    void Start()
    {
        // Normalize launch direction
        launchDirection = launchDirection.normalized;
        
        if (volleyballPrefab == null)
        {
            Debug.LogError("Volleyball Prefab is not assigned!");
        }
    }
    
    void Update()
    {
        // Auto launch
        if (autoLaunch && Time.time >= nextLaunchTime)
        {
            if (currentBallCount < maxBallsInScene)
            {
                LaunchBall();
                nextLaunchTime = Time.time + launchInterval;
            }
        }
        
        // Manuel fırlatma (Space tuşu)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LaunchBall();
        }
        
        // R tuşu ile reset
        if (Input.GetKeyDown(KeyCode.R))
        {
            ClearAllBalls();
        }
    }
    
    void LaunchBall()
    {
        if (volleyballPrefab == null) return;
        
        // Top oluştur
        GameObject ball = Instantiate(volleyballPrefab, transform.position, Quaternion.identity);
        
        // Rigidbody al
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null)
        {
            Debug.LogError("Ball doesn't have a Rigidbody!");
            Destroy(ball);
            return;
        }
        
        // Fırlatma yönünü hesapla
        Vector3 finalLaunchDir = launchDirection;
        if (randomizeDirection)
        {
            finalLaunchDir = RandomizeDirection(launchDirection, randomAngleRange);
        }
        
        // Kuvvet uygula
        ballRb.AddForce(finalLaunchDir * launchForce, ForceMode.Impulse);
        
        // Top sayısını takip et
        currentBallCount++;
        
        // Belirli süre sonra topu yok et (performans için)
        Destroy(ball, 10f);
        StartCoroutine(DecreaseBallCount(10f));
        
        Debug.Log($"Ball launched! Direction: {finalLaunchDir}, Force: {launchForce}");
    }
    
    Vector3 RandomizeDirection(Vector3 baseDirection, float angleRange)
    {
        // Rastgele açılar ekle
        float randomYaw = Random.Range(-angleRange, angleRange);
        float randomPitch = Random.Range(-angleRange * 0.5f, angleRange * 0.5f);
        
        // Quaternion ile döndür
        Quaternion rotation = Quaternion.Euler(randomPitch, randomYaw, 0);
        return rotation * baseDirection;
    }
    
    void ClearAllBalls()
    {
        // Sahnedeki tüm topları bul ve sil
        VolleyballBall[] balls = FindObjectsOfType<VolleyballBall>();
        foreach (var ball in balls)
        {
            Destroy(ball.gameObject);
        }
        currentBallCount = 0;
        Debug.Log("All balls cleared!");
    }
    
    System.Collections.IEnumerator DecreaseBallCount(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentBallCount--;
    }
    
    // Gizmos ile fırlatma yönünü göster
    void OnDrawGizmos()
    {
        if (!showLaunchDirection) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, launchDirection * debugLineLength);
        
        // Launcher pozisyonunu göster
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
        
        // Random açı aralığını göster
        if (randomizeDirection)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Vector3 leftBound = Quaternion.Euler(0, -randomAngleRange, 0) * launchDirection;
            Vector3 rightBound = Quaternion.Euler(0, randomAngleRange, 0) * launchDirection;
            
            Gizmos.DrawRay(transform.position, leftBound * debugLineLength);
            Gizmos.DrawRay(transform.position, rightBound * debugLineLength);
        }
    }
    
    // Inspector'da test butonu
    [ContextMenu("Launch Test Ball")]
    void TestLaunch()
    {
        LaunchBall();
    }
}