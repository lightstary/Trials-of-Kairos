using UnityEngine;

/// <summary>
/// Simple floating animation for decorative celestial orbs in the HUB level.
/// </summary>
public class HubFloatingOrb : MonoBehaviour
{
    private const float BOB_SPEED     = 0.8f;
    private const float BOB_AMPLITUDE = 0.3f;
    private const float ROTATE_SPEED  = 15f;

    private Vector3 _startPos;
    private float   _phaseOffset;

    void Start()
    {
        _startPos    = transform.position;
        _phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * BOB_SPEED + _phaseOffset) * BOB_AMPLITUDE;
        transform.position = _startPos + Vector3.up * y;
        transform.Rotate(Vector3.up, ROTATE_SPEED * Time.deltaTime);
    }
}
