using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the retro GBA / 8-bit arcade scoreboard UI.
/// Tracks zero-padded scores (0000), combo multipliers, high scores, and arcade popup animations.
/// Listens to ScoreTrigger and PaperBall events.
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _streakText;
    [SerializeField] private TextMeshProUGUI _bestText;
    [SerializeField] private TextMeshProUGUI _popupText;

    [Header("UI Panels & Controls")]
    [SerializeField] private GameObject _scoreboardPanel;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _settingsButton;

    [Header("Score Settings")]
    [SerializeField] private int _pointsPerScore = 1;

    private int _currentScore = 0;
    private int _currentStreak = 0;
    private int _bestScore = 0;
    private int _lastScoredThrowId = -1;
    private int _lastLaunchedThrowId = -1;
    private Coroutine _popupCoroutine;
    private Coroutine _scorePunchCoroutine;

    public int CurrentStreak => _currentStreak;
    public int CurrentScore => _currentScore;

    private const string BestScorePrefKey = "AR_PaperToss_BestScore";

    private void Awake()
    {
        _bestScore = PlayerPrefs.GetInt(BestScorePrefKey, 0);
        UpdateDisplay();

        // Auto-find references if not assigned in inspector
        if (_scoreboardPanel == null)
            _scoreboardPanel = GameObject.Find("ScoreboardPanel");

        if (_scoreboardPanel != null)
        {
            if (_scoreboardPanel.GetComponent<SafeArea>() == null)
                _scoreboardPanel.AddComponent<SafeArea>();

            if (_refreshButton == null)
            {
                var refBtnObj = _scoreboardPanel.transform.Find("RefreshButton");
                if (refBtnObj != null) _refreshButton = refBtnObj.GetComponent<Button>();
            }

            if (_settingsButton == null)
            {
                var setBtnObj = _scoreboardPanel.transform.Find("SettingsButton");
                if (setBtnObj != null) _settingsButton = setBtnObj.GetComponent<Button>();
            }

            // Hide scoreboard initially until game starts
            _scoreboardPanel.SetActive(false);
        }

        if (_popupText != null)
            _popupText.gameObject.SetActive(false);
    }

    public void SetScoreboardVisible(bool visible)
    {
        if (_scoreboardPanel != null)
            _scoreboardPanel.SetActive(visible);
    }

    private void OnEnable()
    {
        ScoreTrigger.OnSuccessfulScore += HandleScore;
        PaperBall.OnBallScored += HandleBallScored;
        PaperBall.OnBallMissed += HandleBallMissed;
        PaperBall.OnBallLaunched += HandleBallLaunched;
        PlacementController.OnTrashCanPlaced += HandleCanPlaced;
        SettingsManager.OnHighScoreReset += HandleHighScoreReset;

        if (_refreshButton != null)
        {
            _refreshButton.onClick.RemoveListener(RefreshGame);
            _refreshButton.onClick.AddListener(RefreshGame);
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.RemoveListener(OpenSettings);
            _settingsButton.onClick.AddListener(OpenSettings);
        }
    }

    private void OnDisable()
    {
        ScoreTrigger.OnSuccessfulScore -= HandleScore;
        PaperBall.OnBallScored -= HandleBallScored;
        PaperBall.OnBallMissed -= HandleBallMissed;
        PaperBall.OnBallLaunched -= HandleBallLaunched;
        PlacementController.OnTrashCanPlaced -= HandleCanPlaced;
        SettingsManager.OnHighScoreReset -= HandleHighScoreReset;

        if (_refreshButton != null)
            _refreshButton.onClick.RemoveListener(RefreshGame);

        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(OpenSettings);
    }

    private void HandleHighScoreReset()
    {
        _bestScore = 0;
        UpdateDisplay();
        ShowPopup("[ BEST RESET ]", new Color(1f, 0.4f, 0.4f, 1f));
    }

    private void OpenSettings()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        var settingsUI = FindFirstObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
        if (settingsUI != null)
        {
            settingsUI.OpenSettings();
        }
    }

    /// <summary>
    /// Triggered by the Refresh button or externally to reset the game run and re-enable AR trash can positioning.
    /// </summary>
    public void RefreshGame()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _currentScore = 0;
        _currentStreak = 0;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResetStreakSFX();
        UpdateDisplay();

        // 1. Reset AR placement to let player reposition trash can
        var placementController = FindFirstObjectByType<PlacementController>();
        if (placementController != null)
        {
            placementController.ResetPlacement();
        }

        // 2. Clear all loose paper balls and reset launcher
        var launcher = FindFirstObjectByType<PaperBallLauncher>();
        if (launcher != null)
        {
            launcher.ResetLauncher();
        }

        // 3. Animate refresh button punch
        if (_refreshButton != null)
        {
            StartCoroutine(RetroPunchScale(_refreshButton.transform, 1.25f, 0.18f));
        }

        // 4. Show retro feedback popup
        ShowPopup("[ REFRESH ]", new Color(0f, 0.9f, 1f, 1f));
    }

    private void HandleCanPlaced()
    {
        if (_scoreboardPanel != null)
            _scoreboardPanel.SetActive(true);
    }

    private void HandleScore()
    {
        _currentStreak++;
        _currentScore += _pointsPerScore;

        bool isNewRecord = false;
        if (_currentScore > _bestScore)
        {
            _bestScore = _currentScore;
            PlayerPrefs.SetInt(BestScorePrefKey, _bestScore);
            PlayerPrefs.Save();
            isNewRecord = true;
        }

        UpdateDisplay();

        // Retro stepped punch animation on score text
        if (_scoreText != null)
        {
            if (_scorePunchCoroutine != null) StopCoroutine(_scorePunchCoroutine);
            _scorePunchCoroutine = StartCoroutine(RetroPunchScale(_scoreText.transform, 1.35f, 0.22f));
        }

        // Show retro floating combo banner
        string popupMsg;
        Color popupColor;

        if (isNewRecord && _currentScore > 1)
        {
            popupMsg = "* NEW RECORD! *";
            popupColor = new Color(0.2f, 1.0f, 0.4f, 1.0f); // Phosphor Green
        }
        else if (_currentStreak > 1)
        {
            popupMsg = $"* COMBO x{_currentStreak}! *";
            popupColor = new Color(1.0f, 0.85f, 0.1f, 1.0f); // Retro Gold
        }
        else
        {
            popupMsg = "* SWISH! *";
            popupColor = new Color(0.2f, 1.0f, 0.4f, 1.0f); // Retro Green
        }

        ShowPopup(popupMsg, popupColor);
    }

    private void HandleBallScored(PaperBall ball)
    {
        if (ball != null)
        {
            _lastScoredThrowId = ball.ThrowId;
        }
    }

    private void HandleBallLaunched(PaperBall ball)
    {
        if (ball != null)
        {
            _lastLaunchedThrowId = ball.ThrowId;
        }
    }

    private void HandleBallMissed(PaperBall ball)
    {
        if (ball == null || ball.HasScored) return;

        // If this miss was thrown BEFORE our latest basket, IGNORE IT!
        // (Prevents a delayed miss timer from wiping a fresh score made right after)
        if (ball.ThrowId < _lastScoredThrowId) return;

        if (_currentStreak > 0)
        {
            _currentStreak = 0;
            UpdateDisplay();
            ShowPopup("[ MISS! ]", new Color(1f, 0.25f, 0.35f, 1f));
        }
    }

    private void UpdateDisplay()
    {
        if (_scoreText != null)
            _scoreText.text = _currentScore.ToString("D4"); // Zero-padded 8-bit score (e.g. 0000, 0001, 0025)

        if (_streakText != null)
        {
            if (_currentStreak > 1)
            {
                _streakText.text = $"COMBO x{_currentStreak}";
                _streakText.gameObject.SetActive(true);
            }
            else
            {
                _streakText.gameObject.SetActive(false);
            }
        }

        if (_bestText != null)
            _bestText.text = $"HI {_bestScore:D4}";
    }

    private void ShowPopup(string text, Color color)
    {
        if (_popupText == null) return;

        if (_popupCoroutine != null)
            StopCoroutine(_popupCoroutine);

        _popupCoroutine = StartCoroutine(PopupRoutine(text, color));
    }

    private IEnumerator PopupRoutine(string text, Color color)
    {
        _popupText.text = text;
        _popupText.color = color;
        _popupText.gameObject.SetActive(true);
        _popupText.transform.localScale = Vector3.one * 0.8f;

        float elapsed = 0f;
        float duration = 0.85f;

        // Position popup dynamically underneath the scoreboard panel
        Vector3 basePos = _popupText.transform.localPosition;
        if (_scoreboardPanel != null)
        {
            RectTransform panelRt = _scoreboardPanel.GetComponent<RectTransform>();
            if (panelRt != null)
            {
                basePos = new Vector3(0f, panelRt.anchoredPosition.y - panelRt.rect.height - 20f, 0f);
            }
        }
        _popupText.transform.localPosition = basePos;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Stepped scale animation for retro pixel feel
            float scale = Mathf.Lerp(1.25f, 1.0f, t);
            _popupText.transform.localScale = Vector3.one * scale;
            _popupText.transform.localPosition = basePos + new Vector3(0f, t * 35f, 0f);

            // Retro arcade blinking near the end of the duration
            Color c = color;
            if (t > 0.6f)
            {
                // Blink effect
                float blink = Mathf.Floor((t - 0.6f) * 15f) % 2 == 0 ? 1f : 0.2f;
                c.a = Mathf.Clamp01((1f - t) * 2.5f) * blink;
            }
            else
            {
                c.a = 1f;
            }
            _popupText.color = c;

            yield return null;
        }

        _popupText.transform.localPosition = basePos;
        _popupText.gameObject.SetActive(false);
    }

    private IEnumerator RetroPunchScale(Transform target, float punchScale, float duration)
    {
        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Stepped spring bounce for authentic retro vibe
            float s = Mathf.Sin(t * Mathf.PI) * (punchScale - 1f) + 1f;
            s = Mathf.Round(s * 20f) / 20f; // Quantize scale to steps
            target.localScale = originalScale * s;
            yield return null;
        }
        target.localScale = originalScale;
    }

    public void ResetScore()
    {
        _currentScore = 0;
        _currentStreak = 0;
        UpdateDisplay();
    }
}
