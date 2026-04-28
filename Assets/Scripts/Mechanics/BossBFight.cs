using UnityEngine;
using System.Collections;

/// <summary>
/// Garden Boss (Boss B) — a tug-of-war duel on the time scale meter.
///
/// Both the player and the boss have separate pointers on the meter.
/// Each round, the boss pointer pushes toward one end of the scale.
/// The player contests by setting their time state to the OPPOSITE direction,
/// which stops the boss pointer. Going the SAME direction accelerates the boss.
///
/// During frozen time, both pointers pause briefly, then the boss resumes
/// (the boss can ignore time constraints, forcing the player to reposition).
///
/// The player's own timescale still accrues normally — hitting +/-10 kills them
/// just like in normal gameplay. This creates a dual threat: manage your own
/// meter position while fighting the boss pointer.
///
/// Fight length: 20–30 seconds across short rounds.
/// Lose: boss pointer reaches either edge of the meter.
/// Win: survive the full duration.
/// </summary>
public class BossBFight : MonoBehaviour
{
    public static BossBFight Instance;

    [Header("Survival")]
    [Tooltip("Total seconds the player must survive to win.")]
    public float survivalTime = 25f;

    [Header("Rounds")]
    [Tooltip("Starting seconds between boss direction changes.")]
    public float roundDuration = 4f;

    [Tooltip("Minimum round duration as the fight ramps up.")]
    public float minRoundDuration = 2f;

    [Tooltip("Seconds removed from round duration each round.")]
    public float roundDurationShrink = 0.3f;

    [Header("Boss Pointer")]
    [Tooltip("Starting movement speed of the boss pointer (units/sec).")]
    public float bossStartSpeed = 1.8f;

    [Tooltip("Speed increase each round.")]
    public float bossSpeedIncrease = 0.35f;

    [Tooltip("Speed multiplier when the player goes the same direction as the boss.")]
    public float sameDirectionMultiplier = 1.5f;

    [Tooltip("How much the boss pointer is slowed when contested (0 = full stop).")]
    public float contestSlowFactor = 0f;

    [Header("Frozen Time")]
    [Tooltip("How long frozen time pauses both pointers before the boss resumes.")]
    public float frozenPauseDuration = 1.5f;

    // ── Runtime state ───────────────────────────────────────────────────
    private float _bossPointerValue;
    private float _bossSpeed;
    private int _bossDirection = 1;
    private int _roundIndex;
    private float _survivalTimer;
    private float _currentRoundDuration;

    private bool _isContesting;
    private bool _frozenPauseActive;
    private float _frozenPauseTimer;
    private bool _wasFrozenLastFrame;

    /// <summary>True when the boss fight is active.</summary>
    public bool bossActive { get; private set; }

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
    }

    void Update()
    {
        if (!bossActive) return;

        _survivalTimer += Time.deltaTime;

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

    /// <summary>Flips boss direction each round, increases speed, shortens rounds.</summary>
    private IEnumerator RunRounds()
    {
        while (bossActive)
        {
            yield return new WaitForSeconds(_currentRoundDuration);
            if (!bossActive) yield break;

            _roundIndex++;
            _bossDirection *= -1;
            _bossSpeed += bossSpeedIncrease;
            _currentRoundDuration = Mathf.Max(minRoundDuration, _currentRoundDuration - roundDurationShrink);

            Debug.Log($"[BossB] Round {_roundIndex}: boss pushing {(_bossDirection > 0 ? "FORWARD" : "REVERSE")}, speed={_bossSpeed:F1}");
        }
    }

    /// <summary>Handles the frozen-time pause: both pointers freeze briefly, then boss resumes.</summary>
    private void UpdateFrozenPause()
    {
        if (TimeState.Instance == null) return;

        bool isFrozen = TimeState.Instance.currentState == TimeState.State.Frozen;

        // Entering frozen state starts a new pause window
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

    /// <summary>Moves the boss pointer based on the player's current time state.</summary>
    private void UpdateBossPointer()
    {
        if (TimeScaleLogic.Instance == null || TimeState.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        // During frozen pause, both pointers are fully frozen
        if (_frozenPauseActive)
        {
            UpdateMeter(minV, maxV);
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

        // Move boss pointer
        if (contesting)
        {
            // Player opposing — boss is slowed or fully stopped
            float slowedSpeed = _bossSpeed * contestSlowFactor;
            _bossPointerValue += _bossDirection * slowedSpeed * Time.deltaTime;
        }
        else if (playerFrozen && !_frozenPauseActive)
        {
            // Frozen pause expired — boss ignores frozen and pushes normally
            _bossPointerValue += _bossDirection * _bossSpeed * Time.deltaTime;
        }
        else if (sameDirection)
        {
            // Player going same way — boss accelerates
            _bossPointerValue += _bossDirection * _bossSpeed * sameDirectionMultiplier * Time.deltaTime;
        }
        else
        {
            _bossPointerValue += _bossDirection * _bossSpeed * Time.deltaTime;
        }

        _bossPointerValue = Mathf.Clamp(_bossPointerValue, minV, maxV);

        UpdateMeter(minV, maxV);
    }

    /// <summary>Pushes the boss pointer state to the meter UI.</summary>
    private void UpdateMeter(float minV, float maxV)
    {
        if (_meter != null)
            _meter.SetBossPointer(_bossPointerValue, minV, maxV, _isContesting);
    }

    /// <summary>Checks if the boss pointer reached either edge — instant lose.</summary>
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
