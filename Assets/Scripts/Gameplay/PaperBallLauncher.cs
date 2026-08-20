using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles swipe/flick input to launch paper balls towards the trash can.
/// Physics tuned for smooth, visible, satisfying toss arcs.
/// </summary>
public class PaperBallLauncher : MonoBehaviour
{
    [Header("Prefabs & Spawning")]
    [SerializeField] private GameObject _paperBallPrefab;
    [Tooltip("Offset in front of the camera where the ball rests before throwing.")]
    [SerializeField] private Vector3 _spawnOffset = new Vector3(0f, -0.15f, 0.4f);

    [Header("Toss Physics (Meters per Second)")]
    [Tooltip("Base forward velocity (m/s).")]
    [SerializeField] private float _baseForwardVelocity = 3.6f;
    [Tooltip("Upward arc velocity (m/s).")]
    [SerializeField] private float _baseUpwardVelocity = 1.6f;
    [Tooltip("How strongly swipe speed affects throw distance (0 = consistent, 1 = sensitive).")]
    [SerializeField] private float _swipeSensitivity = 0.5f;
    [Tooltip("Side-to-side curve multiplier based on swipe angle.")]
    [SerializeField] private float _curveMultiplier = 0.002f;
    [Tooltip("Maximum allowed throw speed (m/s).")]
    [SerializeField] private float _maxSpeed = 6.5f;
    [Tooltip("Delay in seconds before spawning the next ball after a throw.")]
    [SerializeField] private float _respawnDelay = 1.0f;

    [Header("Screen Anchoring")]
    [Tooltip("Viewport X position (0.5 = center of screen).")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float _viewportX = 0.5f;

    [Tooltip("Viewport Y position (0.12 = bottom of screen, above home bar).")]
    [Range(0.05f, 0.4f)]
    [SerializeField] private float _viewportY = 0.12f;

    [Tooltip("Distance in meters in front of the camera.")]
    [SerializeField] private float _cameraDistance = 0.38f;

    [Header("Input Filtering")]
    [SerializeField] private float _minSwipeDistance = 30f;
    [SerializeField] private float _maxSwipeTime = 0.7f;

    // References
    private Camera _arCamera;
    private GameObject _currentBall;
    private bool _canThrow = false;
    private bool _isTrackingSwipe = false;
    private Vector2 _swipeStartPosition;
    private Vector2 _currentPointerPosition;
    private float _swipeStartTime;

    private bool _isPaused = false;

    private void Awake()
    {
        _arCamera = Camera.main;
    }

    private void OnEnable()
    {
        PlacementController.OnTrashCanPlaced += EnableThrowing;
        PlacementController.OnPlacementReset += DisableThrowing;
        SettingsManager.OnSensitivityMultiplierChanged += HandleSensitivityChanged;
        SettingsManager.OnPauseStateChanged += HandlePauseStateChanged;
    }

    private void OnDisable()
    {
        PlacementController.OnTrashCanPlaced -= EnableThrowing;
        PlacementController.OnPlacementReset -= DisableThrowing;
        SettingsManager.OnSensitivityMultiplierChanged -= HandleSensitivityChanged;
        SettingsManager.OnPauseStateChanged -= HandlePauseStateChanged;
    }

    private void Start()
    {
        if (_arCamera == null)
            _arCamera = Camera.main;

        if (SettingsManager.Instance != null)
        {
            _swipeSensitivity = SettingsManager.Instance.GetSensitivityMultiplier();
        }
    }

    private void HandleSensitivityChanged(float newSensitivity)
    {
        _swipeSensitivity = newSensitivity;
    }

    private void HandlePauseStateChanged(bool paused)
    {
        _isPaused = paused;
    }

    public void EnableThrowing()
    {
        _canThrow = true;
        ResetLauncher();
    }

    public void DisableThrowing()
    {
        _canThrow = false;
        ClearAllBalls();
    }

    public void ResetLauncher()
    {
        ClearAllBalls();
        if (_canThrow)
        {
            SpawnReadyBall();
        }
    }

    private void ClearAllBalls()
    {
        if (_currentBall != null)
        {
            Destroy(_currentBall);
            _currentBall = null;
        }

        PaperBall[] balls = FindObjectsByType<PaperBall>(FindObjectsSortMode.None);
        foreach (var b in balls)
        {
            if (b != null) Destroy(b.gameObject);
        }
    }

    private void Update()
    {
        if (!_canThrow || _isPaused || _arCamera == null) return;

        // Keep the ready ball anchored to the bottom of the screen / follow thumb drag
        if (_currentBall != null)
        {
            Vector2 dragOffset = _isTrackingSwipe ? (_currentPointerPosition - _swipeStartPosition) : Vector2.zero;
            Vector3 targetPos = GetReadyBallPosition(dragOffset);
            _currentBall.transform.position = targetPos;
            _currentBall.transform.rotation = _arCamera.transform.rotation;
        }

        ProcessInput();
    }

    private Vector3 GetReadyBallPosition(Vector2 dragDeltaPixels)
    {
        if (_arCamera == null) _arCamera = Camera.main;
        if (_arCamera == null) return Vector3.zero;

        float viewWidth = Screen.width > 0 ? (float)Screen.width : 1080f;
        float viewHeight = Screen.height > 0 ? (float)Screen.height : 1920f;

        float dragNormX = Mathf.Clamp(dragDeltaPixels.x / viewWidth, -0.2f, 0.2f);
        float dragNormY = Mathf.Clamp(dragDeltaPixels.y / viewHeight, -0.04f, 0.12f);

        Vector3 viewportPos = new Vector3(_viewportX + dragNormX, _viewportY + dragNormY, _cameraDistance);
        return _arCamera.ViewportToWorldPoint(viewportPos);
    }

    private void ProcessInput()
    {
        // Don't process throws if pointer started over UI
        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
#if UNITY_EDITOR
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
#else
            if (Touchscreen.current != null)
            {
                int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId)) return;
            }
#endif
        }

        bool pointerDown = false;
        bool pointerUp = false;
        Vector2 pointerPos = Vector2.zero;

