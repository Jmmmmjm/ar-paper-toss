using System.Collections;
using UnityEngine;

/// <summary>
/// Controls procedural juice and physical reaction animations on the Trash Can:
/// - Spawn-in drop and elastic floor bounce
/// - Rim hit impact tilt and decaying spring wobble
/// - Clean score celebratory squash, stretch, and hop
/// </summary>
public class TrashCanAnimator : MonoBehaviour
{
    [Header("Visual Target")]
    [Tooltip("Child transform to animate (or this transform if null).")]
    [SerializeField] private Transform _visualModel;

    [Header("Spawn Animation")]
    [SerializeField] private float _spawnDropHeight = 0.55f;
    [SerializeField] private float _spawnDuration = 0.58f;
    [SerializeField] private float _spawnWobbleAngle = 14f;

    [Header("Score Swish Animation")]
    [SerializeField] private float _scoreHopHeight = 0.04f;
    [SerializeField] private float _scoreSquashFactor = 0.18f;
    [SerializeField] private float _scoreDuration = 0.38f;

    [Header("Impact Recoil")]
    [SerializeField] private float _maxTiltAngle = 6.5f;
    [SerializeField] private float _recoilDuration = 0.28f;

    private Vector3 _baseLocalPosition;
    private Quaternion _baseLocalRotation;
    private Vector3 _baseLocalScale;

    private Coroutine _activeAnimationCoroutine;

    private void Awake()
    {
        if (_visualModel == null)
            _visualModel = transform;

        _baseLocalPosition = _visualModel.localPosition;
        _baseLocalRotation = _visualModel.localRotation;
        _baseLocalScale = _visualModel.localScale;
    }

    private void OnEnable()
    {
        ScoreTrigger.OnSuccessfulScore += HandleScore;
    }

    private void OnDisable()
    {
        ScoreTrigger.OnSuccessfulScore -= HandleScore;
        ResetTransform();
    }

    private void HandleScore()
    {
        PlayScoreSwishAnimation();
    }

    /// <summary>
    /// Resets the animated transform back to rest pose.
    /// </summary>
    public void ResetTransform()
    {
        if (_activeAnimationCoroutine != null)
        {
            StopCoroutine(_activeAnimationCoroutine);
            _activeAnimationCoroutine = null;
        }

        if (_visualModel != null)
        {
            _visualModel.localPosition = _baseLocalPosition;
            _visualModel.localRotation = _baseLocalRotation;
            _visualModel.localScale = _baseLocalScale;
        }
    }

