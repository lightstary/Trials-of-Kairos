using UnityEngine;

public class MovingTile : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right;
    public float moveDistance = 2f;
    public float tickInterval = 1f; // seconds between each snap

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float journeyProgress = 0f;
    private float currentDirection = 1f;
    private float tickTimer = 0f;

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
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    SnapMove(1f);
                }
                break;

            case TimeState.State.Frozen:
                tickTimer = 0f; // reset so it waits full second after unfreezing
                break;

            case TimeState.State.Reverse:
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    SnapMove(-1f);
                }
                break;
        }
    }

    void SnapMove(float timeDirection)
    {
        // Move one snap step in the current direction
        journeyProgress += currentDirection * timeDirection * (1f / moveDistance);

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