#if UNITY_EDITOR
        if (Mouse.current != null)
        {
            pointerDown = Mouse.current.leftButton.wasPressedThisFrame;
            pointerUp = Mouse.current.leftButton.wasReleasedThisFrame;
            pointerPos = Mouse.current.position.ReadValue();
        }
#else
        // NOTE: wasReleasedThisFrame is true the frame the finger lifts,
        // which is the SAME frame isPressed becomes false.
        // We must NOT gate the whole read behind isPressed or we miss the release.
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            pointerDown = touch.press.wasPressedThisFrame;
            pointerUp   = touch.press.wasReleasedThisFrame;
            // Only update position while the finger is down (or just released this frame)
            if (touch.press.isPressed || pointerUp || pointerDown)
            {
                pointerPos = touch.position.ReadValue();
            }
        }
#endif

        _currentPointerPosition = pointerPos;

        if (pointerDown)
        {
            _isTrackingSwipe = true;
            _swipeStartPosition = pointerPos;
            _swipeStartTime = Time.time;
        }
        else if (pointerUp && _isTrackingSwipe)
        {
            _isTrackingSwipe = false;
            float duration = Time.time - _swipeStartTime;
            Vector2 swipeDelta = pointerPos - _swipeStartPosition;

            if (swipeDelta.y > _minSwipeDistance && duration < _maxSwipeTime)
            {
                ExecuteThrow(swipeDelta, duration);
            }
        }
    }

    private void ExecuteThrow(Vector2 swipeDelta, float duration)
    {
        if (_currentBall == null) return;

        // Calculate swipe speed factor
        float pixelSpeed = swipeDelta.y / Mathf.Max(duration, 0.05f);
        float normalizedSpeed = Mathf.Clamp(pixelSpeed / 1000f, 0.5f, 2.0f);
        float speedMultiplier = Mathf.Lerp(1.0f, normalizedSpeed, _swipeSensitivity);

        // Direction vectors relative to AR Camera
        Vector3 forward = _arCamera.transform.forward;
        Vector3 up = _arCamera.transform.up;
        Vector3 right = _arCamera.transform.right;

        float curveX = Mathf.Clamp(swipeDelta.x * _curveMultiplier, -1.5f, 1.5f);

        // Build gentle, visible velocity arc
        Vector3 launchVelocity = (forward * (_baseForwardVelocity * speedMultiplier)) +
                                (up * (_baseUpwardVelocity * speedMultiplier)) +
                                (right * curveX);

        launchVelocity = Vector3.ClampMagnitude(launchVelocity, _maxSpeed);

        // Realistic tumble spin
        Vector3 spin = new Vector3(Random.Range(3f, 8f), Random.Range(-2f, 2f), Random.Range(-2f, 2f));

        PaperBall ball = _currentBall.GetComponent<PaperBall>();
        if (ball != null)
        {
            ball.Launch(launchVelocity, spin);
        }

        _currentBall = null;
        StartCoroutine(RespawnRoutine());
    }

    private void SpawnReadyBall()
    {
        if (_paperBallPrefab == null || _arCamera == null) return;

        Vector3 spawnPos = GetReadyBallPosition(Vector2.zero);
        _currentBall = Instantiate(_paperBallPrefab, spawnPos, _arCamera.transform.rotation);

        Rigidbody rb = _currentBall.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(_respawnDelay);
        if (_canThrow)
        {
            SpawnReadyBall();
        }
    }
}
