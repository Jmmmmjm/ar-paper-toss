using System.Collections;
using UnityEngine;

public enum AirplaneFlightPattern
{
    HoopHoverBlock,
    LoopDeLoop,
    FigureEight,
    Orbit
}

/// <summary>
/// Controls the looping, hovering, and acrobatic flight of the retro 3D Folded Paper Airplane.
/// Features dynamic aerodynamic banking, hoop hover blocking, and mid-air collision reactions.
/// </summary>
public class PaperAirplaneController : MonoBehaviour
{
    [Header("Flight Settings")]
    [SerializeField] private AirplaneFlightPattern _flightPattern = AirplaneFlightPattern.HoopHoverBlock;
    [SerializeField] private float _flightSpeed = 1.35f;
    [SerializeField] private Vector3 _patrolRadius = new Vector3(0.26f, 0.12f, 0.22f);
    [SerializeField] private float _altitudeOffset = 0.22f;

    [Header("Aerodynamic Physics")]
    [SerializeField] private float _bankSensitivity = 40f;
    [SerializeField] private float _flutterStrength = 2.2f;

    [Header("Visual Model")]
    [SerializeField] private Transform _visualModel;
    [SerializeField] private TrailRenderer _trailRenderer;

    private Transform _trashCanTransform;
    private Vector3 _centerOrigin;
    private float _timeCounter = 0f;
    private Vector3 _lastPosition;
    private bool _isSpinningOut = false;
    private Coroutine _spinOutCoroutine;

    private void Awake()
    {
        if (_visualModel == null)
            _visualModel = transform;
    }

    private void Start()
    {
        UpdateOrigin();
        _lastPosition = transform.position;

        if (_trailRenderer != null)
        {
            _trailRenderer.time = 0.45f;
            _trailRenderer.startWidth = 0.025f;
            _trailRenderer.endWidth = 0.003f;
        }
    }

    public void SetFlightPattern(AirplaneFlightPattern pattern)
    {
        _flightPattern = pattern;
        _timeCounter = 0f;
    }

    public void UpdateOrigin()
    {
        // Anchor directly to the Trash Can top rim
        var canAnimator = FindFirstObjectByType<TrashCanAnimator>(FindObjectsInactive.Include);
        if (canAnimator != null)
        {
            _trashCanTransform = canAnimator.transform;
            _centerOrigin = _trashCanTransform.position + Vector3.up * _altitudeOffset;
        }
        else
        {
            var trashCan = GameObject.FindWithTag("Respawn") ?? GameObject.Find("TrashCan(Clone)") ?? GameObject.Find("TrashCan");
            if (trashCan != null)
            {
                _trashCanTransform = trashCan.transform;
                _centerOrigin = _trashCanTransform.position + Vector3.up * _altitudeOffset;
            }
            else
            {
                Camera cam = Camera.main;
                if (cam != null)
                    _centerOrigin = cam.transform.position + cam.transform.forward * 1.5f + Vector3.up * _altitudeOffset;
                else
                    _centerOrigin = transform.position;
            }
        }
    }

    private void Update()
    {
        if (_isSpinningOut) return;

        if (_trashCanTransform != null)
        {
            _centerOrigin = _trashCanTransform.position + Vector3.up * _altitudeOffset;
        }

        _timeCounter += Time.deltaTime * _flightSpeed;

        Vector3 targetPos = CalculatePathPosition(_timeCounter);
        transform.position = targetPos;

        // Calculate velocity and aerodynamic rotation
        Vector3 delta = targetPos - _lastPosition;
        if (delta.sqrMagnitude > 0.00001f)
        {
            Vector3 flightDirection = delta.normalized;

            // Pitch along flight vector
            Quaternion targetRotation = Quaternion.LookRotation(flightDirection, Vector3.up);

            // Dynamic banking roll into turns
            float horizontalTurn = Vector3.SignedAngle(Vector3.ProjectOnPlane(transform.forward, Vector3.up), Vector3.ProjectOnPlane(flightDirection, Vector3.up), Vector3.up);
            float bankRoll = Mathf.Clamp(-horizontalTurn * _bankSensitivity, -50f, 50f);

            // Micro-turbulence flutter
            float flutter = Mathf.Sin(Time.time * 16f) * _flutterStrength;

            Quaternion banking = Quaternion.Euler(0f, 0f, bankRoll + flutter);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation * banking, Time.deltaTime * 10f);
        }