    /// <summary>
    /// Plays an elastic spawn-in drop bounce when the trash can is placed in AR.
    /// </summary>
    public void PlaySpawnInAnimation()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_activeAnimationCoroutine != null) StopCoroutine(_activeAnimationCoroutine);
        _activeAnimationCoroutine = StartCoroutine(SpawnInRoutine());
    }

    private IEnumerator SpawnInRoutine()
    {
        float elapsed = 0f;
        Vector3 startPos = _baseLocalPosition + Vector3.up * _spawnDropHeight;
        bool playedPoof = false;

        _visualModel.localPosition = startPos;
        _visualModel.localScale = _baseLocalScale;
        _visualModel.localRotation = _baseLocalRotation;

        while (elapsed < _spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _spawnDuration);

            // 1. Multi-Bounce Height Curve (Juicy physics-like parabolic rebounds)
            float yOffset = 0f;
            float squashY = 1f;

            if (t < 0.28f)
            {
                // Fall 1: Accelerate down
                float fallT = t / 0.28f;
                yOffset = Mathf.Lerp(_spawnDropHeight, 0f, fallT * fallT);
                // Stretch in-flight
                squashY = Mathf.Lerp(1.30f, 0.70f, fallT * fallT);
            }
            else if (t < 0.54f)
            {
                // Impact 1 dust puff
                if (!playedPoof)
                {
                    playedPoof = true;
                    if (VFXManager.Instance != null)
                        VFXManager.Instance.PlayImpactPoof(transform.position, Vector3.up, 1.2f);
                }

                // Bounce 1: Rebound to 28% height
                float bounce1T = (t - 0.28f) / 0.26f;
                yOffset = Mathf.Sin(bounce1T * Mathf.PI) * (_spawnDropHeight * 0.28f);
                // Heavy squash on landing, stretch on way up, squash on way down
                float sCurve = Mathf.Sin(bounce1T * Mathf.PI);
                squashY = Mathf.Lerp(0.60f, 1.22f, sCurve);
            }
            else if (t < 0.76f)
            {
                // Bounce 2: Rebound to 8% height
                float bounce2T = (t - 0.54f) / 0.22f;
                yOffset = Mathf.Sin(bounce2T * Mathf.PI) * (_spawnDropHeight * 0.08f);
                float sCurve = Mathf.Sin(bounce2T * Mathf.PI);
                squashY = Mathf.Lerp(0.80f, 1.10f, sCurve);
            }
            else
            {
                // Settle: Damped harmonic oscillation
                float settleT = (t - 0.76f) / 0.24f;
                float envelope = Mathf.Exp(-8f * settleT) * Mathf.Sin(settleT * Mathf.PI * 4f);
                yOffset = Mathf.Max(0f, envelope * 0.02f);
                squashY = 1f + envelope * 0.12f;
            }

            _visualModel.localPosition = _baseLocalPosition + Vector3.up * yOffset;

            // 2. Exaggerated Squash & Stretch Scale
            float stretchXZ = 1f / Mathf.Sqrt(Mathf.Max(0.1f, squashY)); // Volume preservation
            _visualModel.localScale = new Vector3(
                _baseLocalScale.x * stretchXZ,
                _baseLocalScale.y * squashY,
                _baseLocalScale.z * stretchXZ
            );

            // 3. Playful Rotational Wobble / Jelly Tilt
            float wobbleDecay = Mathf.Exp(-5.0f * t);
            float rotZ = Mathf.Sin(t * Mathf.PI * 9f) * _spawnWobbleAngle * wobbleDecay;
            float rotX = Mathf.Cos(t * Mathf.PI * 7f) * (_spawnWobbleAngle * 0.65f) * wobbleDecay;

            _visualModel.localRotation = _baseLocalRotation * Quaternion.Euler(rotX, 0f, rotZ);

            yield return null;
        }

        ResetTransform();
    }

    /// <summary>
    /// Celebratory squash, stretch, and hop triggered when a basket is made.
    /// </summary>
    public void PlayScoreSwishAnimation()
    {
        if (!gameObject.activeInHierarchy) return;

        if (_activeAnimationCoroutine != null) StopCoroutine(_activeAnimationCoroutine);
        _activeAnimationCoroutine = StartCoroutine(ScoreSwishRoutine());
    }

    private IEnumerator ScoreSwishRoutine()
    {
        float elapsed = 0f;

        while (elapsed < _scoreDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _scoreDuration);

            // Damped harmonic oscillation: sin(frequency * t) * exp(-decay * t)
            float freq = 24f;
            float decay = 8.5f;
            float envelope = Mathf.Exp(-decay * t) * Mathf.Sin(freq * t);

            // Vertical hop & squash-stretch
            float hopY = Mathf.Max(0f, envelope) * _scoreHopHeight;
            float squashY = 1f + (envelope * _scoreSquashFactor);
            float stretchXZ = 1f - (envelope * _scoreSquashFactor * 0.5f);

            _visualModel.localPosition = _baseLocalPosition + Vector3.up * hopY;
            _visualModel.localScale = new Vector3(_baseLocalScale.x * stretchXZ, _baseLocalScale.y * squashY, _baseLocalScale.z * stretchXZ);

            yield return null;
        }

        ResetTransform();
    }

    /// <summary>
    /// Tilts and shakes the trash can when hit on the rim or outer wall by the paper ball.
    /// </summary>
    public void PlayRimHitRecoil(Vector3 impactPoint, Vector3 impactVelocity)
    {
        if (!gameObject.activeInHierarchy) return;

        if (_activeAnimationCoroutine != null) StopCoroutine(_activeAnimationCoroutine);
        _activeAnimationCoroutine = StartCoroutine(RimHitRecoilRoutine(impactPoint, impactVelocity));
    }

    private IEnumerator RimHitRecoilRoutine(Vector3 impactPoint, Vector3 impactVelocity)
    {
        float speed = Mathf.Clamp(impactVelocity.magnitude, 0.5f, 5.0f);
        float tiltMagnitude = Mathf.Lerp(1.5f, _maxTiltAngle, (speed - 0.5f) / 4.5f);

        // Direction from center of trash can to impact point
        Vector3 localImpact = transform.InverseTransformPoint(impactPoint);
        Vector3 tiltAxis = Vector3.Cross(Vector3.up, new Vector3(localImpact.x, 0f, localImpact.z).normalized);

        if (tiltAxis.sqrMagnitude < 0.001f)
            tiltAxis = Vector3.right;

        float elapsed = 0f;

        while (elapsed < _recoilDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / _recoilDuration);

            // Fast decaying spring oscillation
            float freq = 32f;
            float decay = 12f;
            float envelope = Mathf.Exp(-decay * t) * Mathf.Cos(freq * t);

            float currentTilt = envelope * tiltMagnitude;
            _visualModel.localRotation = _baseLocalRotation * Quaternion.AngleAxis(currentTilt, tiltAxis);

            // Micro-squash on impact
            float squash = envelope * 0.05f;
            _visualModel.localScale = new Vector3(_baseLocalScale.x * (1f + squash), _baseLocalScale.y * (1f - squash), _baseLocalScale.z * (1f + squash));

            yield return null;
        }

        ResetTransform();
    }

    private void OnCollisionEnter(Collision collision)
    {
        PaperBall ball = collision.gameObject.GetComponent<PaperBall>();
        if (ball != null && collision.contactCount > 0)
        {
            ContactPoint contact = collision.GetContact(0);
            PlayRimHitRecoil(contact.point, collision.relativeVelocity);
        }
    }

    private static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1)
            return n1 * t * t;
        if (t < 2f / d1)
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        if (t < 2.5f / d1)
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        return n1 * (t -= 2.625f / d1) * t + 0.984375f;
    }
}
