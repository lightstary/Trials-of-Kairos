using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full-width strip that slides in from the top to show the area title,
/// holds briefly, then slides back out.
/// </summary>
public class AreaTitleIntro : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform   stripPanel;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private CanvasGroup     canvasGroup;

    [Header("Animation")]
    [SerializeField] private float slideDistance  = 80f;
    [SerializeField] private float slideInDuration  = 0.4f;
    [SerializeField] private float holdDuration     = 2.5f;
    [SerializeField] private float slideOutDuration = 0.3f;

    private Vector2 hiddenPosition;
    private Vector2 visiblePosition;

    void Awake()
    {
        if (stripPanel != null)
        {
            visiblePosition = stripPanel.anchoredPosition;
            hiddenPosition  = visiblePosition + Vector2.up * slideDistance;
        }
        SetHidden();
    }

    /// <summary>Shows the area title. Format: "TRIAL IV  —  THE PENDULUM VAULT"</summary>
    public void ShowTitle(string trialNumber, string areaName)
    {
        if (titleLabel != null)
            titleLabel.text = $"{trialNumber}  \u2014  {areaName}".ToUpper();
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    /// <summary>Shows any title string directly.</summary>
    public void ShowTitle(string fullTitle)
    {
        if (titleLabel != null) titleLabel.text = fullTitle.ToUpper();
        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        float elapsed = 0f;
        while (elapsed < slideInDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseOut(Mathf.Clamp01(elapsed / slideInDuration));
            if (stripPanel   != null) stripPanel.anchoredPosition = Vector2.Lerp(hiddenPosition, visiblePosition, t);
            if (canvasGroup  != null) canvasGroup.alpha = t;
            yield return null;
        }
        SetVisible();

        yield return new WaitForSeconds(holdDuration);

        elapsed = 0f;
        while (elapsed < slideOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = EaseIn(Mathf.Clamp01(elapsed / slideOutDuration));
            if (stripPanel   != null) stripPanel.anchoredPosition = Vector2.Lerp(visiblePosition, hiddenPosition, t);
            if (canvasGroup  != null) canvasGroup.alpha = 1f - t;
            yield return null;
        }
        SetHidden();
    }

    private void SetHidden()
    {
        if (stripPanel  != null) stripPanel.anchoredPosition = hiddenPosition;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void SetVisible()
    {
        if (stripPanel  != null) stripPanel.anchoredPosition = visiblePosition;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }

    private static float EaseOut(float t) => 1f - Mathf.Pow(1f - t, 3f);
    private static float EaseIn(float t)  => t * t * t;
}
