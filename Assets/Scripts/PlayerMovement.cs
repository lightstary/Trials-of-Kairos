using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool isMoving = false;

    public enum Orientation { Standing, FlatX, FlatZ, UpsideDown, FlatX_R, FlatZ_R }
    public Orientation orientation = Orientation.Standing;

    private const float TILE_TOP       = 0.1f;
    private const float AXIS_THRESHOLD = 0.5f;

    private float prevH = 0f;
    private float prevV = 0f;

    private float inputBufferTime = 0.8f;
    private float inputBufferTimer = 0f;
    private Vector3 bufferedInput = Vector3.zero;

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
        if (Time.timeScale == 0f) return;

        Vector3 input = Vector3.zero;

        if (Input.GetKeyDown(KeyCode.UpArrow)         || Input.GetKeyDown(KeyCode.W)) input = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.DownArrow)  || Input.GetKeyDown(KeyCode.S)) input = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)  || Input.GetKeyDown(KeyCode.A)) input = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) input = Vector3.right;
        else
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

            if      (v >  AXIS_THRESHOLD && prevV <=  AXIS_THRESHOLD) input = Vector3.forward;
            else if (v < -AXIS_THRESHOLD && prevV >= -AXIS_THRESHOLD) input = Vector3.back;
            else if (h < -AXIS_THRESHOLD && prevH >= -AXIS_THRESHOLD) input = Vector3.left;
            else if (h >  AXIS_THRESHOLD && prevH <=  AXIS_THRESHOLD) input = Vector3.right;

            prevH = h;
            prevV = v;
        }

        if (input != Vector3.zero)
        {
            bufferedInput = input;
            inputBufferTimer = inputBufferTime;
        }

        if (inputBufferTimer > 0f)
            inputBufferTimer -= Time.deltaTime;
        else
            bufferedInput = Vector3.zero;

        if (!isMoving && bufferedInput != Vector3.zero)
        {
            StartCoroutine(Roll(bufferedInput));
            bufferedInput = Vector3.zero;
            inputBufferTimer = 0f;
        }
    }

    IEnumerator Roll(Vector3 dir)
    {
        isMoving = true;

        // Play move sound
        SoundManager.Instance?.PlayMove();

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
            case Orientation.Standing:   return 1.0f;
            case Orientation.UpsideDown: return 1.0f;
            case Orientation.FlatX:
            case Orientation.FlatX_R:   return 0.5f;
            case Orientation.FlatZ:
            case Orientation.FlatZ_R:   return 0.5f;
            default: return 1.0f;
        }
    }

    float GetLeadingEdge(Vector3 dir)
    {
        bool alongX = Mathf.Abs(dir.x) > 0.5f;

        switch (orientation)
        {
            case Orientation.Standing:
            case Orientation.UpsideDown: return 0.5f;
            case Orientation.FlatX:
            case Orientation.FlatX_R:   return alongX ? 1.0f : 0.5f;
            case Orientation.FlatZ:
            case Orientation.FlatZ_R:   return alongX ? 0.5f : 1.0f;
            default: return 0.5f;
        }
    }

    Orientation GetNextOrientation(Vector3 dir)
    {
        bool alongX = Mathf.Abs(dir.x) > 0.5f;

        switch (orientation)
        {
            case Orientation.Standing:
                return alongX ? Orientation.FlatX : Orientation.FlatZ;

            case Orientation.FlatX:
                return alongX ? Orientation.UpsideDown : Orientation.FlatX;
            case Orientation.FlatZ:
                return alongX ? Orientation.FlatZ : Orientation.UpsideDown;

            case Orientation.UpsideDown:
                return alongX ? Orientation.FlatX_R : Orientation.FlatZ_R;

            case Orientation.FlatX_R:
                return alongX ? Orientation.Standing : Orientation.FlatX_R;
            case Orientation.FlatZ_R:
                return alongX ? Orientation.FlatZ_R : Orientation.Standing;

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

    public void ResetMovement()
    {
        isMoving = false;
        StopAllCoroutines();
    }
}