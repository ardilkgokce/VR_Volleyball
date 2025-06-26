using UnityEngine;

public class HandTouchDetector : MonoBehaviour
{
    [Header("Hand References")]
    [SerializeField] private Transform leftHandTransform; // Sol eli Inspector'dan atayacağız
    [SerializeField] private Transform leftHandVisual; // Sol el görsel modeli (opsiyonel)
    [SerializeField] private Transform rightHandVisual; // Sağ el görsel modeli (opsiyonel)
    
    [Header("Settings")]
    [SerializeField] private MeshRenderer wallRenderer;
    [SerializeField] private Material greenMaterial;
    [SerializeField] private Material yellowMaterial;
    [SerializeField] private float touchDistance = 0.3f; // Ellerin "temas" sayılacağı mesafe
    
    [Header("Visual Debug")]
    [SerializeField] private bool showLineInGame = true;
    [SerializeField] private float lineWidth = 0.02f;
    
    private bool handsWereTouching = false;
    private LineRenderer handConnectionLine;
    
    void Start()
    {
        // Sol el referansı kontrolü
        if (leftHandTransform == null)
        {
            Debug.LogError("Left Hand Transform is not assigned! Please assign it in Inspector.");
            enabled = false;
            return;
        }
        
        // Eğer visual referanslar atanmamışsa, collider'ları bul
        if (leftHandVisual == null)
        {
            Collider leftCol = leftHandTransform.GetComponentInChildren<Collider>();
            if (leftCol != null)
            {
                leftHandVisual = leftCol.transform;
                Debug.Log($"Left hand visual auto-found: {leftHandVisual.name}");
            }
        }
        
        if (rightHandVisual == null)
        {
            Collider rightCol = GetComponentInChildren<Collider>();
            if (rightCol != null)
            {
                rightHandVisual = rightCol.transform;
                Debug.Log($"Right hand visual auto-found: {rightHandVisual.name}");
            }
        }
        
        // Wall renderer kontrolü
        if (wallRenderer == null)
        {
            Debug.LogError("Wall Renderer is not assigned! Please assign it in Inspector.");
        }
        else
        {
            Debug.Log($"Wall Renderer found: {wallRenderer.gameObject.name}");
            // Başlangıç duvar rengi
            wallRenderer.material.color = Color.red;
            Debug.Log("Initial wall color set to RED");
        }
        
        // Oyun içi çizgi oluştur
        if (showLineInGame)
        {
            CreateHandConnectionLine();
        }
        
        Debug.Log($"HandTouchDetector initialized on RIGHT HAND");
    }
    
    void CreateHandConnectionLine()
    {
        // LineRenderer için GameObject oluştur
        GameObject lineObj = new GameObject("Hand Connection Line");
        handConnectionLine = lineObj.AddComponent<LineRenderer>();
        
        // LineRenderer ayarları
        handConnectionLine.startWidth = lineWidth;
        handConnectionLine.endWidth = lineWidth;
        handConnectionLine.positionCount = 2;
        
        // Basit unlit material oluştur
        Material lineMat = new Material(Shader.Find("Unlit/Color"));
        handConnectionLine.material = lineMat;
        handConnectionLine.material.color = Color.red;
        
        // Gölge kapalı
        handConnectionLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        handConnectionLine.receiveShadows = false;
        
        Debug.Log("Hand connection line created");
    }
    
    void Update()
    {
        if (leftHandTransform == null) return;
        
        // Gerçek el pozisyonlarını al (visual veya collider pozisyonları)
        Vector3 leftPos = leftHandVisual != null ? leftHandVisual.position : leftHandTransform.position;
        Vector3 rightPos = rightHandVisual != null ? rightHandVisual.position : transform.position;
        
        // Mesafe kontrolü - gerçek el pozisyonları arası
        float distance = Vector3.Distance(rightPos, leftPos);
        bool handsAreTouching = distance <= touchDistance;
        
        // Çizgiyi güncelle (gerçek pozisyonlarla)
        UpdateHandConnectionLine(distance, handsAreTouching, rightPos, leftPos);
        
        // Durum değişimi kontrolü
        if (handsAreTouching && !handsWereTouching)
        {
            // Eller birleşti
            Debug.Log($"*** HANDS TOUCHING! Distance: {distance:F3} ***");
            
            // Topları güncelle
            UpdateAllBalls(true);
            
            // Duvar rengini değiştir
            if (wallRenderer != null)
            {
                wallRenderer.material.color = Color.yellow;
                Debug.Log("Wall color changed to YELLOW - hands touching");
            }
            
            handsWereTouching = true;
        }
        else if (!handsAreTouching && handsWereTouching)
        {
            // Eller ayrıldı
            Debug.Log($"*** HANDS SEPARATED! Distance: {distance:F3} ***");
            
            // Topları güncelle
            UpdateAllBalls(false);
            
            // Duvar rengini değiştir
            if (wallRenderer != null)
            {
                wallRenderer.material.color = Color.red;
            }
            
            handsWereTouching = false;
        }
        
        // T tuşu ile mesafe testi (debug)
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log($"Distance between hands: {distance}");
            Debug.Log($"Left pos: {leftPos}, Right pos: {rightPos}");
        }
        
        // Mesafe bazlı gradyan renk
        if (wallRenderer != null && !handsAreTouching)
        {
            // Mesafe 0.3 - 1.0 arası normalize et
            float normalizedDistance = Mathf.InverseLerp(touchDistance, 1.0f, distance);
            // Sarıdan kırmızıya geçiş
            Color currentColor = Color.Lerp(Color.yellow, Color.red, normalizedDistance);
            wallRenderer.material.color = currentColor;
        }
    }
    
    void UpdateHandConnectionLine(float distance, bool touching, Vector3 rightPos, Vector3 leftPos)
    {
        if (handConnectionLine == null || !showLineInGame) return;
        
        // Çizgi pozisyonlarını güncelle - gerçek el pozisyonları
        handConnectionLine.SetPosition(0, rightPos);
        handConnectionLine.SetPosition(1, leftPos);
        
        // Mesafeye göre renk
        if (touching)
        {
            // Yeşil - eller temas halinde
            handConnectionLine.material.color = Color.green;
        }
        else
        {
            // Mesafeye göre kırmızıdan sarıya geçiş
            float normalizedDist = Mathf.InverseLerp(1.0f, touchDistance, distance);
            Color lineColor = Color.Lerp(Color.red, Color.yellow, normalizedDist);
            handConnectionLine.material.color = lineColor;
        }
        
        // Çok uzaksa çizgiyi gizle
        handConnectionLine.enabled = distance < 2.0f;
    }
    
    void UpdateAllBalls(bool touching)
    {
        VolleyballBall[] allBalls = FindObjectsOfType<VolleyballBall>();
        foreach (var ball in allBalls)
        {
            if (ball != null)
                ball.SetHandsTouchingState(touching);
        }
        
        Debug.Log($"Updated {allBalls.Length} balls - hands touching state: {touching}");
    }
    
    // Collision tabanlı sistem (backup - mesafe sistemi çalışmazsa)
    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"{gameObject.name} collided with {collision.gameObject.name} (Tag: {collision.gameObject.tag})");
        
        // Eğer gerçekten çarpışma olursa (nadiren)
        if (collision.gameObject.CompareTag("Hand") && collision.gameObject != gameObject)
        {
            Debug.Log($"*** PHYSICAL COLLISION - HANDS REALLY TOUCHING! ***");
        }
    }
    
}