using UnityEngine;

/// <summary>
/// Manages in-game obstacles (such as the Paper Airplane) during gameplay.
/// Automatically spawns, aligns, and coordinates obstacle lifecycles with AR placement.
/// </summary>
public class ObstacleManager : MonoBehaviour
{
    public static ObstacleManager Instance { get; private set; }

    [Header("Obstacle Prefabs")]
    [SerializeField] private GameObject _paperAirplanePrefab;

    [Header("Spawning Settings")]
    [SerializeField] private bool _enableAirplaneObstacle = true;
    [SerializeField] private AirplaneFlightPattern _defaultPattern = AirplaneFlightPattern.LoopDeLoop;

    private GameObject _spawnedAirplane;
    private PaperAirplaneController _airplaneController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        PlacementController.OnTrashCanPlaced += HandleTrashCanPlaced;
        PlacementController.OnPlacementReset += HandlePlacementReset;
    }

    private void OnDisable()
    {
        PlacementController.OnTrashCanPlaced -= HandleTrashCanPlaced;
        PlacementController.OnPlacementReset -= HandlePlacementReset;
    }

    private void Start()
    {
        var placement = FindFirstObjectByType<PlacementController>();
        if (placement != null && placement.IsTrashCanPlaced)
        {
            HandleTrashCanPlaced();
        }
    }

    public void HandleTrashCanPlaced()
    {
        if (!_enableAirplaneObstacle) return;

        if (_spawnedAirplane == null)
        {
            if (_paperAirplanePrefab != null)
            {
                _spawnedAirplane = Instantiate(_paperAirplanePrefab);
                _airplaneController = _spawnedAirplane.GetComponent<PaperAirplaneController>();
            }
        }

        if (_spawnedAirplane != null)
        {
            _spawnedAirplane.SetActive(true);
            if (_airplaneController != null)
            {
                _airplaneController.SetFlightPattern(_defaultPattern);
                _airplaneController.UpdateOrigin();
            }
        }
    }

    public void HandlePlacementReset()
    {
        if (_spawnedAirplane != null)
        {
            _spawnedAirplane.SetActive(false);
        }
    }

    public void SetAirplaneActive(bool active)
    {
        _enableAirplaneObstacle = active;
        if (_spawnedAirplane != null)
            _spawnedAirplane.SetActive(active);
    }
}
