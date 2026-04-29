using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Garden Boss (Boss B) — a phase-based tug-of-war on the time scale.
///
/// The boss pushes the shared time scale value toward one edge.
/// The player counters by orienting in the opposite direction.
///   - Opposing the boss → time scale FULLY STOPS
///   - Same direction   → time scale moves FASTER toward death
///   - Frozen stance    → brief pause, then boss resumes pushing
///
/// Progression is driven by tile drop phases (not a timer):
///   - Each phase: safe tiles glow, danger tiles blink then fall
///   - Surviving a phase → boss reverses direction + speeds up
///   - Win = survive all phases
///   - Lose = time scale hits +/-10 OR player falls off
/// </summary>
public class BossBFight : MonoBehaviour
{
    public static BossBFight Instance;

    [Header("Arena Tiles")]
    [Tooltip("Parent transform containing all boss arena tiles.")]
    public Transform bossTilesParent;

    [Header("Phases")]
    [Tooltip("Total tile-drop phases the player must survive to win.")]
    public int totalPhases = 4;

    [Tooltip("Safe tiles that stay up each phase.")]
    public int safeTilesPerPhase = 3;

    [Tooltip("Seconds safe tiles glow before blinking starts.")]
    public float glowDuration = 3.5f;

    [Tooltip("Seconds danger tiles blink before falling.")]
    public float blinkDuration = 2.5f;

    [Tooltip("Stagger delay between each danger tile falling.")]
    public float fallDelay = 0.25f;

    [Tooltip("Seconds between phases (tiles reset, player repositions).")]
    public float phasePauseDuration = 2.5f;

    [Header("Boss Pointer")]
    [Tooltip("Starting movement speed of the boss (units/sec on the time scale).")]
    public float bossStartSpeed = 0.6f;

    [Tooltip("Speed increase each phase.")]
    public float bossSpeedIncrease = 0.25f;

    [Tooltip("Speed multiplier when player goes the same direction as the boss.")]
    public float sameDirectionMultiplier = 1.5f;

    [Header("Frozen Time")]
    [Tooltip("How long frozen stance pauses both pointers before the boss resumes.")]
    public float frozenPauseDuration = 1.5f;

    [Header("Colors")]
    public Color safeColor = new Color(0f, 1f, 0.3f);
    public Color dangerColor = new Color(1f, 0.2f, 0f);
    public Color defaultColor = new Color(1f, 1f, 1f);

    // ── Runtime state ───────────────────────────────────────────────────
    private float _bossSpeed;
    private int _bossDirection = 1;
    private int _currentPhase;

    /// <summary>Independent display value for the boss pointer visual. Moves toward the edge.</summary>
    private float _bossPointerDisplayValue;

    /// <summary>True during inter-phase pauses — boss does NOT push time scale.</summary>
    private bool _phasePaused;

    private bool _isContesting;
    private bool _frozenPauseActive;
    private float _frozenPauseTimer;
    private bool _wasFrozenLastFrame;

    private List<GameObject> _allTiles = new List<GameObject>();
    private List<GameObject> _safeTiles = new List<GameObject>();
    private List<GameObject> _dangerTiles = new List<GameObject>();
    private Dictionary<GameObject, Vector3> _originalPositions = new Dictionary<GameObject, Vector3>();
    private Vector3 _arenaCenter;

    /// <summary>True when the boss fight is active.</summary>
    public bool bossActive { get; private set; }

    /// <summary>Returns all tracked arena tiles for external systems like TilePlayerGlow.</summary>
    public List<GameObject> GetAllTiles() => _allTiles;

    /// <summary>Returns the current list of safe tiles for external queries.</summary>
    public List<GameObject> GetSafeTiles() => _safeTiles;

    /// <summary>True when the player is successfully opposing the boss pointer.</summary>
    public bool IsContesting => _isContesting;

    /// <summary>Current boss pointer direction (+1 toward max, -1 toward min).</summary>
    public int BossDirection => _bossDirection;

