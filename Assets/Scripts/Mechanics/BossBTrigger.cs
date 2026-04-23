using UnityEngine;

public class BossBTrigger : MonoBehaviour
{
    public BossBFight bossBFight;
    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        if (bossBFight != null)
            bossBFight.StartBossFight();
    }

    void Update()
    {
        if (triggered && bossBFight != null && !bossBFight.bossActive)
            triggered = false;
    }
}