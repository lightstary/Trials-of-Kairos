using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BossFight : MonoBehaviour
{
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

    [Header("Next Level")]
    public string nextSceneName = "Level2";

    private List<GameObject> safeTiles = new List<GameObject>();
    private List<GameObject> dangerTiles = new List<GameObject>();
    private int currentRound = 0;
    public bool bossActive = false;
    private Dictionary<GameObject, Vector3> originalPositions = new Dictionary<GameObject, Vector3>();

    void Awake()
    {
        foreach (GameObject tile in allTiles)
            if (tile != null)
                originalPositions[tile] = tile.transform.position;
    }

    public void StartBossFight()
    {
        if (bossActive) return;
        bossActive = true;
        StartCoroutine(RunBossFight());
    }

    public void StopBossFight()
    {
        StopAllCoroutines();
        bossActive = false;
        currentRound = 0;
        ResetArena();
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
                FindObjectOfType<FallDetection>().Respawn();
                yield break;
            }

            // Player survived! Flash and reset for next round
            yield return StartCoroutine(FlashSafeTiles());
            yield return new WaitForSeconds(roundResetDelay);
            ResetArena();
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Boss defeated! Loading next level...");
        yield return new WaitForSeconds(1.5f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
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

        // Show safe tiles green
        foreach (GameObject tile in safeTiles)
            SetTileColor(tile, safeColor);

        yield return new WaitForSeconds(glowDuration);

        // Turn off all glows
        foreach (GameObject tile in allTiles)
            SetTileColor(tile, defaultColor);

        // Blink danger tiles
        yield return StartCoroutine(BlinkTiles(dangerTiles));

        // Drop danger tiles one by one
        foreach (GameObject tile in dangerTiles)
        {
            StartCoroutine(DropTile(tile));
            yield return new WaitForSeconds(fallDelay);
        }

        // Wait for tiles to finish falling
        yield return new WaitForSeconds(1f);

        // Check if player survived
        bool survived = PlayerOnSafeTile();
        callback(survived);
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
            elapsed += 0.3f;
            yield return new WaitForSeconds(0.3f);
        }

        foreach (GameObject tile in tiles)
            SetTileColor(tile, dangerColor);
    }

    IEnumerator FlashSafeTiles()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (GameObject tile in safeTiles)
                SetTileColor(tile, safeColor);
            yield return new WaitForSeconds(0.3f);
            foreach (GameObject tile in safeTiles)
                SetTileColor(tile, defaultColor);
            yield return new WaitForSeconds(0.3f);
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

    bool PlayerOnSafeTile()
    {
        foreach (GameObject safeTile in safeTiles)
        {
            if (safeTile == null) continue;

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
                tile.transform.position = originalPositions[tile];
                SetTileColor(tile, defaultColor);
            }
        }
    }

    void SetTileColor(GameObject tile, Color color)
    {
        if (tile == null) return;

        Transform lightTransform = tile.transform.Find("BossGlow");
        Light glowLight;

        if (lightTransform == null)
        {
            GameObject lightObj = new GameObject("BossGlow");
            lightObj.transform.SetParent(tile.transform);
            lightObj.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            lightObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            glowLight = lightObj.AddComponent<Light>();
            glowLight.type = LightType.Spot;
            glowLight.spotAngle = 15f;
            glowLight.range = 8f;
            glowLight.intensity = 3f;
        }
        else
        {
            glowLight = lightTransform.GetComponent<Light>();
        }

        if (color == defaultColor)
            glowLight.enabled = false;
        else
        {
            glowLight.enabled = true;
            glowLight.color = color;
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