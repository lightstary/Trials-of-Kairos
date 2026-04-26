using UnityEngine;
using System.Collections;

/// <summary>
/// Garden Boss (Boss B) — a pointer-vs-pointer duel on the time scale meter.
///
/// The boss has a pointer that moves toward one end of the meter each round.
/// The player must set their time state to the OPPOSITE direction to contest
/// and push back. When both pointers overlap, the boss pointer stops.
///
/// Frozen time pauses both pointers briefly before the boss resumes
/// (the boss can ignore time constraints).
///
/// The fight is designed to last 20–30 seconds across several short rounds.
/// Lose condition: boss pointer reaches either edge of the meter.
/// Win condition: survive the full duration.
/// </summary>
public class BossBFight : MonoBehaviour
{
    public static BossBFight Instance;

    [Header("Survival Settings")]
    [Tooltip("Total time (seconds) the player must survive to win.")]
    public float survivalTime = 25f;

    [Header("Round Settings")]
    [Tooltip("Seconds between boss direction changes.")]
    public float roundDuration = 4f;

    [Tooltip("Minimum round duration as the fight progresses.")]
    public float minRoundDuration = 2f;

    [Tooltip("Seconds removed from round duration each round.")]
    public float roundDurationShrink = 0.3f;

    [Header("Boss Pointer Settings")]
    [Tooltip("Starting movement speed of the boss pointer (units/sec).")]
    public float bossStartSpeed = 1.8f;

    [Tooltip("Speed increase each round.")]
    public float bossSpeedIncrease = 0.35f;

    [Tooltip("Speed multiplier when the player moves in the same direction as the boss.")]
    public float sameDirectionMultiplier = 1.5f;

    [Tooltip("How much the boss pointer is slowed when contested (0 = full stop, 0.5 = half speed).")]
    public float contestSlowFactor = 0f;

    [Header("Frozen Time")]
    [Tooltip("How long frozen time pauses both pointers before the boss resumes.")]
    public float frozenPauseDuration = 1.5f;

    // ── Runtime state ───────────────────────────────────────────────────
    private float _bossPointerValue;
    private float _bossSpeed;
    private int _bossDirection = 1; // +1 = toward max, -1 = toward min
    private int _roundIndex;
    private float _survivalTimer;
    private float _currentRoundDuration;

    private bool _isContesting;
    private bool _frozenPauseActive;
    private float _frozenPauseTimer;
    private bool _wasFrozenLastFrame;

    public bool bossActive { get; private set; }

    /// <summary>True when the player is successfully opposing the boss pointer.</summary>
    public bool IsContesting => _isContesting;

    /// <summary>Current boss pointer direction (+1 or -1).</summary>
    public int BossDirection => _bossDirection;

