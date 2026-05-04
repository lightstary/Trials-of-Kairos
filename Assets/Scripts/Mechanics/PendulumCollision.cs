using UnityEngine;

/// <summary>
/// Kills the player when a pendulum physically collides with the hourglass.
/// Attach to the player GameObject. Pendulums must be tagged "Pendulum".
/// </summary>
public class PendulumCollision : MonoBehaviour
{
    private FallDetection fallDetection;

    void Start()
    {
        fallDetection = GetComponent<FallDetection>();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Pendulum"))
        {
            if (fallDetection != null)
            {
                fallDetection.TriggerPendulumDeath();
            }
        }
    }
}
