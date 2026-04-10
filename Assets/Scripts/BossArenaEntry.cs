using UnityEngine;

public class BossArenaEntry : MonoBehaviour
{
    public BossFight bossFight;

    void Update()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 2f))
        {
            if (hit.collider.CompareTag("Player") && !bossFight.bossActive)
            {
                bossFight.StartBossFight();
                Debug.Log("Boss fight started!");
            }
        }
    }
}