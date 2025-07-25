using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Bot Prefabs")]
    [Tooltip("Farklı bot modellerini buraya ekleyin")]
    public GameObject[] botPrefabVariants;
    
    [Header("Team Bot Lists")]
    [Tooltip("Blue takım için kullanılacak bot prefab'ları")]
    public List<GameObject> blueTeamBotPrefabs = new List<GameObject>();
    
    [Tooltip("Red takım için kullanılacak bot prefab'ları")]
    public List<GameObject> redTeamBotPrefabs = new List<GameObject>();
    
    [Header("Ball Prefab")]
    public GameObject ballPrefab;
    
    [Header("Team Positions")]
    public Transform[] blueTeamPositions;
    public Transform[] redTeamPositions;
    
    [Header("Bot Spawn Settings")]
    [Tooltip("Y pozisyonu offset'i - botların ayaklarının yere değmesi için")]
    public float botGroundOffset = 0.07096839f;
    
    [Tooltip("Rastgele bot seçimi yapılsın mı?")]
    public bool useRandomBotSelection = true;
    
    [Header("VR Player")]
    public Transform vrPlayerPosition;
    public GameObject vrPlayerPrefab;
    
    [Header("Score System")]
    [SerializeField] private int redTeamScore = 0;
    [SerializeField] private int blueTeamScore = 0;
    public int winningScore = 21; // Normal set için 21 puan
    public int finalSetWinningScore = 15; // 5. set için 15 puan
    public int minimumDifference = 2;
    
    [Header("Set System")]
    [SerializeField] private int redTeamSets = 0;
    [SerializeField] private int blueTeamSets = 0;
    [SerializeField] private int currentSet = 1;
    public int totalSets = 5; // Best of 5
    public int setsToWin = 3; // 3 set kazanmak gerekiyor
    
    [Header("UI Elements")]
    [Tooltip("Blue takım skor text'i")]
    public TextMeshProUGUI blueTeamScoreText;
    
    [Tooltip("Blue takım set text'i")]
    public TextMeshProUGUI blueTeamSetText;
    
    [Tooltip("Red takım skor text'i")]
    public TextMeshProUGUI redTeamScoreText;
    
    [Tooltip("Red takım set text'i")]
    public TextMeshProUGUI redTeamSetText;
    
    [Tooltip("Oyun durumu text'i (set/maç sonu mesajları için)")]
    public TextMeshProUGUI gameStatusText;
    
    [Tooltip("Mevcut set numarası text'i")]
    public TextMeshProUGUI currentSetText;
    
    [Tooltip("Servis atan takım text'i")]
    public TextMeshProUGUI servingTeamText;
    
    [Header("Win Panels")]
    [Tooltip("Blue takım kazandığında gösterilecek panel")]
    public GameObject blueWinPanel;
    
    [Tooltip("Red takım kazandığında gösterilecek panel")]
    public GameObject redWinPanel;
    
    [Header("Audio System")]
    [Tooltip("Oyun başlangıç düdüğü")]
    public AudioClip gameStartWhistle;
    [Tooltip("Oyun bitiş düdüğü")]
    public AudioClip gameEndWhistle;
    [Tooltip("Sayı kazanma sesi")]
    public AudioClip scoreSound;
    [Tooltip("Set kazanma sesi")]
    public AudioClip setWinSound;
    [Tooltip("Maç kazanma sesi")]
    public AudioClip matchWinSound;
    private AudioSource audioSource;
    
    [Header("Scene Management")]
    [Tooltip("Oyun bittiğinde dönülecek sahne adı")]
    public string mainMenuSceneName = "MainMenu";
    [Tooltip("Ana menüye dönmeden önceki bekleme süresi")]
    public float returnToMenuDelay = 5f;
    
    [Header("Game State")]
    public bool isGameActive = true;
    private bool isRallyActive = true;
    private bool isMatchActive = true; // Maç devam ediyor mu
    private Team servingTeam = Team.Blue;
    private Team lastSetWinner = Team.Blue; // Son seti kazanan takım
    private bool isFirstRally = true; // İlk rally kontrolü için
    
    [Header("XRI Input Actions")]
    [SerializeField] private InputActionAsset xriInputActions;
    private InputAction restartAction;
    
    // Event sistemi
    public delegate void ScoreChangedEvent(Team team, int redScore, int blueScore);
    public static event ScoreChangedEvent OnScoreChanged;
    
    public delegate void SetEndedEvent(Team winnerTeam, int setNumber);
    public static event SetEndedEvent OnSetEnded;
    
    public delegate void MatchEndedEvent(Team winnerTeam, int redSets, int blueSets);
    public static event MatchEndedEvent OnMatchEnded;
    
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
        
        // Audio Source ekle
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Bot listelerini otomatik doldur (eğer boşlarsa)
        if (blueTeamBotPrefabs.Count == 0 && botPrefabVariants != null && botPrefabVariants.Length > 0)
        {
            Debug.Log("Blue team bot list is empty. Using default bot prefab.");
            for (int i = 0; i < blueTeamPositions.Length; i++)
            {
                blueTeamBotPrefabs.Add(botPrefabVariants[0]);
            }
        }
        
        if (redTeamBotPrefabs.Count == 0 && botPrefabVariants != null && botPrefabVariants.Length > 0)
        {
            Debug.Log("Red team bot list is empty. Using default bot prefab.");
            for (int i = 0; i < redTeamPositions.Length; i++)
            {
                redTeamBotPrefabs.Add(botPrefabVariants[0]);
            }
        }
    }
    
    void Start()
    {
        ValidateUIElements();
        
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
        
        UpdateAllUI();
    }
    
    void ValidateUIElements()
    {
        // UI elementlerinin atanıp atanmadığını kontrol et
        if (blueTeamScoreText == null)
            Debug.LogWarning("Blue Team Score Text is not assigned!");
        
        if (blueTeamSetText == null)
            Debug.LogWarning("Blue Team Set Text is not assigned!");
            
        if (redTeamScoreText == null)
            Debug.LogWarning("Red Team Score Text is not assigned!");
            
        if (redTeamSetText == null)
            Debug.LogWarning("Red Team Set Text is not assigned!");
            
        if (gameStatusText == null)
            Debug.LogWarning("Game Status Text is not assigned!");
            
        if (currentSetText == null)
            Debug.LogWarning("Current Set Text is not assigned!");
            
        if (servingTeamText == null)
            Debug.LogWarning("Serving Team Text is not assigned!");
            
        if (blueWinPanel == null)
            Debug.LogWarning("Blue Win Panel is not assigned!");
            
        if (redWinPanel == null)
            Debug.LogWarning("Red Win Panel is not assigned!");
        
        // Win panellerini başlangıçta kapat
        if (blueWinPanel != null)
            blueWinPanel.SetActive(false);
            
        if (redWinPanel != null)
            redWinPanel.SetActive(false);
    }
    
    GameObject GetBotPrefabForPosition(Team team, int positionIndex)
    {
        GameObject selectedPrefab = null;
        
        if (team == Team.Blue)
        {
            if (useRandomBotSelection && botPrefabVariants != null && botPrefabVariants.Length > 0)
            {
                // Rastgele seçim
                selectedPrefab = botPrefabVariants[Random.Range(0, botPrefabVariants.Length)];
            }
            else if (positionIndex < blueTeamBotPrefabs.Count && blueTeamBotPrefabs[positionIndex] != null)
            {
                // Listeden belirli pozisyon için bot al
                selectedPrefab = blueTeamBotPrefabs[positionIndex];
            }
            else if (blueTeamBotPrefabs.Count > 0)
            {
                // Listenin ilk elemanını kullan
                selectedPrefab = blueTeamBotPrefabs[0];
            }
        }
        else // Team.Red
        {
            if (useRandomBotSelection && botPrefabVariants != null && botPrefabVariants.Length > 0)
            {
                // Rastgele seçim
                selectedPrefab = botPrefabVariants[Random.Range(0, botPrefabVariants.Length)];
            }
            else if (positionIndex < redTeamBotPrefabs.Count && redTeamBotPrefabs[positionIndex] != null)
            {
                // Listeden belirli pozisyon için bot al
                selectedPrefab = redTeamBotPrefabs[positionIndex];
            }
            else if (redTeamBotPrefabs.Count > 0)
            {
                // Listenin ilk elemanını kullan
                selectedPrefab = redTeamBotPrefabs[0];
            }
        }
        
        // Fallback - eğer hala null ise ve botPrefabVariants varsa ilkini kullan
        if (selectedPrefab == null && botPrefabVariants != null && botPrefabVariants.Length > 0)
        {
            Debug.LogWarning($"No bot prefab found for {team} team position {positionIndex}, using fallback");
            selectedPrefab = botPrefabVariants[0];
        }
        
        return selectedPrefab;
    }
    
    void SetupGame()
    {
        ResetScore();
        isFirstRally = true;
        
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        bool hasVRPlayer = vrPlayer != null;
        
        int blueCount = blueTeamPositions != null ? blueTeamPositions.Length : 0;
        int redCount = redTeamPositions != null ? redTeamPositions.Length : 0;
        
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
        
        bots = new GameObject[totalBots];
        int botIndex = 0;
        
        // Blue takım botlarını oluştur
        for (int i = 0; i < blueCount; i++)
        {
            if (blueTeamPositions[i] != null)
            {
                Vector3 position = blueTeamPositions[i].position;
                // Y pozisyonunu düzelt - ayaklar yere değsin
                position.y = botGroundOffset;
                
                // Bu pozisyon için bot prefab'ını al
                GameObject botPrefab = GetBotPrefabForPosition(Team.Blue, i);
                
                if (botPrefab != null)
                {
                    bots[botIndex] = Instantiate(botPrefab, position, blueTeamPositions[i].rotation);
                    bots[botIndex].name = $"BlueBot{i + 1}_{botPrefab.name}";
                    bots[botIndex].tag = "Bot";
                    
                    BotController controller = bots[botIndex].GetComponent<BotController>();
                    controller.team = Team.Blue;
                    
                    // Default pozisyonu ayarla
                    controller.SetDefaultPosition(position, blueTeamPositions[i].rotation);
                    
                    botIndex++;
                    
                    Debug.Log($"Created {bots[botIndex-1].name} at position Y: {position.y}");
                }
                else
                {
                    Debug.LogError($"No bot prefab available for Blue team position {i}!");
                }
            }
        }
        
        // Red takım botlarını oluştur
        for (int i = 0; i < redCount; i++)
        {
            if (redTeamPositions[i] != null)
            {
                Vector3 position = redTeamPositions[i].position;
                // Y pozisyonunu düzelt - ayaklar yere değsin
                position.y = botGroundOffset;
                
                // Bu pozisyon için bot prefab'ını al
                GameObject botPrefab = GetBotPrefabForPosition(Team.Red, i);
                
                if (botPrefab != null)
                {
                    bots[botIndex] = Instantiate(botPrefab, position, redTeamPositions[i].rotation);
                    bots[botIndex].name = $"RedBot{i + 1}_{botPrefab.name}";
                    bots[botIndex].tag = "Bot";
                    
                    BotController controller = bots[botIndex].GetComponent<BotController>();
                    controller.team = Team.Red;
                    
                    // Default pozisyonu ayarla
                    controller.SetDefaultPosition(position, redTeamPositions[i].rotation);
                    
                    botIndex++;
                    
                    Debug.Log($"Created {bots[botIndex-1].name} at position Y: {position.y}");
                }
                else
                {
                    Debug.LogError($"No bot prefab available for Red team position {i}!");
                }
            }
        }
        
        StartCoroutine(StartGameAfterPositioning());
    }
    
    public void AddScore(Team scoringTeam, string reason = "")
    {
        if (!isGameActive || !isRallyActive || !isMatchActive) 
        {
            Debug.Log($"Score ignored - Game Active: {isGameActive}, Rally Active: {isRallyActive}, Match Active: {isMatchActive}");
            return;
        }
        
        isRallyActive = false;
        Debug.Log("Rally ended - scoring disabled until new rally");
        
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
        
        // Sayı sesi çal
        PlaySound(scoreSound);
        
        // Sayı olduğunda tüm botları default pozisyonlarına gönder
        ReturnAllBotsToDefaultPosition();
        
        servingTeam = scoringTeam;
        
        UpdateScoreUI();
        
        OnScoreChanged?.Invoke(scoringTeam, redTeamScore, blueTeamScore);
        
        CheckSetEnd();
        
        if (isGameActive && isMatchActive)
        {
            StartCoroutine(StartNewRally(scoringTeam));
        }
    }
    
    void UpdateAllUI()
    {
        UpdateScoreUI();
        UpdateSetUI();
        UpdateGameInfoUI();
    }
    
    void UpdateScoreUI()
    {
        // Blue takım skorunu güncelle
        if (blueTeamScoreText != null)
        {
            blueTeamScoreText.text = blueTeamScore.ToString();
        }
        
        // Red takım skorunu güncelle
        if (redTeamScoreText != null)
        {
            redTeamScoreText.text = redTeamScore.ToString();
        }
    }
    
    void UpdateSetUI()
    {
        // Blue takım set skorunu güncelle
        if (blueTeamSetText != null)
        {
            blueTeamSetText.text = blueTeamSets.ToString();
        }
        
        // Red takım set skorunu güncelle
        if (redTeamSetText != null)
        {
            redTeamSetText.text = redTeamSets.ToString();
        }
    }
    
    void UpdateGameInfoUI()
    {
        // Mevcut set numarasını güncelle
        if (currentSetText != null)
        {
            int targetScore = (currentSet == 5) ? finalSetWinningScore : winningScore;
            currentSetText.text = $"Set {currentSet} - İlk {targetScore}";
        }
        
        // Servis atan takımı güncelle
        if (servingTeamText != null)
        {
            servingTeamText.text = $"Servis: {(servingTeam == Team.Red ? "KIRMIZI" : "MAVİ")}";
        }
    }
    
    void CheckSetEnd()
    {
        bool setEnded = false;
        Team setWinner = Team.Blue;
        
        // 5. set mi kontrol et
        int targetScore = (currentSet == 5) ? finalSetWinningScore : winningScore;
        
        // Set kazanma koşulları
        if (redTeamScore >= targetScore && (redTeamScore - blueTeamScore) >= minimumDifference)
        {
            setEnded = true;
            setWinner = Team.Red;
            redTeamSets++;
        }
        else if (blueTeamScore >= targetScore && (blueTeamScore - redTeamScore) >= minimumDifference)
        {
            setEnded = true;
            setWinner = Team.Blue;
            blueTeamSets++;
        }
        
        if (setEnded)
        {
            isGameActive = false;
            lastSetWinner = setWinner;
            
            // Set kazanma sesi
            PlaySound(setWinSound);
            
            Debug.Log($"SET {currentSet} ENDED! {setWinner} team wins! Set score: RED {redTeamScore} - BLUE {blueTeamScore}");
            Debug.Log($"Total Sets - RED: {redTeamSets}, BLUE: {blueTeamSets}");
            
            UpdateSetUI();
            
            OnSetEnded?.Invoke(setWinner, currentSet);
            
            // Maç bitişini kontrol et
            CheckMatchEnd();
            
            if (isMatchActive)
            {
                // Sonraki set için hazırlık
                StartCoroutine(PrepareNextSet());
            }
        }
    }
    
    void CheckMatchEnd()
    {
        if (redTeamSets >= setsToWin || blueTeamSets >= setsToWin)
        {
            isMatchActive = false;
            Team matchWinner = redTeamSets >= setsToWin ? Team.Red : Team.Blue;
            
            // Maç bitme sesi
            PlaySound(matchWinSound);
            
            ShowMatchEndUI(matchWinner);
            
            OnMatchEnded?.Invoke(matchWinner, redTeamSets, blueTeamSets);
            
            Debug.Log($"MATCH OVER! {matchWinner} team wins! Final set score: RED {redTeamSets} - BLUE {blueTeamSets}");
            
            // Ana menüye dönüş
            StartCoroutine(ReturnToMainMenu());
        }
    }
    
    IEnumerator ReturnToMainMenu()
    {
        // Oyun bitiş düdüğü
        yield return new WaitForSeconds(1f); // Kısa bir bekleme
        PlaySound(gameEndWhistle);
        
        // Belirlenen süre kadar bekle
        yield return new WaitForSeconds(returnToMenuDelay);
        
        // Ana menüye dön
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.Log($"Returning to main menu: {mainMenuSceneName}");
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("Main menu scene name is not set! Cannot return to menu.");
        }
    }
    
    IEnumerator PrepareNextSet()
    {
        // Set arası UI göster
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(true);
            string winnerColor = lastSetWinner == Team.Red ? "#FF4444" : "#0080FF";
            string winnerName = lastSetWinner.ToString().ToUpper();
            
            gameStatusText.text = $"<size=120%><b>SET {currentSet} BİTTİ!</b></size>\n" +
                                 $"<size=100%><color={winnerColor}>{winnerName} SETİ KAZANDI!</color></size>\n" +
                                 $"<size=90%>Skor: {redTeamScore} - {blueTeamScore}</size>\n" +
                                 $"<size=100%>Set Durumu: RED {redTeamSets} - {blueTeamSets} BLUE</size>\n" +
                                 $"<size=80%>Sonraki set 3 saniye sonra başlayacak...</size>";
        }
        
        // 3 saniye bekle
        yield return new WaitForSeconds(3f);
        
        // UI'ı kapat
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(false);
        }
        
        // Sonraki sete geç
        currentSet++;
        redTeamScore = 0;
        blueTeamScore = 0;
        isFirstRally = true;
        
        // Servis atan takım son seti kaybeden takım olur
        servingTeam = lastSetWinner == Team.Red ? Team.Blue : Team.Red;
        
        isGameActive = true;
        isRallyActive = true;
        
        UpdateAllUI();
        
        // Yeni rally başlat
        StartCoroutine(StartNewRally(servingTeam));
    }
    
    void ShowMatchEndUI(Team winner)
    {
        // Game status text'i kapat (artık gerekli değil)
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(false);
        }
        
        // Kazanan takımın panelini göster
        if (winner == Team.Blue)
        {
            if (blueWinPanel != null)
            {
                blueWinPanel.SetActive(true);
                Debug.Log("Blue team win panel activated!");
            }
            else
            {
                Debug.LogWarning("Blue Win Panel is not assigned!");
            }
        }
        else // Team.Red
        {
            if (redWinPanel != null)
            {
                redWinPanel.SetActive(true);
                Debug.Log("Red team win panel activated!");
            }
            else
            {
                Debug.LogWarning("Red Win Panel is not assigned!");
            }
        }
    }
    
    IEnumerator StartNewRally(Team servingTeam)
    {
        yield return new WaitForSeconds(2f);
        
        if (ball != null)
        {
            Destroy(ball);
        }
        
        isRallyActive = true;
        Debug.Log("New rally starting - scoring enabled");
        
        // İlk rally ise başlangıç düdüğünü çal
        if (isFirstRally)
        {
            isFirstRally = false;
            PlaySound(gameStartWhistle);
            yield return new WaitForSeconds(0.5f); // Düdük sesinden sonra kısa bekleme
        }
        
        Transform server = GetServerForTeam(servingTeam);
        
        if (server != null)
        {
            Vector3 ballPosition = server.position + Vector3.up * 1.5f;
            ball = Instantiate(ballPrefab, ballPosition, Quaternion.identity);
            ball.name = "Ball";
            ball.tag = "Ball";
            
            Rigidbody ballRb = ball.GetComponent<Rigidbody>();
            if (ballRb == null)
            {
                ballRb = ball.AddComponent<Rigidbody>();
            }
            ballRb.mass = 0.5f;
            ballRb.drag = 0.1f;
            ballRb.angularDrag = 0.5f;
            ballRb.useGravity = false;
            
            if (ball.GetComponent<Collider>() == null)
            {
                ball.AddComponent<SphereCollider>();
            }
            
            BotController botController = server.GetComponent<BotController>();
            if (botController != null)
            {
                botController.StartWithBall(ball, true);
                Debug.Log($"{server.name} is serving for {servingTeam} team to opponent!");
            }
        }
        else
        {
            Debug.LogError($"No bot found to serve for {servingTeam} team!");
            isRallyActive = true;
        }
    }
    
    Transform GetServerForTeam(Team team)
    {
        List<Transform> teamMembers = new List<Transform>();
        
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
        
        if (teamMembers.Count > 0)
        {
            return teamMembers[Random.Range(0, teamMembers.Count)];
        }
        
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
    
    public void ResetScore()
    {
        // Tüm skorları sıfırla
        redTeamScore = 0;
        blueTeamScore = 0;
        redTeamSets = 0;
        blueTeamSets = 0;
        currentSet = 1;
        servingTeam = Team.Blue;
        isGameActive = true;
        isRallyActive = true;
        isMatchActive = true;
        isFirstRally = true;
        
        UpdateAllUI();
        
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(false);
        }
        
        // Win panellerini kapat
        if (blueWinPanel != null)
            blueWinPanel.SetActive(false);
            
        if (redWinPanel != null)
            redWinPanel.SetActive(false);
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
    
    IEnumerator StartGameAfterPositioning()
    {
        yield return null;
        
        if (bots == null || bots.Length == 0)
        {
            Debug.LogError("Bot bulunamadı!");
            yield break;
        }
        
        GameObject vrPlayer = GameObject.FindWithTag("Player");
        VRPlayerProxy vrProxy = null;
        
        if (vrPlayer != null)
        {
            vrProxy = vrPlayer.GetComponent<VRPlayerProxy>();
        }
        
        for (int i = 0; i < bots.Length; i++)
        {
            if (bots[i] != null)
            {
                BotController controller = bots[i].GetComponent<BotController>();
                controller.canCatchBall = true;
            }
        }
        
        if (vrProxy != null)
        {
            vrProxy.canCatchBall = true;
        }
        
        isRallyActive = true;
        
        StartCoroutine(StartNewRally(servingTeam));
    }
    
    void RestartGame()
    {
        if (bots != null)
        {
            foreach (GameObject bot in bots)
            {
                if (bot != null) Destroy(bot);
            }
        }
        if (ball != null) Destroy(ball);
        
        SetupGame();
    }
    
    void RestartScene()
    {
        Debug.Log("Restarting scene...");
        
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    
    public bool IsRallyActive()
    {
        return isRallyActive;
    }
    
    public void EndRally()
    {
        isRallyActive = false;
        Debug.Log("Rally manually ended");
    }
    
    // Ses çalma metodu
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    // Tüm botları default pozisyonlarına gönder
    void ReturnAllBotsToDefaultPosition()
    {
        if (bots == null) return;
        
        foreach (GameObject bot in bots)
        {
            if (bot != null)
            {
                BotController controller = bot.GetComponent<BotController>();
                if (controller != null)
                {
                    // Bot'u default pozisyonuna gönder
                    controller.ForceReturnToDefault();
                }
            }
        }
        
        Debug.Log("All bots returning to default positions after score");
    }
}