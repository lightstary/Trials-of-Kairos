using UnityEngine;

public class BossBTrigger : MonoBehaviour
{
    public BossBFight bossBFight;
    private bool triggered = false;
    private bool _introShown = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;
        if (bossBFight == null) return;
        if (bossBFight.bossActive) return;

        triggered = true;

        if (!_introShown)
        {
            _introShown = true;
            ShowIntroThenStart();
        }
        else
        {
            bossBFight.StartBossFight();
        }
    }

    void Update()
    {
        if (triggered && bossBFight != null && !bossBFight.bossActive)
            triggered = false;
    }

    void OnDestroy()
    {
        BossIntroModal.OnPageChanged -= OnTutorialPageChanged;
    }

    private void ShowIntroThenStart()
    {
        string[] pages = BossIntroContent.GetPages("THE GARDEN");

        // Make the boss pointer visible on the meter during the tutorial
        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>();
        if (meter != null)
        {
            float minV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.minValue : -10f;
            float maxV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.maxValue : 10f;
            meter.SetBossPointer(0f, minV, maxV);
        }

        // Listen for page changes to toggle the boss pointer glow
        BossIntroModal.OnPageChanged += OnTutorialPageChanged;

        BossIntroModal.Show(pages, () =>
        {
            BossIntroModal.OnPageChanged -= OnTutorialPageChanged;
            BossBFight.SetPointerGlowVisible(false);

            if (bossBFight != null && !bossBFight.bossActive)
            {
                bossBFight.StartBossFight();
                Debug.Log("[BossBTrigger] Boss B fight started after intro.");
            }
        });
    }

    /// <summary>Shows/hides the boss pointer glow ring based on the current tutorial page.</summary>
    private void OnTutorialPageChanged(int currentPage, int totalPages)
    {
        bool showGlow = currentPage == BossIntroContent.GARDEN_POINTER_GLOW_PAGE;
        BossBFight.SetPointerGlowVisible(showGlow);
    }
}
