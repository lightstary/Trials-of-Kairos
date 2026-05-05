using UnityEngine;

// Kills the player on pendulum collision. Pendulums must be tagged "Pendulum".
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
