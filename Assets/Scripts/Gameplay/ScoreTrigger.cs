using UnityEngine;

/// <summary>
/// Placed on a trigger collider inside the interior of the trash can.
/// Validates entry into the inner cavity (allows rim rolls and bank shots, rejects outside misses).
/// </summary>
public class ScoreTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Particle system to play on a successful score.")]
    [SerializeField] private ParticleSystem _scoreConfetti;

    [Header("Validation")]
    [Tooltip("Maximum horizontal distance from center axis allowed to count as inside the can (local space). Inner opening is ~0.75m radius.")]
    [SerializeField] private float _maxInnerRadius = 0.78f;

    [Tooltip("Maximum upward velocity allowed when scoring.")]
    [SerializeField] private float _maxUpwardVelocity = 0.35f;

    public static event System.Action OnSuccessfulScore;

    private void OnTriggerEnter(Collider other)
    {
        PaperBall ball = other.GetComponent<PaperBall>();
        if (ball == null)
            ball = other.GetComponentInParent<PaperBall>();

        if (ball != null && !ball.HasScored)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb == null) return;

            // 1. Validate ball is not flying fast upwards
            if (rb.linearVelocity.y > _maxUpwardVelocity) return;

            // 2. Validate ball is within the full inner rim radius of the can (0.78m)
            Vector3 localPos = transform.InverseTransformPoint(ball.transform.position);
            float horizontalDistance = new Vector2(localPos.x, localPos.z).magnitude;

            if (horizontalDistance <= _maxInnerRadius)
            {
                ball.MarkScored();

                if (_scoreConfetti != null)
                    _scoreConfetti.Play();

                OnSuccessfulScore?.Invoke();
                Debug.Log($"[ScoreTrigger] SWISH! Basket registered at localPos: {localPos}, dist: {horizontalDistance:F3}m <= {_maxInnerRadius}m");
            }
            else
            {
                Debug.Log($"[ScoreTrigger] Rejecting ball outside inner radius. Distance: {horizontalDistance:F3}m > {_maxInnerRadius}m");
            }
        }
    }
}
