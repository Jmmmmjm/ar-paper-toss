using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the retro 8-bit Settings modal window.
/// Handles audio/haptic toggles, throw sensitivity adjustments, best score resetting, and game pause.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    [Header("Modal Containers")]
    [SerializeField] private GameObject _modalRoot;
    [SerializeField] private RectTransform _modalDialog;

    [Header("Button Controls")]
    [SerializeField] private Button _sfxButton;
    [SerializeField] private TextMeshProUGUI _sfxText;

    [SerializeField] private Button _hapticsButton;
    [SerializeField] private TextMeshProUGUI _hapticsText;

    [SerializeField] private Button _sensitivityButton;
    [SerializeField] private TextMeshProUGUI _sensitivityText;

    [SerializeField] private Button _resetScoreButton;
    [SerializeField] private TextMeshProUGUI _resetScoreText;

    [SerializeField] private Button _closeButton;

    private bool _confirmingScoreReset = false;
    private Coroutine _animateCoroutine;

    private void Awake()
    {
        if (_modalRoot != null)
            _modalRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_sfxButton != null) _sfxButton.onClick.AddListener(OnSFXClicked);
        if (_hapticsButton != null) _hapticsButton.onClick.AddListener(OnHapticsClicked);
        if (_sensitivityButton != null) _sensitivityButton.onClick.AddListener(OnSensitivityClicked);
        if (_resetScoreButton != null) _resetScoreButton.onClick.AddListener(OnResetScoreClicked);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseSettings);
    }

    private void OnDisable()
    {
        if (_sfxButton != null) _sfxButton.onClick.RemoveListener(OnSFXClicked);
        if (_hapticsButton != null) _hapticsButton.onClick.RemoveListener(OnHapticsClicked);
        if (_sensitivityButton != null) _sensitivityButton.onClick.RemoveListener(OnSensitivityClicked);
        if (_resetScoreButton != null) _resetScoreButton.onClick.RemoveListener(OnResetScoreClicked);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(CloseSettings);
    }

    public void OpenSettings()
    {
        _confirmingScoreReset = false;
        UpdateUI();

        if (_modalRoot != null)
            _modalRoot.SetActive(true);

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetGamePaused(true);

        if (_modalDialog != null)
        {
            if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);
            _animateCoroutine = StartCoroutine(AnimatePop(_modalDialog, true));
        }
    }

    public void CloseSettings()
    {
        _confirmingScoreReset = false;

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetGamePaused(false);

        if (_modalDialog != null && gameObject.activeInHierarchy)
        {
            if (_animateCoroutine != null) StopCoroutine(_animateCoroutine);
            _animateCoroutine = StartCoroutine(AnimatePop(_modalDialog, false, () =>
            {
                if (_modalRoot != null) _modalRoot.SetActive(false);
            }));
        }
        else
        {
            if (_modalRoot != null) _modalRoot.SetActive(false);
        }
    }

    private void OnSFXClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ToggleSFX();
            UpdateUI();
        }
    }

    private void OnHapticsClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.ToggleHaptics();
            UpdateUI();
        }
    }

    private void OnSensitivityClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.CycleSensitivity();
            UpdateUI();
        }
    }

    private void OnResetScoreClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (!_confirmingScoreReset)
        {
            _confirmingScoreReset = true;
            if (_resetScoreText != null)
                _resetScoreText.text = "SURE? [TAP]";
        }
        else
        {
            _confirmingScoreReset = false;
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.ResetBestScore();
            }
            if (_resetScoreText != null)
                _resetScoreText.text = "CLEARED!";

            StartCoroutine(ResetScoreButtonLabelDelayed());
        }
    }

    private IEnumerator ResetScoreButtonLabelDelayed()
    {
        yield return new WaitForSeconds(1.2f);
        if (_resetScoreText != null)
            _resetScoreText.text = "RESET BEST";
    }

    private void UpdateUI()
    {
        if (SettingsManager.Instance == null) return;

        Color pocketNavy = new Color(0.133f, 0.137f, 0.235f, 1f); // #22233C (High contrast dark navy)
        Color mutedPink = new Color(0.55f, 0.35f, 0.45f, 1f);      // Muted state for OFF
        Color rubyCrimson = new Color(0.72f, 0.08f, 0.22f, 1f);    // Distinct red/crimson for Reset Best

        if (_sfxText != null)
        {
            bool sfx = SettingsManager.Instance.SFXEnabled;
            _sfxText.text = $"SFX SOUNDS: {(sfx ? "ON" : "OFF")}";
            _sfxText.color = sfx ? pocketNavy : mutedPink;
        }

        if (_hapticsText != null)
        {
            bool haptics = SettingsManager.Instance.HapticsEnabled;
            _hapticsText.text = $"VIBRATION: {(haptics ? "ON" : "OFF")}";
            _hapticsText.color = haptics ? pocketNavy : mutedPink;
        }

        if (_sensitivityText != null)
        {
            string label = SettingsManager.Instance.GetSensitivityLabel();
            _sensitivityText.text = $"FLICK POWER: {label}";
            _sensitivityText.color = pocketNavy;
        }

        if (_resetScoreText != null && !_confirmingScoreReset)
        {
            _resetScoreText.text = "RESET BEST SCORE";
            _resetScoreText.color = rubyCrimson;
        }
    }

    private IEnumerator AnimatePop(RectTransform target, bool opening, System.Action onComplete = null)
    {
        float elapsed = 0f;
        float duration = 0.18f;
        Vector3 startScale = opening ? Vector3.one * 0.7f : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : Vector3.one * 0.7f;

        target.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Stepped quantization for retro feel
            float curve = opening ? Mathf.Sin(t * Mathf.PI * 0.5f) : (1f - t);
            curve = Mathf.Round(curve * 10f) / 10f;
            target.localScale = Vector3.Lerp(startScale, endScale, curve);
            yield return null;
        }

        target.localScale = endScale;
        onComplete?.Invoke();
    }
}
