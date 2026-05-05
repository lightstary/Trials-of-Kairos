using UnityEngine;

public class BossCTrigger : MonoBehaviour
{
    public BossCFight bossCFight;

    private bool _triggered;
    private bool _introShown;

    void OnTriggerEnter(Collider other)
    {
        if (_triggered) return;
        if (!other.CompareTag("Player")) return;
        if (bossCFight == null) return;
        if (bossCFight.bossActive) return;

        _triggered = true;

        if (!_introShown)
        {
            _introShown = true;
            ShowIntroThenStart();
        }
        else
        {
            bossCFight.StartBossFight();
        }
    }

    void Update()
    {
        if (_triggered && bossCFight != null && !bossCFight.bossActive)
            _triggered = false;
    }

    private void ShowIntroThenStart()
    {
        string[] pages = BossIntroContent.GetPages("THE CLOCK");

        BossIntroModal.Show(pages, () =>
        {
            if (bossCFight != null && !bossCFight.bossActive)
            {
                bossCFight.StartBossFight();
                Debug.Log("[BossCTrigger] Boss C fight started after intro.");
            }
        });
    }
}