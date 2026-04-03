using UnityEngine;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    public Vector2Int gridPos; // Player's main tile
    public float moveSpeed = 5f;

    private bool isMoving = false;

    public enum PlayerState { Upright, FlatX, FlatZ }
    public PlayerState state = PlayerState.Upright;

    private Vector2Int lastMoveDir = Vector2Int.zero;

    void Update()
    {
        if (isMoving) return;

        Vector2Int input = GetInput();
        if (input != Vector2Int.zero)
        {
            AttemptMove(input);
        }
    }

    Vector2Int GetInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) return Vector2Int.up;
        if (Input.GetKeyDown(KeyCode.DownArrow)) return Vector2Int.down;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) return Vector2Int.left;
        if (Input.GetKeyDown(KeyCode.RightArrow)) return Vector2Int.right;
        return Vector2Int.zero;
    }

    void AttemptMove(Vector2Int dir)
    {
        // Determine next state based on current state and move direction
        PlayerState nextState = state;
        Vector3 offset = Vector3.zero;

        if (state == PlayerState.Upright)
        {
            // Falling flat along move axis
            if (dir.x != 0)
            {
                nextState = PlayerState.FlatX;
                offset = new Vector3(dir.x * 0.5f, 0, 0);
            }
            else
            {
                nextState = PlayerState.FlatZ;
                offset = new Vector3(0, 0, dir.y * 0.5f);
            }
        }
        else if (state == PlayerState.FlatX)
        {
            if (dir.x != 0)
            {
                nextState = PlayerState.Upright;
                offset = new Vector3(dir.x * 0.5f, 0.5f, 0);
            }
            else
            {
                nextState = PlayerState.FlatZ;
                offset = new Vector3(0, -0.25f, dir.y * 0.5f);
            }
        }
        else if (state == PlayerState.FlatZ)
        {
            if (dir.y != 0)
            {
                nextState = PlayerState.Upright;
                offset = new Vector3(0, 0.5f, dir.y * 0.5f);
            }
            else
            {
                nextState = PlayerState.FlatX;
                offset = new Vector3(dir.x * 0.5f, -0.25f, 0);
            }
        }

        // Update main grid position
        gridPos += dir;

        state = nextState;

        // Move smoothly to new position
        StopAllCoroutines();
        StartCoroutine(MoveTo(gridPos, offset));
    }

    IEnumerator MoveTo(Vector2Int targetGrid, Vector3 offset)
    {
        isMoving = true;

        Vector3 start = transform.position;
        Vector3 end = new Vector3(targetGrid.x, 0, targetGrid.y) + offset;

        while (Vector3.Distance(transform.position, end) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, end, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = end;
        isMoving = false;
    }
}