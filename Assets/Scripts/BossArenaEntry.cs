using UnityEngine;

public class BossArenaEntry : MonoBehaviour
{
    [Header("References")]
    public BossFight bossFight;

    [Header("Boss Info")]
    public string bossName = "THE CITADEL";

    private bool _introShown;
    private bool _triggered;

    void Update()
    {
        if (_triggered && bossFight != null && !bossFight.bossActive)
            _triggered = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (bossFight == null) return;
        if (bossFight.bossActive) return;
        if (_triggered) return;

        _triggered = true;

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

    public void ResetIntro()
    {
        _introShown = false;
    }
}