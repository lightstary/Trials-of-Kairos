using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class FallDetection : MonoBehaviour
{
    const float TILE_TOP = 0.1f;
    const float RAY_LENGTH = 1.5f;

    private Vector3 spawnPosition;
    private Quaternion spawnRotation;
    private PlayerMovement playerMovement;
    private bool isFalling = false;

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
        if (playerMovement.isMoving) return;
        if (isFalling) return;
        if (IsRidingMovingTile()) return;

        int supported = 0;
        Vector3[] points = GetFootprintPoints();

        foreach (Vector3 checkPoint in points)
        {
            if (IsTileBelow(checkPoint))
                supported++;
        }

        if (supported == 0)
            Respawn();
    }

    bool IsRidingMovingTile()
    {
        foreach (MovingTile tile in FindObjectsOfType<MovingTile>())
        {
            if (tile.IsPlayerOnTile()) return true;
        }
        return false;
    }

    Vector3[] GetFootprintPoints()
    {
        Vector3 center = transform.position;
        center.y = TILE_TOP + 0.05f;

        switch (playerMovement.orientation)
        {
            case PlayerMovement.Orientation.Standing:
            case PlayerMovement.Orientation.UpsideDown:
                return new Vector3[]
                {
                    center + new Vector3( 0.4f, 0,  0.4f),
                    center + new Vector3(-0.4f, 0,  0.4f),
                    center + new Vector3( 0.4f, 0, -0.4f),
                    center + new Vector3(-0.4f, 0, -0.4f)
                };

            case PlayerMovement.Orientation.FlatX:
            case PlayerMovement.Orientation.FlatX_R:
                return new Vector3[]
                {
                    center + new Vector3( 0.9f, 0,  0.4f),
                    center + new Vector3(-0.9f, 0,  0.4f),
                    center + new Vector3( 0.9f, 0, -0.4f),
                    center + new Vector3(-0.9f, 0, -0.4f)
                };

            case PlayerMovement.Orientation.FlatZ:
            case PlayerMovement.Orientation.FlatZ_R:
                return new Vector3[]
                {
                    center + new Vector3( 0.4f, 0,  0.9f),
                    center + new Vector3(-0.4f, 0,  0.9f),
                    center + new Vector3( 0.4f, 0, -0.9f),
                    center + new Vector3(-0.4f, 0, -0.9f)
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

    public void Respawn()
    {
        if (isFalling) return;
        StartCoroutine(FallThenRespawn());
    }

    IEnumerator FallThenRespawn()
    {
        isFalling = true;

        BossFight bossFight = FindObjectOfType<BossFight>();
        if (bossFight != null)
            bossFight.StopBossFight();

        BossBFight bossBFight = FindObjectOfType<BossBFight>();
        if (bossBFight != null)
            bossBFight.StopBossFight();

        BossCFight bossCFight = FindObjectOfType<BossCFight>();
        if (bossCFight != null)
            bossCFight.StopBossFight();

        playerMovement.ResetMovement();
        playerMovement.enabled = false;

        SoundManager.Instance?.PlayFall();

        float elapsed = 0f;
        float fallTime = 0.8f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 10f;

        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / fallTime);
            yield return null;
        }

        playerMovement.enabled = true;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        playerMovement.orientation = PlayerMovement.Orientation.Standing;
        playerMovement.ResetMovement();

        SoundManager.Instance?.PlayRespawn();

        isFalling = false;
    }

    public void UpdateSpawnPoint(Vector3 newPosition, Quaternion newRotation)
    {
        spawnPosition = new Vector3(
            Mathf.Round(newPosition.x),
            newPosition.y + 1.1f,
            Mathf.Round(newPosition.z)
        );
        spawnRotation = Quaternion.identity;
        Debug.Log("Spawn point updated to: " + spawnPosition);
    }
}