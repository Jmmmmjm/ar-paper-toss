using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Attached to the AR Plane prefab.
/// Manages physics colliders on all detected surfaces (walls, floors, tables).
/// Keeps vertical walls invisible while enabling solid colliders to bounce stray paper balls.
/// Hides horizontal plane visuals once the trash can is placed.
/// </summary>
[RequireComponent(typeof(ARPlane))]
public class ARPlaneCollisionHandler : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private PhysicsMaterial _paperMaterial;

    private ARPlane _arPlane;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;

    private void Awake()
    {
        _arPlane = GetComponent<ARPlane>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        if (_meshCollider != null && _paperMaterial != null)
        {
            _meshCollider.material = _paperMaterial;
        }

        UpdateVisualsAndCollision();
    }

    private void OnEnable()
    {
        if (_arPlane != null)
        {
            _arPlane.boundaryChanged += HandleBoundaryChanged;
        }

        UpdateVisualsAndCollision();
    }

    private void OnDisable()
    {
        if (_arPlane != null)
        {
            _arPlane.boundaryChanged -= HandleBoundaryChanged;
        }
    }

    private void HandleBoundaryChanged(ARPlaneBoundaryChangedEventArgs args)
    {
        UpdateVisualsAndCollision();
    }

    private void UpdateVisualsAndCollision()
    {
        if (_arPlane == null) return;

        // Ensure physics collider is always active so paper balls bounce off real walls, floors, and furniture
        if (_meshCollider != null)
        {
            _meshCollider.enabled = true;
        }

        // Keep all plane visual meshes completely invisible so the camera feed and room look 100% clean and natural
        if (_meshRenderer != null)
        {
            _meshRenderer.enabled = false;
        }
    }
}
