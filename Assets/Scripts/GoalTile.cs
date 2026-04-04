using UnityEngine;

public class GoalTile : MonoBehaviour
{
    public Color goalColor = new Color(1f, 0.8f, 0f); // gold color

    private bool isCompleted = false;
    private Renderer tileRenderer;

    void Start()
    {
        tileRenderer = GetComponent<Renderer>();

        if (tileRenderer != null)
            tileRenderer.material.color = goalColor;

        Debug.Log("Goal tile ready!");
    }

    void Update()
    {
        if (isCompleted) return;
        CheckPlayerAbove();
    }

    void CheckPlayerAbove()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 0.2f;
        RaycastHit hit;

        Debug.DrawRay(rayOrigin, Vector3.up * 2f, Color.yellow);

        if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 2f))
        {
            if (hit.collider.CompareTag("Player"))
            {
                isCompleted = true;
                Debug.Log("Level Complete!!");
                // We'll hook up scene loading / win screen here later
            }
        }
    }
}