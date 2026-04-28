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

    private const float RIDE_RADIUS = 0.7f;
    private const float RIDE_HEIGHT = 2.5f;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float currentTime;
    private Transform _player;
    private Vector3 _frameDelta;

    void Start()
    {
        startPosition = transform.position;
        endPosition = startPosition + moveDirection.normalized * moveDistance;
        currentTime = minTime;
    }

    void Update()
    {
        if (TimeState.Instance == null) return;

        Vector3 posBeforeMove = transform.position;

        float rate = tickInterval > 0f ? (1f / tickInterval) : 1f;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward:
                if (currentTime < maxTime)
                {
                    currentTime += rate * Time.deltaTime;
                    currentTime = Mathf.Min(currentTime, maxTime);
                }
                break;
            case TimeState.State.Frozen:
                break;
            case TimeState.State.Reverse:
                if (currentTime > minTime)
                {
                    currentTime -= rate * Time.deltaTime;
                    currentTime = Mathf.Max(currentTime, minTime);
                }
                break;
        }

        float progress = Mathf.InverseLerp(minTime, maxTime, currentTime);
        transform.position = Vector3.Lerp(startPosition, endPosition, progress);

        _frameDelta = transform.position - posBeforeMove;
    }

    void LateUpdate()
    {
        if (_frameDelta.sqrMagnitude < 0.000001f) return;

        if (_player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) _player = p.transform;
        }

        if (_player != null && IsPlayerOnTile())
        {
            _player.position += _frameDelta;
        }
    }

    /// <summary>Checks whether the player is standing on this tile.</summary>
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
}
