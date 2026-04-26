using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossFight : MonoBehaviour
{
    public static BossFight Instance;
	
	private Renderer tileRenderer;

    [Header("Boss Arena Tiles")]
    public List<GameObject> allTiles = new List<GameObject>();

    [Header("Settings")]
    public int totalRounds = 3;
    public int safeTilesPerRound = 2;
    public float glowDuration = 4f;
    public float blinkDuration = 3f;
    public float fallDelay = 0.3f;
    public float roundResetDelay = 2f;

    [Header("Colors")]
    public Color safeColor = new Color(0f, 1f, 0.3f);
    public Color dangerColor = new Color(1f, 0.2f, 0f);
    public Color defaultColor = new Color(1f, 1f, 1f);

    [Header("Scene Names")]
    public string mainMenuScene = "MainMenu";

    private List<GameObject> safeTiles = new List<GameObject>();
    private List<GameObject> dangerTiles = new List<GameObject>();

    /// <summary>Returns the current list of safe tiles for external queries.</summary>
    public List<GameObject> GetSafeTiles() => safeTiles;
    private int currentRound = 0;
    public bool bossActive = false;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();
    private Vector3 arenaCenter;

    private float TrialElapsedTime => Time.realtimeSinceStartup - MainMenuController.GameplayStartRealtime;

    void Awake()
    {
        Instance = this;

        foreach (GameObject tile in allTiles)
            if (tile != null)
                originalPositions[tile] = tile.transform.position;

        arenaCenter = Vector3.zero;
        foreach (GameObject tile in allTiles)
            if (tile != null)
                arenaCenter += tile.transform.position;
        arenaCenter /= allTiles.Count;
    }
	
	void Start()
	{
		tileRenderer = GetComponent<Renderer>();
	}
	
	public void StartBossFight()
    {
        if (bossActive) return;
        bossActive = true;

        if (HUDController.Instance != null)
            HUDController.Instance.SetBossObjective(0, totalRounds);

        SoundManager.Instance?.PlayBossMusic();

        StartCoroutine(RunBossFight());
    }

    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive = false;
        currentRound = 0;

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
        for (currentRound = 0; currentRound < totalRounds; currentRound++)
        {
            bool survived = false;
            yield return StartCoroutine(RunRound(result => survived = result));

            if (!survived)
            {
                StopBossFight();
                SoundManager.Instance?.PlayLose();

                // Tile-based death → respawn at checkpoint (not game over)
                FallDetection fd = FindObjectOfType<FallDetection>();
                if (fd != null) fd.Respawn();
                yield break;
            }

            yield return StartCoroutine(FlashSafeTiles());
            SoundManager.Instance?.PlayRoundClear();

            if (HUDController.Instance != null)
                HUDController.Instance.SetBossObjective(currentRound + 1, totalRounds);

            yield return new WaitForSeconds(roundResetDelay);
            ResetArena();
            yield return new WaitForSeconds(1f);
        }

        bossActive = false;
        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.ResetMeter();

        float completionTime = TrialElapsedTime;
        SoundManager.Instance?.PlayWin();

        Time.timeScale = 0f;

        WinScreenController winScreen = FindObjectOfType<WinScreenController>(true);
        if (winScreen != null)
        {
            winScreen.gameObject.SetActive(true);
            winScreen.Show("THE CITADEL", completionTime, 3, false, true, true, true);
        }
        else
        {
            Debug.LogWarning("[BossFight] WinScreenController not found in scene.");
        }
    }

    IEnumerator RunRound(System.Action<bool> callback)
    {
        safeTiles.Clear();
        dangerTiles.Clear();

        List<GameObject> shuffled = new List<GameObject>(allTiles);
        Shuffle(shuffled);

        for (int i = 0; i < shuffled.Count; i++)
        {
            if (i < safeTilesPerRound)
                safeTiles.Add(shuffled[i]);
            else
                dangerTiles.Add(shuffled[i]);
        }

        foreach (GameObject tile in safeTiles)
		{
			tileRenderer = tile.GetComponent<Renderer>();
			if(tileRenderer != null)
			{
				tileRenderer.materials[1].SetColor("_EmissionColor", safeColor * 4);
			}
		}

        yield return new WaitForSeconds(glowDuration);

        foreach (GameObject tile in allTiles)
		{
			tileRenderer = tile.GetComponent<Renderer>();
			if(tileRenderer != null)
			{
				tileRenderer.materials[1].SetColor("_EmissionColor", safeColor * 4);
			}
		}

        yield return StartCoroutine(BlinkTiles(dangerTiles));

        List<GameObject> orderedDangerTiles = GetFallOrder(dangerTiles, currentRound);

        foreach (GameObject tile in orderedDangerTiles)
        {
            StartCoroutine(DropTile(tile));
            yield return new WaitForSeconds(fallDelay);
        }

        yield return new WaitForSeconds(1f);

        bool survived = PlayerOnSafeTile();
        callback(survived);
    }

    List<GameObject> GetFallOrder(List<GameObject> tiles, int round)
    {
        List<GameObject> ordered = new List<GameObject>(tiles);

        Vector3 safeCenter = Vector3.zero;
        foreach (GameObject safeTile in safeTiles)
            if (safeTile != null)
                safeCenter += safeTile.transform.position;
        safeCenter /= safeTiles.Count;

        switch (round)
        {
            case 0:
                Shuffle(ordered);
                ordered.Sort((a, b) =>
                {
                    float distA = Vector3.Distance(a.transform.position, safeCenter);
                    float distB = Vector3.Distance(b.transform.position, safeCenter);
                    return distB.CompareTo(distA);
                });
                break;

            case 1:
                ordered.Sort((a, b) =>
                {
                    float scoreA = a.transform.position.x -
                        (Vector3.Distance(a.transform.position, safeCenter) * 0.3f);
                    float scoreB = b.transform.position.x -
                        (Vector3.Distance(b.transform.position, safeCenter) * 0.3f);
                    return scoreA.CompareTo(scoreB);
                });
                break;

            case 2:
                ordered.Sort((a, b) =>
                {
                    float distFromCenterA = Vector3.Distance(a.transform.position, arenaCenter);
                    float distFromSafeA = Vector3.Distance(a.transform.position, safeCenter);
                    float distFromCenterB = Vector3.Distance(b.transform.position, arenaCenter);
                    float distFromSafeB = Vector3.Distance(b.transform.position, safeCenter);

                    float scoreA = distFromCenterB - (distFromSafeA * 0.5f);
                    float scoreB = distFromCenterA - (distFromSafeB * 0.5f);
                    return scoreA.CompareTo(scoreB);
                });
                break;
        }

        return ordered;
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
				if(tileRenderer != null)
				{
					tileRenderer.materials[1].SetColor("_EmissionColor", colorToggle ? dangerColor : defaultColor * 3);
				}
			}

            colorToggle = !colorToggle;
            elapsed += 0.3f;
            yield return new WaitForSeconds(0.3f);
        }

        foreach (GameObject tile in tiles)
		{
			tileRenderer = tile.GetComponent<Renderer>();
			if(tileRenderer != null)
			{
				tileRenderer.materials[1].SetColor("_EmissionColor", dangerColor * 3);
			}
		}
    }

    IEnumerator FlashSafeTiles()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (GameObject tile in safeTiles)
			{
				tileRenderer = tile.GetComponent<Renderer>();
				if(tileRenderer != null)
				{
					tileRenderer.materials[1].SetColor("_EmissionColor", safeColor * 4);
				}
			}
            yield return new WaitForSeconds(0.3f);
            foreach (GameObject tile in safeTiles)
			{
				tileRenderer = tile.GetComponent<Renderer>();
				if(tileRenderer != null)
				{
					tileRenderer.materials[1].SetColor("_EmissionColor", defaultColor * 4);
				}
			}
            yield return new WaitForSeconds(0.3f);
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
            // Intensity ramps up over time — starts subtle, gets urgent
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

        // Drop
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

    bool PlayerOnSafeTile()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        RaycastHit[] hits = Physics.RaycastAll(player.transform.position, Vector3.down, 3f);

        foreach (RaycastHit hit in hits)
        {
            foreach (GameObject safeTile in safeTiles)
            {
                if (safeTile != null && hit.collider.gameObject == safeTile)
                    return true;
            }
        }

        return false;
    }

    void ResetArena()
    {
        foreach (GameObject tile in allTiles)
        {
            tileRenderer = tile.GetComponent<Renderer>();
			if(tileRenderer != null)
			{
				tileRenderer.materials[1].SetColor("_EmissionColor", defaultColor * 2);
			}
			
			if (tile != null)
            {
                tile.SetActive(true);
                tile.transform.position = originalPositions[tile];
            }
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