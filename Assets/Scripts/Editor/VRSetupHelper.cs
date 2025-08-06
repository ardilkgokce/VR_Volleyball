using UnityEngine;
using UnityEditor;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.Management;
using UnityEditor.XR.Management;

public class VRSetupHelper : EditorWindow
{
    [MenuItem("VR Volleyball/Setup VR Support")]
    static void ShowWindow()
    {
        var window = GetWindow<VRSetupHelper>();
        window.titleContent = new GUIContent("VR Setup Helper");
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("VR Platform Setup", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Quest Support", GUILayout.Height(30)))
        {
            SetupQuestSupport();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup HTC Vive Support", GUILayout.Height(30)))
        {
            SetupViveSupport();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Setup Both Platforms", GUILayout.Height(30)))
        {
            SetupQuestSupport();
            SetupViveSupport();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Bu araç OpenXR ayarlarını otomatik olarak yapılandırır.\n" +
            "Manuel olarak da Project Settings > XR Plug-in Management > OpenXR'dan ayarlayabilirsiniz.",
            MessageType.Info
        );
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Open XR Settings"))
        {
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management/OpenXR");
        }
    }
    
    void SetupQuestSupport()
    {
        Debug.Log("Setting up Quest support...");
        
        // Android için XR ayarları
        var androidSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
        if (androidSettings != null && androidSettings.Manager != null)
        {
            // OpenXR loader'ı ekle
            var loaders = androidSettings.Manager.activeLoaders;
            bool hasOpenXR = false;
            foreach (var loader in loaders)
            {
                if (loader.GetType().Name.Contains("OpenXR"))
                {
                    hasOpenXR = true;
                    break;
                }
            }
            
            if (!hasOpenXR)
            {
                Debug.Log("OpenXR loader Android için ekleniyor...");
                // Loader ekleme kodu burada olacak
            }
        }
        
        EditorUtility.DisplayDialog("Quest Setup", 
            "Quest desteği için:\n" +
            "1. Project Settings > XR Plug-in Management'a gidin\n" +
            "2. Android sekmesinde OpenXR'ı etkinleştirin\n" +
            "3. OpenXR ayarlarında Oculus Touch Controller Profile'ı ekleyin\n" +
            "4. Meta Quest Support feature'ı etkinleştirin", 
            "Tamam");
    }
    
    void SetupViveSupport()
    {
        Debug.Log("Setting up HTC Vive support...");
        
        // Standalone için XR ayarları
        var standaloneSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if (standaloneSettings != null && standaloneSettings.Manager != null)
        {
            // OpenXR loader'ı ekle
            var loaders = standaloneSettings.Manager.activeLoaders;
            bool hasOpenXR = false;
            foreach (var loader in loaders)
            {
                if (loader.GetType().Name.Contains("OpenXR"))
                {
                    hasOpenXR = true;
                    break;
                }
            }
            
            if (!hasOpenXR)
            {
                Debug.Log("OpenXR loader Standalone için ekleniyor...");
                // Loader ekleme kodu burada olacak
            }
        }
        
        EditorUtility.DisplayDialog("Vive Setup", 
            "HTC Vive desteği için:\n" +
            "1. Project Settings > XR Plug-in Management'a gidin\n" +
            "2. PC, Mac & Linux Standalone sekmesinde OpenXR'ı etkinleştirin\n" +
            "3. OpenXR ayarlarında HTC Vive Controller Profile'ı ekleyin\n" +
            "4. Vive Tracker desteği için SteamVR Unity Plugin kurulması gerekebilir", 
            "Tamam");
    }
    
    [MenuItem("VR Volleyball/Create VR Device Selector UI")]
    static void CreateDeviceSelectorUI()
    {
        // Canvas oluştur veya bul
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        
        // Device Selection Panel
        GameObject panel = new GameObject("DeviceSelectionPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(400, 300);
        panelRect.anchoredPosition = Vector2.zero;
        
        UnityEngine.UI.Image panelImage = panel.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        
        // Title
        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        RectTransform titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.sizeDelta = new Vector2(0, 50);
        titleRect.anchoredPosition = new Vector2(0, -25);
        
        TMPro.TextMeshProUGUI titleText = titleGO.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "Select VR Input Device";
        titleText.fontSize = 24;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        
        // Quest Controller Button
        GameObject questButton = CreateButton("Quest Controllers", panel.transform, new Vector2(0, 30));
        
        // Vive Tracker Button
        GameObject viveButton = CreateButton("Vive Trackers", panel.transform, new Vector2(0, -30));
        
        // Current Device Text
        GameObject currentDeviceGO = new GameObject("CurrentDevice");
        currentDeviceGO.transform.SetParent(panel.transform, false);
        RectTransform currentRect = currentDeviceGO.AddComponent<RectTransform>();
        currentRect.anchorMin = new Vector2(0, 0);
        currentRect.anchorMax = new Vector2(1, 0);
        currentRect.sizeDelta = new Vector2(-20, 30);
        currentRect.anchoredPosition = new Vector2(0, 40);
        
        TMPro.TextMeshProUGUI currentText = currentDeviceGO.AddComponent<TMPro.TextMeshProUGUI>();
        currentText.text = "Current Device: Quest Controllers";
        currentText.fontSize = 16;
        currentText.alignment = TMPro.TextAlignmentOptions.Center;
        
        // VRDeviceSelector component ekle
        VRDeviceSelector selector = canvas.gameObject.AddComponent<VRDeviceSelector>();
        selector.deviceSelectionPanel = panel;
        selector.questControllerButton = questButton.GetComponent<UnityEngine.UI.Button>();
        selector.viveTrackerButton = viveButton.GetComponent<UnityEngine.UI.Button>();
        selector.currentDeviceText = currentText;
        
        Debug.Log("VR Device Selector UI created!");
    }
    
    static GameObject CreateButton(string text, Transform parent, Vector2 position)
    {
        GameObject buttonGO = new GameObject(text + "Button");
        buttonGO.transform.SetParent(parent, false);
        
        RectTransform buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(300, 50);
        buttonRect.anchoredPosition = position;
        
        UnityEngine.UI.Image buttonImage = buttonGO.AddComponent<UnityEngine.UI.Image>();
        buttonImage.color = Color.white;
        
        UnityEngine.UI.Button button = buttonGO.AddComponent<UnityEngine.UI.Button>();
        
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        
        TMPro.TextMeshProUGUI buttonText = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = 20;
        buttonText.alignment = TMPro.TextAlignmentOptions.Center;
        buttonText.color = Color.black;
        
        return buttonGO;
    }
}