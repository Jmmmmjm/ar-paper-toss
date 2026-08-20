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

    public bool IsInFlight => _isInFlight;
    public bool HasScored => _hasScored;

    public static event Action<PaperBall> OnBallLaunched;
    public static event Action<PaperBall> OnBallLanded;
    public static event Action<PaperBall> OnBallScored;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _flutterNoiseOffset = new Vector3(UnityEngine.Random.value * 100f, UnityEngine.Random.value * 100f, UnityEngine.Random.value * 100f);
    }

    /// <summary>
    /// Launches the ball with a direct velocity vector (m/s) and spin.
    /// </summary>
    public void Launch(Vector3 velocity, Vector3 torque)
    {
        _isInFlight = true;
        _flightTime = 0f;
        _rb.isKinematic = false;
        _rb.useGravity = true;

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
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isInFlight) return;

        _isInFlight = false;
        OnBallLanded?.Invoke(this);

        // Crumpled paper energy absorption: dampen angular spin on first contact
        _rb.angularVelocity *= 0.35f;

        Destroy(gameObject, _autoDestroyDelay);
    }

    public void MarkScored()
    {
        if (_hasScored) return;
        _hasScored = true;
        OnBallScored?.Invoke(this);
    }
}
