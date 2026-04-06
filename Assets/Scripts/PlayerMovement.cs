using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool isMoving = false;

    public enum Orientation { Standing, FlatX, FlatZ }
    public Orientation orientation = Orientation.Standing;

    private const float TILE_TOP       = 0.1f;
    private const float AXIS_THRESHOLD = 0.5f;

    // Previous-frame axis values for rising-edge detection
    private float prevH = 0f;
    private float prevV = 0f;

    void Start()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x);
        pos.z = Mathf.Round(pos.z);
        pos.y = TILE_TOP + 1.0f;
        transform.position = pos;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        // Skip while rolling or while the menu/pause has frozen time
        if (isMoving || Time.timeScale == 0f) return;

        // "Horizontal" / "Vertical" axes map to Xbox left stick (Axis 1 & 2) in Unity's default Input Manager
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 input = Vector3.zero;

        // Rising-edge detection: only fire once per tilt, not while held
        if      (v >  AXIS_THRESHOLD && prevV <=  AXIS_THRESHOLD) input = Vector3.forward;
        else if (v < -AXIS_THRESHOLD && prevV >= -AXIS_THRESHOLD) input = Vector3.back;
        else if (h < -AXIS_THRESHOLD && prevH >= -AXIS_THRESHOLD) input = Vector3.left;
        else if (h >  AXIS_THRESHOLD && prevH <=  AXIS_THRESHOLD) input = Vector3.right;

        prevH = h;
        prevV = v;

        if (input != Vector3.zero)
            StartCoroutine(Roll(input));
    }

    IEnumerator Roll(Vector3 dir)
    {
        isMoving = true;

        float halfHeight = GetHalfHeight();
        Vector3 bottomCenter = transform.position;
        bottomCenter.y = transform.position.y - halfHeight;

        float edgeOffset = GetLeadingEdge(dir);
        Vector3 pivot = bottomCenter + dir * edgeOffset;
        pivot.y = TILE_TOP;

        Vector3 rotAxis = new Vector3(dir.z, 0f, -dir.x);

        float totalAngle = 0f;
        while (totalAngle < 90f)
        {
            float step = moveSpeed * Time.deltaTime * 90f;
            if (totalAngle + step > 90f) step = 90f - totalAngle;
            transform.RotateAround(pivot, rotAxis, step);
            totalAngle += step;
            yield return null;
        }

        orientation = GetNextOrientation(dir);
        SnapAfterRoll();

        isMoving = false;
    }

    float GetHalfHeight()
    {
        switch (orientation)
        {
            case Orientation.Standing: return 1.0f;
            case Orientation.FlatX:    return 0.5f;
            case Orientation.FlatZ:    return 0.5f;
            default: return 1.0f;
        }
    }

    float GetLeadingEdge(Vector3 dir)
    {
        bool alongX = Mathf.Abs(dir.x) > 0.5f;

        switch (orientation)
        {
            case Orientation.Standing: return 0.5f;
            case Orientation.FlatX:    return alongX ? 1.0f : 0.5f;
            case Orientation.FlatZ:    return alongX ? 0.5f : 1.0f;
            default: return 0.5f;
        }
    }

    Orientation GetNextOrientation(Vector3 dir)
    {
        bool alongX = Mathf.Abs(dir.x) > 0.5f;

        switch (orientation)
        {
            case Orientation.Standing: return alongX ? Orientation.FlatX : Orientation.FlatZ;
            case Orientation.FlatX:    return alongX ? Orientation.Standing : Orientation.FlatX;
            case Orientation.FlatZ:    return alongX ? Orientation.FlatZ : Orientation.Standing;
            default: return Orientation.Standing;
        }
    }

    void SnapAfterRoll()
    {
        float halfHeight = GetHalfHeight();

        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x * 2f) / 2f;
        pos.z = Mathf.Round(pos.z * 2f) / 2f;
        pos.y = TILE_TOP + halfHeight;
        transform.position = pos;

        Vector3 euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        transform.eulerAngles = euler;
    }

    // Called by FallDetection to reset after respawn
    public void ResetMovement()
    {
        isMoving = false;
        StopAllCoroutines();
    }
}