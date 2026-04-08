using System;
using UnityEngine;

public class TimeState : MonoBehaviour
{
    public enum State { Forward, Frozen, Reverse }
    public State currentState { get; private set; } = State.Forward;

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

            case PlayerMovement.Orientation.UpsideDown:
                currentState = State.Reverse;
                break;

            case PlayerMovement.Orientation.FlatX_R:
            case PlayerMovement.Orientation.FlatZ_R:
                currentState = State.Frozen;
                break;

            default:
                currentState = State.Forward;
                break;
        }

        if (currentState != previous)
            OnStateChanged?.Invoke(currentState);
    }
}