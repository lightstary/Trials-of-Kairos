using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Shows a brief trial name title card at the start of each level.
/// Auto-spawns once per scene load and fades in/out cleanly.
/// </summary>
public class TrialIntroFlash : MonoBehaviour
{
    private static readonly Color GOLD = new Color(0.961f, 0.784f, 0.259f, 1f);

    private const float FADE_IN_DUR  = 0.5f;
    private const float HOLD_DUR     = 1.2f;
    private const float FADE_OUT_DUR = 0.8f;
    private const float TITLE_SIZE   = 64f;
    private const float CHAR_SPACING = 16f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // MainScene hosts both Trial Selection and the Citadel.
        // The flash for MainScene is triggered manually from
        // MainMenuController.SkipToGameplay() instead.
        if (sceneName == "MainScene") return;

        string trialName = GetTrialName(sceneName);
        if (string.IsNullOrEmpty(trialName)) return;

        TriggerFlash(trialName);
    }

    /// <summary>
    /// Manually triggers a trial intro flash. Call this when loading into the Citadel
    /// from Trial Selection (since MainScene is shared).
    /// </summary>
    public static void TriggerFlash(string trialName)
    {
        if (string.IsNullOrEmpty(trialName)) return;
        GameObject go = new GameObject("[TrialIntroFlash]");
        TrialIntroFlash flash = go.AddComponent<TrialIntroFlash>();
        flash.StartCoroutine(flash.ShowFlash(trialName));
    }

    /// <summary>Maps scene names to display trial names.</summary>
    private static string GetTrialName(string sceneName)
    {
        switch (sceneName)
        {
            case "MainScene":   return "THE CITADEL";
            case "GardenScene": return "THE GARDEN";
            case "ClockScene":  return "THE CLOCK";
            case "HubScene":    return "THE HUB";
            default:            return null;
        }
    }

    private IEnumerator ShowFlash(string trialName)
    {
        // Wait a frame for the canvas to be ready
        yield return null;

        // Find or create a canvas
        Canvas canvas = null;
        HUDController hud = HUDController.Instance;
        if (hud != null) canvas = hud.GetComponentInParent<Canvas>();
        if (canvas == null) canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // Create overlay
        GameObject overlayGO = new GameObject("TrialFlashOverlay");
        overlayGO.transform.SetParent(canvas.transform, false);
        RectTransform overlayRT = overlayGO.AddComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        Image overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0f);
        overlayImg.raycastTarget = false;

        // Create title text
        GameObject textGO = new GameObject("TrialFlashText");
        textGO.transform.SetParent(overlayGO.transform, false);
        RectTransform textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0.1f, 0.35f);
        textRT.anchorMax = new Vector2(0.9f, 0.65f);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = trialName;
        tmp.fontSize = TITLE_SIZE;
        tmp.characterSpacing = CHAR_SPACING;
        tmp.color = new Color(GOLD.r, GOLD.g, GOLD.b, 0f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        CinzelFontHelper.Apply(tmp, true);

        // Animate: fade in
        float elapsed = 0f;
        while (elapsed < FADE_IN_DUR)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_IN_DUR);
            float eased = t * t; // ease in
            tmp.color = new Color(GOLD.r, GOLD.g, GOLD.b, eased);
            overlayImg.color = new Color(0f, 0f, 0f, eased * 0.35f);
            yield return null;
        }
        tmp.color = new Color(GOLD.r, GOLD.g, GOLD.b, 1f);

        // Hold
        yield return new WaitForSeconds(HOLD_DUR);

        // Fade out
        elapsed = 0f;
        while (elapsed < FADE_OUT_DUR)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_OUT_DUR);
            float alpha = 1f - t;
            tmp.color = new Color(GOLD.r, GOLD.g, GOLD.b, alpha);
            overlayImg.color = new Color(0f, 0f, 0f, alpha * 0.35f);
            yield return null;
        }

        Destroy(overlayGO);
        Destroy(gameObject);
    }
}