    /// <summary>Fired when contesting state changes.</summary>
    public event System.Action<bool> OnContestingChanged;

    // Tutorial glow
    internal static bool _showPointerGlow;

    /// <summary>Shows or hides the tutorial glow ring around the boss pointer.</summary>
    public static void SetPointerGlowVisible(bool visible) => _showPointerGlow = visible;

    private TimeScaleMeter _meter;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _meter = FindObjectOfType<TimeScaleMeter>();
        CacheTiles();
    }

    void Update()
    {
        if (!bossActive) return;

        UpdateFrozenPause();
        UpdateTimeScale();
    }

    // ── Tile caching ───────────────────────────────────────────────────

    /// <summary>Collects all tiles from the bossTilesParent and caches their positions.</summary>
    private void CacheTiles()
    {
        _allTiles.Clear();
        _originalPositions.Clear();

        if (bossTilesParent == null)
        {
            GameObject bt = GameObject.Find("BossTiles");
            if (bt != null) bossTilesParent = bt.transform;
        }

        if (bossTilesParent == null)
        {
            Debug.LogWarning("[BossB] No BossTiles parent found. Tile phases will be skipped.");
            return;
        }

        foreach (Transform child in bossTilesParent)
        {
            _allTiles.Add(child.gameObject);
            _originalPositions[child.gameObject] = child.position;
        }

        _arenaCenter = Vector3.zero;
        foreach (GameObject tile in _allTiles)
            _arenaCenter += tile.transform.position;
        if (_allTiles.Count > 0)
            _arenaCenter /= _allTiles.Count;
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>Starts the boss fight with a full state reset.</summary>
    public void StartBossFight()
    {
        // Always stop first to clear stale coroutines/state
        StopAllCoroutines();
        bossActive = false;

        // Full reset
        _currentPhase = 0;
        _bossSpeed = bossStartSpeed;
        _bossDirection = Random.value > 0.5f ? 1 : -1;
        _bossPointerDisplayValue = 0f;
        _phasePaused = false;
        _frozenPauseActive = false;
        _frozenPauseTimer = 0f;
        _wasFrozenLastFrame = false;
        _isContesting = false;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        // Re-find meter in case HUD was rebuilt
        if (_meter == null)
            _meter = FindObjectOfType<TimeScaleMeter>();

        bossActive = true;

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(0, totalPhases);

        SoundManager.Instance?.PlayBossMusic();

        Debug.Log($"[BossB] Fight started. Direction={_bossDirection}, Speed={_bossSpeed:F2}");
        StartCoroutine(RunPhases());
    }

    /// <summary>Stops the boss fight and fully resets state.</summary>
    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive = false;
        _phasePaused = false;
        _bossSpeed = bossStartSpeed;
        _frozenPauseActive = false;
        _isContesting = false;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        // Hide boss pointer on meter
        if (_meter != null)
            _meter.SetBossPointer(0f, -10f, 10f, false);

        if (HUDController.Instance != null)
            HUDController.Instance.ClearBossObjective();

        ResetArena();
        SoundManager.Instance?.PlayGameMusic();

        Debug.Log("[BossB] Fight stopped and state reset.");
    }

    // ── Phase-based fight loop ──────────────────────────────────────────

    /// <summary>Runs all tile-drop phases. Each phase survived reverses + speeds up the boss.</summary>
    private IEnumerator RunPhases()
    {
        // Short delay before first phase so player can orient
        _phasePaused = true;
        yield return new WaitForSeconds(1.5f);
        _phasePaused = false;

        for (_currentPhase = 0; _currentPhase < totalPhases; _currentPhase++)
        {
            if (!bossActive) yield break;

            bool survived = false;
            yield return StartCoroutine(RunTilePhase(result => survived = result));

            if (!survived)
            {
                // Player fell off — show boss fail UI
                ShowBossFailUI();
                yield break;
            }

            // Phase survived — flash safe tiles as feedback
            yield return StartCoroutine(FlashSafeTiles());
            SoundManager.Instance?.PlayRoundClear();

            if (HUDController.Instance != null)
                HUDController.Instance.SetBossObjective(_currentPhase + 1, totalPhases);

            // ── Inter-phase pause: boss stops pushing, tiles reset ──
            _phasePaused = true;

            // Boss reverses direction + speeds up for the next phase
            _bossDirection *= -1;
            _bossSpeed += bossSpeedIncrease;

            // Snap display value to current shared value so it starts clean
            _bossPointerDisplayValue = TimeScaleLogic.Instance != null
                ? TimeScaleLogic.Instance.CurrentValue
                : 0f;

            Debug.Log($"[BossB] Phase {_currentPhase + 1}/{totalPhases} survived. " +
                      $"Boss now dir={_bossDirection}, speed={_bossSpeed:F2}");

            yield return new WaitForSeconds(phasePauseDuration);
            ResetArena();
            yield return new WaitForSeconds(1f);

            _phasePaused = false;
        }

        // All phases survived — player wins
        WinBossFight();
    }

    /// <summary>Runs a single tile-drop phase: glow safe tiles, blink + drop danger tiles, check survival.</summary>
    private IEnumerator RunTilePhase(System.Action<bool> callback)
    {
        _safeTiles.Clear();
        _dangerTiles.Clear();

        // Randomly pick safe vs danger tiles
        List<GameObject> shuffled = new List<GameObject>(_allTiles);
        Shuffle(shuffled);

        int safeCount = Mathf.Min(safeTilesPerPhase, shuffled.Count);
        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i < safeCount)
                _safeTiles.Add(shuffled[i]);
            else
                _dangerTiles.Add(shuffled[i]);
        }

        // Glow safe tiles
        foreach (GameObject tile in _safeTiles)
            SetTileEmission(tile, safeColor * 4f);

        yield return new WaitForSeconds(glowDuration);

        // Mark all tiles as safe color briefly so danger blinking is clear
        foreach (GameObject tile in _allTiles)
            SetTileEmission(tile, safeColor * 4f);

        // Blink danger tiles
        yield return StartCoroutine(BlinkTiles(_dangerTiles));

        // Drop danger tiles with stagger
        foreach (GameObject tile in _dangerTiles)
        {
            StartCoroutine(DropTile(tile));
            yield return new WaitForSeconds(fallDelay);
        }

        yield return new WaitForSeconds(1f);

        bool survived = PlayerOnSafeTile();
        callback(survived);
    }

    // ── Time scale control ──────────────────────────────────────────────

    /// <summary>Handles the frozen-time pause: both pointers freeze briefly, then boss resumes.</summary>
    private void UpdateFrozenPause()
    {
        if (TimeState.Instance == null) return;

        bool isFrozen = TimeState.Instance.currentState == TimeState.State.Frozen;

        if (isFrozen && !_wasFrozenLastFrame)
        {
            _frozenPauseActive = true;
            _frozenPauseTimer = frozenPauseDuration;
        }

        _wasFrozenLastFrame = isFrozen;

        if (_frozenPauseActive)
        {
            _frozenPauseTimer -= Time.deltaTime;
            if (_frozenPauseTimer <= 0f)
                _frozenPauseActive = false;
        }
    }

    /// <summary>
    /// Drives the shared time scale value based on player vs boss direction.
    /// Opposing = full stop. Same direction = faster. Frozen = temp pause.
    /// Also advances the boss pointer display value independently.
    /// </summary>
    private void UpdateTimeScale()
    {
        if (TimeScaleLogic.Instance == null || TimeState.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        // During frozen pause OR inter-phase pause, everything stops
        if (_frozenPauseActive || _phasePaused)
        {
            PushMeterVisual(minV, maxV);
            return;
        }

        bool playerForward = TimeState.Instance.currentState == TimeState.State.Forward;
        bool playerReverse = TimeState.Instance.currentState == TimeState.State.Reverse;
        bool playerFrozen  = TimeState.Instance.currentState == TimeState.State.Frozen;
        bool bossGoingForward = _bossDirection > 0;

        // Contesting: player opposes boss direction
        bool contesting = (playerReverse && bossGoingForward)
                       || (playerForward && !bossGoingForward);

        bool sameDirection = (playerForward && bossGoingForward)
                          || (playerReverse && !bossGoingForward);

        if (contesting != _isContesting)
        {
            _isContesting = contesting;
            OnContestingChanged?.Invoke(_isContesting);
        }

        // Boss pointer display always moves toward the edge independently
        float displaySpeed = contesting ? _bossSpeed * 0.15f : _bossSpeed;
        _bossPointerDisplayValue += _bossDirection * displaySpeed * Time.deltaTime;
        _bossPointerDisplayValue = Mathf.Clamp(_bossPointerDisplayValue, minV, maxV);

        // Determine effective boss push on the shared time scale
        float bossPush = 0f;

        if (contesting)
        {
            // Player opposing → time scale FULLY STOPS
            bossPush = 0f;
        }
        else if (playerFrozen && !_frozenPauseActive)
        {
            // Frozen pause expired → boss pushes normally
            bossPush = _bossDirection * _bossSpeed;
        }
        else if (sameDirection)
        {
            // Player going same way → accelerated push
            bossPush = _bossDirection * _bossSpeed * sameDirectionMultiplier;
        }
        else
        {
            // Default: boss pushes at normal speed
            bossPush = _bossDirection * _bossSpeed;
        }

        // Apply boss push to the shared time scale value
        if (Mathf.Abs(bossPush) > 0.001f)
        {
            float current = TimeScaleLogic.Instance.CurrentValue;
            float next = current + bossPush * Time.deltaTime;
            next = Mathf.Clamp(next, minV, maxV);
            TimeScaleLogic.Instance.SetValue(next);
        }

        PushMeterVisual(minV, maxV);
    }

    /// <summary>Sends the boss pointer display value to the meter UI every frame.</summary>
    private void PushMeterVisual(float minV, float maxV)
    {
        if (_meter == null)
            _meter = FindObjectOfType<TimeScaleMeter>();

        if (_meter != null)
            _meter.SetBossPointer(_bossPointerDisplayValue, minV, maxV, _isContesting);
    }

    // ── Tile helpers ────────────────────────────────────────────────────

    /// <summary>Sets the emission color on a tile's second material slot (glow material).</summary>
    private void SetTileEmission(GameObject tile, Color color)
    {
        if (tile == null) return;
        Renderer r = tile.GetComponent<Renderer>();
        if (r == null || r.materials.Length < 2) return;

        // Access materials array once — Unity instantiates per-renderer copies
        // on first access, so subsequent calls reuse the same instances
        Material[] mats = r.materials;
        mats[1].SetColor("_EmissionColor", color);
        mats[1].EnableKeyword("_EMISSION");
        r.materials = mats;
    }

    /// <summary>Blinks the given tiles between danger and default colors.</summary>
    private IEnumerator BlinkTiles(List<GameObject> tiles)
    {
        float elapsed = 0f;
        bool toggle = false;

        while (elapsed < blinkDuration)
        {
            foreach (GameObject tile in tiles)
                SetTileEmission(tile, toggle ? dangerColor : defaultColor * 3f);

            toggle = !toggle;
            elapsed += 0.3f;
            yield return new WaitForSeconds(0.3f);
        }

        foreach (GameObject tile in tiles)
            SetTileEmission(tile, dangerColor * 3f);
    }

    /// <summary>Shakes then drops a tile below the arena.</summary>
    private IEnumerator DropTile(GameObject tile)
    {
        if (tile == null) yield break;

        float shakeTime = 0.9f;
        float shakeElapsed = 0f;
        Vector3 originalPos = tile.transform.position;
        float shakeIntensity = 0.05f;
        float shakeFrequency = 9f;

        while (shakeElapsed < shakeTime)
        {
            shakeElapsed += Time.deltaTime;
            float t = shakeElapsed / shakeTime;
            float ramp = t * t;
            float currentIntensity = shakeIntensity * (0.3f + ramp * 0.7f);
            float wave = Mathf.Sin(shakeElapsed * shakeFrequency);
            Vector3 offset = new Vector3(
                wave * currentIntensity,
                Mathf.Sin(shakeElapsed * shakeFrequency * 0.7f) * currentIntensity * 0.4f,
                Mathf.Cos(shakeElapsed * shakeFrequency * 0.9f) * currentIntensity
            );
            tile.transform.position = originalPos + offset;
            yield return null;
        }
        tile.transform.position = originalPos;

        float dropElapsed = 0f;
        float dropTime = 0.5f;
        Vector3 startPos = originalPos;
        Vector3 endPos = startPos + Vector3.down * 10f;

        while (dropElapsed < dropTime)
        {
            dropElapsed += Time.deltaTime;
            tile.transform.position = Vector3.Lerp(startPos, endPos, dropElapsed / dropTime);
            yield return null;
        }

        tile.SetActive(false);
    }

    /// <summary>Flashes safe tiles green after a successful phase.</summary>
    private IEnumerator FlashSafeTiles()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (GameObject tile in _safeTiles)
                SetTileEmission(tile, safeColor * 4f);
            yield return new WaitForSeconds(0.3f);
            foreach (GameObject tile in _safeTiles)
                SetTileEmission(tile, defaultColor * 4f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>Checks if the player is standing on a safe tile.</summary>
    private bool PlayerOnSafeTile()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        RaycastHit[] hits = Physics.RaycastAll(player.transform.position, Vector3.down, 3f);
        foreach (RaycastHit hit in hits)
        {
            foreach (GameObject safeTile in _safeTiles)
            {
                if (safeTile != null && hit.collider.gameObject == safeTile)
                    return true;
            }
        }
        return false;
    }

    /// <summary>Restores all tiles to their original positions and emission.</summary>
    private void ResetArena()
    {
        foreach (GameObject tile in _allTiles)
        {
            if (tile == null) continue;
            tile.SetActive(true);
            if (_originalPositions.ContainsKey(tile))
                tile.transform.position = _originalPositions[tile];
            SetTileEmission(tile, defaultColor * 2f);
        }
    }

    // ── Outcomes ────────────────────────────────────────────────────────

    /// <summary>Shows the boss fail UI with checkpoint/restart/trial-select options.</summary>
    private void ShowBossFailUI()
    {
        StopAllCoroutines();
        bossActive = false;
        _phasePaused = false;

        SoundManager.Instance?.PlayLose();
        Time.timeScale = 0f;

        BossFailUI failUI = FindObjectOfType<BossFailUI>(true);
        if (failUI == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas != null)
            {
                GameObject go = new GameObject("BossFailUI");
                go.transform.SetParent(canvas.transform, false);
                failUI = go.AddComponent<BossFailUI>();
            }
        }

        if (failUI != null)
            failUI.ShowFail();
    }

    /// <summary>Called when the player survives all phases.</summary>
    private void WinBossFight()
    {
        bossActive = false;
        _phasePaused = false;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        SoundManager.Instance?.PlayWin();
        Time.timeScale = 0f;

        float elapsed = Time.realtimeSinceStartup - MainMenuController.GameplayStartRealtime;
        WinScreenController winScreen = FindObjectOfType<WinScreenController>(true);
        if (winScreen != null)
        {
            winScreen.gameObject.SetActive(true);
            winScreen.Show("THE GARDEN", elapsed, 1, false, true, true, true);
        }
    }

    // ── Utility ─────────────────────────────────────────────────────────

    /// <summary>Fisher-Yates shuffle.</summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}
