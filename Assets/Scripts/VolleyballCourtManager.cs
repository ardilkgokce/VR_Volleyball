using UnityEngine;

public class VolleyballCourtManager : MonoBehaviour
{
    [Header("Court Dimensions")]
    public float courtLength = 18f;
    public float courtWidth = 9f;
    public float netHeight = 2.43f;
    public float lineWidth = 0.05f;
    
    [Header("Materials")]
    public Material courtMaterial;
    public Material lineMaterial;
    public Material netMaterial;
    public Material blueSideMaterial;
    public Material redSideMaterial;
    
    [Header("Game References")]
    public GameObject botPrefab;
    public GameObject ballPrefab;
    public GameManager gameManager;
    
    [Header("Debug Settings")]
    public bool showDebugGizmos = true;
    public bool showCourtBounds = true;
    public bool showTeamAreas = true;
    public bool showNetPosition = true;
    
    [Header("Renderer Toggle")]
    public bool showCourtGround = true;
    public bool showTeamSides = true;
    public bool showCourtLines = true;
    public bool showNet = true;
    public bool showNetPosts = true;
    
    // Renderer referanslarını tutmak için
    private Renderer courtGroundRenderer;
    private Renderer blueSideRenderer;
    private Renderer redSideRenderer;
    private Renderer[] lineRenderers;
    private Renderer netRenderer;
    private Renderer netPost1Renderer;
    private Renderer netPost2Renderer;
    private Renderer netTopBandRenderer;
    
    void Awake()
    {
        // Sahayı Awake'de oluştur ki GameManager Start'ta bulabilsin
        CreateCourt();
        CreateNet();
        CreateBoundaries();
    }
    
    void Start()
    {
        SetupGameManager();
        
        // Renderer referanslarını bul
        CacheRendererReferences();
        
        // Başlangıç durumunu ayarla
        ApplyRendererSettings();
    }
    
    void CreateCourt()
    {
        // Ana zemin
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Court Ground";
        ground.transform.position = new Vector3(0, -0.05f, 0);
        ground.transform.localScale = new Vector3(courtLength * 1.5f, 0.1f, courtWidth * 1.5f);
        ground.tag = "Ground"; // Ground tag'ini ekle
        
        if (courtMaterial != null)
            ground.GetComponent<Renderer>().material = courtMaterial;
        else
            ground.GetComponent<Renderer>().material.color = new Color(0.9f, 0.8f, 0.7f);
        
        // Renderer referansını kaydet
        courtGroundRenderer = ground.GetComponent<Renderer>();
        
        // Takım alanları (görsel ayrım için)
        CreateTeamSides();
        
        // Çizgiler
        CreateLines();
    }
    
    void CreateTeamSides()
    {
        float halfLength = courtLength / 2f;
        
        // Mavi takım alanı
        GameObject blueSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blueSide.name = "Blue Team Side";
        blueSide.transform.position = new Vector3(halfLength / 2f, 0.01f, 0);
        blueSide.transform.localScale = new Vector3(halfLength - lineWidth, 0.01f, courtWidth - lineWidth);
        
        if (blueSideMaterial != null)
            blueSide.GetComponent<Renderer>().material = blueSideMaterial;
        else
        {
            Material blueMat = blueSide.GetComponent<Renderer>().material;
            blueMat.color = new Color(0.2f, 0.5f, 1f, 0.3f);
        }
        
        // Renderer referansını kaydet
        blueSideRenderer = blueSide.GetComponent<Renderer>();
        
        Destroy(blueSide.GetComponent<Collider>());
        
        // Kırmızı takım alanı
        GameObject redSide = GameObject.CreatePrimitive(PrimitiveType.Cube);
        redSide.name = "Red Team Side";
        redSide.transform.position = new Vector3(-halfLength / 2f, 0.01f, 0);
        redSide.transform.localScale = new Vector3(halfLength - lineWidth, 0.01f, courtWidth - lineWidth);
        
        if (redSideMaterial != null)
            redSide.GetComponent<Renderer>().material = redSideMaterial;
        else
        {
            Material redMat = redSide.GetComponent<Renderer>().material;
            redMat.color = new Color(1f, 0.3f, 0.3f, 0.3f);
        }
        
        // Renderer referansını kaydet
        redSideRenderer = redSide.GetComponent<Renderer>();
        
        Destroy(redSide.GetComponent<Collider>());
    }
    
