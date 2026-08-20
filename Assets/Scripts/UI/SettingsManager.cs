using System;
using UnityEngine;

/// <summary>
/// Central manager for user preferences: Audio, Haptics, Throw Sensitivity, and Score Resets.
/// Persists settings across sessions via PlayerPrefs.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    public enum Sensitivity
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    private const string SFXPrefKey = "AR_PaperToss_SFX";
    private const string HapticsPrefKey = "AR_PaperToss_Haptics";
    private const string SensitivityPrefKey = "AR_PaperToss_Sensitivity";
    public const string BestScorePrefKey = "AR_PaperToss_BestScore";

    public bool SFXEnabled { get; private set; } = true;
    public bool HapticsEnabled { get; private set; } = true;
    public Sensitivity CurrentSensitivity { get; private set; } = Sensitivity.Normal;

    public static event Action<bool> OnSFXToggled;
    public static event Action<bool> OnHapticsToggled;
    public static event Action<float> OnSensitivityMultiplierChanged;
    public static event Action OnHighScoreReset;
    public static event Action<bool> OnPauseStateChanged; // true when menu is open

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        LoadSettings();
    }

    private void LoadSettings()
    {
        SFXEnabled = PlayerPrefs.GetInt(SFXPrefKey, 1) == 1;
        HapticsEnabled = PlayerPrefs.GetInt(HapticsPrefKey, 1) == 1;
        CurrentSensitivity = (Sensitivity)PlayerPrefs.GetInt(SensitivityPrefKey, (int)Sensitivity.Normal);
    }

    public void ToggleSFX()
    {
        SFXEnabled = !SFXEnabled;
        PlayerPrefs.SetInt(SFXPrefKey, SFXEnabled ? 1 : 0);
        PlayerPrefs.Save();
        OnSFXToggled?.Invoke(SFXEnabled);
    }

    public void ToggleHaptics()
    {
        HapticsEnabled = !HapticsEnabled;
        PlayerPrefs.SetInt(HapticsPrefKey, HapticsEnabled ? 1 : 0);
        PlayerPrefs.Save();
        OnHapticsToggled?.Invoke(HapticsEnabled);
    }

    public void CycleSensitivity()
    {
        CurrentSensitivity = (Sensitivity)(((int)CurrentSensitivity + 1) % 3);
        PlayerPrefs.SetInt(SensitivityPrefKey, (int)CurrentSensitivity);
        PlayerPrefs.Save();
        OnSensitivityMultiplierChanged?.Invoke(GetSensitivityMultiplier());
    }

    public float GetSensitivityMultiplier()
    {
        switch (CurrentSensitivity)
        {
            case Sensitivity.Low: return 0.40f;
            case Sensitivity.Normal: return 0.60f;
            case Sensitivity.High: return 0.90f;
            default: return 0.60f;
        }
    }

    public string GetSensitivityLabel()
    {
        switch (CurrentSensitivity)
        {
            case Sensitivity.Low: return "LOW";
            case Sensitivity.Normal: return "MED";
            case Sensitivity.High: return "HIGH";
            default: return "MED";
        }
    }

    public void ResetBestScore()
    {
        PlayerPrefs.DeleteKey(BestScorePrefKey);
        PlayerPrefs.Save();
        OnHighScoreReset?.Invoke();
    }

    public void SetGamePaused(bool paused)
    {
        OnPauseStateChanged?.Invoke(paused);
    }
}
