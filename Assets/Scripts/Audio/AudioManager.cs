using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central 8-bit Audio Manager that plays retro sound effects for throws, combo streaks, impacts, misses, and UI.
/// Features a self-healing AudioSource pool and explicit clip preloading.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Source Pool")]
    [SerializeField] private int _poolSize = 8;
    private List<AudioSource> _sources = new List<AudioSource>();

    [Header("Throw SFX")]
    [SerializeField] private AudioClip[] _throwClips;

    [Header("Score SFX (Tiered by Combo Streak)")]
    [SerializeField] private AudioClip _scoreTier1Clip;
    [SerializeField] private AudioClip _scoreTier2Clip;
    [SerializeField] private AudioClip _scoreTier3Clip;
    [SerializeField] private AudioClip _scoreTier4Clip;
    [SerializeField] private AudioClip _scoreTier5MegaClip;
    [SerializeField] private AudioClip _newRecordClip;

    [Header("Impact SFX")]
    [SerializeField] private AudioClip[] _impactClips;

    [Header("Miss / Error SFX")]
    [SerializeField] private AudioClip _missClip;

    [Header("Placement SFX")]
    [SerializeField] private AudioClip _placementClip;

    [Header("UI SFX")]
    [SerializeField] private AudioClip _buttonClickClip;
    [SerializeField] private AudioClip _menuOpenClip;
    [SerializeField] private AudioClip _menuCloseClip;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeSources();
    }

    private void OnEnable()
    {
        InitializeSources();

        PaperBall.OnBallLaunched += HandleBallLaunched;
        PaperBall.OnBallMissed += HandleBallMissed;
        PlacementController.OnTrashCanPlaced += HandleCanPlaced;
        ScoreTrigger.OnSuccessfulScore += HandleScore;
        SettingsManager.OnPauseStateChanged += HandlePauseStateChanged;
    }

    private void OnDisable()
    {
        PaperBall.OnBallLaunched -= HandleBallLaunched;
        PaperBall.OnBallMissed -= HandleBallMissed;
        PlacementController.OnTrashCanPlaced -= HandleCanPlaced;
        ScoreTrigger.OnSuccessfulScore -= HandleScore;
        SettingsManager.OnPauseStateChanged -= HandlePauseStateChanged;
    }

    private void InitializeSources()
    {
        if (_sources == null) _sources = new List<AudioSource>();
        _sources.Clear();

        // Find existing AudioSources on this object
        var existing = GetComponents<AudioSource>();
        foreach (var s in existing)
        {
            s.playOnAwake = false;
            s.spatialBlend = 0f; // 2D Sound
            s.volume = 1f;
            _sources.Add(s);
        }

        // Fill pool up to _poolSize
        while (_sources.Count < _poolSize)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.volume = 1f;
            _sources.Add(src);
        }
    }

    private void HandleBallLaunched(PaperBall ball)
    {
        PlayRandomClip(_throwClips, 0.95f, 0.95f, 1.15f);
    }

    private void HandleScore()
    {
        int streak = 1;
        var scoreboard = FindFirstObjectByType<ScoreboardUI>();
        if (scoreboard != null)
        {
            streak = scoreboard.CurrentStreak;
        }

        PlayScoreSound(streak);
    }

    private bool _hasTriggeredMaxMultiplierSFX = false;

    private void HandleBallMissed(PaperBall ball)
    {
        _hasTriggeredMaxMultiplierSFX = false;
        PlayClip(_missClip, 0.85f, 1.0f);
    }

    private void HandleCanPlaced()
    {
        _hasTriggeredMaxMultiplierSFX = false;
        PlayClip(_placementClip, 1.0f, 1.0f);
    }

    private void HandlePauseStateChanged(bool isOpen)
    {
        if (isOpen)
            PlayClip(_menuOpenClip, 0.9f, 1.0f);
        else
            PlayClip(_menuCloseClip, 0.9f, 1.0f);
    }

    public void ResetStreakSFX()
    {
        _hasTriggeredMaxMultiplierSFX = false;
    }

    public void PlayScoreSound(int streak)
    {
        if (streak <= 1)
        {
            _hasTriggeredMaxMultiplierSFX = false;
            PlayClip(_scoreTier1Clip, 1.0f, 1.0f);
        }
        else if (streak == 2)
        {
            PlayClip(_scoreTier2Clip, 1.0f, 1.05f);
        }
        else if (streak == 3)
        {
            PlayClip(_scoreTier3Clip, 1.0f, 1.10f);
        }
        else if (streak == 4)
        {
            PlayClip(_scoreTier4Clip, 1.0f, 1.15f);
        }
        else
        {
            // Last / Max Multiplier Tier (5x+)
            if (!_hasTriggeredMaxMultiplierSFX)
            {
                // Triggered ONLY the first time reaching the last multiplier
                _hasTriggeredMaxMultiplierSFX = true;
                PlayClip(_scoreTier5MegaClip, 1.0f, 1.0f);
            }
            else
            {
                // Subsequent consecutive baskets maintain high-energy tier 4 sound without repeating the full fanfare
                float pitch = Random.Range(1.15f, 1.25f);
                PlayClip(_scoreTier4Clip != null ? _scoreTier4Clip : _scoreTier1Clip, 1.0f, pitch);
            }
        }
    }

    public void PlayImpactSound(float speed = 1.0f)
    {
        float vol = Mathf.Clamp(0.5f + speed * 0.15f, 0.5f, 1.0f);
        PlayRandomClip(_impactClips, vol, 0.9f, 1.15f);
    }

    public void PlayButtonClick()
    {
        PlayClip(_buttonClickClip, 0.95f, 1.0f);
    }

    public void PlayNewRecordSound()
    {
        PlayClip(_newRecordClip != null ? _newRecordClip : _scoreTier5MegaClip, 1.0f, 1.0f);
    }

    public void PlayClip(AudioClip clip, float volume = 1f, float pitch = 1f)
    {
        if (clip == null) return;
        if (SettingsManager.Instance != null && !SettingsManager.Instance.SFXEnabled) return;

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            clip.LoadAudioData();
        }

        AudioSource src = GetAvailableSource();
        if (src != null)
        {
            src.pitch = pitch;
            src.PlayOneShot(clip, volume);
            Debug.Log($"[AudioManager] Played SFX: '{clip.name}' (Vol: {volume}, Pitch: {pitch:F2})");
        }
    }

    public void PlayRandomClip(AudioClip[] clips, float volume = 1f, float minPitch = 0.95f, float maxPitch = 1.05f)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        float pitch = Random.Range(minPitch, maxPitch);
        PlayClip(clip, volume, pitch);
    }

    private AudioSource GetAvailableSource()
    {
        if (_sources == null || _sources.Count == 0)
        {
            InitializeSources();
        }

        foreach (var s in _sources)
        {
            if (s != null && !s.isPlaying) return s;
        }

        return _sources != null && _sources.Count > 0 ? _sources[0] : null;
    }
}
