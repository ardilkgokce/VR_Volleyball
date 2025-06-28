using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject botPrefab;
    public GameObject ballPrefab;
    
    [Header("Team Positions")]
    public Transform[] blueTeamPositions;
    public Transform[] redTeamPositions;
    
    private GameObject[] bots;
    private GameObject ball;
    
    void Start()
    {
        SetupGame();
    }
    
    void SetupGame()
    {
        // Pozisyon sayısını kontrol et
        int blueCount = blueTeamPositions != null ? blueTeamPositions.Length : 0;
        int redCount = redTeamPositions != null ? redTeamPositions.Length : 0;
        int totalBots = blueCount + redCount;
        
        if (totalBots == 0)
        {
            Debug.LogError("Takım pozisyonları atanmamış!");
            return;
        }
        
        // Bot dizisini oluştur
        bots = new GameObject[totalBots];
        int botIndex = 0;
        
        // Blue takım botlarını oluştur
        for (int i = 0; i < blueCount; i++)
        {
            if (blueTeamPositions[i] != null)
            {
                Vector3 position = blueTeamPositions[i].position;
                
                bots[botIndex] = Instantiate(botPrefab, position, Quaternion.identity);
                bots[botIndex].name = $"BlueBot{i + 1}";
                bots[botIndex].tag = "Bot";
                
                BotController controller = bots[botIndex].GetComponent<BotController>();
                controller.team = Team.Blue;
                
                botIndex++;
            }
        }
        
        // Red takım botlarını oluştur
        for (int i = 0; i < redCount; i++)
        {
            if (redTeamPositions[i] != null)
            {
                Vector3 position = redTeamPositions[i].position;
                
                bots[botIndex] = Instantiate(botPrefab, position, Quaternion.identity);
                bots[botIndex].name = $"RedBot{i + 1}";
                bots[botIndex].tag = "Bot";
                
                BotController controller = bots[botIndex].GetComponent<BotController>();
                controller.team = Team.Red;
                
                botIndex++;
            }
        }
        
        // Botların pozisyonlarına yerleşmesini bekle ve oyunu başlat
        StartCoroutine(StartGameAfterPositioning());
    }
    
    System.Collections.IEnumerator StartGameAfterPositioning()
    {
        // Bir frame bekle - botların pozisyonlarına yerleşmesi için
        yield return null;
        
        if (bots == null || bots.Length == 0)
        {
            Debug.LogError("Bot bulunamadı!");
            yield break;
        }
        
        // Rastgele bir bot seç ve başlat
        int startingBot = Random.Range(0, bots.Length);
        
        // Diğer botların yakalama iznini kapat
        for (int i = 0; i < bots.Length; i++)
        {
            if (bots[i] != null)
            {
                BotController controller = bots[i].GetComponent<BotController>();
                controller.canCatchBall = (i == startingBot);
            }
        }
        
        // Topu seçilen botun GÜNCEL pozisyonunda oluştur
        Vector3 ballPosition = bots[startingBot].transform.position + Vector3.up * 1.5f;
        ball = Instantiate(ballPrefab, ballPosition, Quaternion.identity);
        ball.name = "Ball";
        ball.tag = "Ball";
        
        // Top için Rigidbody ekle
        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null)
        {
            ballRb = ball.AddComponent<Rigidbody>();
        }
        ballRb.mass = 0.5f;
        ballRb.drag = 0.1f;
        ballRb.angularDrag = 0.5f;
        ballRb.useGravity = false;
        
        // Top için collider ekle
        if (ball.GetComponent<Collider>() == null)
        {
            ball.AddComponent<SphereCollider>();
        }
        
        // Seçilen bota topu ver
        BotController selectedController = bots[startingBot].GetComponent<BotController>();
        selectedController.StartWithBall(ball);
        
        Debug.Log($"İlk atışı yapacak bot: {bots[startingBot].name} (Takım: {selectedController.team})");
        Debug.Log($"Toplam bot sayısı: {bots.Length}");
    }
    
    void Update()
    {
        // R tuşu ile oyunu yeniden başlat
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }
    
    void RestartGame()
    {
        // Eski objeleri temizle
        if (bots != null)
        {
            foreach (GameObject bot in bots)
            {
                if (bot != null) Destroy(bot);
            }
        }
        if (ball != null) Destroy(ball);
        
        // Oyunu yeniden kur
        SetupGame();
    }
    
    void OnDrawGizmos()
    {
        // Blue takım pozisyonlarını göster
        if (blueTeamPositions != null)
        {
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);
            foreach (Transform pos in blueTeamPositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireSphere(pos.position, 0.5f);
                    Gizmos.DrawLine(pos.position, pos.position + Vector3.up * 2f);
                }
            }
        }
        
        // Red takım pozisyonlarını göster
        if (redTeamPositions != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            foreach (Transform pos in redTeamPositions)
            {
                if (pos != null)
                {
                    Gizmos.DrawWireSphere(pos.position, 0.5f);
                    Gizmos.DrawLine(pos.position, pos.position + Vector3.up * 2f);
                }
            }
        }
    }
}