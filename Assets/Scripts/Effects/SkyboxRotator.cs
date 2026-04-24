using UnityEngine;

/// <summary>
/// Slowly rotates the camera to create a dynamic, gently shifting skybox background.
/// Uses unscaled time so it works even when Time.timeScale is 0 (e.g. in menus).
/// </summary>
public class SkyboxRotator : MonoBehaviour
{
    [Tooltip("Rotation speed in degrees per second around the Y axis.")]
    public float rotationSpeed = 3f;

    [Tooltip("Gentle pitch oscillation amplitude in degrees.")]
    public float pitchAmplitude = 2f;

    [Tooltip("Pitch oscillation speed.")]
    public float pitchSpeed = 0.15f;

    private float _basePitch;

    void Start()
    {
        _basePitch = transform.eulerAngles.x;
    }

    void Update()
    {
        float t = Time.unscaledTime;
        float yaw = t * rotationSpeed;
        float pitch = _basePitch + Mathf.Sin(t * pitchSpeed) * pitchAmplitude;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
