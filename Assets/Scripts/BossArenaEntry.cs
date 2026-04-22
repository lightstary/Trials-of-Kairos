using UnityEngine;

/// <summary>
/// Detects when the player steps onto the boss arena entry tile.
/// Shows a boss intro modal the first time, then starts the boss fight on dismiss.
/// On subsequent entries (e.g. after death/reset), starts the fight directly.
/// </summary>
public class BossArenaEntry : MonoBehaviour
{
    [Header("References")]
    public BossFight bossFight;

    [Header("Boss Info")]
    [Tooltip("The boss name used to look up intro content (e.g. THE CITADEL).")]
    public string bossName = "THE CITADEL";

    private bool _introShown;

    void Update()
    {
        if (bossFight == null) return;
        if (bossFight.bossActive) return;
        if (BossIntroModal.IsOpen) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 2f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                if (!_introShown)
                {
                    _introShown = true;
                    ShowIntroThenStart();
                }
                else
                {
                    bossFight.StartBossFight();
                }
            }
        }
    }

    /// <summary>Shows the boss intro modal, then starts the fight when dismissed.</summary>
    private void ShowIntroThenStart()
    {
        string[] pages = BossIntroContent.GetPages(bossName);

        BossIntroModal.Show(pages, () =>
        {
            if (bossFight != null && !bossFight.bossActive)
            {
                bossFight.StartBossFight();
                Debug.Log($"[BossArenaEntry] Boss fight started after intro: {bossName}");
            }
        });
    }

    /// <summary>
    /// Called when the boss fight is reset (e.g. player dies).
    /// Allows the intro to show again on the next entry if desired.
    /// Currently keeps introShown=true so retries skip the intro.
    /// </summary>
    public void ResetIntro()
    {
        _introShown = false;
    }
}
