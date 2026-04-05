using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TrialsOfKairos.UI
{
    /// <summary>
    /// Singleton overlay that provides screen-wide crossfade and flash transitions.
    /// Auto-creates its own full-screen Canvas + overlay Images if not assigned.
    /// </summary>
    public class ScreenTransitionManager : MonoBehaviour
    {
        public static ScreenTransitionManager Instance { get; private set; }

        [Header("Overlay References (auto-created if null)")]
        [SerializeField] private Image flashOverlay;
        [SerializeField] private Image fadeOverlay;

        private Coroutine _activeTransition;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureOverlays();
            SetOverlayAlpha(flashOverlay, 0f);
            SetOverlayAlpha(fadeOverlay, 0f);
        }

        /// <summary>Single white/gold flash, then fades out quickly.</summary>
        public void FlashGold(float duration = 0.4f)
        {
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(FlashRoutine(new Color(1f, 0.843f, 0f, 0.8f), duration));
        }

        /// <summary>Red flash -- used on damage/game over.</summary>
        public void FlashRed(float duration = 0.3f)
        {
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(FlashRoutine(new Color(0.898f, 0.196f, 0.106f, 0.6f), duration));
        }

        /// <summary>Fades to black, executes the midpoint action, then fades back in.</summary>
        public void CrossFade(float outDuration, float holdDuration, float inDuration, Action onMidpoint = null)
        {
            if (_activeTransition != null) StopCoroutine(_activeTransition);
            _activeTransition = StartCoroutine(CrossFadeRoutine(outDuration, holdDuration, inDuration, onMidpoint));
        }

        private IEnumerator FlashRoutine(Color flashColor, float duration)
        {
            if (flashOverlay == null) yield break;
            flashOverlay.color = flashColor;
            Color target = new Color(flashColor.r, flashColor.g, flashColor.b, 0f);
            yield return LerpColor(flashOverlay, target, duration);
        }

        private IEnumerator CrossFadeRoutine(float outDuration, float holdDuration, float inDuration, Action onMidpoint)
        {
            if (fadeOverlay == null) yield break;
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            yield return LerpColor(fadeOverlay, new Color(0f, 0f, 0f, 1f), outDuration);
            yield return new WaitForSecondsRealtime(holdDuration);
            onMidpoint?.Invoke();
            yield return LerpColor(fadeOverlay, new Color(0f, 0f, 0f, 0f), inDuration);
        }

        private static IEnumerator LerpColor(Image img, Color target, float duration)
        {
            if (img == null) yield break;
            Color start = img.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                img.color = Color.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            img.color = target;
        }

        private static void SetOverlayAlpha(Image img, float alpha)
        {
            if (img == null) return;
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }

        /// <summary>Creates overlay Canvas + Images if serialized references are null.</summary>
        private void EnsureOverlays()
        {
            if (flashOverlay != null && fadeOverlay != null) return;

            GameObject overlayRoot = new GameObject("TransitionOverlayCanvas");
            overlayRoot.transform.SetParent(transform, false);
            Canvas canvas = overlayRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            overlayRoot.AddComponent<CanvasScaler>();

            if (fadeOverlay == null)
            {
                GameObject fadeGO = new GameObject("FadeOverlay");
                fadeGO.transform.SetParent(overlayRoot.transform, false);
                fadeOverlay = fadeGO.AddComponent<Image>();
                fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
                fadeOverlay.raycastTarget = false;
                StretchFull(fadeGO.GetComponent<RectTransform>());
            }

            if (flashOverlay == null)
            {
                GameObject flashGO = new GameObject("FlashOverlay");
                flashGO.transform.SetParent(overlayRoot.transform, false);
                flashOverlay = flashGO.AddComponent<Image>();
                flashOverlay.color = new Color(1f, 1f, 1f, 0f);
                flashOverlay.raycastTarget = false;
                StretchFull(flashGO.GetComponent<RectTransform>());
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
