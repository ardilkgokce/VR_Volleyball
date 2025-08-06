using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VRDeviceSelector : MonoBehaviour
{
    [Header("UI References")]
    public GameObject deviceSelectionPanel;
    public Button questControllerButton;
    public Button viveTrackerButton;
    public TextMeshProUGUI currentDeviceText;
    public Toggle autoDetectToggle;
    
    [Header("Settings")]
    public bool showOnStart = true;
    public float autoHideDelay = 5f;
    
    private VRPlayerSetup vrPlayerSetup;
    private bool isAutoDetectEnabled = true;
    
    void Start()
    {
        // VRPlayerSetup'ı bul
        vrPlayerSetup = FindObjectOfType<VRPlayerSetup>();
        if (vrPlayerSetup == null)
        {
            Debug.LogError("VRPlayerSetup not found!");
            enabled = false;
            return;
        }
        
        // Button listener'ları ekle
        if (questControllerButton != null)
            questControllerButton.onClick.AddListener(SelectQuestControllers);
            
        if (viveTrackerButton != null)
            viveTrackerButton.onClick.AddListener(SelectViveTrackers);
            
        if (autoDetectToggle != null)
        {
            autoDetectToggle.isOn = isAutoDetectEnabled;
            autoDetectToggle.onValueChanged.AddListener(OnAutoDetectToggled);
        }
        
        // VRInputManager'dan değişiklikleri dinle
        if (VRInputManager.Instance != null)
        {
            VRInputManager.Instance.OnInputDeviceChanged += OnInputDeviceChanged;
        }
        
        // Başlangıçta panel göster
        if (showOnStart && deviceSelectionPanel != null)
        {
            deviceSelectionPanel.SetActive(true);
            Invoke(nameof(HidePanel), autoHideDelay);
        }
        
        // Auto-detect başlat
        if (isAutoDetectEnabled)
        {
            AutoDetectDevices();
        }
        
        UpdateUI();
    }
    
    void OnDestroy()
    {
        if (questControllerButton != null)
            questControllerButton.onClick.RemoveListener(SelectQuestControllers);
            
        if (viveTrackerButton != null)
            viveTrackerButton.onClick.RemoveListener(SelectViveTrackers);
            
        if (autoDetectToggle != null)
            autoDetectToggle.onValueChanged.RemoveListener(OnAutoDetectToggled);
            
        if (VRInputManager.Instance != null)
        {
            VRInputManager.Instance.OnInputDeviceChanged -= OnInputDeviceChanged;
        }
    }
    
    void SelectQuestControllers()
    {
        Debug.Log("Switching to Quest Controllers");
        vrPlayerSetup.SwitchToControllers();
        UpdateUI();
        
        // Panel'i kapat
        if (deviceSelectionPanel != null)
            deviceSelectionPanel.SetActive(false);
    }
    
    void SelectViveTrackers()
    {
        Debug.Log("Switching to Vive Trackers");
        vrPlayerSetup.SwitchToTrackers();
        UpdateUI();
        
        // Panel'i kapat
        if (deviceSelectionPanel != null)
            deviceSelectionPanel.SetActive(false);
    }
    
    void OnAutoDetectToggled(bool value)
    {
        isAutoDetectEnabled = value;
        if (value)
        {
            AutoDetectDevices();
        }
    }
    
    void AutoDetectDevices()
    {
        // Bağlı cihazları kontrol et
        bool hasTrackers = false;
        bool hasControllers = false;
        
        var devices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
        UnityEngine.XR.InputDevices.GetDevices(devices);
        
        foreach (var device in devices)
        {
            if (device.name.ToLower().Contains("tracker"))
            {
                hasTrackers = true;
            }
            else if ((device.characteristics & UnityEngine.XR.InputDeviceCharacteristics.Controller) != 0)
            {
                hasControllers = true;
            }
        }
        
        // Otomatik seçim yap
        if (hasTrackers && VRInputManager.Instance.settings.inputDevice != VRInputDevice.ViveTracker)
        {
            Debug.Log("Auto-detected Vive Trackers");
            SelectViveTrackers();
        }
        else if (hasControllers && !hasTrackers && VRInputManager.Instance.settings.inputDevice != VRInputDevice.Controller)
        {
            Debug.Log("Auto-detected Controllers");
            SelectQuestControllers();
        }
    }
    
    void OnInputDeviceChanged(VRInputDevice device, bool useTrackers)
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (currentDeviceText != null)
        {
            string deviceName = VRInputManager.Instance.settings.inputDevice == VRInputDevice.ViveTracker 
                ? "Vive Trackers" : "Quest Controllers";
            currentDeviceText.text = $"Current Device: {deviceName}";
        }
        
        // Button durumlarını güncelle
        if (questControllerButton != null)
        {
            var colors = questControllerButton.colors;
            colors.normalColor = VRInputManager.Instance.settings.inputDevice == VRInputDevice.Controller 
                ? Color.green : Color.white;
            questControllerButton.colors = colors;
        }
        
        if (viveTrackerButton != null)
        {
            var colors = viveTrackerButton.colors;
            colors.normalColor = VRInputManager.Instance.settings.inputDevice == VRInputDevice.ViveTracker 
                ? Color.green : Color.white;
            viveTrackerButton.colors = colors;
        }
    }
    
    void HidePanel()
    {
        if (deviceSelectionPanel != null)
            deviceSelectionPanel.SetActive(false);
    }
    
    public void ShowPanel()
    {
        if (deviceSelectionPanel != null)
        {
            deviceSelectionPanel.SetActive(true);
            CancelInvoke(nameof(HidePanel));
            Invoke(nameof(HidePanel), autoHideDelay);
        }
    }
    
    // In-game menu'den çağrılabilir
    public void ToggleDeviceSelection()
    {
        if (deviceSelectionPanel != null)
        {
            bool isActive = !deviceSelectionPanel.activeSelf;
            deviceSelectionPanel.SetActive(isActive);
            
            if (isActive)
            {
                CancelInvoke(nameof(HidePanel));
                Invoke(nameof(HidePanel), autoHideDelay);
            }
        }
    }
}