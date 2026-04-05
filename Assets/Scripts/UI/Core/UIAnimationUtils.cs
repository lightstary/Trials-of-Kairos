using System.Collections;
using UnityEngine;

namespace TrialsOfKairos.UI
{
    /// <summary>Shared coroutine-based animation helpers used across all UI screens.</summary>
    public static class UIAnimationUtils
    {
        // ── Easing curves ──────────────────────────────────────────────────────────────

        public static readonly AnimationCurve EaseOut  = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        public static readonly AnimationCurve EaseIn   = new AnimationCurve(new Keyframe(0f, 0f, 0f, 2f), new Keyframe(1f, 1f, 0f, 0f));
        public static readonly AnimationCurve Overshoot = new AnimationCurve(
            new Keyframe(0f, 0f), new Keyframe(0.7f, 1.05f), new Keyframe(1f, 1f));

        // ── Fade ───────────────────────────────────────────────────────────────────────

        /// <summary>Fades a CanvasGroup alpha from current to target over duration.</summary>
        public static IEnumerator FadeCanvasGroup(CanvasGroup group, float target, float duration)
        {
            float start   = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed    += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, EaseOut.Evaluate(elapsed / duration));
                yield return null;
            }
            group.alpha = target;
        }

        // ── Slide ──────────────────────────────────────────────────────────────────────

        /// <summary>Slides a RectTransform anchoredPosition from current to target over duration.</summary>
        public static IEnumerator SlideRect(RectTransform rect, Vector2 target, float duration)
        {
            Vector2 start   = rect.anchoredPosition;
            float   elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed                 += Time.unscaledDeltaTime;
                rect.anchoredPosition    = Vector2.LerpUnclamped(start, target, EaseOut.Evaluate(elapsed / duration));
                yield return null;
            }
            rect.anchoredPosition = target;
        }

        // ── Scale ──────────────────────────────────────────────────────────────────────

        /// <summary>Scales a RectTransform from start scale to target scale over duration.</summary>
        public static IEnumerator ScaleRect(RectTransform rect, Vector3 from, Vector3 to, float duration, AnimationCurve curve = null)
        {
            curve           ??= EaseOut;
            float elapsed    = 0f;
            rect.localScale  = from;
            while (elapsed < duration)
            {
                elapsed          += Time.unscaledDeltaTime;
                rect.localScale   = Vector3.LerpUnclamped(from, to, curve.Evaluate(elapsed / duration));
                yield return null;
            }
            rect.localScale = to;
        }

        /// <summary>Pulses a RectTransform scale once with overshoot then returns to original.</summary>
        public static IEnumerator PulseScale(RectTransform rect, float pulseScale, float duration)
        {
            Vector3 original = rect.localScale;
            Vector3 big      = original * pulseScale;
            float   half     = duration * 0.5f;
            yield return ScaleRect(rect, original, big, half, Overshoot);
            yield return ScaleRect(rect, big, original, half, EaseOut);
        }

        // ── Color ──────────────────────────────────────────────────────────────────────

        /// <summary>Lerps an Image color to target over duration.</summary>
        public static IEnumerator LerpImageColor(UnityEngine.UI.Image image, Color target, float duration)
        {
            if (image == null) yield break;
            Color start   = image.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (image == null) yield break;
                elapsed       += Time.unscaledDeltaTime;
                image.color    = Color.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            if (image != null) image.color = target;
        }

        /// <summary>
        /// Pulses an Image alpha between minAlpha and maxAlpha at the given frequency, looping
        /// forever (stop via StopCoroutine). Exits silently if the image is destroyed.
        /// </summary>
        public static IEnumerator PulseGlow(UnityEngine.UI.Image image, Color glowColor,
            float frequency = 1.2f, float minAlpha = 0.15f, float maxAlpha = 0.65f, float duration = 0f)
        {
            if (image == null) yield break;
            float elapsed = 0f;
            while (duration <= 0f || elapsed < duration)
            {
                if (image == null) yield break;
                float t = (Mathf.Sin(Time.unscaledTime * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
                Color c = glowColor;
                c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
                image.color  = c;
                elapsed     += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Sweeps a TMP label's color to shimmerColor and back once, creating a glimmer pass.
        /// </summary>
        public static IEnumerator ShimmerLabel(TMPro.TextMeshProUGUI label, Color shimmerColor, float halfDuration)
        {
            if (label == null) yield break;
            Color original = label.color;
            float elapsed  = 0f;
            while (elapsed < halfDuration)
            {
                elapsed    += Time.unscaledDeltaTime;
                label.color = Color.Lerp(original, shimmerColor, EaseOut.Evaluate(elapsed / halfDuration));
                yield return null;
            }
            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed    += Time.unscaledDeltaTime;
                label.color = Color.Lerp(shimmerColor, original, EaseOut.Evaluate(elapsed / halfDuration));
                yield return null;
            }
            label.color = original;
        }

        /// <summary>
        /// Ripple flash: fades a CanvasGroup to peakAlpha then back to zero in one shot.
        /// Use for full-screen light washes.
        /// </summary>
        public static IEnumerator RippleFlash(CanvasGroup group, float peakAlpha,
            float riseDuration, float fallDuration)
        {
            yield return FadeCanvasGroup(group, peakAlpha, riseDuration);
            yield return FadeCanvasGroup(group, 0f, fallDuration);
        }

        // ── Misc ───────────────────────────────────────────────────────────────────────

        /// <summary>Shakes a RectTransform horizontally.</summary>
        public static IEnumerator ShakeRect(RectTransform rect, float magnitude, float duration)
        {
            Vector2 origin  = rect.anchoredPosition;
            float   elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = elapsed / duration;
                float x  = Mathf.Sin(t * Mathf.PI * 14f) * magnitude * (1f - t);
                rect.anchoredPosition = origin + new Vector2(x, 0f);
                yield return null;
            }
            rect.anchoredPosition = origin;
        }

        /// <summary>Rotates a RectTransform to a target Z angle over duration.</summary>
        public static IEnumerator RotateTo(RectTransform rect, float targetZ, float duration, AnimationCurve curve = null)
        {
            curve         ??= EaseOut;
            float startZ   = rect.localEulerAngles.z;
            float elapsed  = 0f;
            while (elapsed < duration)
            {
                elapsed  += Time.unscaledDeltaTime;
                float z   = Mathf.LerpAngle(startZ, targetZ, curve.Evaluate(elapsed / duration));
                rect.localEulerAngles = new Vector3(0f, 0f, z);
                yield return null;
            }
            rect.localEulerAngles = new Vector3(0f, 0f, targetZ);
        }

        /// <summary>Counts a TMP label from 0 to value over duration.</summary>
        public static IEnumerator CountUp(TMPro.TextMeshProUGUI label, float targetValue, float duration, string format = "0")
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed  += Time.unscaledDeltaTime;
                float val = Mathf.Lerp(0f, targetValue, elapsed / duration);
                label.text = val.ToString(format);
                yield return null;
            }
            label.text = targetValue.ToString(format);
        }

        /// <summary>Reveals text one character at a time.</summary>
        public static IEnumerator TypewriterReveal(TMPro.TextMeshProUGUI label, string text, float totalDuration)
        {
            label.text = text;
            label.maxVisibleCharacters = 0;
            float perChar = totalDuration / Mathf.Max(1, text.Length);
            for (int i = 0; i <= text.Length; i++)
            {
                label.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(perChar);
            }
        }

        /// <summary>Fades a TMP label alpha from 0 to 1 while sliding it on Y.</summary>
        public static IEnumerator FadeSlideLabel(TMPro.TextMeshProUGUI label, float slideY,
            float duration, bool slideIn = true)
        {
            RectTransform rt      = label.rectTransform;
            Vector2 origin        = rt.anchoredPosition;
            Vector2 offscreen     = origin + new Vector2(0f, slideIn ? -slideY : slideY);
            Color   colorShown    = label.color;
            Color   colorHidden   = colorShown; colorHidden.a = 0f;

            if (slideIn) { rt.anchoredPosition = offscreen; label.color = colorHidden; }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t  = EaseOut.Evaluate(elapsed / duration);
                rt.anchoredPosition = Vector2.LerpUnclamped(slideIn ? offscreen : origin,
                                                             slideIn ? origin    : offscreen, t);
                label.color         = Color.Lerp(slideIn ? colorHidden : colorShown,
                                                 slideIn ? colorShown  : colorHidden, t);
                yield return null;
            }
            rt.anchoredPosition = slideIn ? origin : offscreen;
            label.color         = slideIn ? colorShown : colorHidden;
        }
    }
}