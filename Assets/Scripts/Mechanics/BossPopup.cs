using UnityEngine;
using System.Collections;
using TMPro;

public class BossPopup : MonoBehaviour
{
    public static BossPopup Instance;

    [Header("Settings")]
    public float displayDuration = 3f;
    public float fadeDuration = 1f;

    private TextMeshPro textMesh;
    private bool isShowing = false;

    void Awake()
    {
        Instance = this;

        // Create TextMeshPro or whatver UI Daniel wants
        textMesh = gameObject.AddComponent<TextMeshPro>();
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 8f;
        textMesh.fontStyle = FontStyles.Bold;
        CinzelFontHelper.Apply(textMesh, true);

        // Hide on start
        Color c = textMesh.color;
        c.a = 0f;
        textMesh.color = c;
    }

    public void ShowWin()
    {
        if (!isShowing)
            StartCoroutine(ShowMessage("You Defeated\nthe Boss!", new Color(0f, 1f, 0.3f), true));
    }

    public void ShowLose()
    {
        if (!isShowing)
            StartCoroutine(ShowMessage("You Lost!", new Color(1f, 0.2f, 0f), false));
    }

    IEnumerator ShowMessage(string message, Color color, bool goToMenu)
    {
        isShowing = true;
        textMesh.text = message;
        textMesh.color = new Color(color.r, color.g, color.b, 0f);

        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = textMesh.color;
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            textMesh.color = c;
            yield return null;
        }

        // Stay visible
        yield return new WaitForSeconds(displayDuration);

        // Fades out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            Color c = textMesh.color;
            c.a = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            textMesh.color = c;
            yield return null;
        }

        isShowing = false;

        // Load main menu or UI Daniel wants (this is for now)
        if (goToMenu)
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}