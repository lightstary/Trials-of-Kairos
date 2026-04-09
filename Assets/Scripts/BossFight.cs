using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossFight : MonoBehaviour
{
    [Header("Boss Arena Tiles")]
    public List<GameObject> allTiles = new List<GameObject>(); // drag all boss tiles here

    [Header("Settings")]
    public int totalRounds = 3;
    public int safeTilesPerRound = 2;
    public float glowDuration = 3f;    // how long safe tiles glow before round starts
    public float blinkDuration = 2f;   // how long unsafe tiles blink before falling
    public float fallDelay = 0.3f;     // delay between each tile falling
    public float roundResetDelay = 2f; // delay before next round starts

    [Header("Colors")]
    public Color safeColor = new Color(0f, 1f, 0.3f);    // green
    public Color dangerColor = new Color(1f, 0.2f, 0f);  // red
    public Color defaultColor = new Color(1f, 1f, 1f);   // white

    [Header("Next Level")]
    public string nextSceneName = "Level2";

    private List<GameObject> safeTiles = new List<GameObject>();
    private List<GameObject> dangerTiles = new List<GameObject>();
    private int currentRound = 0;
    private bool bossActive = false;
    private bool roundActive = false;

    // Call this when player enters the boss arena
    public void StartBossFight()
    {
        if (bossActive) return;
        bossActive = true;
        StartCoroutine(RunBossFight());
    }

    IEnumerator RunBossFight()
    {
        for (currentRound = 0; currentRound < totalRounds; currentRound++)
        {
            yield return StartCoroutine(RunRound());

            // Check if player survived
            if (!PlayerOnSafeTile())
            {
                // Player failed — respawn
                FindObjectOfType<FallDetection>().Respawn();
                // Reset boss fight
                ResetArena();
                currentRound = -1; // will increment to 0
                yield return new WaitForSeconds(roundResetDelay);
                continue;
            }

            // Wait before next round
            yield return new WaitForSeconds(roundResetDelay);
            ResetArena();
        }

        // All rounds cleared!
        Debug.Log("Boss defeated! Loading next level...");
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator RunRound()
    {
        roundActive = true;

        // Pick random safe tiles
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

        // Glow safe tiles green
        foreach (GameObject tile in safeTiles)
            SetTileColor(tile, safeColor);

        // Wait for player to see safe tiles
        yield return new WaitForSeconds(glowDuration);

        // Reset safe tile color so player has to remember
        foreach (GameObject tile in safeTiles)
            SetTileColor(tile, defaultColor);

        // Blink danger tiles red
        StartCoroutine(BlinkTiles(dangerTiles));
        yield return new WaitForSeconds(blinkDuration);

        // Drop danger tiles one by one
        foreach (GameObject tile in dangerTiles)
        {
            StartCoroutine(DropTile(tile));
            yield return new WaitForSeconds(fallDelay);
        }

        // Wait for all tiles to fall
        yield return new WaitForSeconds(0.5f);
        roundActive = false;
    }

    IEnumerator BlinkTiles(List<GameObject> tiles)
    {
        float elapsed = 0f;
        bool colorToggle = false;

        while (elapsed < blinkDuration)
        {
            foreach (GameObject tile in tiles)
                SetTileColor(tile, colorToggle ? dangerColor : defaultColor);

            colorToggle = !colorToggle;
            elapsed += 0.2f;
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator DropTile(GameObject tile)
    {
        if (tile == null) yield break;

        // Animate tile falling down
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

    bool PlayerOnSafeTile()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return false;

        foreach (GameObject safeTile in safeTiles)
        {
            if (safeTile == null) continue;

            // Check if player is above this safe tile
            Vector3 rayOrigin = safeTile.transform.position + Vector3.up * 0.2f;
            RaycastHit hit;
            if (Physics.Raycast(rayOrigin, Vector3.up, out hit, 3f))
            {
                if (hit.collider.CompareTag("Player"))
                    return true;
            }
        }
        return false;
    }

    void ResetArena()
    {
        foreach (GameObject tile in allTiles)
        {
            if (tile != null)
            {
                tile.SetActive(true);
                SetTileColor(tile, defaultColor);
                // Reset position if it fell
                tile.transform.position = GetOriginalPosition(tile);
            }
        }
    }

    // Store original positions on start
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    void Awake()
    {
        // Cache all original tile positions
        foreach (GameObject tile in allTiles)
        {
            if (tile != null)
                originalPositions[tile] = tile.transform.position;
        }
    }

    Vector3 GetOriginalPosition(GameObject tile)
    {
        if (originalPositions.ContainsKey(tile))
            return originalPositions[tile];
        return tile.transform.position;
    }

    void SetTileColor(GameObject tile, Color color)
    {
        if (tile == null) return;
        Renderer r = tile.GetComponent<Renderer>();
        if (r != null) r.material.color = color;
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