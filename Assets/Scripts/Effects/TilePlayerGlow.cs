using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Glowing edge quads on boss tiles the player is standing on.
// Pulses green (safe) or red (danger). Auto-attaches to the Player.
public class TilePlayerGlow : MonoBehaviour
{
    private const float GLOW_PULSE_SPEED = 3f;
    private const float GLOW_MIN_ALPHA = 0.3f;
    private const float GLOW_MAX_ALPHA = 0.9f;
    private const float GLOW_EMISSION_INTENSITY = 5f;
    private const float EDGE_THICKNESS = 0.08f;
    private const float TRANSITION_SPEED = 10f;

    private const float FOOTPRINT_SHRINK = 0.35f; // shrink inward to avoid edge bleed
    private const float Y_TOLERANCE = 1.5f; // max vertical gap to count as "on" a tile

    private static readonly Color SAFE_COLOR   = new Color(0f, 1f, 0.3f);
    private static readonly Color DANGER_COLOR = new Color(1f, 0.15f, 0f);

    private readonly HashSet<GameObject> _currentTiles = new HashSet<GameObject>();
    private readonly List<GameObject> _edgeQuads = new List<GameObject>();
    private readonly Dictionary<GameObject, Material> _tileMaterials = new Dictionary<GameObject, Material>();
    private Collider _playerCollider;
    private Material _baseMaterial;
    private bool _glowActive;

