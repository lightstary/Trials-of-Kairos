using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f; // speed of tip rotation
    private bool isMoving = false;

    public enum PlayerState { Upright, Flat }
    public PlayerState state = PlayerState.Upright;

    private Vector3 pivot; // rotation pivot point
    private Vector3 rotationAxis; // axis to rotate around

    void Update()
    {
        if (isMoving) return;

        Vector3 input = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow)) input = Vector3.forward;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) input = Vector3.back;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) input = Vector3.left;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) input = Vector3.right;

        if (input != Vector3.zero)
            StartCoroutine(Roll(input));
    }

    IEnumerator Roll(Vector3 dir)
    {
        isMoving = true;

        // Determine rotation axis (cross with up vector)
        rotationAxis = Vector3.Cross(Vector3.up, dir);

        // Determine pivot point based on state
        float halfHeight = 0.543f; // half of your rectangle height (1.086 / 2)
        float halfWidth = 0.5f;     // half width of your rectangle (assuming 1)

        if (state == PlayerState.Upright)
        {
            // Upright → Flat
            pivot = transform.position + (dir * halfWidth) + Vector3.down * halfHeight;
            state = PlayerState.Flat;
        }
        else // Flat → Upright
        {
            // Flat → Upright: use halfHeight as horizontal offset to pivot
            pivot = transform.position + (dir * halfHeight) + Vector3.down * halfWidth;
            state = PlayerState.Upright;
        }

        float angle = 0f;
        while (angle < 90f)
        {
            float step = moveSpeed * Time.deltaTime * 90f; // degrees per frame
            if (angle + step > 90f) step = 90f - angle;

            transform.RotateAround(pivot, rotationAxis, step);

            // Lock Y at tile top (0.6)
            Vector3 pos = transform.position;
            pos.y = 0.6f;
            transform.position = pos;

            angle += step;
            yield return null;
        }

        // Snap X/Z to grid, Y stays at 0.6
        transform.position = new Vector3(
            Mathf.Round(transform.position.x),
            0.6f,
            Mathf.Round(transform.position.z)
        );

        isMoving = false;
    }
}