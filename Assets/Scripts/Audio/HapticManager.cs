using UnityEngine;

/// <summary>
/// Provides haptic vibration feedback for toss actions, basket scores, and button clicks.
/// Respects SettingsManager haptics toggle state.
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        PaperBall.OnBallLaunched += HandleBallLaunched;
        ScoreTrigger.OnSuccessfulScore += HandleScore;
    }

    private void OnDisable()
    {
        PaperBall.OnBallLaunched -= HandleBallLaunched;
        ScoreTrigger.OnSuccessfulScore -= HandleScore;
    }

    private void HandleBallLaunched(PaperBall ball)
    {
        PlayLightHaptic();
    }

    private void HandleScore()
    {
        PlaySuccessHaptic();
    }

    public void PlayLightHaptic()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.HapticsEnabled) return;

#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }

    public void PlaySuccessHaptic()
    {
        if (SettingsManager.Instance != null && !SettingsManager.Instance.HapticsEnabled) return;

#if UNITY_IOS || UNITY_ANDROID
        Handheld.Vibrate();
#endif
    }
}
