using UnityEngine;
using UnityEditor;

public class CreateTrackerPrefab : EditorWindow
{
    [MenuItem("VR Volleyball/Create Vive Tracker Prefab")]
    static void CreatePrefab()
    {
        // Ana GameObject
        GameObject trackerGO = new GameObject("ViveTracker_Prefab");
        
        // VRTrackerController ekle
        VRTrackerController trackerController = trackerGO.AddComponent<VRTrackerController>();
        trackerController.positionOffset = new Vector3(0, -0.05f, 0); // Bilek offset'i
        
        // VRHandController ekle (el collision için)
        VRHandController handController = trackerGO.AddComponent<VRHandController>();
        handController.hitRadius = 0.15f;
        handController.hitForceMultiplier = 2f;
        
        // Görsel temsil için basit bir küre ekle (opsiyonel)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(trackerGO.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.1f;
        
        // Collider component'ini ayarla
        SphereCollider col = visual.GetComponent<SphereCollider>();
        col.enabled = false; // VRHandController kendi collider'ını ekleyecek
        
        // Renderer'ı yarı saydam yap
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = new Color(0.5f, 0.5f, 1f, 0.5f);
            
            // Transparency için material ayarları
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0); // Alpha
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            
            renderer.material = mat;
        }
        
        // Prefab olarak kaydet
        string path = "Assets/Prefabs/VR/ViveTracker_Prefab.prefab";
        
        // Klasör yoksa oluştur
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/VR"))
            AssetDatabase.CreateFolder("Assets/Prefabs", "VR");
            
        // Prefab'i kaydet
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(trackerGO, path);
        
        // Geçici GameObject'i sil
        DestroyImmediate(trackerGO);
        
        // Prefab'i seç
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        
        Debug.Log($"Vive Tracker Prefab created at: {path}");
    }
}