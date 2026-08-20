using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the top scoreboard UI, tracking current score, streak, best score, and animated score popups.
/// Listens to ScoreTrigger and PaperBall events.
/// </summary>
public class ScoreboardUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _streakText;
    [SerializeField] private TextMeshProUGUI _bestText;
    [SerializeField] private TextMeshProUGUI _popupText;

    [Header("UI Panels")]
    [SerializeField] private GameObject _scoreboardPanel;

    [Header("Score Settings")]
    [SerializeField] private int _pointsPerScore = 1;

    private int _currentScore = 0;
    private int _currentStreak = 0;
    private int _bestScore = 0;
    private Coroutine _popupCoroutine;

    private const string BestScorePrefKey = "AR_PaperToss_BestScore";

    private void Awake()
    {
        _bestScore = PlayerPrefs.GetInt(BestScorePrefKey, 0);
        UpdateDisplay();

        if (_popupText != null)
            _popupText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        ScoreTrigger.OnSuccessfulScore += HandleScore;
        PaperBall.OnBallLanded += HandleBallLanded;
        PlacementController.OnTrashCanPlaced += HandleCanPlaced;
    }

    private void OnDisable()
    {
        ScoreTrigger.OnSuccessfulScore -= HandleScore;
        PaperBall.OnBallLanded -= HandleBallLanded;
        PlacementController.OnTrashCanPlaced -= HandleCanPlaced;
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

        if (_currentScore > _bestScore)
        {
            _bestScore = _currentScore;
            PlayerPrefs.SetInt(BestScorePrefKey, _bestScore);
            PlayerPrefs.Save();
        }

        UpdateDisplay();

        // Punch animation on score text
        if (_scoreText != null)
            StartCoroutine(PunchScale(_scoreText.transform, 1.3f, 0.2f));

        // Show floating combo text
        string popupMsg = _currentStreak > 1 ? $"STREAK ×{_currentStreak}!" : "SWISH!";
        ShowPopup(popupMsg, _currentStreak > 1 ? Color.yellow : Color.green);
    }

    private void HandleBallLanded(PaperBall ball)
    {
        // If the ball landed without scoring, reset streak
        if (ball != null && !ball.HasScored)
        {
            if (_currentStreak > 0)
            {
                _currentStreak = 0;
                UpdateDisplay();
                ShowPopup("MISSED!", new Color(1f, 0.4f, 0.4f, 1f));
            }
        }
    }

    private void UpdateDisplay()
    {
        if (_scoreText != null)
            _scoreText.text = _currentScore.ToString();

        if (_streakText != null)
        {
            if (_currentStreak > 1)
            {
                _streakText.text = $"🔥 ×{_currentStreak}";
                _streakText.gameObject.SetActive(true);
            }
            else
            {
                _streakText.gameObject.SetActive(false);
            }
        }

        if (_bestText != null)
            _bestText.text = $"BEST: {_bestScore}";
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
        _popupText.transform.localScale = Vector3.one * 0.7f;

        float elapsed = 0f;
        float duration = 0.8f;
        Vector3 basePos = _popupText.transform.localPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            // Scale pop and float upwards
            float scale = Mathf.Lerp(1.2f, 1.0f, t);
            _popupText.transform.localScale = Vector3.one * scale;
            _popupText.transform.localPosition = basePos + new Vector3(0f, t * 40f, 0f);

            // Fade out
            Color c = color;
            c.a = Mathf.Clamp01(1f - (t * t));
            _popupText.color = c;

            yield return null;
        }

        _popupText.transform.localPosition = basePos;
        _popupText.gameObject.SetActive(false);
    }

    private IEnumerator PunchScale(Transform target, float punchScale, float duration)
    {
        Vector3 originalScale = Vector3.one;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = Mathf.Sin(t * Mathf.PI) * (punchScale - 1f) + 1f;
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
