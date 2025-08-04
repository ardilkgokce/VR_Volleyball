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
    [Tooltip("10 adet rank text elementi (sırasıyla)")]
    public TextMeshProUGUI[] rankTexts = new TextMeshProUGUI[10];
    
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
        // Tüm text'leri güncelle
        for (int i = 0; i < rankTexts.Length; i++)
        {
            if (rankTexts[i] != null)
            {
                if (i < playerScores.Count)
                {
                    // Oyuncu varsa göster
                    string rankText = $"{i + 1}. {playerScores[i].fullName.ToUpper()} {playerScores[i].score} PUAN";
                    rankTexts[i].text = rankText;
                    
                    // İlk 3'ü vurgula (opsiyonel - renk değişikliği)
                    if (i < 3)
                    {
                        rankTexts[i].color = GetRankColor(i);
                    }
                    else
                    {
                        rankTexts[i].color = Color.white;
                    }
                }
                else
                {
                    // Oyuncu yoksa boş göster
                    rankTexts[i].text = $"{i + 1}. --- --- PUAN";
                    rankTexts[i].color = Color.gray;
                }
            }
        }
        
        Debug.Log($"Sıralama güncellendi. Toplam oyuncu: {playerScores.Count}");
    }
    
    Color GetRankColor(int rank)
    {
        switch (rank)
        {
            case 0: // 1. sıra - Altın
                return new Color(1f, 0.843f, 0f); // Gold
            case 1: // 2. sıra - Gümüş
                return new Color(0.75f, 0.75f, 0.75f); // Silver
            case 2: // 3. sıra - Bronz
                return new Color(0.803f, 0.498f, 0.196f); // Bronze
            default:
                return Color.white;
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