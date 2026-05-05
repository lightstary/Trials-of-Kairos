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

    // Raycast detection so the boss starts even after teleport/respawn
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

        // Show boss pointer on the meter during tutorial
        TimeScaleMeter meter = FindObjectOfType<TimeScaleMeter>();
        if (meter != null)
        {
            float minV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.minValue : -10f;
            float maxV = TimeScaleLogic.Instance != null ? TimeScaleLogic.Instance.maxValue : 10f;
            meter.SetBossPointer(0f, minV, maxV);
        }

        // Toggle boss pointer glow based on tutorial page
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

    private void OnTutorialPageChanged(int currentPage, int totalPages)
    {
        bool showGlow = currentPage == BossIntroContent.GARDEN_POINTER_GLOW_PAGE;
        BossBFight.SetPointerGlowVisible(showGlow);
    }
}
