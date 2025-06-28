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
    }
    
    void CreateCourt()
    {
        // Ana zemin
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Court Ground";
        ground.transform.position = new Vector3(0, -0.05f, 0);
        ground.transform.localScale = new Vector3(courtLength * 1.5f, 0.1f, courtWidth * 1.5f);
        
        if (courtMaterial != null)
            ground.GetComponent<Renderer>().material = courtMaterial;
        else
            ground.GetComponent<Renderer>().material.color = new Color(0.9f, 0.8f, 0.7f);
        
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
        
        Destroy(redSide.GetComponent<Collider>());
    }
    
    void CreateLines()
    {
        // Orta çizgi
        CreateLine("Center Line", new Vector3(0, 0.02f, 0), 
                  new Vector3(lineWidth * 2f, 0.01f, courtWidth));
        
        // Dış çizgiler
        float halfLength = courtLength / 2f;
        float halfWidth = courtWidth / 2f;
        
        // Yan çizgiler
        CreateLine("Left Side Line", new Vector3(0, 0.02f, -halfWidth), 
                  new Vector3(courtLength, 0.01f, lineWidth));
        CreateLine("Right Side Line", new Vector3(0, 0.02f, halfWidth), 
                  new Vector3(courtLength, 0.01f, lineWidth));
        
        // Arka çizgiler
        CreateLine("Red Back Line", new Vector3(-halfLength, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth));
        CreateLine("Blue Back Line", new Vector3(halfLength, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth));
        
        // 3 metre çizgileri (hücum çizgisi)
        CreateLine("Red Attack Line", new Vector3(-3f, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth), 
                  new Color(0.8f, 0.2f, 0.2f));
        CreateLine("Blue Attack Line", new Vector3(3f, 0.02f, 0), 
                  new Vector3(lineWidth, 0.01f, courtWidth),
                  new Color(0.2f, 0.5f, 0.8f));
    }
    
    void CreateLine(string name, Vector3 position, Vector3 scale, Color? color = null)
    {
        GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
        line.name = name;
        line.transform.position = position;
        line.transform.localScale = scale;
        
        if (lineMaterial != null)
            line.GetComponent<Renderer>().material = lineMaterial;
        else
            line.GetComponent<Renderer>().material.color = color ?? Color.white;
        
        Destroy(line.GetComponent<Collider>());
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
        
        if (netMaterial != null)
        {
            net.GetComponent<Renderer>().material = netMaterial;
            post1.GetComponent<Renderer>().material = netMaterial;
            post2.GetComponent<Renderer>().material = netMaterial;
            topBand.GetComponent<Renderer>().material = netMaterial;
        }
        else
        {
            net.GetComponent<Renderer>().material.color = new Color(0.9f, 0.9f, 0.9f);
            post1.GetComponent<Renderer>().material.color = Color.black;
            post2.GetComponent<Renderer>().material.color = Color.black;
            topBand.GetComponent<Renderer>().material.color = Color.white;
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
    
    void OnDrawGizmos()
    {
        // Saha sınırlarını göster
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(courtLength, 0, courtWidth));
        
        // Takım alanları
        float halfLength = courtLength / 2f;
        
        // Red takım alanı
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(-halfLength / 2f, 0.1f, 0), 
                           new Vector3(halfLength, 0, courtWidth));
        
        // Blue takım alanı
        Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.5f);
        Gizmos.DrawWireCube(new Vector3(halfLength / 2f, 0.1f, 0), 
                           new Vector3(halfLength, 0, courtWidth));
        
        // File pozisyonu
        Gizmos.color = Color.white;
        Gizmos.DrawLine(new Vector3(0, 0, -courtWidth/2), new Vector3(0, netHeight, -courtWidth/2));
        Gizmos.DrawLine(new Vector3(0, 0, courtWidth/2), new Vector3(0, netHeight, courtWidth/2));
        Gizmos.DrawLine(new Vector3(0, netHeight, -courtWidth/2), new Vector3(0, netHeight, courtWidth/2));
    }
}