using System;
using UnityEngine;

public class TimeState : MonoBehaviour
{
    public enum State { Forward, Frozen, Reverse }
    public State currentState { get; private set; } = State.Forward;

    /// <summary>
    /// Fired whenever the time state changes. Passes the new state.
    /// </summary>
    public event Action<State> OnStateChanged;

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
        State previous = currentState;

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

        if (currentState != previous)
        {
            OnStateChanged?.Invoke(currentState);
        }
    }
}