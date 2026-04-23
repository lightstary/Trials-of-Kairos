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
    public float[] challengeAngles = { 90f, 180f, 270f };
    public float angleThreshold = 5f;
    public float tickInterval = 0.5f;
    public float tickDegrees = 6f;

    [Header("Tile Attack Settings")]
    public int safePairsCount = 2;
    public float glowDuration = 3f;
    public float blinkDuration = 2f;
    public float fallDelay = 0.3f;
    public float tileResetDelay = 3f;
    public float tileAttackInterval = 6f;

    [Header("Colors")]
    public Color safeColor = new Color(0f, 1f, 0.3f);
    public Color dangerColor = new Color(1f, 0.2f, 0f);
    public Color defaultColor = new Color(1f, 1f, 1f);

    public bool bossActive = false;
    private int currentChallenge = 0;
    private float currentHandAngle = 0f;
    private bool challengeComplete = false;
    private float tickTimer = 0f;

    private Renderer tileRenderer;
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

        // Only tick when player is in forward stance
        if (TimeState.Instance.currentState != TimeState.State.Forward) return;

        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            currentHandAngle += tickDegrees;
            currentHandAngle %= 360f;
            clockHand.localRotation = Quaternion.Euler(0f, 0f, -currentHandAngle);

            Debug.Log("Clock hand angle: " + currentHandAngle);
        }
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
            Debug.Log("Challenge " + (currentChallenge + 1) + " complete!");
            StartCoroutine(CompleteChallenge());
        }
    }

    IEnumerator CompleteChallenge()
    {
        currentChallenge++;
        SoundManager.Instance?.PlayRoundClear();

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(currentChallenge, challengeAngles.Length);

        if (currentChallenge >= challengeAngles.Length)
        {
            WinBossFight();
            yield break;
        }

        yield return new WaitForSeconds(1f);
        challengeComplete = false;
    }

    public void StartBossFight()
    {
        if (bossActive) return;
        bossActive = true;
        currentChallenge = 0;
        currentHandAngle = 0f;
        challengeComplete = false;
        tickTimer = 0f;

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(0, challengeAngles.Length);

        SoundManager.Instance?.PlayBossMusic();

        StartCoroutine(RunTileAttacks());
    }

    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive = false;
        currentChallenge = 0;
        currentHandAngle = 0f;
        challengeComplete = false;
        tickTimer = 0f;

        ResetArena();
        safeTiles.Clear();
        dangerTiles.Clear();

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        if (HUDController.Instance != null)
            HUDController.Instance.ClearBossObjective();

        SoundManager.Instance?.PlayGameMusic();
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
            winScreen.Show("THE CITADEL III", 0f, 1, false, true, true, true);
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