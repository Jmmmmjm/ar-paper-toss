using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the retro Pocket GUI Main Menu:
/// - Handles Play Game, How to Play tutorial modal, and Settings navigation
/// - Displays persistent high scores
/// - Coordinates state transitions with PlacementController and ScoreboardUI
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    public static MainMenuUI Instance { get; private set; }

    [Header("Menu Panels")]
    [SerializeField] private GameObject _mainMenuRoot;
    [SerializeField] private RectTransform _mainMenuDialog;
    [SerializeField] private TextMeshProUGUI _bestScoreText;

    [Header("Main Menu Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _howToPlayButton;
    [SerializeField] private Button _settingsButton;

    [Header("How To Play Modal")]
    [SerializeField] private GameObject _howToPlayRoot;
    [SerializeField] private RectTransform _howToPlayDialog;
    [SerializeField] private Button _closeHowToPlayButton;

    private Coroutine _popCoroutine;
    private const string BestScorePrefKey = "AR_PaperToss_BestScore";

    public bool IsMainMenuOpen => _mainMenuRoot != null && _mainMenuRoot.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Show Main Menu on initial game launch
        ShowMainMenu();
    }

    private void OnEnable()
    {
        if (_playButton != null) _playButton.onClick.AddListener(OnPlayClicked);
        if (_howToPlayButton != null) _howToPlayButton.onClick.AddListener(OpenHowToPlay);
        if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
        if (_closeHowToPlayButton != null) _closeHowToPlayButton.onClick.AddListener(CloseHowToPlay);
    }

    private void OnDisable()
    {
        if (_playButton != null) _playButton.onClick.RemoveListener(OnPlayClicked);
        if (_howToPlayButton != null) _howToPlayButton.onClick.RemoveListener(OpenHowToPlay);
        if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (_closeHowToPlayButton != null) _closeHowToPlayButton.onClick.RemoveListener(CloseHowToPlay);
    }

    public void ShowMainMenu(bool animate = true)
    {
        UpdateBestScoreDisplay();

        if (_howToPlayRoot != null)
            _howToPlayRoot.SetActive(false);

        if (_mainMenuRoot != null)
            _mainMenuRoot.SetActive(true);

        // Hide scoreboard during main menu
        var scoreboard = FindFirstObjectByType<ScoreboardUI>(FindObjectsInactive.Include);
        if (scoreboard != null)
        {
            scoreboard.SetScoreboardVisible(false);
        }

        // Disable active throwing on menu without disabling the launcher component
        var launcher = FindFirstObjectByType<PaperBallLauncher>(FindObjectsInactive.Include);
        if (launcher != null)
        {
            launcher.enabled = true;
            launcher.DisableThrowing();
        }

        var placement = FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
        if (placement != null)
        {
            placement.enabled = false;
        }

        if (_mainMenuDialog != null)
        {
            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            if (animate)
            {
                _popCoroutine = StartCoroutine(AnimatePop(_mainMenuDialog, true));
            }
            else
            {
                _mainMenuDialog.localScale = Vector3.one;
            }
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMenuBGM();
        }
    }

    public void OnPlayClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (_mainMenuDialog != null && gameObject.activeInHierarchy)
        {
            if (_popCoroutine != null) StopCoroutine(_popCoroutine);
            _popCoroutine = StartCoroutine(AnimatePop(_mainMenuDialog, false, StartGameFlow));
        }
        else
        {
            StartGameFlow();
        }
    }

    private void StartGameFlow()
    {
        if (_mainMenuRoot != null)
            _mainMenuRoot.SetActive(false);

        // Start Battle / Gameplay BGM
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBattleBGM();
        }

        // Show Scoreboard HUD
        var scoreboard = FindFirstObjectByType<ScoreboardUI>(FindObjectsInactive.Include);
        if (scoreboard != null)
        {
            scoreboard.SetScoreboardVisible(true);
        }

        var launcher = FindFirstObjectByType<PaperBallLauncher>(FindObjectsInactive.Include);
        if (launcher != null)
        {
            launcher.enabled = true;
        }

        // Enable Placement Controller to let player scan & place trash can
        var placement = FindFirstObjectByType<PlacementController>(FindObjectsInactive.Include);
        if (placement != null)
        {
            placement.enabled = true;
            placement.ResetPlacement();
        }
        else if (launcher != null)
        {
            // If no placement controller (or standalone gameplay), enable throwing directly
            launcher.EnableThrowing();
        }
    }

    public void OpenHowToPlay()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (_mainMenuRoot != null)
            _mainMenuRoot.SetActive(false);

        if (_howToPlayRoot != null)
        {
            _howToPlayRoot.SetActive(true);
            _howToPlayRoot.transform.SetAsLastSibling();
        }

        if (_howToPlayDialog != null)
        {
            StartCoroutine(AnimatePop(_howToPlayDialog, true));
        }
    }

    public void CloseHowToPlay()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (_howToPlayDialog != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(AnimatePop(_howToPlayDialog, false, () =>
            {
                if (_howToPlayRoot != null)
                    _howToPlayRoot.SetActive(false);
                ShowMainMenu(animate: false);
            }));
        }
        else
        {
            if (_howToPlayRoot != null)
                _howToPlayRoot.SetActive(false);
            ShowMainMenu(animate: false);
        }
    }

    private void OnSettingsClicked()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayButtonClick();

        if (_mainMenuRoot != null)
            _mainMenuRoot.SetActive(false);

        var settingsUI = FindFirstObjectByType<SettingsMenuUI>(FindObjectsInactive.Include);
        if (settingsUI != null)
        {
            settingsUI.OpenSettings(fromMainMenu: true);
        }
    }

    private void UpdateBestScoreDisplay()
    {
        if (_bestScoreText != null)
        {
            int best = PlayerPrefs.GetInt(BestScorePrefKey, 0);
            _bestScoreText.text = $"BEST: {best:D4}";
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

            // Smooth ease out back for opening, quadratic for snappy close
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
