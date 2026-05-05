using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossBFight : MonoBehaviour
{
    public static BossBFight Instance;

    [Header("Arena Tiles")]
    public Transform bossTilesParent;

    [Header("Phases")]
    public int totalPhases = 4;
    public int safePairsPerPhase = 2;
    public float glowDuration = 3.5f;
    public float blinkDuration = 2.5f;
    public float fallDelay = 0.25f;
    public float phasePauseDuration = 2.5f;

    [Header("Boss Pointer")]
    public float bossStartSpeed = 0.6f;
    public float bossSpeedIncrease = 0.25f;
    public float sameDirectionMultiplier = 1.5f;

    [Header("Frozen Time")]
    public float frozenPauseDuration = 1.5f;

    [Header("Colors")]
    public Color safeColor = new Color(0f, 1f, 0.3f);
    public Color dangerColor = new Color(1f, 0.2f, 0f);
    public Color defaultColor = new Color(1f, 1f, 1f);

    private float _bossSpeed;
    private int _bossDirection = 1;
    private int _currentPhase;
    private float _bossPointerDisplayValue;
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

    public bool bossActive { get; private set; }
    public List<GameObject> GetAllTiles() => _allTiles;
    public List<GameObject> GetSafeTiles() => _safeTiles;
    public bool IsContesting => _isContesting;
    public int BossDirection => _bossDirection;

    public event System.Action<bool> OnContestingChanged;

    internal static bool _showPointerGlow;
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

    public void StartBossFight()
    {
        StopAllCoroutines();
        bossActive = false;

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

        if (_meter == null)
            _meter = FindObjectOfType<TimeScaleMeter>();

        bossActive = true;

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(0, totalPhases);

        SoundManager.Instance?.PlayBossMusic();

        Debug.Log($"[BossB] Fight started. Direction={_bossDirection}, Speed={_bossSpeed:F2}");
        StartCoroutine(RunPhases());
    }

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

        if (_meter != null)
            _meter.SetBossPointer(0f, -10f, 10f, false);

        if (HUDController.Instance != null)
            HUDController.Instance.ClearBossObjective();

        ResetArena();
        SoundManager.Instance?.PlayGameMusic();

        Debug.Log("[BossB] Fight stopped and state reset.");
    }

    private IEnumerator RunPhases()
    {
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
                ShowBossFailUI();
                yield break;
            }

            yield return StartCoroutine(FlashSafeTiles());
            SoundManager.Instance?.PlayRoundClear();

            if (HUDController.Instance != null)
                HUDController.Instance.SetBossObjective(_currentPhase + 1, totalPhases);

            _phasePaused = true;

            _bossDirection *= -1;
            _bossSpeed += bossSpeedIncrease;

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

        WinBossFight();
    }

    private IEnumerator RunTilePhase(System.Action<bool> callback)
    {
        _safeTiles.Clear();
        _dangerTiles.Clear();

        List<GameObject> shuffled = new List<GameObject>(_allTiles);
        Shuffle(shuffled);

        List<GameObject> available = new List<GameObject>(shuffled);
        List<GameObject> chosenSafe = new List<GameObject>();

        for (int p = 0; p < safePairsPerPhase; p++)
        {
            if (available.Count == 0) break;

            GameObject anchor = available[0];
            available.RemoveAt(0);
            chosenSafe.Add(anchor);

            GameObject neighbor = null;
            float closestDist = float.MaxValue;
            foreach (GameObject candidate in available)
            {
                float dist = Vector3.Distance(anchor.transform.position, candidate.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    neighbor = candidate;
                }
            }

            if (neighbor != null)
            {
                chosenSafe.Add(neighbor);
                available.Remove(neighbor);
            }
        }

        foreach (GameObject tile in _allTiles)
        {
            if (chosenSafe.Contains(tile))
                _safeTiles.Add(tile);
            else
                _dangerTiles.Add(tile);
        }

        foreach (GameObject tile in _safeTiles)
            SetTileEmission(tile, safeColor * 4f);

        yield return new WaitForSeconds(glowDuration);

        foreach (GameObject tile in _allTiles)
            SetTileEmission(tile, safeColor * 4f);

        yield return StartCoroutine(BlinkTiles(_dangerTiles));

        List<GameObject> orderedDanger = GetFallOrder(_dangerTiles);
        foreach (GameObject tile in orderedDanger)
        {
            StartCoroutine(DropTile(tile));
            yield return new WaitForSeconds(fallDelay);
        }

        yield return new WaitForSeconds(1f);

        bool survived = PlayerOnSafeTile();
        callback(survived);
    }

    private List<GameObject> GetFallOrder(List<GameObject> tiles)
    {
        Vector3 safeCenter = Vector3.zero;
        int safeCount = 0;
        foreach (GameObject safe in _safeTiles)
        {
            if (safe == null) continue;
            safeCenter += safe.transform.position;
            safeCount++;
        }
        if (safeCount > 0) safeCenter /= safeCount;

        List<GameObject> ordered = new List<GameObject>(tiles);
        ordered.Sort((a, b) =>
        {
            float distA = Vector3.Distance(a.transform.position, safeCenter);
            float distB = Vector3.Distance(b.transform.position, safeCenter);
            return distB.CompareTo(distA);
        });

        return ordered;
    }

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

    private void UpdateTimeScale()
    {
        if (TimeScaleLogic.Instance == null || TimeState.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        if (_frozenPauseActive || _phasePaused)
        {
            PushMeterVisual(minV, maxV);
            return;
        }

        bool playerForward = TimeState.Instance.currentState == TimeState.State.Forward;
        bool playerReverse = TimeState.Instance.currentState == TimeState.State.Reverse;
        bool playerFrozen  = TimeState.Instance.currentState == TimeState.State.Frozen;
        bool bossGoingForward = _bossDirection > 0;

        bool contesting = (playerReverse && bossGoingForward)
                       || (playerForward && !bossGoingForward);

        bool sameDirection = (playerForward && bossGoingForward)
                          || (playerReverse && !bossGoingForward);

        if (contesting != _isContesting)
        {
            _isContesting = contesting;
            OnContestingChanged?.Invoke(_isContesting);
        }

        float displaySpeed = contesting ? _bossSpeed * 0.15f : _bossSpeed;
        _bossPointerDisplayValue += _bossDirection * displaySpeed * Time.deltaTime;
        _bossPointerDisplayValue = Mathf.Clamp(_bossPointerDisplayValue, minV, maxV);

        float bossPush = 0f;

        if (contesting)
        {
            bossPush = 0f;
        }
        else if (playerFrozen && !_frozenPauseActive)
        {
            bossPush = _bossDirection * _bossSpeed;
        }
        else if (sameDirection)
        {
            bossPush = _bossDirection * _bossSpeed * sameDirectionMultiplier;
        }
        else
        {
            bossPush = _bossDirection * _bossSpeed;
        }

        if (Mathf.Abs(bossPush) > 0.001f)
        {
            float current = TimeScaleLogic.Instance.CurrentValue;
            float next = current + bossPush * Time.deltaTime;
            next = Mathf.Clamp(next, minV, maxV);
            TimeScaleLogic.Instance.SetValue(next);
        }

        PushMeterVisual(minV, maxV);
    }

    private void PushMeterVisual(float minV, float maxV)
    {
        if (_meter == null)
            _meter = FindObjectOfType<TimeScaleMeter>();

        if (_meter != null)
            _meter.SetBossPointer(_bossPointerDisplayValue, minV, maxV, _isContesting);
    }

    private void SetTileEmission(GameObject tile, Color color)
    {
        if (tile == null) return;
        Renderer r = tile.GetComponent<Renderer>();
        if (r == null || r.materials.Length < 2) return;

        Material[] mats = r.materials;
        mats[1].SetColor("_EmissionColor", color);
        mats[1].EnableKeyword("_EMISSION");
        r.materials = mats;
    }

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