using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;
using System.IO;
using System.Text;
using System;

public class PlayerDataManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField nameInputField;  // Bu artık hem isim hem soyisim içerecek
    public TMP_InputField phoneInputField;
    public TMP_InputField emailInputField;
    public Button startButton;
    
    [Header("File Settings")]
    public string fileName = "player_data.csv";
    
    [Header("Scene Settings")]
    public string targetSceneName = "Beach";
    
    // Private variables
    private string filePath;
    private InputActionAsset xriInputActions;
    private InputAction aButtonAction;
    
    // Player data structure - updated to have single name field
    [System.Serializable]
    public class PlayerData
    {
        public string fullName;  // İsim ve soyisim birlikte
        public string phone;
        public string email;
        public string date;
        public string time;
        public string score;  // Oyun skoru
        // For future expandability
        public Dictionary<string, string> extraFields = new Dictionary<string, string>();
        
        public PlayerData()
        {
            fullName = "null";
            phone = "null";
            email = "null";
            date = DateTime.Now.ToString("yyyy-MM-dd");
            time = DateTime.Now.ToString("HH:mm:ss");
            score = "0";
        }
    }
    
    void Start()
    {
        // Set file path
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        
        // Create file with header if it doesn't exist
        if (!File.Exists(filePath))
        {
            CreateFileHeader();
        }
        
        // Add listener to start button
        if (startButton != null)
        {
            startButton.onClick.AddListener(SaveData);
        }
        
        // Setup XRI input actions
        SetupXRIInputActions();
        
        Debug.Log("CSV File Path: " + filePath);
    }
    
    void CreateFileHeader()
    {
        try
        {
            // CSV header row - updated header
            string header = "Full Name,Phone,Email,Date,Time,Score\n";
            File.WriteAllText(filePath, header, Encoding.UTF8);
            Debug.Log("CSV file created: " + filePath);
        }
        catch (Exception e)
        {
            Debug.LogError("File creation error: " + e.Message);
        }
    }
    
    void SaveData()
    {
        PlayerData newPlayer = new PlayerData();
        
        // Get data from input fields, mark as "null" if empty
        newPlayer.fullName = string.IsNullOrEmpty(nameInputField.text) ? "null" : nameInputField.text.Trim();
        newPlayer.phone = string.IsNullOrEmpty(phoneInputField.text) ? "null" : phoneInputField.text.Trim();
        newPlayer.email = string.IsNullOrEmpty(emailInputField.text) ? "null" : emailInputField.text.Trim();
        
        // PlayerPrefs'e oyuncu verilerini kaydet (oyun sonunda skor eklemek için)
        PlayerPrefs.SetString("CurrentPlayerName", newPlayer.fullName);
        PlayerPrefs.SetString("CurrentPlayerPhone", newPlayer.phone);
        PlayerPrefs.SetString("CurrentPlayerEmail", newPlayer.email);
        PlayerPrefs.SetString("CurrentPlayerDate", newPlayer.date);
        PlayerPrefs.SetString("CurrentPlayerTime", newPlayer.time);
        // CSV'deki satır numarasını da kaydet
        PlayerPrefs.SetInt("CurrentPlayerLineIndex", GetOrCreatePlayerLineIndex(newPlayer));
        PlayerPrefs.Save();
        
        // Clear input fields (optional)
        ClearInputs();
        
        // User feedback
        Debug.Log("Player data ready for game");
        
        // Load Beach scene after saving data
        SceneManager.LoadScene(targetSceneName);
    }
    
    string ConvertPlayerToCSVLine(PlayerData player)
    {
        // Handle CSV special characters (comma, quotes etc.)
        string fullName = FormatCSVSafe(player.fullName);
        string phone = FormatCSVSafe(player.phone);
        string email = FormatCSVSafe(player.email);
        
        return string.Format("{0},{1},{2},{3},{4},{5}\n", 
            fullName, phone, email, player.date, player.time, player.score);
    }
    
    string FormatCSVSafe(string value)
    {
        // If value contains comma, double quotes or newline, wrap in quotes
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            // Double the quotes
            value = value.Replace("\"", "\"\"");
            // Wrap in quotes
            return "\"" + value + "\"";
        }
        return value;
    }
    
    void AppendToFile(string line)
    {
        try
        {
            // Append to end of file
            File.AppendAllText(filePath, line, Encoding.UTF8);
            Debug.Log("Data successfully added!");
        }
        catch (Exception e)
        {
            Debug.LogError("File write error: " + e.Message);
        }
    }
    
    void ClearInputs()
    {
        nameInputField.text = "";
        phoneInputField.text = "";
        emailInputField.text = "";
    }
    
    // Optional: Read all data
    public List<PlayerData> ReadAllData()
    {
        List<PlayerData> players = new List<PlayerData>();
        
        if (!File.Exists(filePath))
            return players;
        
        try
        {
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            
            // Skip first line (header)
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = ParseCSVLine(lines[i]);
                
                if (values.Length >= 6)  // 6 alan oldu artık
                {
                    PlayerData player = new PlayerData();
                    player.fullName = values[0];
                    player.phone = values[1];
                    player.email = values[2];
                    player.date = values[3];
                    player.time = values[4];
                    player.score = values[5];
                    
                    players.Add(player);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("File read error: " + e.Message);
        }
        
        return players;
    }
    
    // Simple CSV parser (handles commas inside quotes)
    string[] ParseCSVLine(string csvLine)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder currentValue = new StringBuilder();
        
        for (int i = 0; i < csvLine.Length; i++)
        {
            char character = csvLine[i];
            
            if (character == '"')
            {
                if (inQuotes && i + 1 < csvLine.Length && csvLine[i + 1] == '"')
                {
                    // Double quote, convert to single
                    currentValue.Append('"');
                    i++; // Skip second quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (character == ',' && !inQuotes)
            {
                result.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(character);
            }
        }
        
        // Add last value
        result.Add(currentValue.ToString());
        
        return result.ToArray();
    }
    
    // For testing in editor
    [ContextMenu("Test - Add Sample Data")]
    void TestAddSampleData()
    {
        PlayerData test = new PlayerData();
        test.fullName = "Ahmet Yılmaz";  // İsim ve soyisim birlikte
        test.phone = "5551234567";
        test.email = "test@email.com";
        
        string csvLine = ConvertPlayerToCSVLine(test);
        AppendToFile(csvLine);
    }
    
    // XRI Input Action setup
    void SetupXRIInputActions()
    {
        // Try to find XRI Input Actions if not assigned
        if (xriInputActions == null)
        {
            var xrManager = UnityEngine.Object.FindObjectOfType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
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
        
        if (xriInputActions != null)
        {
            var rightHandInteraction = xriInputActions.FindActionMap("XRI RightHand Interaction");
            if (rightHandInteraction != null)
            {
                // Use Scale Toggle action for A button
                aButtonAction = rightHandInteraction.FindAction("Scale Toggle");
                if (aButtonAction != null)
                {
                    aButtonAction.Enable();
                    aButtonAction.performed += OnAButtonPressed;
                    Debug.Log("A Button (Scale Toggle) action setup complete");
                }
                else
                {
                    Debug.LogWarning("Scale Toggle action not found for A button!");
                }
            }
        }
        else
        {
            Debug.LogWarning("XRI Input Actions not found! A button will not work.");
        }
    }
    
    // A button pressed callback
    void OnAButtonPressed(InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Performed)
        {
            Debug.Log("A Button pressed - Saving data and loading scene");
            SaveData();
        }
    }
    
    // Oyuncu satırını bul veya yeni oluştur
    int GetOrCreatePlayerLineIndex(PlayerData player)
    {
        string[] lines = new string[0];
        
        if (File.Exists(filePath))
        {
            lines = File.ReadAllLines(filePath, Encoding.UTF8);
        }
        
        // Mevcut oyuncuyu ara (isim ile)
        for (int i = 1; i < lines.Length; i++) // 1'den başla (header'ı atla)
        {
            string[] values = ParseCSVLine(lines[i]);
            if (values.Length >= 1 && values[0] == player.fullName)
            {
                Debug.Log($"Found existing player at line {i}: {player.fullName}");
                return i;
            }
        }
        
        // Oyuncu bulunamadı, yeni satır ekle
        Debug.Log($"Creating new player entry: {player.fullName}");
        string csvLine = ConvertPlayerToCSVLine(player);
        AppendToFile(csvLine);
        
        // Yeni eklenen satırın index'ini döndür
        return lines.Length; // Yeni satır son satır olacak
    }
    
    // Oyun sonunda skoru güncelleme metodu
    public static void UpdatePlayerScore(int finalScore)
    {
        string playerName = PlayerPrefs.GetString("CurrentPlayerName", "null");
        string playerPhone = PlayerPrefs.GetString("CurrentPlayerPhone", "null");
        string playerEmail = PlayerPrefs.GetString("CurrentPlayerEmail", "null");
        string playerDate = PlayerPrefs.GetString("CurrentPlayerDate", DateTime.Now.ToString("yyyy-MM-dd"));
        string playerTime = PlayerPrefs.GetString("CurrentPlayerTime", DateTime.Now.ToString("HH:mm:ss"));
        int lineIndex = PlayerPrefs.GetInt("CurrentPlayerLineIndex", -1);
        
        if (lineIndex == -1)
        {
            Debug.LogError("Player line index not found!");
            return;
        }
        
        string filePath = Path.Combine(Application.persistentDataPath, "player_data.csv");
        
        try
        {
            // Tüm satırları oku
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            
            if (lineIndex >= 0 && lineIndex < lines.Length)
            {
                // Oyuncu verisini güncelle
                PlayerData player = new PlayerData();
                player.fullName = playerName;
                player.phone = playerPhone;
                player.email = playerEmail;
                player.date = playerDate;
                player.time = playerTime;
                player.score = finalScore.ToString();
                
                // Güncellenen satırı oluştur
                string updatedLine = string.Format("{0},{1},{2},{3},{4},{5}", 
                    FormatCSVSafeStatic(player.fullName), 
                    FormatCSVSafeStatic(player.phone), 
                    FormatCSVSafeStatic(player.email), 
                    player.date, 
                    player.time, 
                    player.score);
                
                // İlgili satırı güncelle
                lines[lineIndex] = updatedLine;
                
                // Dosyayı yeniden yaz
                File.WriteAllLines(filePath, lines, Encoding.UTF8);
                
                Debug.Log($"Player score updated: {playerName} - Score: {finalScore} at line {lineIndex}");
            }
            else
            {
                Debug.LogError($"Invalid line index: {lineIndex}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Score update error: " + e.Message);
        }
        
        // PlayerPrefs'i temizle
        PlayerPrefs.DeleteKey("CurrentPlayerName");
        PlayerPrefs.DeleteKey("CurrentPlayerPhone");
        PlayerPrefs.DeleteKey("CurrentPlayerEmail");
        PlayerPrefs.DeleteKey("CurrentPlayerDate");
        PlayerPrefs.DeleteKey("CurrentPlayerTime");
        PlayerPrefs.DeleteKey("CurrentPlayerLineIndex");
        PlayerPrefs.Save();
    }
    
    // Static version of FormatCSVSafe for static method
    static string FormatCSVSafeStatic(string value)
    {
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        {
            value = value.Replace("\"", "\"\"");
            return "\"" + value + "\"";
        }
        return value;
    }
}