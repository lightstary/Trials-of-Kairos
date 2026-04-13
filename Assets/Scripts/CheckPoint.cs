using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Color activatedColor = new Color(0f, 1f, 0.5f);

    private bool isActivated = false;
    private Renderer tileRenderer;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (isActivated) return;
        CheckPlayerAbove();
    }

    void CheckPlayerAbove()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit hit;

        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 2f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                isActivated = true;

                FallDetection fallDetection = hit.collider.GetComponent<FallDetection>();
                if (fallDetection != null)
                    fallDetection.UpdateSpawnPoint(transform.position, Quaternion.identity);

                if (tileRenderer != null && tileRenderer.materials.Length > 1)
                    tileRenderer.materials[1].SetColor("_EmissionColor", activatedColor * 2);

                // Show checkpoint popup
                if (CheckpointPopup.Instance != null)
                    CheckpointPopup.Instance.Show();
            }
        }
    }
}