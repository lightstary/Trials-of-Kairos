using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    private bool isMoving = false;

    private enum Orientation { Standing, FlatX, FlatZ }
    private Orientation orientation = Orientation.Standing;

    const float TILE_TOP = 0.1f;

    // Half extents of block in each orientation
    // Block is 1x2x1 (W x H x D)
    // Standing:  half height = 1.0,  half width = 0.5
    // FlatX:     half height = 0.5,  half length on X = 1.0
    // FlatZ:     half height = 0.5,  half length on Z = 1.0

    void Start()
    {
        Vector3 pos = transform.position;
        pos.x = Mathf.Round(pos.x);
        pos.z = Mathf.Round(pos.z);
        pos.y = TILE_TOP + 1.0f; // tile top + half height when standing
        transform.position = pos;
        transform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (isMoving) return;

        Vector3 input = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))         input = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.DownArrow))  input = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))  input = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) input = Vector3.right;

        if (input != Vector3.zero)
            StartCoroutine(Roll(input));
    }

    IEnumerator Roll(Vector3 dir)
    {
        isMoving = true;

        // Get the actual bottom center of the block in world space
        float halfHeight = GetHalfHeight();
        Vector3 bottomCenter = transform.position;
        bottomCenter.y = transform.position.y - halfHeight; // actual bottom face

        // Pivot = bottom center + leading edge offset in movement direction
        float edgeOffset = GetLeadingEdge(dir);
        Vector3 pivot = bottomCenter + dir * edgeOffset;
        // Pivot Y must stay exactly on tile top
        pivot.y = TILE_TOP;

        // Rotation axis: tips block forward in movement direction
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
            case Orientation.Standing: return 1.0f; // half of 2
            case Orientation.FlatX:    return 0.5f; // half of 1
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
        pos.y = TILE_TOP + halfHeight; // tile top + new half height after orientation change
        transform.position = pos;

        Vector3 euler = transform.eulerAngles;
        euler.x = Mathf.Round(euler.x / 90f) * 90f;
        euler.y = Mathf.Round(euler.y / 90f) * 90f;
        euler.z = Mathf.Round(euler.z / 90f) * 90f;
        transform.eulerAngles = euler;
    }
}