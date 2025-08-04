using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
using System.IO;
using System.Text;
using System;
using UnityEngine.InputSystem;

public class RankManager : MonoBehaviour
{
    [Header("Rank UI Elements")]
    [Tooltip("10 adet rank text elementi (sırasıyla) - Canvas 1")]
    public TextMeshProUGUI[] rankTexts = new TextMeshProUGUI[10];
    
    [Tooltip("10 adet rank text elementi (sırasıyla) - Canvas 2")]
    public TextMeshProUGUI[] rankTexts2 = new TextMeshProUGUI[10];
    
    [Header("Panel References")]
    [Tooltip("Rank paneli")]
    public GameObject rankPanel;
    
    [Tooltip("Kayıt formu paneli")]
    public GameObject registrationPanel;
    
    [Header("Settings")]
    [Tooltip("CSV dosya yolu")]
    private string csvFilePath;
    
    [Header("Input")]
    [Tooltip("Klavye tuşu ile panel geçişi")]
    public Key switchPanelKey = Key.Space;
    
    // Oyuncu listesi
    private List<PlayerScore> playerScores = new List<PlayerScore>();
    
    // Oyuncu skor sınıfı
    [System.Serializable]
    public class PlayerScore
    {
        public string fullName;
        public int score;
        
        public PlayerScore(string name, int playerScore)
        {
            fullName = name;
            score = playerScore;
        }
    }
    
    void Start()
    {
        // CSV dosya yolunu ayarla
        csvFilePath = Path.Combine(Application.persistentDataPath, "player_data.csv");
        
        // Başlangıçta rank panelini göster
        ShowRankPanel();
        
        // Sıralamayı yükle ve göster
        LoadAndDisplayRanking();
    }
    
    void Update()
    {
        // Klavye kontrolü
        if (Keyboard.current != null && Keyboard.current[switchPanelKey].wasPressedThisFrame)
        {
            SwitchToRegistrationPanel();
        }
    }
    
    void LoadAndDisplayRanking()
    {
        // Listeyi temizle
        playerScores.Clear();
        
        // CSV dosyasını oku
        if (File.Exists(csvFilePath))
        {
            try
            {
                string[] lines = File.ReadAllLines(csvFilePath, Encoding.UTF8);
                
                // İlk satır header, atla
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] values = ParseCSVLine(lines[i]);
                    
                    if (values.Length >= 6) // Full Name, Phone, Email, Date, Time, Score
                    {
                        string name = values[0];
                        string scoreStr = values[5];
                        
                        if (int.TryParse(scoreStr, out int score))
                        {
                            // Aynı isimde oyuncu var mı kontrol et
                            var existingPlayer = playerScores.FirstOrDefault(p => p.fullName == name);
                            
                            if (existingPlayer != null)
                            {
                                // Varsa, en yüksek skoru tut
                                if (score > existingPlayer.score)
                                {
                                    existingPlayer.score = score;
                                }
                            }
                            else
                            {
                                // Yoksa yeni ekle
                                playerScores.Add(new PlayerScore(name, score));
                            }
                        }
                    }
                }
                
                // Skorlara göre sırala (büyükten küçüğe)
                playerScores = playerScores.OrderByDescending(p => p.score).ToList();
                
                // UI'ı güncelle
                UpdateRankUI();
            }
            catch (Exception e)
            {
                Debug.LogError($"CSV okuma hatası: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("CSV dosyası bulunamadı!");
            // Boş sıralama göster
            UpdateRankUI();
        }
    }
    
    void UpdateRankUI()
    {
        // Canvas 1 text'leri güncelle
        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] != null)
            {
                if (i < playerScores.Count)
                {
                    // Oyuncu varsa göster
                    string rankText = $"{i + 1}. {playerScores[i].fullName.ToUpper()} {playerScores[i].score} PUAN";
                    rankTexts[i].text = rankText;
                }
                else
                {
                    // Oyuncu yoksa boş göster
                    rankTexts[i].text = $"{i + 1}. --- --- PUAN";
                }
            }
        }
        
        // Canvas 2 text'leri güncelle
        for (int i = 0; i < rankTexts2.Length; i++)
        {
            if (rankTexts2[i] != null)
            {
                if (i < playerScores.Count)
                {
                    // Oyuncu varsa göster
                    string rankText = $"{i + 1}. {playerScores[i].fullName.ToUpper()} {playerScores[i].score} PUAN";
                    rankTexts2[i].text = rankText;
                }
                else
                {
                    // Oyuncu yoksa boş göster
                    rankTexts2[i].text = $"{i + 1}. --- --- PUAN";
                }
            }
        }
    }
    
    public void ShowRankPanel()
    {
        if (rankPanel != null)
        {
            rankPanel.SetActive(true);
            Debug.Log("Rank panel shown");
        }
        
        if (registrationPanel != null)
        {
            registrationPanel.SetActive(false);
        }
        
        // Sıralamayı yenile
        LoadAndDisplayRanking();
    }
    
    public void SwitchToRegistrationPanel()
    {
        if (rankPanel != null)
        {
            rankPanel.SetActive(false);
        }
        
        if (registrationPanel != null)
        {
            registrationPanel.SetActive(true);
            Debug.Log("Switched to registration panel");
        }
    }
    
    // CSV satırını parse et
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
                    currentValue.Append('"');
                    i++;
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
        
        result.Add(currentValue.ToString());
        return result.ToArray();
    }
    
    // Public metod - dışarıdan sıralamayı yenilemek için
    public void RefreshRanking()
    {
        LoadAndDisplayRanking();
    }
    
    // Test için - Inspector'dan çağırılabilir
    [ContextMenu("Test Ranking Display")]
    void TestRanking()
    {
        LoadAndDisplayRanking();
    }
}