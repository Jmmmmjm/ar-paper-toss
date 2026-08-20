using UnityEngine;

/// <summary>
/// Placed on a trigger collider inside the trash can opening.
/// Validates downward entry (prevents scoring from below or sides) and marks the ball as scored.
/// </summary>
public class ScoreTrigger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Particle system to play on a successful score.")]
    [SerializeField] private ParticleSystem _scoreConfetti;

    [Header("Validation")]
    [Tooltip("Minimum downward vertical velocity required to count as entering the top.")]
    [SerializeField] private float _minDownwardSpeed = 0.1f;

    public static event System.Action OnSuccessfulScore;

    private void OnTriggerEnter(Collider other)
    {
        PaperBall ball = other.GetComponent<PaperBall>();
        if (ball == null)
            ball = other.GetComponentInParent<PaperBall>();

        if (ball != null && !ball.HasScored)
        {
            // Validate ball is moving downwards into the can
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null && rb.linearVelocity.y <= _minDownwardSpeed)
            {
                ball.MarkScored();

                if (_scoreConfetti != null)
                    _scoreConfetti.Play();

                OnSuccessfulScore?.Invoke();
                Debug.Log("[ScoreTrigger] SWISH! Ball scored!");
            }
        }
    }
}
