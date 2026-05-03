using UnityEngine;

public class BossBTrigger : MonoBehaviour
{
    public BossBFight bossBFight;
    private bool triggered = false;
    private bool _introShown = false;

    private const float DETECT_RANGE = 2f;

    void OnTriggerEnter(Collider other)
    {
        TryTrigger(other.gameObject);
    }

    void Update()
    {
        // Reset triggered flag when boss is no longer active so re-entry works
        if (triggered && bossBFight != null && !bossBFight.bossActive)
            triggered = false;

        // Raycast-based detection ensures the boss starts the instant
        // the player is on the checkpoint tile, even after a teleport/respawn
        if (triggered) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        if (Physics.Raycast(rayOrigin, Vector3.up, out RaycastHit hit, DETECT_RANGE))
        {
            if (hit.collider.CompareTag("Player"))
                TryTrigger(hit.collider.gameObject);
        }
    }

    void OnDestroy()
    {
        BossIntroModal.OnPageChanged -= OnTutorialPageChanged;
    }

    /// <summary>Attempts to start the boss intro/fight when the player is detected.</summary>
    private void TryTrigger(GameObject playerObj)
    {
        if (triggered) return;
        if (!playerObj.CompareTag("Player")) return;
        if (bossBFight == null)
        {
            Debug.LogWarning("[BossBTrigger] bossBFight reference is null.");
            return;
        }
        if (bossBFight.bossActive) return;

        triggered = true;
        Debug.Log("[BossBTrigger] Player detected. introShown=" + _introShown);

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