    void CreateLines()
    {
        // Line renderer'ları için liste
        System.Collections.Generic.List<Renderer> lineRendererList = new System.Collections.Generic.List<Renderer>();
        
        // Orta çizgi
        lineRendererList.Add(CreateLine("Center Line", new Vector3(0, 0.02f, 0), 
                  new Vector3(lineWidth * 2f, 0.01f, courtWidth)));
        
        // Dış çizgiler
        float halfLength = courtLength / 2f;
        float halfWidth = courtWidth / 2f;
        
        // Yan çizgiler
        lineRendererList.Add(CreateLine("Left Side Line", new Vector3(0, 0.02f, -halfWidth), 
                  new Vector3(courtLength, 0.01f, lineWidth)));
        lineRendererList.Add(CreateLine("Right Side Line", new Vector3(0, 0.02f, halfWidth), 
                  new Vector3(courtLength, 0.01f, lineWidth)));
        
        // Arka çizgiler
        lineRendererList.Add(CreateLine("Red Back Line", new Vector3(-halfLength, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth)));
        lineRendererList.Add(CreateLine("Blue Back Line", new Vector3(halfLength, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth)));
        
        // 3 metre çizgileri (hücum çizgisi)
        lineRendererList.Add(CreateLine("Red Attack Line", new Vector3(-3f, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth), 
                  new Color(0.8f, 0.2f, 0.2f)));
        lineRendererList.Add(CreateLine("Blue Attack Line", new Vector3(3f, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth),
                  new Color(0.2f, 0.5f, 0.8f)));
        
        // Array'e çevir
        lineRenderers = lineRendererList.ToArray();
    }
    
    Renderer CreateLine(string name, Vector3 position, Vector3 scale, Color? color = null)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.position = position;
        line.transform.localScale = scale;
        
        Renderer lineRenderer = line.GetComponent<Renderer>();
        
        if (lineMaterial != null)
            lineRenderer.material = lineMaterial;
        else
            lineRenderer.material.color = color ?? Color.white;
        
        Destroy(line.GetComponent<Collider>());
        
