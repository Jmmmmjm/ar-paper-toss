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

    [Header("Input Filtering")]
    [SerializeField] private float _minSwipeDistance = 30f;
    [SerializeField] private float _maxSwipeTime = 0.7f;

    // References
    private Camera _arCamera;
    private GameObject _currentBall;
    private bool _canThrow = false;
    private bool _isTrackingSwipe = false;
    private Vector2 _swipeStartPosition;
    private float _swipeStartTime;

    private void Awake()
    {
        _arCamera = Camera.main;
    }

    private void OnEnable()
    {
        PlacementController.OnTrashCanPlaced += EnableThrowing;
    }

    private void OnDisable()
    {
        PlacementController.OnTrashCanPlaced -= EnableThrowing;
    }

    private void Start()
    {
        if (_arCamera == null)
            _arCamera = Camera.main;
    }

    public void EnableThrowing()
    {
        _canThrow = true;
        SpawnReadyBall();
    }

    public void DisableThrowing()
    {
        _canThrow = false;
        if (_currentBall != null)
        {
            Destroy(_currentBall);
            _currentBall = null;
        }
    }

    private void Update()
    {
        if (!_canThrow || _arCamera == null) return;

        // Keep the ready ball floating in front of the camera view
        if (_currentBall != null && !_isTrackingSwipe)
        {
            Vector3 targetPos = _arCamera.transform.TransformPoint(_spawnOffset);
            _currentBall.transform.position = targetPos;
            _currentBall.transform.rotation = _arCamera.transform.rotation;
        }

        ProcessInput();
    }

    private void ProcessInput()
    {
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
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            pointerDown = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            pointerUp = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            pointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
        }
#endif

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

        Vector3 spawnPos = _arCamera.transform.TransformPoint(_spawnOffset);
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
