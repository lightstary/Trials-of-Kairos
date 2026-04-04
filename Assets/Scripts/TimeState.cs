using UnityEngine;

public class TimeState : MonoBehaviour
{
    public enum State { Forward, Frozen, Reverse }
    public State currentState { get; private set; } = State.Forward;

    // Static instance so any script can access it easily
    public static TimeState Instance;

    private PlayerMovement playerMovement;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        UpdateTimeState();
    }

    void UpdateTimeState()
    {
        switch (playerMovement.orientation)
        {
            case PlayerMovement.Orientation.Standing:
                currentState = State.Forward;
                break;

            case PlayerMovement.Orientation.FlatX:
            case PlayerMovement.Orientation.FlatZ:
                currentState = State.Frozen;
                break;

            // Reverse will go here later when upside down is implemented
            default:
                currentState = State.Forward;
                break;
        }
    }
}