        return lineRenderer;
    }
    
    void CreateNet()
    {
        // File
        GameObject net = GameObject.CreatePrimitive(PrimitiveType.Cube);
        net.name = "Net";
        net.transform.position = new Vector3(0, netHeight / 2f, 0);
        net.transform.localScale = new Vector3(0.1f, netHeight, courtWidth + 1f);
        
        // File direği 1
        GameObject post1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post1.name = "Net Post 1";
        post1.transform.position = new Vector3(0, netHeight / 2f, -courtWidth / 2f - 0.5f);
        post1.transform.localScale = new Vector3(0.1f, netHeight / 2f, 0.1f);
        
        // File direği 2
        GameObject post2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post2.name = "Net Post 2";
        post2.transform.position = new Vector3(0, netHeight / 2f, courtWidth / 2f + 0.5f);
        post2.transform.localScale = new Vector3(0.1f, netHeight / 2f, 0.1f);
        
        // File üstü bantları
        GameObject topBand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        topBand.name = "Net Top Band";
        topBand.transform.position = new Vector3(0, netHeight, 0);
        topBand.transform.localScale = new Vector3(0.12f, 0.05f, courtWidth + 1f);
        
        // Renderer referanslarını kaydet
        netRenderer = net.GetComponent<Renderer>();
        netPost1Renderer = post1.GetComponent<Renderer>();
        netPost2Renderer = post2.GetComponent<Renderer>();
        netTopBandRenderer = topBand.GetComponent<Renderer>();
        
        if (netMaterial != null)
        {
            netRenderer.material = netMaterial;
            netPost1Renderer.material = netMaterial;
            netPost2Renderer.material = netMaterial;
            netTopBandRenderer.material = netMaterial;
        }
        else
        {
            netRenderer.material.color = new Color(0.9f, 0.9f, 0.9f);
            netPost1Renderer.material.color = Color.black;
            netPost2Renderer.material.color = Color.black;
            netTopBandRenderer.material.color = Color.white;
        }
        
        // File collider'ını ayarla (top çarpınca geri dönmesi için)
        BoxCollider netCollider = net.GetComponent<BoxCollider>();
        netCollider.size = new Vector3(1f, 1f, 1f);
    }
    
    void CreateBoundaries()
    {
        float boundaryHeight = 15f;
        float boundaryDistance = 20f;
        
        // Sol duvar
        CreateInvisibleWall("Left Boundary", 
            new Vector3(0, boundaryHeight / 2f, -boundaryDistance),
            new Vector3(courtLength * 2f, boundaryHeight, 0.5f));
        
        // Sağ duvar
        CreateInvisibleWall("Right Boundary", 
            new Vector3(0, boundaryHeight / 2f, boundaryDistance),
            new Vector3(courtLength * 2f, boundaryHeight, 0.5f));
        
        // Arka duvar (kırmızı)
        CreateInvisibleWall("Red Back Boundary", 
            new Vector3(-boundaryDistance, boundaryHeight / 2f, 0),
            new Vector3(0.5f, boundaryHeight, courtWidth * 3f));
        
        // Arka duvar (mavi)
        CreateInvisibleWall("Blue Back Boundary", 
            new Vector3(boundaryDistance, boundaryHeight / 2f, 0),
            new Vector3(0.5f, boundaryHeight, courtWidth * 3f));
        
        // Tavan (çok yüksek toplar için)
        CreateInvisibleWall("Ceiling", 
            new Vector3(0, boundaryHeight, 0),
            new Vector3(courtLength * 2f, 0.5f, courtWidth * 3f));
    }
    
    void CreateInvisibleWall(string name, Vector3 position, Vector3 scale)
    {
        GameObject wall = new GameObject(name);
        wall.transform.position = position;
        
        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = scale;
        
        // Layer'ı Boundary olarak ayarla (opsiyonel)
        wall.layer = LayerMask.NameToLayer("Default");
    }
    
    void SetupGameManager()
    {
        // GameManager yoksa oluştur
        if (gameManager == null)
        {
            GameObject gmObject = GameObject.Find("GameManager");
            if (gmObject == null)
            {
                gmObject = new GameObject("GameManager");
                gameManager = gmObject.AddComponent<GameManager>();
            }
            else
            {
                gameManager = gmObject.GetComponent<GameManager>();
            }
        }
        
        // GameManager ayarları
        if (gameManager != null)
        {
            gameManager.botPrefab = botPrefab;
            gameManager.ballPrefab = ballPrefab;
            // gameManager.botCount = 6; // BU SATIRI KALDIRIYORUM - Inspector'daki değeri kullan
            // gameManager.botDistance = 6f; // BU SATIRI DA KALDIRIYORUM
        }
    }
    
    // Debug gizmos'ları kontrol eden metod
    void OnDrawGizmos()
    {
        // Ana toggle kapalıysa hiçbir şey gösterme
        if (!showDebugGizmos) return;
        
        // Saha sınırlarını göster
        if (showCourtBounds)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(courtLength, 0, courtWidth));
            
            // Saha köşelerini işaretle
            float halfLength = courtLength / 2f;
            float halfWidth = courtWidth / 2f;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(new Vector3(halfLength, 0, halfWidth), 0.2f);
            Gizmos.DrawWireSphere(new Vector3(halfLength, 0, -halfWidth), 0.2f);
            Gizmos.DrawWireSphere(new Vector3(-halfLength, 0, halfWidth), 0.2f);
            Gizmos.DrawWireSphere(new Vector3(-halfLength, 0, -halfWidth), 0.2f);
        }
        
        // Takım alanları
        if (showTeamAreas)
        {
            float halfLength = courtLength / 2f;
            
            // Red takım alanı
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(new Vector3(-halfLength / 2f, 0.1f, 0), 
                               new Vector3(halfLength, 0, courtWidth));
            
            // Blue takım alanı
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);
            Gizmos.DrawWireCube(new Vector3(halfLength / 2f, 0.1f, 0), 
                               new Vector3(halfLength, 0, courtWidth));
            
            // Takım merkezlerini işaretle
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(new Vector3(-halfLength / 2f, 1f, 0), 0.5f);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(new Vector3(halfLength / 2f, 1f, 0), 0.5f);
            
            // Hücum çizgilerini vurgula
            Gizmos.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawLine(new Vector3(-3f, 0, -courtWidth/2), new Vector3(-3f, 0, courtWidth/2));
            Gizmos.color = new Color(0.2f, 0.5f, 0.8f, 0.8f);
            Gizmos.DrawLine(new Vector3(3f, 0, -courtWidth/2), new Vector3(3f, 0, courtWidth/2));
        }
        
        // File pozisyonu
        if (showNetPosition)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(new Vector3(0, 0, -courtWidth/2), new Vector3(0, netHeight, -courtWidth/2));
            Gizmos.DrawLine(new Vector3(0, 0, courtWidth/2), new Vector3(0, netHeight, courtWidth/2));
            Gizmos.DrawLine(new Vector3(0, netHeight, -courtWidth/2), new Vector3(0, netHeight, courtWidth/2));
            
            // File yüksekliğini göster
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(new Vector3(0, netHeight/2, 0), new Vector3(0.2f, netHeight, courtWidth));
            
            // File antenleri (direk pozisyonları)
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(new Vector3(0, netHeight, -courtWidth/2 - 0.5f), 0.1f);
            Gizmos.DrawWireSphere(new Vector3(0, netHeight, courtWidth/2 + 0.5f), 0.1f);
        }
    }
    
    // Debug ayarlarını runtime'da değiştirmek için
    void OnDrawGizmosSelected()
    {
        // Seçili olduğunda ek detaylar göster
        if (!showDebugGizmos) return;
        
        // Boundary duvarlarını göster
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        
        float boundaryHeight = 15f;
        float boundaryDistance = 20f;
        
        // Sol duvar
        Gizmos.DrawWireCube(new Vector3(0, boundaryHeight / 2f, -boundaryDistance),
                           new Vector3(courtLength * 2f, boundaryHeight, 0.5f));
        
        // Sağ duvar
        Gizmos.DrawWireCube(new Vector3(0, boundaryHeight / 2f, boundaryDistance),
                           new Vector3(courtLength * 2f, boundaryHeight, 0.5f));
        
        // Arka duvarlar
        Gizmos.DrawWireCube(new Vector3(-boundaryDistance, boundaryHeight / 2f, 0),
                           new Vector3(0.5f, boundaryHeight, courtWidth * 3f));
        
        Gizmos.DrawWireCube(new Vector3(boundaryDistance, boundaryHeight / 2f, 0),
                           new Vector3(0.5f, boundaryHeight, courtWidth * 3f));
        
        // Tavan
        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        Gizmos.DrawWireCube(new Vector3(0, boundaryHeight, 0),
                           new Vector3(courtLength * 2f, 0.5f, courtWidth * 3f));
    }
    
    // Public metodlar - runtime'da debug ayarlarını değiştirmek için
    public void ToggleDebugGizmos()
    {
        showDebugGizmos = !showDebugGizmos;
        Debug.Log($"Debug Gizmos: {showDebugGizmos}");
    }
    
    public void ToggleCourtBounds()
    {
        showCourtBounds = !showCourtBounds;
        Debug.Log($"Court Bounds: {showCourtBounds}");
    }
    
    public void ToggleTeamAreas()
    {
        showTeamAreas = !showTeamAreas;
        Debug.Log($"Team Areas: {showTeamAreas}");
    }
    
    public void ToggleNetPosition()
    {
        showNetPosition = !showNetPosition;
        Debug.Log($"Net Position: {showNetPosition}");
    }
    
    // Tüm debug ayarlarını sıfırla
    public void ResetDebugSettings()
    {
        showDebugGizmos = true;
        showCourtBounds = true;
        showTeamAreas = true;
        showNetPosition = true;
        Debug.Log("Debug settings reset to default");
    }
    
    // Renderer referanslarını cache'le
    void CacheRendererReferences()
    {
        // Eğer runtime'da çağrılıyorsa, renderer'ları GameObject ismi ile bul
        if (courtGroundRenderer == null)
        {
            GameObject courtGround = GameObject.Find("Court Ground");
            if (courtGround != null)
                courtGroundRenderer = courtGround.GetComponent<Renderer>();
        }
        
        if (blueSideRenderer == null)
        {
            GameObject blueSide = GameObject.Find("Blue Team Side");
            if (blueSide != null)
                blueSideRenderer = blueSide.GetComponent<Renderer>();
        }
        
        if (redSideRenderer == null)
        {
            GameObject redSide = GameObject.Find("Red Team Side");
            if (redSide != null)
                redSideRenderer = redSide.GetComponent<Renderer>();
        }
        
        if (netRenderer == null)
        {
            GameObject net = GameObject.Find("Net");
            if (net != null)
                netRenderer = net.GetComponent<Renderer>();
        }
        
        if (netPost1Renderer == null)
        {
            GameObject post1 = GameObject.Find("Net Post 1");
            if (post1 != null)
                netPost1Renderer = post1.GetComponent<Renderer>();
        }
        
        if (netPost2Renderer == null)
        {
            GameObject post2 = GameObject.Find("Net Post 2");
            if (post2 != null)
                netPost2Renderer = post2.GetComponent<Renderer>();
        }
        
        if (netTopBandRenderer == null)
        {
            GameObject topBand = GameObject.Find("Net Top Band");
            if (topBand != null)
                netTopBandRenderer = topBand.GetComponent<Renderer>();
        }
        
        // Line renderer'ları bul
        if (lineRenderers == null || lineRenderers.Length == 0)
        {
            string[] lineNames = { "Center Line", "Left Side Line", "Right Side Line", 
                                  "Red Back Line", "Blue Back Line", "Red Attack Line", "Blue Attack Line" };
            
            System.Collections.Generic.List<Renderer> foundRenderers = new System.Collections.Generic.List<Renderer>();
            
            foreach (string lineName in lineNames)
            {
                GameObject line = GameObject.Find(lineName);
                if (line != null)
                {
                    Renderer lineRenderer = line.GetComponent<Renderer>();
                    if (lineRenderer != null)
                        foundRenderers.Add(lineRenderer);
                }
            }
            
            lineRenderers = foundRenderers.ToArray();
        }
    }
    
    // Renderer ayarlarını uygula
    void ApplyRendererSettings()
    {
        // Court Ground
        if (courtGroundRenderer != null)
            courtGroundRenderer.enabled = showCourtGround;
            
        // Team Sides
        if (blueSideRenderer != null)
            blueSideRenderer.enabled = showTeamSides;
        if (redSideRenderer != null)
            redSideRenderer.enabled = showTeamSides;
            
        // Lines
        if (lineRenderers != null)
        {
            foreach (Renderer lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                    lineRenderer.enabled = showCourtLines;
            }
        }
        
        // Net
        if (netRenderer != null)
            netRenderer.enabled = showNet;
        if (netTopBandRenderer != null)
            netTopBandRenderer.enabled = showNet;
            
        // Net Posts
        if (netPost1Renderer != null)
            netPost1Renderer.enabled = showNetPosts;
        if (netPost2Renderer != null)
            netPost2Renderer.enabled = showNetPosts;
    }
    
    // Inspector'dan değişiklikleri algıla
    void OnValidate()
    {
        // Inspector'da değişiklik yapıldığında
        if (Application.isPlaying)
        {
            CacheRendererReferences();
            ApplyRendererSettings();
        }
    }
    
    // Public toggle metodları
    public void ToggleCourtGround()
    {
        showCourtGround = !showCourtGround;
        if (courtGroundRenderer != null)
            courtGroundRenderer.enabled = showCourtGround;
        Debug.Log($"Court Ground: {showCourtGround}");
    }
    
    public void ToggleTeamSides()
    {
        showTeamSides = !showTeamSides;
        if (blueSideRenderer != null)
            blueSideRenderer.enabled = showTeamSides;
        if (redSideRenderer != null)
            redSideRenderer.enabled = showTeamSides;
        Debug.Log($"Team Sides: {showTeamSides}");
    }
    
    public void ToggleCourtLines()
    {
        showCourtLines = !showCourtLines;
        if (lineRenderers != null)
        {
            foreach (Renderer lineRenderer in lineRenderers)
            {
                if (lineRenderer != null)
                    lineRenderer.enabled = showCourtLines;
            }
        }
        Debug.Log($"Court Lines: {showCourtLines}");
    }
    
    public void ToggleNet()
    {
        showNet = !showNet;
        if (netRenderer != null)
            netRenderer.enabled = showNet;
        if (netTopBandRenderer != null)
            netTopBandRenderer.enabled = showNet;
        Debug.Log($"Net: {showNet}");
    }
    
    public void ToggleNetPosts()
    {
        showNetPosts = !showNetPosts;
        if (netPost1Renderer != null)
            netPost1Renderer.enabled = showNetPosts;
        if (netPost2Renderer != null)
            netPost2Renderer.enabled = showNetPosts;
        Debug.Log($"Net Posts: {showNetPosts}");
    }
    
    // Tüm renderer'ları açar
    public void ShowAllRenderers()
    {
        showCourtGround = true;
        showTeamSides = true;
        showCourtLines = true;
        showNet = true;
        showNetPosts = true;
        ApplyRendererSettings();
        Debug.Log("All renderers enabled");
    }
    
    // Tüm renderer'ları kapatır
    public void HideAllRenderers()
    {
        showCourtGround = false;
        showTeamSides = false;
        showCourtLines = false;
        showNet = false;
        showNetPosts = false;
        ApplyRendererSettings();
        Debug.Log("All renderers disabled");
    }
}