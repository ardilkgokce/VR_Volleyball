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
    public TMP_InputField nameInputField;
    public TMP_InputField surnameInputField;
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
    
    // Player data structure - new fields can be added later
    [System.Serializable]
    public class PlayerData
    {
        public string name;
        public string surname;
        public string phone;
        public string email;
        public string date;
        public string time;
        // For future expandability
        public Dictionary<string, string> extraFields = new Dictionary<string, string>();
        
        public PlayerData()
        {
            name = "null";
            surname = "null";
            phone = "null";
            email = "null";
            date = DateTime.Now.ToString("yyyy-MM-dd");
            time = DateTime.Now.ToString("HH:mm:ss");
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
            // CSV header row
            string header = "Name,Surname,Phone,Email,Date,Time\n";
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
        newPlayer.name = string.IsNullOrEmpty(nameInputField.text) ? "null" : nameInputField.text.Trim();
        newPlayer.surname = string.IsNullOrEmpty(surnameInputField.text) ? "null" : surnameInputField.text.Trim();
        newPlayer.phone = string.IsNullOrEmpty(phoneInputField.text) ? "null" : phoneInputField.text.Trim();
        newPlayer.email = string.IsNullOrEmpty(emailInputField.text) ? "null" : emailInputField.text.Trim();
        
        // Create CSV format line
        string csvLine = ConvertPlayerToCSVLine(newPlayer);
        
        // Append to file
        AppendToFile(csvLine);
        
        // Clear input fields (optional)
        ClearInputs();
        
        // User feedback
        Debug.Log("Data saved: " + csvLine);
        
        // Load Beach scene after saving data
        SceneManager.LoadScene(targetSceneName);
    }
    
    string ConvertPlayerToCSVLine(PlayerData player)
    {
        // Handle CSV special characters (comma, quotes etc.)
        string name = FormatCSVSafe(player.name);
        string surname = FormatCSVSafe(player.surname);
        string phone = FormatCSVSafe(player.phone);
        string email = FormatCSVSafe(player.email);
        
        return string.Format("{0},{1},{2},{3},{4},{5}\n", 
            name, surname, phone, email, player.date, player.time);
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
        surnameInputField.text = "";
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
                
                if (values.Length >= 6)
                {
                    PlayerData player = new PlayerData();
                    player.name = values[0];
                    player.surname = values[1];
                    player.phone = values[2];
                    player.email = values[3];
                    player.date = values[4];
                    player.time = values[5];
                    
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
        test.name = "Test";
        test.surname = "User";
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
    
    // Cleanup on destroy
    void OnDestroy()
    {
        if (aButtonAction != null)
        {
            aButtonAction.performed -= OnAButtonPressed;
            aButtonAction.Disable();
        }
    }
}