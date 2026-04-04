using UnityEngine;
using UnityEngine.SceneManagement;

public class FallDetection : MonoBehaviour
{
    const float TILE_TOP = 0.1f;
    const float RAY_LENGTH = 1.5f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private PlayerMovement playerMovement;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        playerMovement = GetComponent<PlayerMovement>();
    }

    void Update()
    {
        CheckFall();
    }

    void CheckFall()
    {
        // Don't check while rolling — wait until block has fully landed
        if (playerMovement.isMoving) return;

        bool supported = false;

        foreach (Vector3 checkPoint in GetFootprintPoints())
        {
            if (IsTileBelow(checkPoint))
            {
                supported = true;
                break;
            }
        }

        if (!supported)
        {
            Respawn();
        }
    }

    Vector3[] GetFootprintPoints()
    {
        Vector3 center = transform.position;
        center.y = TILE_TOP + 0.05f;

        switch (playerMovement.orientation)
        {
            case PlayerMovement.Orientation.Standing:
                return new Vector3[] { center };

            case PlayerMovement.Orientation.FlatX:
                return new Vector3[]
                {
                    center + Vector3.right * 0.5f,
                    center + Vector3.left  * 0.5f
                };

            case PlayerMovement.Orientation.FlatZ:
                return new Vector3[]
                {
                    center + Vector3.forward * 0.5f,
                    center + Vector3.back    * 0.5f
                };

            default:
                return new Vector3[] { center };
        }
    }

    bool IsTileBelow(Vector3 point)
    {
        RaycastHit[] hits = Physics.RaycastAll(point, Vector3.down, RAY_LENGTH);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Tile"))
                return true;
        }
        return false;
    }

    void Respawn()
    {
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        playerMovement.orientation = PlayerMovement.Orientation.Standing;
        playerMovement.ResetMovement();
    }
}