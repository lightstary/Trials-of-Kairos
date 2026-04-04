using UnityEngine;

public class ColorFeedback : MonoBehaviour
{
    [Header("Time State Colors")]
    public Color forwardColor = new Color(0.1f, 0.08f, 0.05f); // warm yellow tint
    public Color frozenColor  = new Color(0.05f, 0.08f, 0.15f); // cool blue tint
    public Color reverseColor = new Color(0.08f, 0.03f, 0.12f); // dark purple tint

    public float transitionSpeed = 3f;

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        if (TimeState.Instance == null) return;

        Color targetColor = forwardColor;

        switch (TimeState.Instance.currentState)
        {
            case TimeState.State.Forward: targetColor = forwardColor; break;
            case TimeState.State.Frozen:  targetColor = frozenColor;  break;
            case TimeState.State.Reverse: targetColor = reverseColor; break;
        }

        cam.backgroundColor = Color.Lerp(cam.backgroundColor, targetColor, transitionSpeed * Time.deltaTime);
    }
}