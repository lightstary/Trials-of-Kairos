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

        // Get actual up direction of player in world space
        Vector3 playerUp = playerMovement.transform.up;

        if (playerUp.y > 0.7f)
            currentState = State.Forward;   // Y+ facing up = standing = time forward
        else if (playerUp.y < -0.7f)
            currentState = State.Reverse;   // Y- facing up = upside down = time reverse
        else
            currentState = State.Frozen;    // X or Z facing up = on its side = frozen

        if (currentState != previous)
            OnStateChanged?.Invoke(currentState);
    }
}