    /// <summary>Event fired when contesting state changes.</summary>
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
    }

    void Update()
    {
        if (!bossActive) return;

        _survivalTimer += Time.deltaTime;

        // Check win
        if (_survivalTimer >= survivalTime)
        {
            WinBossFight();
            return;
        }

        UpdateFrozenPause();
        UpdateBossPointer();
        CheckBossPointerKill();
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>Starts the boss fight.</summary>
    public void StartBossFight()
    {
        if (bossActive) return;
        bossActive = true;
        _survivalTimer = 0f;
        _roundIndex = 0;
        _bossPointerValue = 0f;
        _bossSpeed = bossStartSpeed;
        _bossDirection = Random.value > 0.5f ? 1 : -1;
        _currentRoundDuration = roundDuration;
        _frozenPauseActive = false;
        _frozenPauseTimer = 0f;
        _wasFrozenLastFrame = false;
        _isContesting = false;

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(0, 1);

        SoundManager.Instance?.PlayBossMusic();

        StartCoroutine(RunRounds());
    }

    /// <summary>Stops the boss fight and resets state.</summary>
    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive = false;
        _survivalTimer = 0f;
        _bossPointerValue = 0f;
        _bossSpeed = bossStartSpeed;
        _frozenPauseActive = false;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        if (HUDController.Instance != null)
            HUDController.Instance.ClearBossObjective();

        SoundManager.Instance?.PlayGameMusic();
    }

    // ── Core loop ───────────────────────────────────────────────────────

    /// <summary>Coroutine that changes boss direction every round.</summary>
    private IEnumerator RunRounds()
    {
        while (bossActive)
        {
            yield return new WaitForSeconds(_currentRoundDuration);

            if (!bossActive) yield break;

            // New round: flip direction, increase speed, shorten round
            _roundIndex++;
            _bossDirection *= -1;
            _bossSpeed += bossSpeedIncrease;
            _currentRoundDuration = Mathf.Max(minRoundDuration, _currentRoundDuration - roundDurationShrink);

            Debug.Log($"[BossB] Round {_roundIndex}: boss now pushing {(_bossDirection > 0 ? "FORWARD" : "REVERSE")}, speed={_bossSpeed:F1}");
        }
    }

    /// <summary>Handles the frozen-time pause mechanic.</summary>
    private void UpdateFrozenPause()
    {
        if (TimeState.Instance == null) return;

        bool isFrozen = TimeState.Instance.currentState == TimeState.State.Frozen;

        // Detect transition INTO frozen state → start pause
        if (isFrozen && !_wasFrozenLastFrame)
        {
            _frozenPauseActive = true;
            _frozenPauseTimer = frozenPauseDuration;
        }

        _wasFrozenLastFrame = isFrozen;

        // Tick down the frozen pause
        if (_frozenPauseActive)
        {
            _frozenPauseTimer -= Time.deltaTime;
            if (_frozenPauseTimer <= 0f)
                _frozenPauseActive = false;
        }
    }

    /// <summary>Moves the boss pointer based on player's time state.</summary>
    private void UpdateBossPointer()
    {
        if (TimeScaleLogic.Instance == null) return;
        if (TimeState.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        // During frozen pause, both pointers are frozen — no movement
        if (_frozenPauseActive)
        {
            UpdateMeter(minV, maxV);
            return;
        }

        bool playerForward = TimeState.Instance.currentState == TimeState.State.Forward;
        bool playerReverse = TimeState.Instance.currentState == TimeState.State.Reverse;
        bool playerFrozen  = TimeState.Instance.currentState == TimeState.State.Frozen;
        bool bossGoingForward = _bossDirection > 0;

        // Determine contestation: player opposes boss direction
        bool contesting = (playerReverse && bossGoingForward) ||
                          (playerForward && !bossGoingForward);

        bool sameDirection = (playerForward && bossGoingForward) ||
                             (playerReverse && !bossGoingForward);

        // Fire event on contesting state change
        if (contesting != _isContesting)
        {
            _isContesting = contesting;
            OnContestingChanged?.Invoke(_isContesting);
        }

        // Move boss pointer
        if (contesting)
        {
            // Player is opposing — boss is slowed or stopped
            float slowedSpeed = _bossSpeed * contestSlowFactor;
            _bossPointerValue += _bossDirection * slowedSpeed * Time.deltaTime;
        }
        else if (playerFrozen && !_frozenPauseActive)
        {
            // Frozen but pause expired — boss ignores frozen and moves normally
            _bossPointerValue += _bossDirection * _bossSpeed * Time.deltaTime;
        }
        else if (sameDirection)
        {
            // Player going same way as boss — boss moves faster
            _bossPointerValue += _bossDirection * _bossSpeed * sameDirectionMultiplier * Time.deltaTime;
        }
        else
        {
            // Normal movement
            _bossPointerValue += _bossDirection * _bossSpeed * Time.deltaTime;
        }

        _bossPointerValue = Mathf.Clamp(_bossPointerValue, minV, maxV);

        UpdateMeter(minV, maxV);
    }

    /// <summary>Updates the meter display.</summary>
    private void UpdateMeter(float minV, float maxV)
    {
        if (_meter != null)
            _meter.SetBossPointer(_bossPointerValue, minV, maxV, _isContesting);
    }

    /// <summary>Checks if the boss pointer reached the edges.</summary>
    private void CheckBossPointerKill()
    {
        if (TimeScaleLogic.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        if (_bossPointerValue >= maxV || _bossPointerValue <= minV)
            LoseBossFight();
    }

    // ── Outcomes ────────────────────────────────────────────────────────

    private void LoseBossFight()
    {
        StopBossFight();
        SoundManager.Instance?.PlayLose();

        GameOverScreenController gosc = FindObjectOfType<GameOverScreenController>(true);
        if (gosc != null)
        {
            Time.timeScale = 0f;
            gosc.Show("BEATEN BY TIME");
        }
        else
        {
            FindObjectOfType<FallDetection>()?.Respawn();
        }
    }

    private void WinBossFight()
    {
        bossActive = false;

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
}
