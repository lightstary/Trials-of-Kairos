using UnityEngine;

public class BossArenaEntry : MonoBehaviour
{
    public BossFight bossFight;
    private bool triggered = false;

    void Update()
    {
        if (triggered) return;

        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit hit;

        Debug.DrawRay(rayOrigin, Vector3.up * 2f, Color.blue);

        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 2f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                triggered = true;
                bossFight.StartBossFight();
                Debug.Log("Boss fight started!");
            }
        }
    }
}