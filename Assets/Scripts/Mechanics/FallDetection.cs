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
    private float respawnGraceTimer = 0f;

    /// <summary>Number of seconds after respawn where fall checks are skipped.</summary>
    private const float RESPAWN_GRACE_PERIOD = 0.3f;

    void Start()
    {
        spawnPosition = transform.position;
        spawnRotation = transform.rotation;
        hasCheckpoint = false;
        playerMovement = GetComponent<PlayerMovement>();
        _cachedOriginalScale = transform.localScale;
        CacheChildTransforms();
    }

    void Update()
    {
        if (respawnGraceTimer > 0f)
        {
            respawnGraceTimer -= Time.unscaledDeltaTime;
            return;
        }
        CheckFall();
    }

    /// <summary>Gentle drift speed toward nearest tile center.</summary>
    private const float CENTER_DRIFT_SPEED = 2f;

    void LateUpdate()
    {
        if (isFalling || playerMovement.isMoving) return;

        // Raycast down from player center to find the tile directly below
        Vector3 rayOrigin = transform.position;
        rayOrigin.y = TILE_TOP + 0.5f;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, RAY_LENGTH))
            return;

        if (!hit.collider.CompareTag("Tile")) return;

        Vector3 tileCenter = hit.collider.transform.position;
        Vector3 playerPos = transform.position;
        float t = CENTER_DRIFT_SPEED * Time.deltaTime;

        var ori = playerMovement.orientation;

        switch (ori)
        {
            case PlayerMovement.Orientation.FlatX:
            case PlayerMovement.Orientation.FlatX_R:
                // Spanning along X — only drift Z
                playerPos.z = Mathf.Lerp(playerPos.z, tileCenter.z, t);
                break;
            case PlayerMovement.Orientation.FlatZ:
            case PlayerMovement.Orientation.FlatZ_R:
                // Spanning along Z — only drift X
                playerPos.x = Mathf.Lerp(playerPos.x, tileCenter.x, t);
                break;
            default:
                // Standing / UpsideDown — drift both axes
                playerPos.x = Mathf.Lerp(playerPos.x, tileCenter.x, t);
                playerPos.z = Mathf.Lerp(playerPos.z, tileCenter.z, t);
                break;
        }

        transform.position = playerPos;
    }

    void CheckFall()
    {
        if (playerMovement.isMoving) return;
        if (isFalling) return;

        // Check if standing on a death tile (instant kill)
        if (IsOnDeathTile())
        {
            StartCoroutine(DeathTileKill());
            return;
        }

        // Skip all fall/topple checks while riding a moving tile to avoid
        // false positives from gaps between continuously moving tiles
        if (IsRidingMovingTile()) return;

        var ori = playerMovement.orientation;
        bool isFlat = ori == PlayerMovement.Orientation.FlatX
                   || ori == PlayerMovement.Orientation.FlatX_R
                   || ori == PlayerMovement.Orientation.FlatZ
                   || ori == PlayerMovement.Orientation.FlatZ_R;

        if (isFlat)
        {
            // When flat, the hourglass spans two tiles.
            // If one half is unsupported, topple over that edge.
            GetFlatHalfPoints(out Vector3[] sideA, out Vector3[] sideB, out Vector3 dirA, out Vector3 dirB);
            bool sideASupported = HasAnySupport(sideA);
            bool sideBSupported = HasAnySupport(sideB);

            if (!sideASupported && !sideBSupported)
            {
                StartFallFlow();
            }
            else if (!sideASupported)
            {
                StartCoroutine(ToppleAndFall(dirA));
            }
            else if (!sideBSupported)
            {
                StartCoroutine(ToppleAndFall(dirB));
            }
        }
        else
        {
            Vector3[] points = GetFootprintPoints();
            if (!HasAnySupport(points))
            {
                StartFallFlow();
            }
        }
    }

    /// <summary>Starts the appropriate fall flow (boss lose or fall modal).</summary>
    private void StartFallFlow()
    {
        if (IsAnyBossActive())
            StartCoroutine(BossFallLose());
        else
            StartCoroutine(FallWithModal());
    }

    /// <summary>Returns true if at least one point in the array has a tile below it.</summary>
    private bool HasAnySupport(Vector3[] points)
    {
        foreach (Vector3 p in points)
        {
            if (IsTileBelow(p)) return true;
        }
        return false;
    }

    /// <summary>
    /// Topples the hourglass over the unsupported edge, then triggers the fall flow.
    /// Pivots around the center-bottom edge of the block (the boundary between the two halves).
    /// </summary>
    private IEnumerator ToppleAndFall(Vector3 toppleDir)
    {
        if (isFalling) yield break;
        isFalling = true;

        playerMovement.ResetMovement();
        playerMovement.enabled = false;

        SoundManager.Instance?.PlayFall();

        // Pivot is at the center-bottom of the hourglass (the boundary between the two tile halves)
        Vector3 pivot = transform.position;
        pivot.y = TILE_TOP;

        // Rotation axis: perpendicular to topple direction on the horizontal plane
        Vector3 rotAxis = new Vector3(toppleDir.z, 0f, -toppleDir.x);

        // Brief wobble before toppling
        float wobbleDuration = 0.45f;
        float wobbleElapsed = 0f;
        float wobbleFreq = 20f;
        float wobbleAmp = 4f;
        Quaternion baseRot = transform.rotation;
        Vector3 wobbleAxis = rotAxis;

        while (wobbleElapsed < wobbleDuration)
        {
            wobbleElapsed += Time.deltaTime;
            float wt = wobbleElapsed / wobbleDuration;
            // Ramp up amplitude then cut off
            float envelope = Mathf.Sin(wt * Mathf.PI) * wobbleAmp;
            float angle = Mathf.Sin(wobbleElapsed * wobbleFreq) * envelope;
            transform.rotation = baseRot * Quaternion.AngleAxis(angle, transform.InverseTransformDirection(wobbleAxis));
            yield return null;
        }
        transform.rotation = baseRot;

        // Topple 90 degrees over the edge
        float toppleSpeed = 5f;
        float totalAngle = 0f;
        while (totalAngle < 90f)
        {
            float step = toppleSpeed * Time.deltaTime * 90f;
            if (totalAngle + step > 90f) step = 90f - totalAngle;
            transform.RotateAround(pivot, rotAxis, step);
            totalAngle += step;
            yield return null;
        }

        // Fall straight down through the ground
        float elapsed = 0f;
        float fallTime = 0.6f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.down * 10f;

        while (elapsed < fallTime)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / fallTime);
            yield return null;
        }

        Time.timeScale = 0f;

        if (IsAnyBossActive())
        {
            if (BossFight.Instance != null) BossFight.Instance.StopBossFight();
            if (BossBFight.Instance != null) BossBFight.Instance.StopBossFight();
            if (BossCFight.Instance != null) BossCFight.Instance.StopBossFight();

            SoundManager.Instance?.PlayLose();

            GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
            if (gosc != null)
            {
                gosc.Show();
            }
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }
        else
        {
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

        // isFalling stays true — DoCheckpointRespawn or scene reload clears it
    }

    /// <summary>
    /// Splits the flat hourglass footprint into two halves with their outward directions.
    /// </summary>
    private void GetFlatHalfPoints(out Vector3[] sideA, out Vector3[] sideB,
                                    out Vector3 dirA, out Vector3 dirB)
    {
        Vector3 center = transform.position;
        center.y = TILE_TOP + 0.05f;

        var ori = playerMovement.orientation;
        bool alongX = ori == PlayerMovement.Orientation.FlatX
                   || ori == PlayerMovement.Orientation.FlatX_R;

        if (alongX)
        {
            // Spans 2 tiles along X: +X side and -X side
            dirA = Vector3.right;
            dirB = Vector3.left;
            sideA = new Vector3[]
            {
                center + new Vector3(0.9f, 0,  0.4f),
                center + new Vector3(0.9f, 0, -0.4f)
            };
            sideB = new Vector3[]
            {
                center + new Vector3(-0.9f, 0,  0.4f),
                center + new Vector3(-0.9f, 0, -0.4f)
            };
        }
        else
        {
            // Spans 2 tiles along Z: +Z side and -Z side
            dirA = Vector3.forward;
            dirB = Vector3.back;
            sideA = new Vector3[]
            {
                center + new Vector3( 0.4f, 0, 0.9f),
                center + new Vector3(-0.4f, 0, 0.9f)
            };
            sideB = new Vector3[]
            {
                center + new Vector3( 0.4f, 0, -0.9f),
                center + new Vector3(-0.4f, 0, -0.9f)
            };
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

    // ════════════════════════════════════════════════════════════════════
    //  DEATH MESSAGES
    // ════════════════════════════════════════════════════════════════════

    private static readonly string[] VINE_TITLES = {
        "ENSNARED",
        "CONSUMED",
        "ENTANGLED",
        "DEVOURED",
        "CLAIMED"
    };

    private static readonly string[] VINE_MESSAGES = {
        "The temporal vines wrapped around you.",
        "Time's roots pulled you under.",
        "The garden has reclaimed what was borrowed.",
        "Entangled in moments that should not exist.",
        "The vines do not forgive trespassers.",
        "Consumed by the grip of living time."
    };

    // ════════════════════════════════════════════════════════════════════
    //  DISINTEGRATION EFFECT
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Duration before the player model is hidden after particles spawn.</summary>
    private const float DISINTEGRATE_HIDE_DELAY = 0.15f;

    /// <summary>Hold time after disintegration before showing death UI, letting particles breathe.</summary>
    private const float POST_DISINTEGRATE_HOLD = 0.6f;

    /// <summary>Total duration of the shrink-to-nothing dissolve.</summary>
    private const float DISINTEGRATE_SHRINK_DUR = 0.55f;

    /// <summary>
    /// Runs the sand disintegration visual: spawns particles, then gradually
    /// shrinks and fades the player model so it dissolves into the particle cloud
    /// rather than just vanishing.
    /// </summary>
    private IEnumerator PlayDisintegration()
    {
        Vector3 effectSize = transform.lossyScale;
        SandDisintegrationEffect.Spawn(transform.position, effectSize);

        // Cache the original local scale
        Vector3 originalScale = transform.localScale;

        // Brief overlap: particles play alongside the full model
        yield return new WaitForSecondsRealtime(DISINTEGRATE_HIDE_DELAY);

        // Gradually shrink the model into the particle cloud
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        float elapsed = 0f;

        while (elapsed < DISINTEGRATE_SHRINK_DUR)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / DISINTEGRATE_SHRINK_DUR);
            // Ease-in: starts slow, accelerates — feels like it's being pulled apart
            float eased = t * t;
            float scale = Mathf.Lerp(1f, 0f, eased);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        // Fully hidden
        transform.localScale = Vector3.zero;
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = false;
        }
    }

    private Vector3 _cachedOriginalScale = Vector3.one;
    private Vector3[] _cachedChildLocalPositions;
    private Quaternion[] _cachedChildLocalRotations;
    private Transform[] _cachedChildren;

    /// <summary>Caches local transforms of direct children so root motion drift can be undone on respawn.</summary>
    private void CacheChildTransforms()
    {
        int count = transform.childCount;
        _cachedChildren = new Transform[count];
        _cachedChildLocalPositions = new Vector3[count];
        _cachedChildLocalRotations = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            Transform child = transform.GetChild(i);
            _cachedChildren[i] = child;
            _cachedChildLocalPositions[i] = child.localPosition;
            _cachedChildLocalRotations[i] = child.localRotation;
        }
    }

    /// <summary>Restores all player renderers, scale, child transforms, and resets the Animator to its default pose.</summary>
    private void RestorePlayerVisuals()
    {
        transform.localScale = _cachedOriginalScale;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = true;
        }

        // Reset the character Animator so bones return to the idle pose
        Animator anim = GetComponentInChildren<Animator>(true);
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // Restore child local transforms to undo any root motion drift
        if (_cachedChildren != null)
        {
            for (int i = 0; i < _cachedChildren.Length; i++)
            {
                if (_cachedChildren[i] != null)
                {
                    _cachedChildren[i].localPosition = _cachedChildLocalPositions[i];
                    _cachedChildren[i].localRotation = _cachedChildLocalRotations[i];
                }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    //  PUBLIC API — for external death triggers (TimeScaleLogic)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called by TimeScaleLogic when the player hits +/-10.
    /// Disintegrates the player, then shows the GameOverScreenController.
    /// </summary>
    public void TriggerTimelineDeath(string subtitle)
    {
        if (isFalling) return;
        StartCoroutine(TimelineDeathRoutine(subtitle));
    }

    private IEnumerator TimelineDeathRoutine(string subtitle)
    {
        isFalling = true;

        playerMovement.ResetMovement();
        playerMovement.enabled = false;

        SoundManager.Instance?.PlayFall();

        yield return StartCoroutine(PlayDisintegration());

        // Brief hold — particles keep drifting into the death screen
        yield return new WaitForSecondsRealtime(POST_DISINTEGRATE_HOLD);

        Time.timeScale = 0f;

        SoundManager.Instance?.PlayLose();

        GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
        if (gosc != null)
        {
            gosc.Show(subtitle);
        }
        else
        {
            Debug.LogWarning("[FallDetection] GameOverScreenController not found. Reloading scene.");
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // isFalling stays true — scene reload clears it
    }

    // ════════════════════════════════════════════════════════════════════
    //  DEATH TILE KILL (vines)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Kills the player on a vine/death tile with disintegration and a vine-themed modal.</summary>
    private IEnumerator DeathTileKill()
    {
        if (isFalling) yield break;
        isFalling = true;

        playerMovement.ResetMovement();
        playerMovement.enabled = false;

        SoundManager.Instance?.PlayFall();

        yield return StartCoroutine(PlayDisintegration());

        // Let particles drift before the death modal
        yield return new WaitForSecondsRealtime(POST_DISINTEGRATE_HOLD);

        Time.timeScale = 0f;

        string vineTitle = VINE_TITLES[Random.Range(0, VINE_TITLES.Length)];
        string vineMsg = VINE_MESSAGES[Random.Range(0, VINE_MESSAGES.Length)];

        FallModal.Show(
            hasCheckpoint: hasCheckpoint,
            onCheckpoint: () =>
            {
                SandDisintegrationEffect.DestroyAll();
                RestorePlayerVisuals();
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
            },
            title: vineTitle,
            message: vineMsg
        );

        // isFalling stays true — DoCheckpointRespawn or scene reload clears it
    }

    // ════════════════════════════════════════════════════════════════════
    //  NORMAL FALL (off edge, no boss)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Non-boss fall: falls through the ground, then shows the fall modal.
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

    // ════════════════════════════════════════════════════════════════════
    //  BOSS FALL (off edge during boss fight)
    // ════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Boss fight fall: falls through the ground, stops boss, shows game over screen.
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

        // isFalling stays true — scene reload clears it
    }

    // ════════════════════════════════════════════════════════════════════
    //  SILENT RESPAWN (boss round failure)
    // ════════════════════════════════════════════════════════════════════

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

    // ════════════════════════════════════════════════════════════════════
    //  CHECKPOINT / SPAWN
    // ════════════════════════════════════════════════════════════════════

    /// <summary>Teleports the player back to the last checkpoint.</summary>
    private void DoCheckpointRespawn()
    {
        // Stop any lingering fall/topple coroutines on this component
        StopAllCoroutines();
        isFalling = false;

        // Clean up any leftover disintegration effects and restore visuals
        SandDisintegrationEffect.DestroyAll();
        RestorePlayerVisuals();

        playerMovement.enabled = true;
        transform.position = spawnPosition;
        transform.rotation = spawnRotation;
        playerMovement.orientation = PlayerMovement.Orientation.Standing;
        playerMovement.ResetMovement();

        // Force physics to recognize the new position before raycasts fire
        Physics.SyncTransforms();

        // Grace period so CheckFall doesn't trigger while physics settles
        respawnGraceTimer = RESPAWN_GRACE_PERIOD;

        SoundManager.Instance?.PlayRespawn();
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
