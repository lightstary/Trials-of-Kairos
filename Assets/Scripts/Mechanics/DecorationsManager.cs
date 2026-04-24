using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages floating decorations with optional orbiting, rotation, and fluid sine-wave
/// bobbing. Movement direction responds to the player's current TimeState:
/// forward plays normally, reverse plays backward, frozen stops.
/// Set orbitRadius and rotateSpeed to 0 for simple float-only behavior.
/// </summary>
public class DecorationsManager : MonoBehaviour
{
    [Header("Decorations List")]
    public List<GameObject> allDecor = new List<GameObject>();

    [Header("Float Settings")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 0.3f;

    [Header("Orbit Settings (0 = disabled)")]
    [Tooltip("Radius of the gentle orbit around each decoration's home position. Set to 0 to disable.")]
    public float orbitRadius = 0f;

    [Tooltip("Base orbit speed multiplier.")]
    public float orbitSpeed = 0f;

    [Header("Rotation Settings (0 = disabled)")]
    [Tooltip("Base rotation speed in degrees per second. Set to 0 to disable.")]
    public float rotateSpeed = 0f;

    [Header("Safety (0 = disabled)")]
    [Tooltip("Minimum horizontal distance from world origin. Set to 0 to disable.")]
    public float minDistanceFromCenter = 0f;

    private struct DecorState
    {
        public Vector3 homePosition;
        public float phaseOffset;
        public float orbitPhase;
        public Vector3 rotationAxis;
        public float rotationRate;
        public float orbitRadiusMul;
        public float orbitSpeedMul;
        public float floatSpeedMul;
    }

    private DecorState[] _states;

    /// <summary>Internal time accumulator driven by TimeState.</summary>
    private float _simulatedTime;

    void Start()
    {
        _states = new DecorState[allDecor.Count];
        for (int i = 0; i < allDecor.Count; i++)
        {
            if (allDecor[i] == null) continue;

            _states[i] = new DecorState
            {
                homePosition = allDecor[i].transform.position,
                phaseOffset = Random.Range(0f, Mathf.PI * 2f),
                orbitPhase = Random.Range(0f, Mathf.PI * 2f),
                rotationAxis = Random.onUnitSphere,
                rotationRate = Random.Range(0.5f, 1.5f),
                orbitRadiusMul = Random.Range(0.6f, 1.4f),
                orbitSpeedMul = Random.Range(0.7f, 1.3f),
                floatSpeedMul = Random.Range(0.8f, 1.2f)
            };
        }
    }

    void Update()
    {
        if (_states == null) return;

        float timeDirection = GetTimeDirection();
        _simulatedTime += Time.deltaTime * timeDirection;

        bool hasOrbit = orbitRadius > 0f && orbitSpeed > 0f;
        bool hasRotation = rotateSpeed > 0f;
        bool hasClamp = minDistanceFromCenter > 0f;

        for (int i = 0; i < allDecor.Count; i++)
        {
            if (allDecor[i] == null) continue;

            ref DecorState s = ref _states[i];
            float t = _simulatedTime;

            // ── Vertical bobbing (sine wave) ──
            float bobY = Mathf.Sin(t * floatSpeed * s.floatSpeedMul + s.phaseOffset) * floatHeight;

            Vector3 newPos = s.homePosition;
            newPos.y += bobY;

            // ── Horizontal orbit (only if enabled) ──
            if (hasOrbit)
            {
                float orbitAngle = t * orbitSpeed * s.orbitSpeedMul + s.orbitPhase;
                float r = orbitRadius * s.orbitRadiusMul;
                newPos.x += Mathf.Cos(orbitAngle) * r;
                newPos.z += Mathf.Sin(orbitAngle * 0.7f + 1.3f) * r * 0.6f;
            }

            // ── Clamp to minimum distance from play area center (only if enabled) ──
            if (hasClamp)
            {
                float horizDist = Mathf.Sqrt(newPos.x * newPos.x + newPos.z * newPos.z);
                if (horizDist < minDistanceFromCenter && horizDist > 0.01f)
                {
                    float scale = minDistanceFromCenter / horizDist;
                    newPos.x *= scale;
                    newPos.z *= scale;
                }
            }

            allDecor[i].transform.position = newPos;

            // ── Gentle rotation (only if enabled) ──
            if (hasRotation && timeDirection != 0f)
            {
                float rotDelta = rotateSpeed * s.rotationRate * Time.deltaTime * timeDirection;
                allDecor[i].transform.Rotate(s.rotationAxis, rotDelta, Space.World);
            }
        }
    }

    /// <summary>
    /// Returns +1 for forward, -1 for reverse, 0 for frozen.
    /// Falls back to +1 if TimeState is unavailable.
    /// </summary>
    private float GetTimeDirection()
    {
        if (TimeState.Instance == null) return 1f;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward: return 1f;
            case TimeState.State.Reverse: return -1f;
            case TimeState.State.Frozen:  return 0f;
            default:                      return 1f;
        }
    }
}
