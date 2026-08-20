using UnityEngine;

/// <summary>
/// Visual holographic particle indicator that follows the AR/physics raycast hit point on detected surfaces.
/// Loops continuously to guide trash can placement.
/// </summary>
public class PlacementIndicator : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("How fast the indicator glides to its new world position.")]
    [SerializeField] private float _moveSpeed = 25f;

    [Tooltip("Slow rotation of the particle ring.")]
    [SerializeField] private float _orbitRotationSpeed = 35f;

    [SerializeField] private GameObject _visualRoot;

    private Vector3 _targetPosition;
    private Quaternion _targetRotation = Quaternion.identity;
    private bool _isVisible = false;

    private void Awake()
    {
        // If _visualRoot is not explicitly assigned, use first child or create one
        if (_visualRoot == null && transform.childCount > 0)
        {
            _visualRoot = transform.GetChild(0).gameObject;
        }

        // Clean up any one-shot destroy scripts from imported VFX prefabs
        var cfxrEffects = GetComponentsInChildren<CartoonFX.CFXR_Effect>(true);
        foreach (var fx in cfxrEffects)
        {
            fx.clearBehavior = CartoonFX.CFXR_Effect.ClearBehavior.None;
        }

        // Ensure all particle systems loop continuously
        var particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in particleSystems)
        {
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            var emission = ps.emission;
            emission.enabled = true;
        }

        SetVisible(false);
    }

    private void Update()
    {
        if (!_isVisible) return;

        // Smoothly glide to target hit position & surface rotation
        transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * _moveSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * _moveSpeed);

        // Gentle ambient rotation around surface normal
        transform.Rotate(Vector3.up, _orbitRotationSpeed * Time.deltaTime, Space.Self);
    }

    public void UpdatePose(Vector3 position, Quaternion rotation)
    {
        _targetPosition = position;
        _targetRotation = rotation;
    }

    public void SetVisible(bool visible)
    {
        _isVisible = visible;

        if (_visualRoot != null)
        {
            _visualRoot.SetActive(visible);
        }
        else
        {
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(visible);
            }
        }
    }
}
