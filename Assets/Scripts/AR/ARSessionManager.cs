using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Manages the AR session lifecycle.
/// Shows a scanning UI overlay until at least one horizontal plane is detected.
/// </summary>
public class ARSessionManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The AR Plane Manager used to detect when surfaces have been found.")]
    [SerializeField] private ARPlaneManager _planeManager;

    [Header("UI")]
    [Tooltip("Root GameObject of the Scan your room instruction overlay.")]
    [SerializeField] private GameObject _scanningOverlay;

    private bool _planeFound = false;

    private void Awake()
    {
        // Auto-find ARPlaneManager if not manually assigned
        if (_planeManager == null)
        {
            _planeManager = FindFirstObjectByType<ARPlaneManager>();
        }
    }

    private void OnEnable()
    {
        if (_planeManager != null)
            _planeManager.trackablesChanged.AddListener(OnPlanesChanged);
    }

    private void OnDisable()
    {
        if (_planeManager != null)
            _planeManager.trackablesChanged.RemoveListener(OnPlanesChanged);
    }

    private void Start()
    {
        SetScanningOverlay(true);
    }

    private void OnPlanesChanged(ARTrackablesChangedEventArgs<ARPlane> args)
    {
        if (_planeFound) return;

        if (args.added.Count > 0)
        {
            _planeFound = true;
            SetScanningOverlay(false);
        }
    }

    private void SetScanningOverlay(bool visible)
    {
        if (_scanningOverlay != null)
            _scanningOverlay.SetActive(visible);
    }
}
