using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;

/// <summary>
/// Handles tap-to-place logic for the Trash Can in AR and Editor simulation.
/// Uses AR Raycasting on device, with smooth Physics Raycast fallback for custom simulation rooms.
/// </summary>
public class PlacementController : MonoBehaviour
{
    [Header("AR References")]
    [SerializeField] private ARRaycastManager _raycastManager;
    [SerializeField] private ARPlaneManager _planeManager;

    [Header("Prefabs")]
    [SerializeField] private GameObject _trashCanPrefab;
    [SerializeField] private PlacementIndicator _placementIndicator;

    [Header("Settings")]
    [Tooltip("Which plane types to raycast against in AR.")]
    [SerializeField] private TrackableType _planeTypes = TrackableType.AllTypes;

    // Internal state
    private GameObject _spawnedTrashCan;
    private bool _placementLocked = false;
    private bool _placementReady = false;
    private Vector3 _currentHitPosition;
    private Quaternion _currentHitRotation = Quaternion.identity;

    private static readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();
    private Camera _cam;

    // Events
    public static event System.Action OnTrashCanPlaced;

    private void Awake()
    {
        _cam = Camera.main;
        if (_cam != null && _cam.farClipPlane < 100f)
            _cam.farClipPlane = 100f;

        if (_raycastManager == null)
            _raycastManager = FindFirstObjectByType<ARRaycastManager>();

        if (_planeManager == null)
            _planeManager = FindFirstObjectByType<ARPlaneManager>();

        // Handle both scene instances and prefab references for the indicator
        if (_placementIndicator != null && !_placementIndicator.gameObject.scene.IsValid())
        {
            _placementIndicator = Instantiate(_placementIndicator);
            _placementIndicator.name = "PlacementIndicator (Active)";
        }
        else if (_placementIndicator == null)
        {
            _placementIndicator = FindFirstObjectByType<PlacementIndicator>();
        }
    }

    private void Update()
    {
        if (_placementLocked) return;

        UpdateIndicator();
        HandleTapInput();
    }

    /// <summary>
    /// Raycasts to find a placement surface across any distance.
    /// Checks AR detected planes and 3D room geometry.
    /// </summary>
    private void UpdateIndicator()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

#if UNITY_EDITOR
        // In Editor, use mouse position if within screen bounds, otherwise screen center
        if (Mouse.current != null)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (mousePos.x >= 0 && mousePos.x <= Screen.width && mousePos.y >= 0 && mousePos.y <= Screen.height)
            {
                screenPoint = mousePos;
            }
        }
#endif

        bool foundHit = false;

        // 1. Try AR Raycast (AR Foundation planes)
        if (_raycastManager != null && _raycastManager.Raycast(screenPoint, _hits, _planeTypes) && _hits.Count > 0)
        {
            Pose hitPose = _hits[0].pose;
            _currentHitPosition = hitPose.position;
            _currentHitRotation = hitPose.rotation;
            foundHit = true;
        }

        // 2. Physics Raycast fallback across ALL loaded scenes and PhysicsScenes (including XR Simulation environment scene)
        if (!foundHit)
        {
            Ray ray = _cam.ScreenPointToRay(screenPoint);
            List<RaycastHit> allHits = new List<RaycastHit>();

            // Query default physics scene
            var defaultHits = Physics.RaycastAll(ray, 100f, ~0);
            if (defaultHits != null && defaultHits.Length > 0)
                allHits.AddRange(defaultHits);

            // Query all other loaded scenes' PhysicsScenes (e.g. XR Simulation environment scene)
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                var physicsScene = scene.GetPhysicsScene();
                if (physicsScene.IsValid() && physicsScene != Physics.defaultPhysicsScene)
                {
                    RaycastHit[] sceneHits = new RaycastHit[30];
                    int hitCount = physicsScene.Raycast(ray.origin, ray.direction, sceneHits, 100f, ~0);
                    for (int h = 0; h < hitCount; h++)
                    {
                        allHits.Add(sceneHits[h]);
                    }
                }
            }

            // Sort by distance to find the closest valid horizontal surface
            allHits.Sort((a, b) => a.distance.CompareTo(b.distance));

            foreach (var physHit in allHits)
            {
                // Accept horizontal surfaces (floors, shelves, table tops, desks)
                if (Mathf.Abs(Vector3.Dot(physHit.normal, Vector3.up)) > 0.3f)
                {
                    _currentHitPosition = physHit.point;
                    Vector3 surfaceNormal = Vector3.Dot(physHit.normal, Vector3.up) >= 0f
                        ? physHit.normal
                        : -physHit.normal;
                    _currentHitRotation = Quaternion.FromToRotation(Vector3.up, surfaceNormal);
                    foundHit = true;
                    break;
                }
            }
        }

        if (foundHit)
        {
            if (_placementIndicator != null)
            {
                _placementIndicator.UpdatePose(_currentHitPosition, _currentHitRotation);
                _placementIndicator.SetVisible(true);
            }
            _placementReady = true;
        }
        else
        {
            if (_placementIndicator != null)
                _placementIndicator.SetVisible(false);
            _placementReady = false;
        }
    }

    /// <summary>
    /// Detect tap or mouse click to place the Trash Can.
    /// </summary>
    private void HandleTapInput()
    {
        if (!_placementReady) return;

        bool tapped = false;

#if UNITY_EDITOR
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            tapped = true;
#else
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            tapped = true;
#endif

        if (tapped)
            PlaceTrashCan();
    }

    private GameObject _floorColliderObject;

    private void PlaceTrashCan()
    {
        if (_trashCanPrefab == null)
        {
            Debug.LogWarning("[PlacementController] Trash Can Prefab is not assigned!");
            return;
        }

        if (_spawnedTrashCan == null)
        {
            _spawnedTrashCan = Instantiate(_trashCanPrefab, _currentHitPosition, _currentHitRotation);
        }
        else
        {
            _spawnedTrashCan.transform.SetPositionAndRotation(_currentHitPosition, _currentHitRotation);
        }

        // Spawn / align an invisible floor physics collider at the exact placed floor height
        SetupFloorCollider(_currentHitPosition);

        LockPlacement();
        OnTrashCanPlaced?.Invoke();
    }

    private void SetupFloorCollider(Vector3 floorPoint)
    {
        if (_floorColliderObject == null)
        {
            _floorColliderObject = new GameObject("AR_FloorPhysicsPlane");
            var col = _floorColliderObject.AddComponent<BoxCollider>();
            col.size = new Vector3(100f, 0.2f, 100f);

            var mat = new PhysicsMaterial("PaperFloorMat")
            {
                dynamicFriction = 0.55f,
                staticFriction = 0.65f,
                bounciness = 0.25f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            col.material = mat;
        }

        // Top surface of the box collider (height 0.2m, centered at y - 0.1m) aligns flush with the floor
        _floorColliderObject.transform.position = new Vector3(floorPoint.x, floorPoint.y - 0.1f, floorPoint.z);
    }

    private void LockPlacement()
    {
        _placementLocked = true;
        if (_placementIndicator != null)
            _placementIndicator.SetVisible(false);

        SetPlanesVisible(false);
    }

    public void AllowReposition()
    {
        _placementLocked = false;
        SetPlanesVisible(true);
    }

    private void SetPlanesVisible(bool visible)
    {
        if (_planeManager == null) return;

        foreach (ARPlane plane in _planeManager.trackables)
        {
            plane.gameObject.SetActive(visible);
        }
    }

    public GameObject GetSpawnedTrashCan() => _spawnedTrashCan;
}
