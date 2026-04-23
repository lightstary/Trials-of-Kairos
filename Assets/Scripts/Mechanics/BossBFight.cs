using UnityEngine;
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

    public bool bossActive = false;
    private float survivalTimer = 0f;

    private Renderer tileRenderer;
    private TimeScaleMeter _meter;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private List<GameObject> safeTiles = new List<GameObject>();
    private List<GameObject> dangerTiles = new List<GameObject>();

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

        // TEMP DEBUG
        Debug.Log("Boss pointer value: " + bossPointerValue + " | Meter: " + (_meter != null ? "FOUND" : "NULL"));
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

        if (!contesting)
        {
            bossPointerValue += bossPointerDirection * bossPointerSpeed * Time.deltaTime;
            bossPointerValue = Mathf.Clamp(bossPointerValue, minV, maxV);
        }

        if (_meter != null)
            _meter.SetBossPointer(bossPointerValue, minV, maxV);
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

        float elapsed = 0f;
        float dropTime = 0.5f;
        Vector3 startPos = tile.transform.position;
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
            winScreen.Show("THE CITADEL II", 0f, 1, false, true, true, true);
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