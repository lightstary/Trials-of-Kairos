using UnityEngine;

public class BossCTrigger : MonoBehaviour
{
    public BossCFight bossCFight;
    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        if (bossCFight != null)
            bossCFight.StartBossFight();
    }

    void Update()
    {
        if (triggered && bossCFight != null && !bossCFight.bossActive)
            triggered = false;
    }
}