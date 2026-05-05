using System.Collections.Generic;
using UnityEngine;

public class DecorationsManager : MonoBehaviour
{
    [Header("Decorations List")]
    public List<GameObject> allDecor = new List<GameObject>();

    [Header("Float Settings")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 0.3f;

    [Header("Orbit Settings (0 = disabled)")]
    public float orbitRadius = 0f;
    public float orbitSpeed = 0f;

    [Header("Rotation Settings (0 = disabled)")]
    public float rotateSpeed = 0f;

    [Header("Safety (0 = disabled)")]
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

            float bobY = Mathf.Sin(t * floatSpeed * s.floatSpeedMul + s.phaseOffset) * floatHeight;

            Vector3 newPos = s.homePosition;
            newPos.y += bobY;

            if (hasOrbit)
            {
                float orbitAngle = t * orbitSpeed * s.orbitSpeedMul + s.orbitPhase;
                float r = orbitRadius * s.orbitRadiusMul;
                newPos.x += Mathf.Cos(orbitAngle) * r;
                newPos.z += Mathf.Sin(orbitAngle * 0.7f + 1.3f) * r * 0.6f;
            }

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

            if (hasRotation && timeDirection != 0f)
            {
                float rotDelta = rotateSpeed * s.rotationRate * Time.deltaTime * timeDirection;
                allDecor[i].transform.Rotate(s.rotationAxis, rotDelta, Space.World);
            }
        }
    }

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