        _lastPosition = targetPos;
    }

    private Vector3 CalculatePathPosition(float t)
    {
        switch (_flightPattern)
        {
            case AirplaneFlightPattern.HoopHoverBlock:
            default:
            {
                // Weaves back and forth directly across the trash can opening to block incoming paper balls
                float hoverTime = t * 1.4f;
                float x = Mathf.Sin(hoverTime) * _patrolRadius.x;
                float z = Mathf.Sin(hoverTime * 2f) * (_patrolRadius.z * 0.65f);
                float y = Mathf.Cos(hoverTime * 2f) * _patrolRadius.y;

                // Periodic acrobatic loop swoop every ~7s
                float loopCycle = Mathf.Repeat(t * 0.25f, 1f);
                if (loopCycle > 0.70f)
                {
                    float lt = (loopCycle - 0.70f) / 0.30f; // 0 to 1
                    float loopAngle = lt * Mathf.PI * 2f;
                    y += (1f - Mathf.Cos(loopAngle)) * 0.12f;
                    z += -Mathf.Sin(loopAngle) * 0.08f;
                }

                return _centerOrigin + new Vector3(x, y, z);
            }

            case AirplaneFlightPattern.LoopDeLoop:
            {
                // Smooth wide oval with a vertical loop-de-loop section
                float angle = t * 0.8f;
                float x = Mathf.Sin(angle) * _patrolRadius.x;
                float z = Mathf.Cos(angle) * _patrolRadius.z;

                float loopPhase = Mathf.Repeat(t * 0.4f, 1f);
                float loopY = 0f;
                float loopZOffset = 0f;

                if (loopPhase > 0.4f && loopPhase < 0.8f)
                {
                    float lt = (loopPhase - 0.4f) / 0.4f;
                    float loopAngle = lt * Mathf.PI * 2f;
                    loopY = (1f - Mathf.Cos(loopAngle)) * 0.5f * _patrolRadius.y * 1.5f;
                    loopZOffset = -Mathf.Sin(loopAngle) * 0.15f;
                }
                else
                {
                    loopY = Mathf.Sin(angle * 2f) * (_patrolRadius.y * 0.35f);
                }

                return _centerOrigin + new Vector3(x, loopY, z + loopZOffset);
            }

            case AirplaneFlightPattern.FigureEight:
            {
                float angle = t;
                float x = Mathf.Sin(angle) * _patrolRadius.x;
                float z = Mathf.Sin(angle * 2f) * 0.5f * _patrolRadius.z;
                float y = Mathf.Cos(angle * 2f) * (_patrolRadius.y * 0.4f);
                return _centerOrigin + new Vector3(x, y, z);
            }

            case AirplaneFlightPattern.Orbit:
            {
                float x = Mathf.Cos(t) * _patrolRadius.x;
                float z = Mathf.Sin(t) * _patrolRadius.z;
                float y = Mathf.Sin(t * 3f) * (_patrolRadius.y * 0.25f);
                return _centerOrigin + new Vector3(x, y, z);
            }
        }
    }

    /// <summary>
    /// Reacts to ball collisions with an acrobatic recovery spin.
    /// </summary>
    public void HitByBall(Vector3 impactVelocity)
    {
        if (_spinOutCoroutine != null) StopCoroutine(_spinOutCoroutine);
        _spinOutCoroutine = StartCoroutine(SpinOutRecoveryRoutine(impactVelocity));
    }

    private IEnumerator SpinOutRecoveryRoutine(Vector3 impactVel)
    {
        _isSpinningOut = true;
        float duration = 0.60f;
        float elapsed = 0f;

        Vector3 pushDir = (impactVel.normalized + Vector3.up * 0.4f).normalized;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float ease = 1f - t;

            // Push & tumble
            transform.position += pushDir * (Time.deltaTime * 0.6f * ease);
            transform.Rotate(new Vector3(360f, 720f, 180f) * (Time.deltaTime * ease), Space.Self);

            yield return null;
        }

        _lastPosition = transform.position;
        _isSpinningOut = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        PaperBall ball = other.GetComponent<PaperBall>() ?? other.GetComponentInParent<PaperBall>();
        if (ball != null)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            Vector3 vel = rb != null ? rb.linearVelocity : transform.forward * 2f;

            // 1. Acrobatic plane spinout
            HitByBall(vel);

            // 2. Different dedicated deflection SFX
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayAirplaneDeflectSound();

            // 3. Subtle, clean light hit VFX
            if (VFXManager.Instance != null)
                VFXManager.Instance.PlayAirplaneHitVFX(transform.position);

            // 4. Inelastic paper deflection physics
            if (rb != null)
            {
                rb.linearVelocity = Vector3.Reflect(vel, Vector3.up) * 0.35f + Vector3.up * 0.5f + Random.insideUnitSphere * 0.2f;
            }
        }
    }
}
