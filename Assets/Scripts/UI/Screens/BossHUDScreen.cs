using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Boss HUD screen handling all three variants (A, B, C)
    /// through sub-panel activation and per-variant logic.
    /// </summary>
    public class BossHUDScreen : UIScreenBase
    {
        [Header("Shared Boss UI")]
        [SerializeField] private BossHealthBar   bossHealthBar;
        [SerializeField] private Image           bossEyeIcon;
        [SerializeField] private TextMeshProUGUI phaseLabel;

        [Header("Variant Sub-Panels")]
        [SerializeField] private GameObject variantA_Panel;
        [SerializeField] private GameObject variantB_Panel;
        [SerializeField] private GameObject variantC_Panel;

        // Variant A: edge limits
        [Header("Variant A")]
        [SerializeField] private RectTransform variantA_MaxPointer;
        [SerializeField] private RectTransform variantA_MinPointer;
        [SerializeField] private Image         variantA_MeterFill;

        // Variant B: moving pointer
        [Header("Variant B")]
        [SerializeField] private RectTransform variantB_DangerPointer;
        [SerializeField] private float         variantB_SweepSpeed    = 85f;
        [SerializeField] private float         variantB_MeterHalfHeight = 150f;

        // Variant C: rhythm zones
        [Header("Variant C")]
        [SerializeField] private RectTransform variantC_ZoneContainer;
        [SerializeField] private GameObject    variantC_ZonePrefab;
        [SerializeField] private float         variantC_ScrollSpeed   = 140f;
        [SerializeField] private float         variantC_SpawnInterval = 1.8f;

        private BossVariant _activeVariant;
        private float       _variantB_Direction = 1f;
        private float       _variantB_CurrentY  = 0f;
        private float       _variantC_SpawnTimer = 0f;
        private bool        _isActive = false;

        private void Update()
        {
            if (!_isActive) return;
            switch (_activeVariant)
            {
                case BossVariant.B: UpdateVariantB(); break;
                case BossVariant.C: UpdateVariantC(); break;
            }
        }

        /// <summary>Configure and activate the correct boss HUD variant.</summary>
        public void Configure(BossVariant variant, string bossName, float healthMax)
        {
            _activeVariant = variant;
            bossHealthBar?.Initialise(bossName, 3);
            SetPhase(1);

            variantA_Panel?.SetActive(variant == BossVariant.A);
            variantB_Panel?.SetActive(variant == BossVariant.B);
            variantC_Panel?.SetActive(variant == BossVariant.C);

            _isActive = true;
        }

        /// <summary>Update boss health normalized (0–1).</summary>
        public void SetBossHealth(float normalizedHealth)
        {
            bossHealthBar?.SetHealth(normalizedHealth);
            int phase = Mathf.CeilToInt(normalizedHealth * 3);
            SetPhase(Mathf.Max(1, phase));

            // Boss B accelerates sweep speed with lower health
            if (_activeVariant == BossVariant.B)
                variantB_SweepSpeed = Mathf.Lerp(200f, 85f, normalizedHealth);
        }

        // ── Variant A ─────────────────────────────────────────────────────────────
        /// <summary>Sets the safe band on Variant A meter (0–1 normalized).</summary>
        public void SetVariantA_SafeBand(float minNorm, float maxNorm, float meterHeight = 300f)
        {
            if (variantA_MinPointer != null)
                variantA_MinPointer.anchoredPosition = new Vector2(0f, minNorm * meterHeight - meterHeight * 0.5f);
            if (variantA_MaxPointer != null)
                variantA_MaxPointer.anchoredPosition = new Vector2(0f, maxNorm * meterHeight - meterHeight * 0.5f);
        }

        // ── Variant B ─────────────────────────────────────────────────────────────
        private void UpdateVariantB()
        {
            if (variantB_DangerPointer == null) return;
            _variantB_CurrentY += variantB_SweepSpeed * _variantB_Direction * Time.unscaledDeltaTime;
            if (Mathf.Abs(_variantB_CurrentY) >= variantB_MeterHalfHeight)
            {
                _variantB_Direction *= -1f;
                _variantB_CurrentY   = Mathf.Clamp(_variantB_CurrentY, -variantB_MeterHalfHeight, variantB_MeterHalfHeight);
                StartCoroutine(PointerBounce());
            }
            variantB_DangerPointer.anchoredPosition = new Vector2(0f, _variantB_CurrentY);
        }

        private IEnumerator PointerBounce()
        {
            if (variantB_DangerPointer == null) yield break;
            yield return UIAnimationUtils.PulseScale(variantB_DangerPointer, 1.3f, 0.2f);
        }

        // ── Variant C ─────────────────────────────────────────────────────────────
        private void UpdateVariantC()
        {
            if (variantC_ZoneContainer == null || variantC_ZonePrefab == null) return;

            // Scroll existing zones leftward
            foreach (Transform child in variantC_ZoneContainer)
            {
                RectTransform rt = child.GetComponent<RectTransform>();
                if (rt != null)
                {
                    Vector2 pos = rt.anchoredPosition;
                    pos.x -= variantC_ScrollSpeed * Time.unscaledDeltaTime;
                    rt.anchoredPosition = pos;
                    if (pos.x < -400f) Destroy(child.gameObject);
                }
            }

            // Spawn new zones at interval
            _variantC_SpawnTimer += Time.unscaledDeltaTime;
            if (_variantC_SpawnTimer >= variantC_SpawnInterval)
            {
                _variantC_SpawnTimer = 0f;
                SpawnVariantC_Zone();
            }
        }

        private void SpawnVariantC_Zone()
        {
            GameObject zone = Instantiate(variantC_ZonePrefab, variantC_ZoneContainer);
            RectTransform rt = zone.GetComponent<RectTransform>();
            if (rt == null) return;
            float randomX = Random.Range(0f, 400f); // random position on meter
            rt.anchoredPosition = new Vector2(400f, randomX);
            bool isDanger = Random.value > 0.35f;
            Image img = zone.GetComponent<Image>();
            if (img != null)
                img.color = isDanger
                    ? new Color(0.898f, 0.196f, 0.106f, 0.75f)
                    : new Color(1f, 0.843f, 0f, 0.3f);
        }

        private void SetPhase(int phase)
        {
            if (phaseLabel != null) phaseLabel.text = $"PHASE {phase}";
        }
    }
}
