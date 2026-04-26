using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public bool isMoving = false;

    public enum Orientation { Standing, FlatX, FlatZ, UpsideDown, FlatX_R, FlatZ_R }
    public Orientation orientation = Orientation.Standing;

    private const float TILE_TOP       = 0.1f;

    /// <summary>Threshold for analog stick input to register as a directional tap.
    /// Falls back to 0.5 when the left stick deadzone is lower.</summary>
    private float AxisThreshold => Mathf.Max(GameSettings.LeftStickDeadzone, 0.25f);

    private float prevH = 0f;
    private float prevV = 0f;

    private const float INPUT_BUFFER_TIME = 0.15f;
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

        // ── Read raw input as 2D intent ─────────────────────────────────
        Vector2 rawIntent = Vector2.zero;

        // Read current axis values (used for both keyboard sync and stick detection)
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if      (Input.GetKeyDown(KeyCode.UpArrow)    || Input.GetKeyDown(KeyCode.W)) rawIntent = Vector2.up;
        else if (Input.GetKeyDown(KeyCode.DownArrow)   || Input.GetKeyDown(KeyCode.S)) rawIntent = Vector2.down;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)   || Input.GetKeyDown(KeyCode.A)) rawIntent = Vector2.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)  || Input.GetKeyDown(KeyCode.D)) rawIntent = Vector2.right;
        else
        {
            // Analog stick threshold crossing (controller only path in practice)
            float threshold = AxisThreshold;

            if      (v >  threshold && prevV <=  threshold) rawIntent = Vector2.up;
            else if (v < -threshold && prevV >= -threshold) rawIntent = Vector2.down;
            else if (h < -threshold && prevH >= -threshold) rawIntent = Vector2.left;
            else if (h >  threshold && prevH <=  threshold) rawIntent = Vector2.right;
        }

        // Always sync axis tracking to prevent the analog threshold-crossing
        // from re-firing after a GetKeyDown already consumed the input.
        prevH = h;
        prevV = v;

        // ── Resolve intent relative to gameplay camera ──────────────────
        if (rawIntent != Vector2.zero)
        {
            input = ResolveCameraRelativeDirection(rawIntent);
        }

        if (input != Vector3.zero)
        {
            bufferedInput = input;
            inputBufferTimer = INPUT_BUFFER_TIME;
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

    /// <summary>
    /// Converts a 2D input intent (up/down/left/right) into a world-space
    /// cardinal direction (Vector3.forward/back/left/right) relative to
    /// the current gameplay camera's facing direction on the horizontal plane.
    /// </summary>
    private Vector3 ResolveCameraRelativeDirection(Vector2 intent)
    {
        Camera cam = Camera.main;
        if (cam == null) return new Vector3(intent.x, 0f, intent.y);

        // Project camera axes onto the horizontal plane
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight   = cam.transform.right;
        camForward.y = 0f;
        camRight.y   = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // If camera is looking straight down, fall back to world axes
        if (camForward.sqrMagnitude < 0.001f)
            return new Vector3(intent.x, 0f, intent.y);

        // Compose the desired world direction from camera-relative intent
        Vector3 desiredDir = camForward * intent.y + camRight * intent.x;

        // Snap to the nearest cardinal direction for the tile/roll grid system
        return SnapToCardinal(desiredDir);
    }

    /// <summary>
    /// Returns the nearest cardinal direction (±X or ±Z) for a given vector.
    /// </summary>
    private static Vector3 SnapToCardinal(Vector3 dir)
    {
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.z))
            return dir.x >= 0f ? Vector3.right : Vector3.left;
        else
            return dir.z >= 0f ? Vector3.forward : Vector3.back;
    }

    IEnumerator Roll(Vector3 dir)
    {
        isMoving = true;

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