using UnityEngine;

/// <summary>
/// Automatically adjusts a RectTransform to avoid being obstructed by the iPhone Dynamic Island,
/// notches, camera punch-holes, status bars, and home indicator bars.
/// Supports both full-screen container stretching and direct top-anchored offset shifting.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[DisallowMultipleComponent]
public class SafeArea : MonoBehaviour
{
    public enum Mode
    {
        [Tooltip("Adjusts anchors (anchorMin/anchorMax) to match the safe area bounds. Best for full-screen parent panels.")]
        FullStretch,

        [Tooltip("Adjusts anchoredPosition.y downwards to clear top notches/Dynamic Island. Best for top-anchored cards like scoreboards.")]
        TopOffsetOnly
    }

    [Header("Mode")]
    [SerializeField] private Mode _mode = Mode.TopOffsetOnly;

    [Header("Offset Settings")]
    [Tooltip("Base top padding in canvas units below the safe area / Dynamic Island.")]
    [SerializeField] private float _topPadding = 20f;

    [Header("Editor Simulation")]
    [Tooltip("Simulate iPhone 15 / Dynamic Island top inset in Editor when no device simulator is active.")]
    [SerializeField] private bool _simulateInEditor = true;
    [SerializeField] private float _editorSimulatedTopInset = 160f; // Canvas units for Dynamic Island simulation

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private Rect _lastSafeArea = Rect.zero;
    private Vector2Int _lastScreenSize = Vector2Int.zero;
    private ScreenOrientation _lastOrientation = ScreenOrientation.AutoRotation;
    private float _initialBaseY = 0f;
    private bool _hasCapturedBaseY = false;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        
        if (!_hasCapturedBaseY && _rectTransform != null)
        {
            _initialBaseY = _rectTransform.anchoredPosition.y;
            _hasCapturedBaseY = true;
        }

        ApplySafeArea();
    }

    private void OnEnable()
    {
        ApplySafeArea();
    }

    private void Update()
    {
        if (HasScreenChanged())
        {
            ApplySafeArea();
        }
    }

    private bool HasScreenChanged()
    {
        return _lastSafeArea != Screen.safeArea ||
               _lastScreenSize.x != Screen.width ||
               _lastScreenSize.y != Screen.height ||
               _lastOrientation != Screen.orientation;
    }

    /// <summary>
    /// Computes and applies safe area positioning.
    /// </summary>
    public void ApplySafeArea()
    {
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        if (_canvas == null)
            _canvas = GetComponentInParent<Canvas>();

        if (Screen.width <= 0 || Screen.height <= 0) return;

        Rect safeArea = Screen.safeArea;
        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        _lastOrientation = Screen.orientation;

        // Calculate top inset in physical screen pixels
        float topInsetPixels = Screen.height - safeArea.yMax;

#if UNITY_EDITOR
        // If running in Editor standard game view without active device safe area cutout
        if (_simulateInEditor && topInsetPixels < 5f)
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            topInsetPixels = _editorSimulatedTopInset * scale;
        }
#endif

        if (_mode == Mode.TopOffsetOnly)
        {
            // Convert physical pixels to Canvas reference units
            float scaleFactor = (_canvas != null && _canvas.scaleFactor > 0.001f) ? _canvas.scaleFactor : 1f;
            float topInsetUnits = topInsetPixels / scaleFactor;

            // Shift anchoredPosition.y down from top
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y = - (topInsetUnits + _topPadding);
            _rectTransform.anchoredPosition = pos;
        }
        else // FullStretch
        {
            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rectTransform.anchorMin = anchorMin;
            _rectTransform.anchorMax = anchorMax;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = new Vector2(0f, -_topPadding);
        }
    }
}
