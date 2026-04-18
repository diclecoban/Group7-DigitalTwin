using UnityEngine;

// Bu script doğrudan "Robot" objesine takılacak.
// Tıpkı gerçek kameranın veya sensörün etrafı taraması gibi Unity içinde çalışır.
public class VirtualSensor : MonoBehaviour
{
    [Header("Sensör Ayarları")]
    public float visionRadius = 5f; // Kameranın görüş/algılama mesafesi
    public float obstacleRayDistance = 3f; // Ultrasonik sensör menzili

    [Header("Bağlantılar")]
    public MapManager mapManager;
    public UIManager uiManager;

    private float scanTimer = 0f;
    private float scanInterval = 1f; // Saniyede 1 kere etrafı tara (kasmaması için)

    private void Start()
    {
        // Manager'ları otomatik bul
        if (mapManager == null) mapManager = FindObjectOfType<MapManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
    }

    private void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanEnvironment();
        }
    }

    private void ScanEnvironment()
    {
        // 1. ENGEL (OBSTACLE) ALGILAMA SİMÜLASYONU (İleriye dönük lazer atışı)
        // Eğer lazer "Obstacle" etiketli bir şeye değerse konsola uyarı yazar.
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit, obstacleRayDistance))
        {
            if (hit.collider.CompareTag("Obstacle"))
            {
                Debug.LogWarning($"[MOD-01] Engel Algılandı! Mesafe: {hit.distance:0.0}m. Çarpışma önleme devrede.");
            }
        }

        // 2. KURBAN (VICTIM) ALGILAMA SİMÜLASYONU (YOLO Yapay Zeka Kamerası)
        // Robotun etrafındaki visionRadius (örn 5m) içindeki tüm objeleri tarar.
        Collider[] colliders = Physics.OverlapSphere(transform.position, visionRadius);
        bool victimInSight = false;

        foreach (var col in colliders)
        {
            if (col.CompareTag("Victim"))
            {
                // Etiket victim ise, üstündeki VictimInfo'yu al
                VictimInfo info = col.GetComponent<VictimInfo>();
                if (info != null)
                {
                    victimInSight = true;

                    // Gerçekten robot bu kurbanı görmüş gibi dashboard'u güncelleyelim:
                    TelemetryData mockData = new TelemetryData
                    {
                        posX = col.transform.position.x, 
                        posY = col.transform.position.z, 
                        temperature = 28.5f,
                        smokeDetected = false,
                        victimStatus = info.severity,
                        acousticHit = false,
                        acousticAngle = 0f
                    };

                    // Haritaya pini yerleştir ve UI'ı güncelle
                    if (mapManager != null) mapManager.PlacePin(mockData.posX, mockData.posY, mockData.victimStatus);
                    if (uiManager != null) uiManager.UpdateVictimStatus(mockData.victimStatus);

                    Debug.Log($"[MOD-02] Vision AI: Kurban Tespiti! Durum: {info.severity}");
                    break; // Aynı anda 1 kişi göstersin yeterli
                }
            }
        }

        if (!victimInSight && uiManager != null)
        {
            // Eğer görüş açısında kurban yoksa arayüzü NONE'a çek
            uiManager.UpdateVictimStatus(VictimStatus.NONE);
        }
    }
}
