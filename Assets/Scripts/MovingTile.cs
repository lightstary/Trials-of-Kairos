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

    [Header("Smooth Movement")]
    [Tooltip("When true, the tile moves continuously instead of jumping on ticks.")]
    public bool smoothMovement = false;

    private const float RIDE_RADIUS = 0.7f;
    private const float RIDE_HEIGHT = 2.5f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float currentTime;
    private float tickTimer = 0f;
    private Transform _player;

    void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + moveDirection.normalized * moveDistance;
        currentTime = minTime;
    }

    void Update()
    {
        if (TimeState.Instance == null) return;

        if (_player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        bool riding = _player != null && IsPlayerOnTile();

        Vector3 posBeforeMove = transform.position;

        if (smoothMovement)
            UpdateSmooth();
        else
            UpdateTicked();

        if (riding)
        {
            Vector3 delta = transform.position - posBeforeMove;
            if (delta.sqrMagnitude > 0.0001f)
            {
                _player.position += delta;
            }
        }
    }

    private void UpdateSmooth()
    {
        float rate = tickInterval > 0f ? (1f / tickInterval) : 1f;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward:
                if (currentTime < maxTime)
                {
                    currentTime += rate * Time.deltaTime;
                    currentTime = Mathf.Min(currentTime, maxTime);
                    ApplyPosition();
                }
                break;
            case TimeState.State.Frozen:
                break;
            case TimeState.State.Reverse:
                if (currentTime > minTime)
                {
                    currentTime -= rate * Time.deltaTime;
                    currentTime = Mathf.Max(currentTime, minTime);
                    ApplyPosition();
                }
                break;
        }
    }

    private void UpdateTicked()
    {
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
                    ApplyPosition();
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
                    ApplyPosition();
                }
                break;
        }
    }

    public bool IsPlayerOnTile()
    {
        if (_player == null) return false;

        Vector3 tilePos = transform.position;
        float tileTopY = tilePos.y + transform.lossyScale.y * 0.5f;
        Vector3 playerPos = _player.position;

        float dx = playerPos.x - tilePos.x;
        float dz = playerPos.z - tilePos.z;
        if (dx * dx + dz * dz > RIDE_RADIUS * RIDE_RADIUS) return false;

        float heightAbove = playerPos.y - tileTopY;
        return heightAbove > -0.1f && heightAbove < RIDE_HEIGHT;
    }

    private void ApplyPosition()
    {
        float progress = Mathf.InverseLerp(minTime, maxTime, currentTime);
        transform.position = Vector3.Lerp(startPosition, endPosition, progress);
    }
}