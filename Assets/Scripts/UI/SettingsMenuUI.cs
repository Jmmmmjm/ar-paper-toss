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

    [SerializeField] private Button _mainMenuButton;
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
        if (_mainMenuButton != null) _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        if (_closeButton != null) _closeButton.onClick.AddListener(CloseSettings);
    }

    private void OnDisable()
    {
        if (_sfxButton != null) _sfxButton.onClick.RemoveListener(OnSFXClicked);
        if (_hapticsButton != null) _hapticsButton.onClick.RemoveListener(OnHapticsClicked);
        if (_sensitivityButton != null) _sensitivityButton.onClick.RemoveListener(OnSensitivityClicked);
        if (_resetScoreButton != null) _resetScoreButton.onClick.RemoveListener(OnResetScoreClicked);
        if (_mainMenuButton != null) _mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
        if (_closeButton != null) _closeButton.onClick.RemoveListener(CloseSettings);
    }

    private bool _openedFromMainMenu = false;

    private void OnMainMenuClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        _openedFromMainMenu = false;
        CloseSettings();

        if (MainMenuUI.Instance != null)
        {
            MainMenuUI.Instance.ShowMainMenu();
        }
    }

    public void OpenSettings(bool fromMainMenu = false)
    {
        _openedFromMainMenu = fromMainMenu;
        _confirmingScoreReset = false;
        UpdateUI();

        gameObject.SetActive(true);

        if (_modalRoot != null)
        {
            _modalRoot.SetActive(true);
            _modalRoot.transform.SetAsLastSibling();
        }
        transform.SetAsLastSibling();

        // Show "RETURN TO MAIN MENU" only when in active gameplay, hide if opened directly from title screen
        if (_mainMenuButton != null)
        {
            _mainMenuButton.gameObject.SetActive(!_openedFromMainMenu);
        }

        if (SettingsManager.Instance != null)
            SettingsManager.Instance.SetGamePaused(true);

        if (_modalDialog != null && gameObject.activeInHierarchy)
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
                gameObject.SetActive(false);

                if (_openedFromMainMenu && MainMenuUI.Instance != null)
                {
                    _openedFromMainMenu = false;
                    MainMenuUI.Instance.ShowMainMenu(animate: false);
                }
            }));
        }
        else
        {
            if (_modalRoot != null) _modalRoot.SetActive(false);
            gameObject.SetActive(false);

            if (_openedFromMainMenu && MainMenuUI.Instance != null)
            {
                _openedFromMainMenu = false;
                MainMenuUI.Instance.ShowMainMenu(animate: false);
            }
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
        float duration = opening ? 0.14f : 0.08f;
        Vector3 startScale = opening ? Vector3.one * 0.85f : Vector3.one;
        Vector3 endScale = opening ? Vector3.one : Vector3.one * 0.85f;

        target.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            float curve;
            if (opening)
            {
                float c1 = 1.3f;
                float c3 = c1 + 1f;
                curve = 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
            }
            else
            {
                curve = 1f - (t * t);
            }

            target.localScale = Vector3.LerpUnclamped(startScale, endScale, curve);
            yield return null;
        }

        target.localScale = opening ? Vector3.one : endScale;
        if (!opening)
        {
            target.localScale = Vector3.one; // Reset for next open
        }
        onComplete?.Invoke();
    }
}
