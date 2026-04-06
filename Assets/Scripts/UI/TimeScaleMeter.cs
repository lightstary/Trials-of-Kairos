using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Vertical TimeScale meter with fill, danger threshold, and shake feedback.
/// </summary>
public class TimeScaleMeter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image          fillImage;
    [SerializeField] private RectTransform  thresholdMarker;
    [SerializeField] private Image          thresholdMarkerImage;
    [SerializeField] private TextMeshProUGUI valueLabel;

    [Header("Settings")]
    [SerializeField] private float maxValue       = 100f;
    [SerializeField] private float dangerThreshold = 80f;
    [SerializeField] private float fillLerpSpeed   = 5f;

    private const float SHAKE_FREQUENCY = 12f;
    private const float SHAKE_AMPLITUDE = 3f;
    private const float SHAKE_DURATION  = 0.3f;

    private float          currentValue;
    private float          displayValue;
    private bool           isInDanger;
    private float          shakeTimer;
    private Vector2        originalPosition;
    private RectTransform  rectTransform;

    void Awake()
    {
        rectTransform    = GetComponent<RectTransform>();
        originalPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
    }

    void OnEnable()
    {
        if (TimeStateUIManager.Instance != null)
            TimeStateUIManager.Instance.OnStateColorChanged += HandleStateColorChanged;
    }

    void OnDisable()
    {
        if (TimeStateUIManager.Instance != null)
            TimeStateUIManager.Instance.OnStateColorChanged -= HandleStateColorChanged;
    }

    void Start()
    {
        UpdateThresholdPosition();
        if (TimeStateUIManager.Instance != null)
            HandleStateColorChanged(TimeStateUIManager.Instance.GetCurrentStateColor());
    }

    void Update()
    {
        displayValue = Mathf.Lerp(displayValue, currentValue, fillLerpSpeed * Time.deltaTime);

        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(displayValue / maxValue);

        if (valueLabel != null)
            valueLabel.text = $"{Mathf.RoundToInt(displayValue)}%";

        bool wasDanger = isInDanger;
        isInDanger = currentValue >= dangerThreshold;

        if (isInDanger && !wasDanger)
            shakeTimer = SHAKE_DURATION;

        if (isInDanger)
        {
            if (fillImage != null)
            {
                Color danger = TimeStateUIManager.Instance != null
                    ? TimeStateUIManager.Instance.dangerColor : Color.red;
                fillImage.color = danger;
            }

            if (thresholdMarkerImage != null)
            {
                float pulse = Mathf.Sin(Time.time * 8f) * 0.3f + 0.7f;
                Color mc = thresholdMarkerImage.color; mc.a = pulse;
                thresholdMarkerImage.color = mc;
            }
        }

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float offset = Mathf.Sin(Time.time * SHAKE_FREQUENCY * Mathf.PI * 2f) * SHAKE_AMPLITUDE;
            if (rectTransform != null)
                rectTransform.anchoredPosition = originalPosition + new Vector2(offset, 0f);
        }
        else if (rectTransform != null)
        {
            rectTransform.anchoredPosition = originalPosition;
        }
    }

    /// <summary>Sets the current meter value (0 – maxValue).</summary>
    public void SetValue(float value) => currentValue = Mathf.Clamp(value, 0f, maxValue);

    /// <summary>Returns true when the meter is in the danger zone.</summary>
    public bool IsInDanger() => isInDanger;

    private void HandleStateColorChanged(Color color)
    {
        if (!isInDanger && fillImage != null)
            fillImage.color = color;
    }

    private void UpdateThresholdPosition()
    {
        if (thresholdMarker == null || fillImage == null) return;
        RectTransform parentRect = fillImage.GetComponent<RectTransform>();
        if (parentRect == null) return;
        float h     = parentRect.rect.height;
        float yPos  = (dangerThreshold / maxValue) * h - h * 0.5f;
        thresholdMarker.anchoredPosition = new Vector2(thresholdMarker.anchoredPosition.x, yPos);
    }
}
