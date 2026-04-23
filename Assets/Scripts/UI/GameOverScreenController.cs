using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Game Over screen — fracture animation, red vignette, Chronos quote.
/// </summary>
public class GameOverScreenController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject      gameOverPanel;
    [SerializeField] private CanvasGroup     canvasGroup;

    [Header("Title")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private RectTransform   titleRect;

    [Header("Quote")]
    [SerializeField] private TextMeshProUGUI quoteLabel;
    [SerializeField] private TextMeshProUGUI quoteAttribution;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnToHubButton;

    [Header("Visual")]
    [SerializeField] private Image fractureOverlay;
    [SerializeField] private Image redVignette;

    [Header("Config")]
    #pragma warning disable CS0414 // Reserved for Inspector configuration
    [SerializeField] private string hubSceneName       = "HubScene";
    #pragma warning restore CS0414
    [SerializeField] private float  fractureDuration   = 0.8f;
    [SerializeField] private float  titleShakeAmount   = 8f;
    [SerializeField] private float  titleShakeDuration = 0.1f;
    [SerializeField] private float  quoteDelay         = 0.6f;

    private static readonly string[] QUOTES = {
        "The hourglass that falters shall be\nswallowed by the void between seconds.",
        "Time does not mourn those\nwho fail to hold its weight.",
        "You crumble as all mortals do—\nbeneath the gaze of eternity.",
        "The sands care nothing\nfor your intentions.",
        "Every second you wasted\nwas a gift I shall not repeat."
    };

    private const string TITLE    = "TEMPORAL  FAILURE";
    private const string SUBTITLE = "CHRONOS RECLAIMS YOU";

    void Start()
    {
        if (retryButton       != null) retryButton.onClick.AddListener(RetryTrial);
        if (returnToHubButton != null) returnToHubButton.onClick.AddListener(ReturnToHub);
        if (gameOverPanel     != null) gameOverPanel.SetActive(false);
    }

    /// <summary>Shows the game over screen.</summary>
    public void Show(string customSubtitle = null)
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        StartCoroutine(Animate(customSubtitle));
    }

    private IEnumerator Animate(string subtitle)
    {
        if (canvasGroup      != null) canvasGroup.alpha = 0f;
        if (quoteLabel       != null) quoteLabel.alpha  = 0f;
        if (quoteAttribution != null) quoteAttribution.alpha = 0f;

        if (titleLabel    != null) titleLabel.text    = TITLE;
        if (subtitleLabel != null) subtitleLabel.text = subtitle ?? SUBTITLE;

        string q = QUOTES[Random.Range(0, QUOTES.Length)];
        if (quoteLabel       != null) quoteLabel.text       = $"\"{q}\"";
        if (quoteAttribution != null) quoteAttribution.text = "\u2014 Chronos";

        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.RedFracture();

        StartCoroutine(AnimateFracture());
        if (redVignette != null) StartCoroutine(AnimateVignette());

        // Fade in panel
        float elapsed = 0f, dur = 0.4f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(elapsed / dur);
            yield return null;
        }
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (titleRect != null) yield return ShakeTitle();

        yield return new WaitForSecondsRealtime(quoteDelay);

        elapsed = 0f; dur = 0.6f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / dur);
            if (quoteLabel       != null) quoteLabel.alpha       = t;
            if (quoteAttribution != null) quoteAttribution.alpha = t;
            yield return null;
        }
    }

    private IEnumerator AnimateFracture()
    {
        if (fractureOverlay == null) yield break;
        fractureOverlay.gameObject.SetActive(true);
        RectTransform r = fractureOverlay.GetComponent<RectTransform>();
        float elapsed = 0f;
        while (elapsed < fractureDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fractureDuration);
            if (r != null) r.localScale = Vector3.one * Mathf.Lerp(0.3f, 1.2f, EaseOutQuart(t));
            Color c = fractureOverlay.color; c.a = t * 0.6f; fractureOverlay.color = c;
            yield return null;
        }
    }

    private IEnumerator AnimateVignette()
    {
        Color danger = TimeStateUIManager.Instance != null
            ? TimeStateUIManager.Instance.dangerColor : Color.red;

        // Initial fade in
        float elapsed = 0f, dur = 0.5f;
        while (elapsed < dur)
        {
            elapsed += Time.unscaledDeltaTime;
            danger.a = Mathf.Clamp01(elapsed / dur) * 0.35f;
            if (redVignette != null) redVignette.color = danger;
            yield return null;
        }

        // Continuous pulsing vignette
        while (redVignette != null && redVignette.gameObject.activeInHierarchy)
        {
            float pulse = Mathf.Sin(Time.unscaledTime * 3f) * 0.12f + 0.30f;
            danger.a = pulse;
            redVignette.color = danger;
            yield return null;
        }
    }

    private IEnumerator ShakeTitle()
    {
        Vector2 orig = titleRect.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < titleShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            titleRect.anchoredPosition = orig + new Vector2(
                Random.Range(-titleShakeAmount, titleShakeAmount),
                Random.Range(-titleShakeAmount * 0.5f, titleShakeAmount * 0.5f));
            yield return null;
        }
        titleRect.anchoredPosition = orig;
    }

    private void RetryTrial()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ReturnToHub()
    {
        Time.timeScale = 1f;
        MainMenuController.RequestTrialSelectOnLoad();
        SceneManager.LoadScene("MainScene");
    }

    private static float EaseOutQuart(float t) => 1f - Mathf.Pow(1f - t, 4f);
}
