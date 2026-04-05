using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Boss health bar with phase dividers and phase-transition burst effect.
    /// </summary>
    public class BossHealthBar : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image           fillImage;
        [SerializeField] private Image           trackImage;
        [SerializeField] private TextMeshProUGUI bossNameLabel;
        [SerializeField] private Transform       phaseDividerContainer;
        [SerializeField] private GameObject      phaseDividerPrefab;
        [SerializeField] private RectTransform   barRect;

        [Header("Settings")]
        [SerializeField] private int   phaseCount  = 3;
        [SerializeField] private Color fillColor   = new Color(0.898f, 0.196f, 0.106f, 1f);
        [SerializeField] private float lerpSpeed   = 4f;

        private float _targetFill = 1f;
        private float _currentFill = 1f;
        private int   _lastPhase = -1;

        private void Update()
        {
            _currentFill = Mathf.Lerp(_currentFill, _targetFill, lerpSpeed * Time.unscaledDeltaTime);
            if (fillImage != null) fillImage.fillAmount = _currentFill;

            int currentPhase = Mathf.FloorToInt(_currentFill * phaseCount);
            if (currentPhase != _lastPhase)
            {
                _lastPhase = currentPhase;
                StartCoroutine(PhaseTransitionBurst());
            }
        }

        /// <summary>Configure the boss bar with name and phase count.</summary>
        public void Initialise(string bossName, int phases)
        {
            phaseCount = phases;
            if (bossNameLabel != null) bossNameLabel.text = bossName.ToUpper();
            if (fillImage != null)
            {
                fillImage.fillAmount = 1f;
                fillImage.color = fillColor;
            }
            _targetFill  = 1f;
            _currentFill = 1f;
            _lastPhase   = phases;
            BuildPhaseDividers();
        }

        /// <summary>Sets boss health (0–1 normalized).</summary>
        public void SetHealth(float normalizedHealth)
        {
            _targetFill = Mathf.Clamp01(normalizedHealth);
        }

        private void BuildPhaseDividers()
        {
            if (phaseDividerContainer == null || phaseDividerPrefab == null) return;
            foreach (Transform child in phaseDividerContainer) Destroy(child.gameObject);

            float barWidth = barRect != null ? barRect.rect.width : 600f;
            for (int i = 1; i < phaseCount; i++)
            {
                float t     = (float)i / phaseCount;
                GameObject d = Instantiate(phaseDividerPrefab, phaseDividerContainer);
                RectTransform dr = d.GetComponent<RectTransform>();
                if (dr != null) dr.anchoredPosition = new Vector2(barWidth * t, 0f);
            }
        }

        private IEnumerator PhaseTransitionBurst()
        {
            if (barRect == null) yield break;
            yield return UIAnimationUtils.PulseScale(barRect, 1.04f, 0.3f);
        }
    }
}
