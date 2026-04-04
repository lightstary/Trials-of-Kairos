using UnityEngine;

public class MovingTile : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right;
    public float moveDistance = 2f;
    public float moveSpeed = 2f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float journeyProgress = 0f;
    private float currentDirection = 1f; 

    void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + moveDirection.normalized * moveDistance;
    }

    void Update()
    {
        if (TimeState.Instance == null) return;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward:
                MoveTile(1f);
                break;

            case TimeState.State.Frozen:
                // Do nothing
                break;

            case TimeState.State.Reverse:
                MoveTile(-1f);
                break;
        }
    }

    void MoveTile(float timeDirection)
    {
        // Advance progress
        journeyProgress += currentDirection * timeDirection * moveSpeed * Time.deltaTime / moveDistance;

        // Flip direction at each end
        if (journeyProgress >= 1f)
        {
            journeyProgress = 1f;
            currentDirection = -1f;
        }
        else if (journeyProgress <= 0f)
        {
            journeyProgress = 0f;
            currentDirection = 1f;
        }

        transform.position = Vector3.Lerp(startPosition, endPosition, journeyProgress);
    }
}