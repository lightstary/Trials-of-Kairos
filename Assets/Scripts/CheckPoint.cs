using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public Color activatedColor = new Color(0f, 1f, 0.5f);

    private bool isActivated = false;
    private Renderer tileRenderer;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();
        Debug.Log("Checkpoint ready on: " + gameObject.name);
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

        
        Debug.DrawRay(rayOrigin, Vector3.up * 2f, Color.green);

        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 2f))
        {
            Debug.Log("Ray hit: " + hit.collider.gameObject.name + " tag: " + hit.collider.tag);

            if (hit.collider.CompareTag("Player"))
            {
                isActivated = true;

                FallDetection fallDetection = hit.collider.GetComponent<FallDetection>();
                if (fallDetection != null)
                {
                    fallDetection.UpdateSpawnPoint(transform.position, Quaternion.identity);
                    Debug.Log("Spawn point updated!");
                }
                else
                    Debug.Log("FallDetection not found!");

                if (tileRenderer != null)
                    tileRenderer.material.color = activatedColor;

                Debug.Log("Checkpoint activated!");
            }
        }
        else
        {
            //  when nothing is hit
            // Debug.Log("Ray hit nothing");
        }
    }
}