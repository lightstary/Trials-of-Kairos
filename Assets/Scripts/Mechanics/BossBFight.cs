using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class BossBFight : MonoBehaviour
{
    public static BossBFight Instance;

    [Header("Boss Arena Tiles")]
    public List<GameObject> allTiles = new List<GameObject>();

    [Header("Survival Settings")]
    public float survivalTime = 30f;
    public float tileAttackInterval = 6f;

    [Header("Boss Pointer Settings")]
    public float bossPointerStartSpeed = 0.5f;
    public float bossPointerSpeedIncrease = 0.4f;
    public float sameDirectionMultiplier = 1.6f;

    [Header("Tile Attack Settings")]
    public int safePairsCount = 2;
    public float glowDuration = 3f;
    public float blinkDuration = 2f;
    public float fallDelay = 0.3f;
    public float tileResetDelay = 3f;

    [Header("Colors")]
    public Color safeColor = new Color(0f, 1f, 0.3f);
    public Color dangerColor = new Color(1f, 0.2f, 0f);
    public Color defaultColor = new Color(1f, 1f, 1f);

    private float bossPointerValue = 0f;
    private float bossPointerSpeed = 0f;
    private int bossPointerDirection = 1;
    private int attackCount = 0;
    private bool _isContesting = false;

    public bool bossActive = false;
    private float survivalTimer = 0f;

    /// <summary>True when the player is successfully opposing the boss pointer.</summary>
    public bool IsContesting => _isContesting;

    /// <summary>Current boss pointer direction (+1 or -1).</summary>
    public int BossDirection => bossPointerDirection;

    /// <summary>Event fired when contesting state changes. True = player is blocking the boss.</summary>
    public event System.Action<bool> OnContestingChanged;

    private Renderer tileRenderer;
    private TimeScaleMeter _meter;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private List<GameObject> safeTiles = new List<GameObject>();
    private List<GameObject> dangerTiles = new List<GameObject>();

    // Tutorial glow
    private static GameObject _bossPointerGlow;
    internal static bool _showPointerGlow = false;

    /// <summary>Returns the current list of safe tiles for external queries.</summary>
    public List<GameObject> GetSafeTiles() => safeTiles;

    /// <summary>Shows or hides the tutorial glow ring around the boss pointer.</summary>
    public static void SetPointerGlowVisible(bool visible) => _showPointerGlow = visible;

    void Awake()
    {
        Instance = this;

        foreach (GameObject tile in allTiles)
            if (tile != null)
                originalPositions[tile] = tile.transform.position;
    }

    void Start()
    {
        _meter = FindObjectOfType<TimeScaleMeter>();
    }

    void Update()
    {
        if (!bossActive) return;

        survivalTimer += Time.deltaTime;
        UpdateBossPointer();
        CheckBossPointerKill();
    }

    void UpdateBossPointer()
    {
        if (TimeScaleLogic.Instance == null) return;
        if (TimeState.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        bool playerForward = TimeState.Instance.currentState == TimeState.State.Forward;
        bool playerReverse = TimeState.Instance.currentState == TimeState.State.Reverse;
        bool bossGoingForward = bossPointerDirection == 1;
        bool bossGoingReverse = bossPointerDirection == -1;

        bool contesting = (playerReverse && bossGoingForward) ||
                          (playerForward && bossGoingReverse);

        bool sameDirection = (playerForward && bossGoingForward) ||
                             (playerReverse && bossGoingReverse);

        // Fire event on contesting state change
        if (contesting != _isContesting)
        {
            _isContesting = contesting;
            OnContestingChanged?.Invoke(_isContesting);
        }

        if (contesting)
        {
            // Player fully neutralizes the boss — no movement
        }
        else
        {
            float speedMod = sameDirection ? sameDirectionMultiplier : 1f;
            bossPointerValue += bossPointerDirection * bossPointerSpeed * speedMod * Time.deltaTime;
            bossPointerValue = Mathf.Clamp(bossPointerValue, minV, maxV);
        }

        if (_meter != null)
            _meter.SetBossPointer(bossPointerValue, minV, maxV, _isContesting);
    }

    void CheckBossPointerKill()
    {
        if (TimeScaleLogic.Instance == null) return;

        float minV = TimeScaleLogic.Instance.minValue;
        float maxV = TimeScaleLogic.Instance.maxValue;

        if (bossPointerValue >= maxV || bossPointerValue <= minV)
            LoseBossFight();
    }

    public void StartBossFight()
    {
        if (bossActive) return;
        bossActive = true;
        survivalTimer = 0f;
        attackCount = 0;
        bossPointerValue = 0f;
        bossPointerSpeed = bossPointerStartSpeed;
        bossPointerDirection = 1;

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(0, 1);

        SoundManager.Instance?.PlayBossMusic();

        StartCoroutine(RunBossFight());
    }

    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive = false;
        survivalTimer = 0f;
        attackCount = 0;
        bossPointerValue = 0f;
        bossPointerSpeed = bossPointerStartSpeed;

        ResetArena();
        safeTiles.Clear();
        dangerTiles.Clear();

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        if (HUDController.Instance != null)
            HUDController.Instance.ClearBossObjective();

        SoundManager.Instance?.PlayGameMusic();
    }

    IEnumerator RunBossFight()
    {
        float nextAttackTime = tileAttackInterval;

        while (survivalTimer < survivalTime)
        {
            if (HUDController.Instance != null)
                HUDController.Instance.SetBossObjective(0, 1);

            if (survivalTimer >= nextAttackTime)
            {
                nextAttackTime += tileAttackInterval;
                yield return StartCoroutine(TileAttack());
            }

            yield return null;
        }

        WinBossFight();
    }

    IEnumerator TileAttack()
    {
        attackCount++;

        bossPointerDirection *= -1;
        bossPointerSpeed += bossPointerSpeedIncrease;

        safeTiles.Clear();
        dangerTiles.Clear();

        List<GameObject> activeTiles = new List<GameObject>();
        foreach (GameObject tile in allTiles)
            if (tile != null && tile.activeSelf)
                activeTiles.Add(tile);

        Shuffle(activeTiles);

        List<GameObject> chosenSafeTiles = new List<GameObject>();

        for (int p = 0; p < safePairsCount; p++)
        {
            if (activeTiles.Count == 0) break;

            GameObject anchor = activeTiles[0];
            activeTiles.RemoveAt(0);
            chosenSafeTiles.Add(anchor);

            GameObject neighbor = null;
            float closestDist = float.MaxValue;

            foreach (GameObject candidate in activeTiles)
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
                chosenSafeTiles.Add(neighbor);
                activeTiles.Remove(neighbor);
            }
        }

        foreach (GameObject tile in allTiles)
        {
            if (tile == null || !tile.activeSelf) continue;
            if (chosenSafeTiles.Contains(tile))
                safeTiles.Add(tile);
            else
                dangerTiles.Add(tile);
        }

        foreach (GameObject tile in safeTiles)
        {
            tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null)
                tileRenderer.materials[1].SetColor("_EmissionColor", safeColor * 4);
        }

        yield return new WaitForSeconds(glowDuration);

        yield return StartCoroutine(BlinkTiles(dangerTiles));

        foreach (GameObject tile in dangerTiles)
        {
            StartCoroutine(DropTile(tile));
            yield return new WaitForSeconds(fallDelay);
        }

        yield return new WaitForSeconds(tileResetDelay);
        ResetArena();
    }

    IEnumerator BlinkTiles(List<GameObject> tiles)
    {
        float elapsed = 0f;
        bool colorToggle = false;

        while (elapsed < blinkDuration)
        {
            foreach (GameObject tile in tiles)
            {
                tileRenderer = tile.GetComponent<Renderer>();
                if (tileRenderer != null)
                    tileRenderer.materials[1].SetColor("_EmissionColor",
                        colorToggle ? dangerColor : defaultColor * 3);
            }
            colorToggle = !colorToggle;
            elapsed += 0.3f;
            yield return new WaitForSeconds(0.3f);
        }

        foreach (GameObject tile in tiles)
        {
            tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null)
                tileRenderer.materials[1].SetColor("_EmissionColor", dangerColor * 3);
        }
    }

    IEnumerator DropTile(GameObject tile)
    {
        if (tile == null) yield break;

        // Shake before falling to warn the player
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

        float elapsed = 0f;
        float dropTime = 0.5f;
        Vector3 startPos = originalPos;
        Vector3 endPos = startPos + Vector3.down * 10f;

        while (elapsed < dropTime)
        {
            elapsed += Time.deltaTime;
            tile.transform.position = Vector3.Lerp(startPos, endPos, elapsed / dropTime);
            yield return null;
        }

        tile.SetActive(false);
    }

    void ResetArena()
    {
        foreach (GameObject tile in allTiles)
        {
            if (tile == null) continue;
            tile.SetActive(true);
            tile.transform.position = originalPositions[tile];

            tileRenderer = tile.GetComponent<Renderer>();
            if (tileRenderer != null)
                tileRenderer.materials[1].SetColor("_EmissionColor", defaultColor * 2);
        }
    }

    void LoseBossFight()
    {
        StopBossFight();

        if (BossPopup.Instance != null)
            BossPopup.Instance.ShowLose();

        SoundManager.Instance?.PlayLose();
        FindObjectOfType<FallDetection>()?.Respawn();
    }

    void WinBossFight()
    {
        bossActive = false;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        SoundManager.Instance?.PlayWin();
        Time.timeScale = 0f;

        WinScreenController winScreen = FindObjectOfType<WinScreenController>(true);
        if (winScreen != null)
        {
            winScreen.gameObject.SetActive(true);
            winScreen.Show("THE GARDEN", 0f, 1, false, true, true, true);
        }
    }

    void Shuffle<T>(List<T> list)
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