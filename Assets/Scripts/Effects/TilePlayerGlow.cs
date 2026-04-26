using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Detects tiles beneath the player during boss fights and creates
/// visible glowing edge quads that pulse green (safe) or red (danger)
/// from the outer edges of each tile. Checks XZ overlap between the
/// player's collider footprint and each tracked boss tile to ensure
/// only tiles the player is physically on will glow.
/// Auto-attaches to the Player at runtime.
/// </summary>
public class TilePlayerGlow : MonoBehaviour
{
    private const float GLOW_PULSE_SPEED = 3f;
    private const float GLOW_MIN_ALPHA = 0.3f;
    private const float GLOW_MAX_ALPHA = 0.9f;
    private const float GLOW_EMISSION_INTENSITY = 5f;
    private const float EDGE_THICKNESS = 0.08f;
    private const float TRANSITION_SPEED = 10f;

    /// <summary>How much to shrink the player footprint inward on each side to avoid edge bleed.</summary>
    private const float FOOTPRINT_SHRINK = 0.35f;

    /// <summary>Max vertical distance between player bottom and tile top to count as "on" it.</summary>
    private const float Y_TOLERANCE = 1.5f;

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

    /// <summary>Checks if any boss fight is currently active.</summary>
    private bool IsBossActive()
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive) return true;
        if (BossBFight.Instance != null && BossBFight.Instance.bossActive) return true;
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive) return true;
        return false;
    }

    /// <summary>
    /// Iterates every tracked boss tile and checks if the player's shrunk
    /// XZ footprint overlaps with it. No raycasting -- pure geometry check
    /// that works correctly regardless of player orientation.
    /// </summary>
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

    /// <summary>Returns the tile list from whichever boss fight is active.</summary>
    private List<GameObject> GetAllTrackedTiles()
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive)
            return BossFight.Instance.allTiles;
        // Boss B no longer uses tile attacks — no tiles to track
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive)
            return BossCFight.Instance.allTiles;
        return null;
    }

    /// <summary>Creates 4 glowing edge quads on the top surface edges of a tile.</summary>
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

    /// <summary>Creates a single edge quad with a specific material, oriented face-up.</summary>
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

    /// <summary>Pulses all tile glow materials.</summary>
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

    /// <summary>
    /// Creates a glow material using the same approach as GlowOrbSetup (Fade mode
    /// with _ALPHABLEND_ON) to ensure the shader variant is included in builds.
    /// Uses additive destination blend for the bright glow effect.
    /// </summary>
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

    /// <summary>Checks all active boss fight safe tile lists.</summary>
    private bool IsTileSafe(GameObject tile)
    {
        if (BossFight.Instance != null && BossFight.Instance.bossActive)
            return BossFight.Instance.GetSafeTiles().Contains(tile);
        // Boss B no longer uses tile attacks — no safe/danger tiles
        if (BossCFight.Instance != null && BossCFight.Instance.bossActive)
            return BossCFight.Instance.GetSafeTiles().Contains(tile);
        return false;
    }

    /// <summary>Destroys all glow edge quads and resets state.</summary>
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

    /// <summary>Compares two tile sets for equality.</summary>
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
