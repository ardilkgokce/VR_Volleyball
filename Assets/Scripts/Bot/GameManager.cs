using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro; // UI için
using System.Collections; // IEnumerator için

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
    
    [Header("Score System")]
    [SerializeField] private int redTeamScore = 0;
    [SerializeField] private int blueTeamScore = 0;
    public int winningScore = 21; // Voleybol set kazanma skoru
    public int minimumDifference = 2; // Minimum fark
    
    [Header("Score UI")]
    public string scoreTextObjectName = "ScoreText";
    public string gameStatusTextObjectName = "GameStatusText";
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI gameStatusText;
    
    [Header("Game State")]
    public bool isGameActive = true;
    private bool isRallyActive = true; // Rally kontrolü için yeni değişken
    private Team servingTeam = Team.Blue; // Servis atan takım
    
    [Header("XRI Input Actions")]
    [SerializeField] private InputActionAsset xriInputActions;
    private InputAction restartAction;
    
    // Event sistemi
    public delegate void ScoreChangedEvent(Team team, int redScore, int blueScore);
    public static event ScoreChangedEvent OnScoreChanged;
    
    public delegate void GameEndedEvent(Team winnerTeam, int redScore, int blueScore);
    public static event GameEndedEvent OnGameEnded;
    
    private GameObject[] bots;
    private GameObject ball;
    
    // Singleton pattern
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            return instance;
        }
    }
    
    void Awake()
    {
        instance = this;
    }
    
    void Start()
    {
        // UI elementlerini bul
        FindUIElements();
        
        // XRI Input Actions'ı otomatik bul
        if (xriInputActions == null)
        {
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
        
        // Başlangıç skorunu göster
        UpdateScoreUI();
    }
    
    void FindUIElements()
    {
        // Score Text'i bul
        GameObject scoreObject = GameObject.Find(scoreTextObjectName);
        if (scoreObject != null)
        {
            scoreText = scoreObject.GetComponent<TextMeshProUGUI>();
            if (scoreText == null)
            {
                Debug.LogError($"GameObject '{scoreTextObjectName}' found but doesn't have TextMeshProUGUI component!");
            }
        }
        else
        {
            Debug.LogWarning($"Cannot find GameObject named '{scoreTextObjectName}' in the scene!");
        }
        
        // Game Status Text'i bul
        GameObject statusObject = GameObject.Find(gameStatusTextObjectName);
        if (statusObject != null)
        {
            gameStatusText = statusObject.GetComponent<TextMeshProUGUI>();
            if (gameStatusText != null)
            {
                gameStatusText.gameObject.SetActive(false); // Başlangıçta gizle
            }
        }
    }
    
    // Skor ekleme metodu - VolleyballBall'dan çağrılacak
    public void AddScore(Team scoringTeam, string reason = "")
    {
        // Oyun veya rally aktif değilse skor ekleme
        if (!isGameActive || !isRallyActive) 
        {
            Debug.Log($"Score ignored - Game Active: {isGameActive}, Rally Active: {isRallyActive}");
            return;
        }
        
        // Rally'yi hemen durdur (birden fazla skor eklenmesini önle)
        isRallyActive = false;
        Debug.Log("Rally ended - scoring disabled until new rally");
        
        // Skoru artır
        if (scoringTeam == Team.Red)
        {
            redTeamScore++;
            Debug.Log($"Red team scores! Reason: {reason}. Score: {redTeamScore}-{blueTeamScore}");
        }
        else
        {
            blueTeamScore++;
            Debug.Log($"Blue team scores! Reason: {reason}. Score: {redTeamScore}-{blueTeamScore}");
        }
        
        // Servis hakkı skor alan takıma geçer
        servingTeam = scoringTeam;
        
        // UI'ı güncelle
        UpdateScoreUI();
        
        // Event'i tetikle
        OnScoreChanged?.Invoke(scoringTeam, redTeamScore, blueTeamScore);
        
        // Oyun bitişini kontrol et
        CheckGameEnd();
        
        // Rally'yi yeniden başlat
        if (isGameActive)
        {
            StartCoroutine(StartNewRally(scoringTeam));
        }
    }
    
    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            string redColor = "#FF4444";
            string blueColor = "#0080FF";
            
            scoreText.text = $"<color={redColor}>RED: {redTeamScore}</color> - <color={blueColor}>BLUE: {blueTeamScore}</color>\n" +
                            $"<size=80%>Servis: {(servingTeam == Team.Red ? "RED" : "BLUE")}</size>";
        }
    }
    
    void CheckGameEnd()
    {
        bool redWins = false;
        bool blueWins = false;
        
        // Kazanma koşulları
        if (redTeamScore >= winningScore && (redTeamScore - blueTeamScore) >= minimumDifference)
        {
            redWins = true;
        }
        else if (blueTeamScore >= winningScore && (blueTeamScore - redTeamScore) >= minimumDifference)
        {
            blueWins = true;
        }
        
        if (redWins || blueWins)
        {
            isGameActive = false;
            Team winner = redWins ? Team.Red : Team.Blue;
            
            // Oyun sonu UI'ı göster
            ShowGameEndUI(winner);
            
            // Event'i tetikle
            OnGameEnded?.Invoke(winner, redTeamScore, blueTeamScore);
            
            Debug.Log($"GAME OVER! {winner} team wins! Final score: RED {redTeamScore} - BLUE {blueTeamScore}");
        }
    }
    
    void ShowGameEndUI(Team winner)
    {
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(true);
            
            string winnerColor = winner == Team.Red ? "#FF4444" : "#0080FF";
            string winnerName = winner.ToString().ToUpper();
            
            gameStatusText.text = $"<size=150%><b>OYUN BİTTİ!</b></size>\n" +
                                 $"<size=120%><color={winnerColor}>{winnerName} KAZANDI!</color></size>\n" +
                                 $"<size=100%>Skor: {redTeamScore} - {blueTeamScore}</size>\n" +
                                 $"<size=80%>Yeniden başlatmak için R tuşuna basın</size>";
        }
    }
    
    IEnumerator StartNewRally(Team servingTeam)
    {
        // Rally başlamadan önce biraz bekle
        yield return new WaitForSeconds(2f);
        
        // Eski topu yok et
        if (ball != null)
        {
            Destroy(ball);
        }
        
        // Yeni rally için rally durumunu aktif et
        isRallyActive = true;
        Debug.Log("New rally starting - scoring enabled");
        
        // Servis atacak botu belirle (VR oyuncu servis atamaz)
        Transform server = GetServerForTeam(servingTeam);
        
        if (server != null)
        {
            // Yeni topu oluştur
            Vector3 ballPosition = server.position + Vector3.up * 1.5f;
            ball = Instantiate(ballPrefab, ballPosition, Quaternion.identity);
            ball.name = "Ball";
            ball.tag = "Ball";
            
            // Rigidbody ayarları
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                ballRb = ball.AddComponent<Rigidbody>();
            }
            ballRb.mass = 0.5f;
            ballRb.drag = 0.1f;
            ballRb.angularDrag = 0.5f;
            ballRb.useGravity = false;
            
            // Collider
            if (ball.GetComponent<Collider>() == null)
            {
                ball.AddComponent<SphereCollider>();
            }
            
            // Bot'a topu ver ve servis attığını belirt
            BotController botController = server.GetComponent<BotController>();
            if (botController != null)
            {
                // StartWithBall metoduna isServing parametresi eklenecek
                botController.StartWithBall(ball, true); // true = servis atıyor
                Debug.Log($"{server.name} is serving for {servingTeam} team to opponent!");
            }
        }
        else
        {
            Debug.LogError($"No bot found to serve for {servingTeam} team!");
            // Rally'yi tekrar aktif et
            isRallyActive = true;
        }
    }
    
    Transform GetServerForTeam(Team team)
    {
        List<Transform> teamMembers = new List<Transform>();
        
        // Sadece botları ekle (VR oyuncu servis atamaz)
        foreach (GameObject bot in bots)
        {
            if (bot != null)
            {
                BotController bc = bot.GetComponent<BotController>();
                if (bc != null && bc.team == team)
                {
                    teamMembers.Add(bot.transform);
                }
            }
        }
        
        // VR oyuncu servis atamaz, bu yüzden onu ekleMİyoruz
        
        // Rastgele bir bot seç
        if (teamMembers.Count > 0)
        {
            return teamMembers[Random.Range(0, teamMembers.Count)];
        }
        
        // Eğer bu takımda bot yoksa (sadece VR oyuncu varsa), karşı takımdan bir bot seç
        Debug.LogWarning($"No bots found in {team} team for serving. Selecting from opposite team.");
        
        Team oppositeTeam = team == Team.Red ? Team.Blue : Team.Red;
        foreach (GameObject bot in bots)
        {
            if (bot != null)
            {
                BotController bc = bot.GetComponent<BotController>();
                if (bc != null && bc.team == oppositeTeam)
                {
                    teamMembers.Add(bot.transform);
                }
            }
        }
        
        if (teamMembers.Count > 0)
        {
            return teamMembers[Random.Range(0, teamMembers.Count)];
        }
        
        return null;
    }
    
    // Skoru sıfırla
    public void ResetScore()
    {
        redTeamScore = 0;
        blueTeamScore = 0;
        servingTeam = Team.Blue;
        isGameActive = true;
        isRallyActive = true; // Rally'yi de aktif et
        
        UpdateScoreUI();
        
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(false);
        }
    }
    
    void SetupInputActions()
    {
        if (xriInputActions != null)
        {
            var rightHandInteraction = xriInputActions.FindActionMap("XRI RightHand Interaction");
            if (rightHandInteraction != null)
            {
                restartAction = rightHandInteraction.FindAction("Select");
                if (restartAction != null)
                {
                    restartAction.Enable();
                    restartAction.performed += OnRestartButtonPressed;
                    Debug.Log("Using XRI RightHand Select action for restart");
                }
                
                var activateAction = rightHandInteraction.FindAction("Activate");
                if (activateAction != null && restartAction == null)
                {
                    restartAction = activateAction;
                    restartAction.Enable();
                    restartAction.performed += OnRestartButtonPressed;
                    Debug.Log("Using XRI RightHand Activate action for restart");
                }
            }
            
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
        if (Input.GetKeyDown(KeyCode.JoystickButton0) || 
            Input.GetKeyDown(KeyCode.JoystickButton1) || 
            Input.GetKeyDown(KeyCode.JoystickButton2) || 
            Input.GetKeyDown(KeyCode.JoystickButton3))
        {
            Debug.Log("Controller button pressed via legacy input");
            RestartScene();
        }
    }
    
    void SetupGame()
    {
        // Skoru sıfırla
        ResetScore();
        
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
    
    IEnumerator StartGameAfterPositioning()
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
        VRPlayerProxy vrProxy = null;
        
        if (vrPlayer != null)
        {
            vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
        }
        
        // TÜM botların yakalama iznini aç (başlangıçta)
        for (int i = 0; i < bots.Length; i++)
        {
            if (bots[i] != null)
            {
                BotController controller = bots[i].GetComponent<BotController>();
                controller.canCatchBall = true;
            }
        }
        
        // VR oyuncu varsa ona da izin ver
        if (vrProxy != null)
        {
            vrProxy.canCatchBall = true;
        }
        
        // Rally durumunu aktif et
        isRallyActive = true;
        
        // İlk servisi başlat
        StartCoroutine(StartNewRally(servingTeam));
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
    
    // Rally durumunu kontrol eden public metod
    public bool IsRallyActive()
    {
        return isRallyActive;
    }
    
    // Rally'yi manuel olarak bitiren metod (debug için)
    public void EndRally()
    {
        isRallyActive = false;
        Debug.Log("Rally manually ended");
    }
    
}