    private const string BASE_MATERIAL_PATH = "Materials/TileGlowBase";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryAttach();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryAttach();
    }

    private static void TryAttach()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null && player.GetComponent<TilePlayerGlow>() == null)
            player.AddComponent<TilePlayerGlow>();
    }

    void Start()
    {
        _playerCollider = GetComponent<Collider>();
        _baseMaterial = Resources.Load<Material>(BASE_MATERIAL_PATH);
        if (_baseMaterial == null)
            Debug.LogError("[TilePlayerGlow] Base material not found at Resources/" + BASE_MATERIAL_PATH);
    }

    void Update()
    {
        if (!IsBossActive())
        {
            ClearGlow();
            return;
        }

        HashSet<GameObject> tilesBelow = GetTilesPlayerIsOn();

        if (!SetsEqual(tilesBelow, _currentTiles))
        {
            ClearGlow();
            foreach (GameObject tile in tilesBelow)
            {
                _currentTiles.Add(tile);
                CreateEdgeGlow(tile);
            }
        }

        if (_glowActive)
            AnimateGlow();
    }

    void OnDisable()
    {
        ClearGlow();
    }

    void OnDestroy()
    {
        ClearGlow();
        foreach (var kvp in _tileMaterials)
        {
            if (kvp.Value != null)
                Object.Destroy(kvp.Value);
        }
        _tileMaterials.Clear();
    }

    private bool IsBossActive()
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive) return true;
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive) return true;
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive) return true;
        return false;
    }

    // XZ footprint overlap check (no raycasting, works regardless of player orientation).
    private HashSet<GameObject> GetTilesPlayerIsOn()
    {
        HashSet<GameObject> tiles = new HashSet<GameObject>();

        List<GameObject> tracked = GetAllTrackedTiles();
        if (tracked == null || tracked.Count == 0 || _playerCollider == null)
            return tiles;

        Bounds pb = _playerCollider.bounds;

        // Shrink player footprint inward so edges don't bleed into adjacent tiles
        float pMinX = pb.min.x + FOOTPRINT_SHRINK;
        float pMaxX = pb.max.x - FOOTPRINT_SHRINK;
        float pMinZ = pb.min.z + FOOTPRINT_SHRINK;
        float pMaxZ = pb.max.z - FOOTPRINT_SHRINK;
        float pBottomY = pb.min.y;

        foreach (GameObject tile in tracked)
        {
            if (tile == null || !tile.activeInHierarchy) continue;

            Collider tileCol = tile.GetComponent<Collider>();
            if (tileCol == null) continue;

            Bounds tb = tileCol.bounds;

            // XZ overlap -- player footprint must genuinely intersect tile area
            bool overlapsX = pMinX < tb.max.x && pMaxX > tb.min.x;
            bool overlapsZ = pMinZ < tb.max.z && pMaxZ > tb.min.z;

            // Y proximity -- player's bottom must be near the tile's top surface
            float tileTopY = tb.max.y;
            bool nearSurface = pBottomY >= tileTopY - 0.3f && pBottomY <= tileTopY + Y_TOLERANCE;

            if (overlapsX && overlapsZ && nearSurface)
                tiles.Add(tile);
        }

        return tiles;
    }

    private List<GameObject> GetAllTrackedTiles()
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive)
            return BossFight.Instance.allTiles;
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive)
            return BossBFight.Instance.GetAllTiles();
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive)
            return BossCFight.Instance.allTiles;
        return null;
    }

    private void CreateEdgeGlow(GameObject tile)
    {
        Material mat = CreateGlowMaterial();
        if (mat == null) return;

        _tileMaterials[tile] = mat;

        bool isSafe = IsTileSafe(tile);
        Color col = isSafe ? SAFE_COLOR : DANGER_COLOR;
        mat.SetColor("_Color", new Color(col.r, col.g, col.b, GLOW_MIN_ALPHA));
        mat.SetColor("_EmissionColor", col * GLOW_EMISSION_INTENSITY);

        Renderer tileRend = tile.GetComponent<Renderer>();
        if (tileRend == null) return;

        Bounds bounds = tileRend.bounds;
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;
        float topY = center.y + size.y * 0.5f + 0.005f;

        CreateEdgeQuad("GlowEdge_Front", mat,
            new Vector3(center.x, topY, center.z + size.z * 0.5f - EDGE_THICKNESS * 0.5f),
            new Vector2(size.x + EDGE_THICKNESS, EDGE_THICKNESS));

        CreateEdgeQuad("GlowEdge_Back", mat,
            new Vector3(center.x, topY, center.z - size.z * 0.5f + EDGE_THICKNESS * 0.5f),
            new Vector2(size.x + EDGE_THICKNESS, EDGE_THICKNESS));

        CreateEdgeQuad("GlowEdge_Left", mat,
            new Vector3(center.x - size.x * 0.5f + EDGE_THICKNESS * 0.5f, topY, center.z),
            new Vector2(EDGE_THICKNESS, size.z + EDGE_THICKNESS));

        CreateEdgeQuad("GlowEdge_Right", mat,
            new Vector3(center.x + size.x * 0.5f - EDGE_THICKNESS * 0.5f, topY, center.z),
            new Vector2(EDGE_THICKNESS, size.z + EDGE_THICKNESS));

        _glowActive = true;
    }

    private void CreateEdgeQuad(string name, Material mat, Vector3 worldPos, Vector2 size)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = name;

        Collider col = quad.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);

        quad.transform.position = worldPos;
        quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);

        Renderer rend = quad.GetComponent<Renderer>();
        rend.material = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        _edgeQuads.Add(quad);
    }

    private void AnimateGlow()
    {
        float pulse = Mathf.Sin(Time.time * GLOW_PULSE_SPEED) * 0.5f + 0.5f;
        float alpha = Mathf.Lerp(GLOW_MIN_ALPHA, GLOW_MAX_ALPHA, pulse);
        float emissionPulse = Mathf.Lerp(GLOW_EMISSION_INTENSITY * 0.5f, GLOW_EMISSION_INTENSITY, pulse);

        foreach (var kvp in _tileMaterials)
        {
            GameObject tile = kvp.Key;
            Material mat = kvp.Value;
            if (mat == null || tile == null) continue;

            bool isSafe = IsTileSafe(tile);
            Color targetCol = isSafe ? SAFE_COLOR : DANGER_COLOR;

            Color current = mat.GetColor("_Color");
            Color blended = Color.Lerp(
                new Color(current.r, current.g, current.b, 1f),
                targetCol,
                TRANSITION_SPEED * Time.deltaTime
            );

            mat.SetColor("_Color", new Color(blended.r, blended.g, blended.b, alpha));
            mat.SetColor("_EmissionColor", blended * emissionPulse);
        }
    }

    // Uses Fade mode + additive blend for bright glow. Falls back to Standard shader.
    private Material CreateGlowMaterial()
    {
        Material mat;

        if (_baseMaterial != null)
        {
            mat = new Material(_baseMaterial);
        }
        else
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                Debug.LogError("[TilePlayerGlow] Standard shader not found in build.");
                return null;
            }
            mat = new Material(shader);
        }

        // Fade mode with additive destination blend for bright glow
        mat.SetFloat("_Mode", 2f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3100;

        // Emission for glow
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Glossiness", 0.8f);

        return mat;
    }

    private bool IsTileSafe(GameObject tile)
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive)
            return BossFight.Instance.GetSafeTiles().Contains(tile);
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive)
            return BossBFight.Instance.GetSafeTiles().Contains(tile);
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive)
            return BossCFight.Instance.GetSafeTiles().Contains(tile);
        return false;
    }

    private void ClearGlow()
    {
        foreach (GameObject quad in _edgeQuads)
        {
            if (quad != null)
                Object.Destroy(quad);
        }
        _edgeQuads.Clear();

        foreach (var kvp in _tileMaterials)
        {
            if (kvp.Value != null)
                Object.Destroy(kvp.Value);
        }
        _tileMaterials.Clear();
        _currentTiles.Clear();
        _glowActive = false;
    }

    private static bool SetsEqual(HashSet<GameObject> a, HashSet<GameObject> b)
    {
        if (a.Count != b.Count) return false;
        foreach (GameObject go in a)
        {
            if (!b.Contains(go)) return false;
        }
        return true;
    }
}
