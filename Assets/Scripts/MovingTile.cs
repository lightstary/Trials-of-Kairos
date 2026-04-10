using UnityEngine;

public class MovingTile : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 moveDirection = Vector3.right;
    public float moveDistance = 2f;
    public float tickInterval = 1f;

    [Header("Timeline Settings")]
    public float minTime = -4f;
    public float maxTime = 4f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float currentTime = 0f;
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
                if (currentTime >= maxTime) return;
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    currentTime += 1f;
                    currentTime = Mathf.Clamp(currentTime, minTime, maxTime);
                    UpdatePosition();
                }
                break;

            case TimeState.State.Frozen:
                tickTimer = 0f;
                break;

            case TimeState.State.Reverse:
                if (currentTime <= minTime) return;
                tickTimer += Time.deltaTime;
                if (tickTimer >= tickInterval)
                {
                    tickTimer = 0f;
                    currentTime -= 1f;
                    currentTime = Mathf.Clamp(currentTime, minTime, maxTime);
                    UpdatePosition();
                }
                break;
        }
    }

    void UpdatePosition()
    {
        float progress = Mathf.InverseLerp(minTime, maxTime, currentTime);
        transform.position = Vector3.Lerp(startPosition, endPosition, progress);
    }
}