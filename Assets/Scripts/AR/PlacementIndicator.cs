using UnityEngine;

/// <summary>
/// Visual ring indicator that follows the AR/physics raycast hit point on detected surfaces.
/// Smoothly glides across floors and pulses to indicate ready placement.
/// </summary>
public class PlacementIndicator : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("How fast the indicator pulses in scale.")]
    [SerializeField] private float _pulseSpeed = 3f;
    [SerializeField] private float _pulseAmount = 0.08f;

    [Tooltip("How quickly the indicator lerps to its new world position.")]
    [SerializeField] private float _moveSpeed = 25f;

    private Renderer[] _renderers;
    private Vector3 _targetPosition;
    private Quaternion _targetRotation = Quaternion.identity;
    private Vector3 _baseScale = new Vector3(0.35f, 0.005f, 0.35f);
    private bool _isVisible = false;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        if (transform.localScale != Vector3.zero)
            _baseScale = transform.localScale;

        SetVisible(false);
    }

    private void Update()
    {
        if (!_isVisible) return;

        // Smoothly glide to target hit position & surface rotation
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _moveSpeed);

        // Gentle pulsing effect
        float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmount;
        transform.localScale = _baseScale * pulse;
    }

    public void UpdatePose(Vector3 position, Quaternion rotation)
    {
        _targetPosition = position;
        _targetRotation = rotation;
    }

    public void SetVisible(bool visible)
    {
        _isVisible = visible;
        if (_renderers == null || _renderers.Length == 0)
            _renderers = GetComponentsInChildren<Renderer>();

        foreach (var rend in _renderers)
        {
            if (rend != null)
                rend.enabled = visible;
        }
    }
}
