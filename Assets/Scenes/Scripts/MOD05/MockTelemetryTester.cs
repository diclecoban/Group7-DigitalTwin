using UnityEngine;

public class MockTelemetryTester : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private MapManager mapManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private MapManager_AcousticBeam acousticBeamManager;

    [Header("Demo Flow")]
    [SerializeField] private bool applyOnStart = true;
    [SerializeField] private bool loopDemo = true;
    [SerializeField] private float stepIntervalSeconds = 1.5f;
    [SerializeField] private AcousticBeamStyle beamStyle = AcousticBeamStyle.DirectionArrow;

    [Header("Route")]
    [SerializeField] private Vector2[] routePoints = new Vector2[]
    {
        new Vector2(1f, 1f),
        new Vector2(2f, 2f),
        new Vector2(3f, 2f),
        new Vector2(4f, 3f),
        new Vector2(5f, 3f),
        new Vector2(6f, 4f)
    };

    [Header("Telemetry Baseline")]
    [SerializeField] private float baseTemperature = 27f;
    [SerializeField] private float temperatureVariation = 18f;
    [SerializeField] private float beamAngle = -45f;

    private float stepTimer;
    private int routeIndex = -1;
    private bool demoRunning;

    private void Awake()
    {
        if (mapManager == null)
        {
            mapManager = FindObjectOfType<MapManager>();
        }

        if (uiManager == null)
        {
            uiManager = FindObjectOfType<UIManager>();
        }

        if (acousticBeamManager == null)
        {
            acousticBeamManager = FindObjectOfType<MapManager_AcousticBeam>();
        }
    }

    private void Start()
    {
        if (applyOnStart)
        {
            StartDemo();
        }
    }

    private void Update()
    {
        if (!demoRunning || routePoints == null || routePoints.Length == 0)
        {
            return;
        }

        stepTimer += Time.deltaTime;
        if (stepTimer < stepIntervalSeconds)
        {
            return;
        }

        stepTimer = 0f;
        AdvanceDemoStep();
    }

    [ContextMenu("Start Demo")]
    public void StartDemo()
    {
        if (mapManager != null)
        {
            mapManager.ClearAllPins();
        }

        if (acousticBeamManager != null)
        {
            acousticBeamManager.HideAcousticBeam();
        }

        routeIndex = -1;
        stepTimer = 0f;
        demoRunning = true;
        AdvanceDemoStep();
    }

    [ContextMenu("Apply Sample Telemetry")]
    public void ApplySampleTelemetry()
    {
        ApplyTelemetry(BuildTelemetryForIndex(0));
    }

    [ContextMenu("Clear Map Pins")]
    public void ClearMapPins()
    {
        if (mapManager != null)
        {
            mapManager.ClearAllPins();
        }

        if (acousticBeamManager != null)
        {
            acousticBeamManager.HideAcousticBeam();
        }
    }

    [ContextMenu("Stop Demo")]
    public void StopDemo()
    {
        demoRunning = false;
    }

    private void AdvanceDemoStep()
    {
        if (routePoints == null || routePoints.Length == 0)
        {
            demoRunning = false;
            return;
        }

        routeIndex++;
        if (routeIndex >= routePoints.Length)
        {
            if (!loopDemo)
            {
                demoRunning = false;
                return;
            }

            routeIndex = 0;
            if (mapManager != null)
            {
                mapManager.ClearAllPins();
            }
        }

        ApplyTelemetry(BuildTelemetryForIndex(routeIndex));
    }

    private TelemetryData BuildTelemetryForIndex(int index)
    {
        Vector2 routePoint = routePoints[Mathf.Clamp(index, 0, routePoints.Length - 1)];

        VictimStatus status;
        switch (index % 4)
        {
            case 1:
                status = VictimStatus.STANDING;
                break;
            case 2:
                status = VictimStatus.LYING;
                break;
            case 3:
                status = VictimStatus.TRAPPED;
                break;
            default:
                status = VictimStatus.NONE;
                break;
        }

        bool hasSmoke = index % 3 != 0;
        bool hasAcousticHit = index % 2 == 0;

        TelemetryData data = new TelemetryData
        {
            posX = routePoint.x,
            posY = routePoint.y,
            temperature = baseTemperature + Mathf.PingPong(index * 6f, temperatureVariation),
            smokeDetected = hasSmoke,
            victimStatus = status,
            priorityLevel = ResolvePriorityLevel(status),
            acousticHit = hasAcousticHit,
            acousticAngle = beamAngle + (index * 15f)
        };

        return data;
    }

    private void ApplyTelemetry(TelemetryData data)
    {
        if (mapManager != null)
        {
            mapManager.UpdateRobotPosition(data.posX, data.posY);
            mapManager.PlacePin(data.posX, data.posY, data.victimStatus);
        }

        if (uiManager != null)
        {
            uiManager.UpdateHUD(data);
        }

        if (acousticBeamManager != null)
        {
            if (data.acousticHit)
            {
                AcousticBeamData beamData = new AcousticBeamData
                {
                    bearingDeg = data.acousticAngle,
                    hitDetected = true,
                    posX = data.posX,
                    posY = data.posY,
                    timestampMs = (uint)(Time.time * 1000f)
                };

                acousticBeamManager.ShowAcousticBeam(beamData, beamStyle);
            }
            else
            {
                acousticBeamManager.HideAcousticBeam();
            }
        }
    }

    private static int ResolvePriorityLevel(VictimStatus status)
    {
        switch (status)
        {
            case VictimStatus.TRAPPED:
                return MapManagerConstants.PIN_PRIORITY_RED;
            case VictimStatus.LYING:
                return MapManagerConstants.PIN_PRIORITY_YELLOW;
            case VictimStatus.STANDING:
                return MapManagerConstants.PIN_PRIORITY_GREEN;
            default:
                return 0;
        }
    }
}
