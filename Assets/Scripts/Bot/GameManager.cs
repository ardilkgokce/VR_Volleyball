using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class GameManager : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject botPrefab;
    public GameObject ballPrefab;
    
    [Header("Team Positions")]
    public Transform[] blueTeamPositions;
    public Transform[] redTeamPositions;
    
    [Header("VR Player")]
    public Transform vrPlayerPosition;
    public GameObject vrPlayerPrefab;
    
    [Header("XRI Input Actions")]
    [SerializeField] private InputActionAsset xriInputActions; // XRI Default Input Actions
    private InputAction restartAction;
    
    private GameObject[] bots;
    private GameObject ball;
    
    void Start()
    {
        // XRI Input Actions'ı otomatik bul
        if (xriInputActions == null)
        {
            // XR Interaction Manager'dan al
            var xrManager = FindObjectOfType<XRInteractionManager>();
            if (xrManager != null)
            {
                var actionAssets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
                foreach (var asset in actionAssets)
                {
                    if (asset.name.Contains("XRI"))
                    {
                        xriInputActions = asset;
                        Debug.Log($"Found XRI Input Actions: {asset.name}");
                        break;
                    }
                }
            }
        }
        
        SetupInputActions();
        SetupGame();
    }
    
    void SetupInputActions()
    {
        if (xriInputActions != null)
        {
            // Mevcut bir action'ı kullan veya yeni bir action ekle
            // Option 1: Activate action'ını kullan (genelde trigger için)
            var rightHandInteraction = xriInputActions.FindActionMap("XRI RightHand Interaction");
            if (rightHandInteraction != null)
            {
                // A Butonu için Select action'ını kullanabiliriz
                restartAction = rightHandInteraction.FindAction("Select");
                if (restartAction != null)
                {
                    restartAction.Enable();
                    restartAction.performed += OnRestartButtonPressed;
                    Debug.Log("Using XRI RightHand Select action for restart");
                }
                
                // Alternatif: Activate action'ı
                var activateAction = rightHandInteraction.FindAction("Activate");
                if (activateAction != null && restartAction == null)
                {
                    restartAction = activateAction;
                    restartAction.Enable();
                    restartAction.performed += OnRestartButtonPressed;
                    Debug.Log("Using XRI RightHand Activate action for restart");
                }
            }
            
            // Option 2: UI action'larını kullan
            var uiActionMap = xriInputActions.FindActionMap("XRI UI");
            if (uiActionMap != null && restartAction == null)
            {
                var submitAction = uiActionMap.FindAction("Submit");
                if (submitAction != null)
                {
                    restartAction = submitAction;
                    restartAction.Enable();
                    restartAction.performed += OnRestartButtonPressed;
                    Debug.Log("Using XRI UI Submit action for restart");
                }
            }
        }
        else
        {
            Debug.LogWarning("XRI Input Actions not found! Please assign in inspector.");
        }
    }
    
    void OnDestroy()
    {
        if (restartAction != null)
        {
            restartAction.performed -= OnRestartButtonPressed;
            restartAction.Disable();
        }
    }
    
    void OnRestartButtonPressed(InputAction.CallbackContext context)
    {
        // Sadece button press'te çalış (hold'da değil)
        if (context.phase == InputActionPhase.Performed)
        {
            Debug.Log($"Restart button pressed via: {context.action.name}");
            RestartScene();
        }
    }
    
    void Update()
    {
        // R tuşu ile oyunu yeniden başlat (PC test için)
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        
        // Alternatif: Legacy input fallback
        if (Input.GetKeyDown(KeyCode.JoystickButton0) || // A
            Input.GetKeyDown(KeyCode.JoystickButton1) || // B
            Input.GetKeyDown(KeyCode.JoystickButton2) || // X
            Input.GetKeyDown(KeyCode.JoystickButton3))   // Y
        {
            Debug.Log("Controller button pressed via legacy input");
            RestartScene();
        }
    }
    
    void SetupGame()
    {
        // VR oyuncu var mı kontrol et
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        bool hasVRPlayer = vrPlayer != null;
        
        // Pozisyon sayısını kontrol et
        int blueCount = blueTeamPositions != null ? blueTeamPositions.Length : 0;
        int redCount = redTeamPositions != null ? redTeamPositions.Length : 0;
        
        // VR oyuncu varsa, onun takımından bir bot eksilt
        if (hasVRPlayer)
        {
            VRPlayerProxy vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
            if (vrProxy != null)
            {
                if (vrProxy.playerTeam == Team.Blue)
                {
                    blueCount = Mathf.Max(0, blueCount - 1);
                    Debug.Log("Blue takımdan 1 bot eksiltildi - VR Player var");
                }
                else
                {
                    redCount = Mathf.Max(0, redCount - 1);
                    Debug.Log("Red takımdan 1 bot eksiltildi - VR Player var");
                }
            }
        }
        
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
        
        // VR oyuncu var mı kontrol et
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        bool shouldStartWithVRPlayer = false;
        VRPlayerProxy vrProxy = null;
        
        if (vrPlayer != null)
        {
            vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
            // %50 şansla VR oyuncu veya bot başlasın
            shouldStartWithVRPlayer = Random.Range(0f, 1f) > 0.5f;
        }
        
        // TÜM botların yakalama iznini aç (başlangıçta)
        for (int i = 0; i < bots.Length; i++)
        {
            if (bots[i] != null)
            {
                BotController controller = bots[i].GetComponent<BotController>();
                controller.canCatchBall = true; // Hepsine izin ver
            }
        }
        
        // VR oyuncu varsa ona da izin ver
        if (vrProxy != null)
        {
            vrProxy.canCatchBall = true;
        }
        
        // Topu oluştur ve başlat
        if (shouldStartWithVRPlayer && vrProxy != null)
        {
            // VR oyuncunun pozisyonunda topu oluştur
            Vector3 ballPosition = vrProxy.GetTargetTransform().position + Vector3.up * 0.5f;
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
            ballRb.useGravity = true; // VR oyuncu için gravity açık
            
            // Top için collider ekle
            if (ball.GetComponent<Collider>() == null)
            {
                ball.AddComponent<SphereCollider>();
            }
            
            Debug.Log($"Oyun VR Player ile başlıyor (Takım: {vrProxy.playerTeam})");
        }
        else
        {
            // Rastgele bir bot seç ve başlat
            int startingBot = Random.Range(0, bots.Length);
            
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
        }
        
        Debug.Log($"Toplam bot sayısı: {bots.Length}");
        Debug.Log($"VR Player var: {vrPlayer != null}");
        
        // Tüm botların durumunu debug et
        foreach (GameObject bot in bots)
        {
            BotController bc = bot.GetComponent<BotController>();
            Debug.Log($"{bot.name} - CanCatch: {bc.canCatchBall}");
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
    
    void RestartScene()
    {
        Debug.Log("Restarting scene...");
        
        // Mevcut sahneyi yeniden yükle
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
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
        
        // VR Player pozisyonunu göster
        if (vrPlayerPosition != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(vrPlayerPosition.position, 0.6f);
            Gizmos.DrawLine(vrPlayerPosition.position, vrPlayerPosition.position + Vector3.up * 2.5f);
            
            // Hangi takım tarafında olduğunu göster
            Gizmos.color = vrPlayerPosition.position.x > 0 ? 
                new Color(0.2f, 0.5f, 1f, 0.8f) : 
                new Color(1f, 0.3f, 0.3f, 0.8f);
            Gizmos.DrawCube(vrPlayerPosition.position + Vector3.up * 3f, Vector3.one * 0.3f);
        }
    }
}