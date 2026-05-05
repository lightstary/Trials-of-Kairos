using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossCFight : MonoBehaviour
{
    public static BossCFight Instance;

    [Header("Boss Arena Tiles")]
    public List<GameObject> allTiles = new List<GameObject>();

    [Header("Clock Settings")]
    public Transform clockHand;
    public float[] challengeAngles = { 120f, 300f, 210f };
    public float angleThreshold = 5f;
    public float tickInterval   = 0.5f;
    public float tickDegrees    = 6f;

    [Header("Tile Attack Settings")]
    public int   safePairsCount     = 2;
    public float glowDuration       = 3f;
    public float blinkDuration      = 2f;
    public float fallDelay          = 0.3f;
    public float tileResetDelay     = 3f;
    public float tileAttackInterval = 6f;

    [Header("Colors")]
    public Color safeColor    = new Color(0f, 1f, 0.3f);
    public Color dangerColor  = new Color(1f, 0.2f, 0f);
    public Color defaultColor = new Color(1f, 1f, 1f);

    public bool bossActive = false;

    public float CurrentHandAngle => currentHandAngle;
    public float TargetAngle => currentChallenge < challengeAngles.Length
        ? challengeAngles[currentChallenge] : 0f;
    public int CurrentChallengeIndex => currentChallenge;
    public int TotalChallenges => challengeAngles.Length;

    public event System.Action<int> OnChallengeComplete;
    public event System.Action OnBossWin;
    public event System.Action OnBossStart;
    public event System.Action OnBossStop;

    private int   currentChallenge  = 0;
    private float currentHandAngle  = 0f;
    private bool  challengeComplete = false;
    #pragma warning disable CS0414
    private float tickTimer         = 0f;
    #pragma warning restore CS0414

    private Renderer tileRenderer;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private List<GameObject> safeTiles   = new List<GameObject>();
    private List<GameObject> dangerTiles = new List<GameObject>();

    public List<GameObject> GetSafeTiles() => safeTiles;

    private Transform _playerTransform;
    private const float TILE_PROXIMITY_SQ = 0.7f * 0.7f;

    private float TrialElapsedTime => Time.realtimeSinceStartup - MainMenuController.GameplayStartRealtime;

    void Awake()
    {
        Instance = this;

        foreach (GameObject tile in allTiles)
            if (tile != null)
                originalPositions[tile] = tile.transform.position;

        PlayerMovement pm = FindObjectOfType<PlayerMovement>();
        if (pm != null) _playerTransform = pm.transform;
    }

    void Update()
    {
        if (!bossActive) return;

        UpdateClockHand();
        CheckChallengeComplete();
    }

    void UpdateClockHand()
    {
        if (clockHand == null) return;
        if (TimeState.Instance == null) return;
        if (challengeComplete) return;

        if (TimeState.Instance.currentState != TimeState.State.Forward) return;
        if (!IsPlayerOnArenaTile()) return;

        float degreesPerSecond = tickDegrees / Mathf.Max(tickInterval, 0.01f);
        currentHandAngle += degreesPerSecond * Time.deltaTime;
        currentHandAngle %= 360f;
        clockHand.localRotation = Quaternion.Euler(0f, 0f, -currentHandAngle);
    }

    private bool IsPlayerOnArenaTile()
    {
        if (_playerTransform == null) return true;
        Vector3 playerPos = _playerTransform.position;
        foreach (GameObject tile in allTiles)
        {
            if (tile == null || !tile.activeSelf) continue;
            Vector3 tilePos = tile.transform.position;
            float dx = playerPos.x - tilePos.x;
            float dz = playerPos.z - tilePos.z;
            if (dx * dx + dz * dz < TILE_PROXIMITY_SQ) return true;
        }
        return false;
    }

    void CheckChallengeComplete()
    {
        if (challengeComplete) return;
        if (currentChallenge >= challengeAngles.Length) return;

        float targetAngle = challengeAngles[currentChallenge];
        float diff = Mathf.Abs(Mathf.DeltaAngle(currentHandAngle, targetAngle));

        if (diff <= angleThreshold)
        {
            challengeComplete = true;
            int completedIndex = currentChallenge;
            StartCoroutine(CompleteChallenge(completedIndex));
        }
    }

    IEnumerator CompleteChallenge(int completedIndex)
    {
        currentChallenge++;
        SoundManager.Instance?.PlayRoundClear();
        OnChallengeComplete?.Invoke(completedIndex);

        if (currentChallenge >= challengeAngles.Length)
        {
            WinBossFight();
            yield break;
        }

        yield return new WaitForSeconds(1.5f);
        challengeComplete = false;
    }

    public void StartBossFight()
    {
        if (bossActive) return;
        bossActive        = true;
        currentChallenge  = 0;
        currentHandAngle  = 0f;
        challengeComplete = false;
        tickTimer         = 0f;

        if (clockHand != null)
            clockHand.localRotation = Quaternion.identity;

        SoundManager.Instance?.PlayBossMusic();
        OnBossStart?.Invoke();
        StartCoroutine(RunTileAttacks());
    }

    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive        = false;
        currentChallenge  = 0;
        currentHandAngle  = 0f;
        challengeComplete = false;
        tickTimer         = 0f;

        if (clockHand != null)
            clockHand.localRotation = Quaternion.identity;

        ResetArena();
        safeTiles.Clear();
        dangerTiles.Clear();

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        SoundManager.Instance?.PlayGameMusic();
        OnBossStop?.Invoke();
    }

    void WinBossFight()
    {
        bossActive = false;

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        float completionTime = TrialElapsedTime;
        SoundManager.Instance?.PlayWin();
        Time.timeScale = 0f;

        OnBossWin?.Invoke();

        WinScreenController winScreen = FindObjectOfType<WinScreenController>(true);
        if (winScreen != null)
        {
            winScreen.gameObject.SetActive(true);
            winScreen.Show("THE CLOCK", completionTime, 3, false, true, true, true);
        }
    }

    IEnumerator RunTileAttacks()
    {
        while (bossActive)
        {
            yield return new WaitForSeconds(tileAttackInterval);
            if (bossActive)
                yield return StartCoroutine(TileAttack());
        }
    }

    IEnumerator TileAttack()
    {
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

    public static string AngleToTimeString(float angle)
    {
        angle = ((angle % 360f) + 360f) % 360f;
        float totalMinutes = (angle / 360f) * 720f;
        int hours = Mathf.FloorToInt(totalMinutes / 60f);
        int minutes = Mathf.RoundToInt(totalMinutes % 60f);
        if (minutes >= 60) { minutes = 0; hours++; }
        if (hours == 0) hours = 12;
        return $"{hours}:{minutes:D2}";
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