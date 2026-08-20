using System;
using UnityEngine;

/// <summary>
/// Attached to the PaperBall prefab.
/// Manages physics, wind force while in-flight, collision detection, and auto-cleanup.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PaperBall : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Seconds after landing before this ball is destroyed.")]
    [SerializeField] private float _autoDestroyDelay = 4.5f;

    [Header("Paper Aerodynamics")]
    [Tooltip("Air resistance factor that slows the ball naturally in flight.")]
    [SerializeField] private float _airDrag = 0.85f;
    [Tooltip("Subtle air wobble/flutter effect typical of lightweight crumpled paper.")]
    [SerializeField] private float _flutterStrength = 0.35f;

    private Rigidbody _rb;
    private bool _isInFlight = false;
    private bool _hasScored = false;
    private float _flightTime = 0f;
    private Vector3 _flutterNoiseOffset;

    private static int _globalThrowCounter = 0;
    private int _throwId = 0;

    public bool IsInFlight => _isInFlight;
    public bool HasScored => _hasScored;
    public int ThrowId => _throwId;

    public static event Action<PaperBall> OnBallLaunched;
    public static event Action<PaperBall> OnBallLanded;
    public static event Action<PaperBall> OnBallScored;
    public static event Action<PaperBall> OnBallMissed;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb != null)
        {
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
        _flutterNoiseOffset = new Vector3(UnityEngine.Random.value * 100f, UnityEngine.Random.value * 100f, UnityEngine.Random.value * 100f);
    }

    /// <summary>
    /// Launches the ball with a direct velocity vector (m/s) and spin.
    /// </summary>
    public void Launch(Vector3 velocity, Vector3 torque)
    {
        _throwId = ++_globalThrowCounter;
        _isInFlight = true;
        _flightTime = 0f;
        _rb.isKinematic = false;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        // Use VelocityChange so launch speed is directly in m/s regardless of mass
        _rb.AddForce(velocity, ForceMode.VelocityChange);
        _rb.AddTorque(torque, ForceMode.VelocityChange);

        OnBallLaunched?.Invoke(this);
    }

    private void Update()
    {
        // Safety cleanup if a ball somehow falls into the void
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        if (!_isInFlight) return;

        // Anti-tunneling sweep against thin or single-sided mesh walls
        PreventTunneling();

        if (!_isInFlight) return;

        _flightTime += Time.fixedDeltaTime;

        // 1. Realistic Quadratic Air Resistance (lightweight paper deceleration)
        Vector3 vel = _rb.linearVelocity;
        float speed = vel.magnitude;
        if (speed > 0.01f)
        {
            // Aerodynamic drag opposing direction of travel
            Vector3 dragForce = -vel.normalized * (_airDrag * speed * speed * 0.5f * _rb.mass);
            _rb.AddForce(dragForce, ForceMode.Force);
        }

        // 2. Micro-Turbulence / Flutter (crumpled paper floats with slight chaotic wobble)
        float noiseX = (Mathf.PerlinNoise(_flutterNoiseOffset.x, _flightTime * 8f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(_flutterNoiseOffset.y, _flightTime * 8f) - 0.5f) * 2f;
        float noiseZ = (Mathf.PerlinNoise(_flutterNoiseOffset.z, _flightTime * 8f) - 0.5f) * 2f;
        Vector3 flutter = new Vector3(noiseX, noiseY * 0.5f, noiseZ) * (_flutterStrength * Mathf.Clamp01(speed / 2f) * _rb.mass);
        _rb.AddForce(flutter, ForceMode.Force);

        // 3. Environmental Wind Force
        if (WindManager.Instance != null)
        {
            Vector3 windForce = WindManager.Instance.CurrentWindVector;
            _rb.AddForce(windForce, ForceMode.Force);
        }

        // 4. Low-speed settling once landed: real paper balls don't roll endlessly
        if (!_isInFlight && _rb != null)
        {
            if (_rb.linearVelocity.sqrMagnitude < 0.04f)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void PreventTunneling()
    {
        if (_rb == null) return;

        Vector3 vel = _rb.linearVelocity;
        float stepDist = vel.magnitude * Time.fixedDeltaTime;
        if (stepDist > 0.005f)
        {
            float radius = transform.lossyScale.x * 0.5f; // Ball radius in world space (~0.04m)
            if (Physics.SphereCast(transform.position, radius * 0.85f, vel.normalized, out RaycastHit hit, stepDist + 0.02f, ~0, QueryTriggerInteraction.Ignore))
            {
                if (!hit.collider.isTrigger)
                {
                    transform.position = hit.point + hit.normal * (radius + 0.005f);
                    // Inelastic paper absorption on impact: dead thud with 20% bounce
                    _rb.linearVelocity = Vector3.Reflect(vel, hit.normal) * 0.20f;
                    ApplyLandingDamping();
                    _isInFlight = false;

                    if (VFXManager.Instance != null && vel.magnitude > 0.6f)
                        VFXManager.Instance.PlayImpactPoof(hit.point, hit.normal, vel.magnitude);

                    if (AudioManager.Instance != null && vel.magnitude > 0.4f)
                        AudioManager.Instance.PlayImpactSound(vel.magnitude);

                    var canAnim = hit.collider.GetComponentInParent<TrashCanAnimator>();
                    if (canAnim != null)
                        canAnim.PlayRimHitRecoil(hit.point, vel);

                    OnBallLanded?.Invoke(this);
                    StartCoroutine(EvaluateScoreOrMissRoutine());
                    Destroy(gameObject, _autoDestroyDelay);
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isInFlight) return;

        _isInFlight = false;

        float impactSpeed = collision.relativeVelocity.magnitude;
        if (collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            if (VFXManager.Instance != null && impactSpeed > 0.6f)
                VFXManager.Instance.PlayImpactPoof(contact.point, contact.normal, impactSpeed);

            var canAnim = collision.gameObject.GetComponentInParent<TrashCanAnimator>();
            if (canAnim != null)
                canAnim.PlayRimHitRecoil(contact.point, collision.relativeVelocity);
        }

        if (AudioManager.Instance != null && impactSpeed > 0.4f)
        {
            AudioManager.Instance.PlayImpactSound(impactSpeed);
        }

        OnBallLanded?.Invoke(this);

        // Real crumpled paper absorbs 75%+ of kinetic energy upon impact (dead thud)
        _rb.linearVelocity *= 0.25f;
        ApplyLandingDamping();

        StartCoroutine(EvaluateScoreOrMissRoutine());
        Destroy(gameObject, _autoDestroyDelay);
    }

    private System.Collections.IEnumerator EvaluateScoreOrMissRoutine()
    {
        // Give 1.5 seconds grace period for rim rolls, bounces, and bank shots to drop in
        float timer = 0f;
        while (timer < 1.5f)
        {
            if (_hasScored) yield break; // Made the basket! Don't trigger miss
            timer += Time.deltaTime;
            yield return null;
        }

        if (!_hasScored)
        {
            OnBallMissed?.Invoke(this);
        }
    }

    private void ApplyLandingDamping()
    {
        if (_rb == null) return;
        // Crumpled paper facets catch the floor: heavy rolling & linear resistance
        _rb.linearDamping = 3.5f;
        _rb.angularDamping = 7.0f;
        _rb.angularVelocity *= 0.15f;
    }

    public void MarkScored()
    {
        if (_hasScored) return;
        _hasScored = true;
        OnBallScored?.Invoke(this);
    }
}
