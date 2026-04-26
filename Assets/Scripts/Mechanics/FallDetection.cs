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
    private bool hasCheckpoint = false;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        hasCheckpoint = false;
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

        // Check if standing on a death tile (instant kill → Game Over)
        if (IsOnDeathTile())
        {
            StartCoroutine(DeathTileKill());
            return;
        }

        int supported = 0;
        Vector3[] points = GetFootprintPoints();

        foreach (Vector3 checkPoint in points)
        {
            if (IsTileBelow(checkPoint))
                supported++;
        }

        if (supported == 0)
        {
            if (IsAnyBossActive())
            {
                // During a boss fight, falling = losing
                StartCoroutine(BossFallLose());
            }
            else
            {
                // Outside boss fight: show the fall modal
                StartCoroutine(FallWithModal());
            }
        }
    }

    bool IsRidingMovingTile()
    {
        foreach (MovingTile tile in FindObjectsOfType<MovingTile>())
        {
            if (tile.IsPlayerOnTile()) return true;
        }
        return false;
    }

    /// <summary>Checks whether any boss fight is currently running.</summary>
    private bool IsAnyBossActive()
    {
        if (BossFight.Instance  != null && BossFight.Instance.bossActive)  return true;
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive) return true;
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive) return true;
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

    /// <summary>
    /// Checks if the player is currently standing on a tile with a <see cref="DeathTile"/> component.
    /// </summary>
    private bool IsOnDeathTile()
    {
        Vector3 origin = transform.position;
        origin.y = TILE_TOP + 0.15f;

        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, RAY_LENGTH);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.GetComponent<DeathTile>() != null)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Kills the player when stepping on a death tile.
    /// Shows the Game Over screen (full restart required).
    /// </summary>
    private IEnumerator DeathTileKill()
    {
        if (isFalling) yield break;
        isFalling = true;

        playerMovement.ResetMovement();
        playerMovement.enabled = false;

        SoundManager.Instance?.PlayFall();
        SoundManager.Instance?.PlayLose();

        float elapsed = 0f;
        float fallTime = 0.6f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 8f;

        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / fallTime);
            yield return null;
        }

        Time.timeScale = 0f;

        GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
        if (gosc != null)
        {
            gosc.Show("CONSUMED BY THE VINES");
        }
        else
        {
            Debug.LogWarning("[FallDetection] GameOverScreenController not found. Reloading scene.");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        isFalling = false;
    }

    /// <summary>
    /// Non-boss fall: plays fall animation, then shows the fall modal.
    /// If no checkpoint has been reached, only "Restart Level" is shown.
    /// </summary>
    private IEnumerator FallWithModal()
    {
        if (isFalling) yield break;
        isFalling = true;

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

        // Pause and show modal
        Time.timeScale = 0f;
        FallModal.Show(
            hasCheckpoint: hasCheckpoint,
            onCheckpoint: () =>
            {
                Time.timeScale = 1f;
                DoCheckpointRespawn();

                if (ScreenTransitionManager.Instance != null)
                    ScreenTransitionManager.Instance.CosmicFadeIn(0.5f);
            },
            onRestartLevel: () =>
            {
                MainMenuController.RequestRestartTrialOnLoad();
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        );
    }

    /// <summary>
    /// Boss fight fall: plays fall animation, stops boss, shows game over screen.
    /// </summary>
    private IEnumerator BossFallLose()
    {
        if (isFalling) yield break;
        isFalling = true;

        // Stop all boss fights
        if (BossFight.Instance  != null) BossFight.Instance.StopBossFight();
        if (BossBFight.Instance != null) BossBFight.Instance.StopBossFight();
        if (BossCFight.Instance != null) BossCFight.Instance.StopBossFight();

        playerMovement.ResetMovement();
        playerMovement.enabled = false;

        SoundManager.Instance?.PlayFall();
        SoundManager.Instance?.PlayLose();

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

        Time.timeScale = 0f;

        GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
        if (gosc != null)
        {
            gosc.Show();
        }
        else
        {
            Debug.LogWarning("[FallDetection] GameOverScreenController not found. Reloading scene.");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        isFalling = false;
    }

    /// <summary>Instantly respawns at checkpoint without modal (used during boss fights).</summary>
    public void RespawnSilent()
    {
        if (isFalling) return;
        StartCoroutine(FallThenRespawn());
    }

    /// <summary>Public respawn for external callers (boss round failure, etc.).</summary>
    public void Respawn()
    {
        if (isFalling) return;
        StartCoroutine(FallThenRespawn());
    }

    private IEnumerator FallThenRespawn()
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

        // Fall animation + simultaneous cosmic fade
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.CosmicFadeOut(0.7f);

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

        // Brief hold at cosmic
        yield return new WaitForSeconds(0.15f);

        DoCheckpointRespawn();

        // Fade back in from cosmic
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.CosmicFadeIn(0.5f);

        isFalling = false;
    }

    /// <summary>Teleports the player back to the last checkpoint.</summary>
    private void DoCheckpointRespawn()
    {
        playerMovement.enabled = true;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        playerMovement.orientation = PlayerMovement.Orientation.Standing;
        playerMovement.ResetMovement();

        SoundManager.Instance?.PlayRespawn();
        isFalling = false;
    }

    /// <summary>Updates the checkpoint spawn position.</summary>
    public void UpdateSpawnPoint(Vector3 newPosition, Quaternion newRotation)
    {
        spawnPosition = new Vector3(
            Mathf.Round(newPosition.x),
            newPosition.y + 1.1f,
            Mathf.Round(newPosition.z)
        );
        spawnRotation = Quaternion.identity;
        hasCheckpoint = true;
        Debug.Log("Spawn point updated to: " + spawnPosition);
    }
}
