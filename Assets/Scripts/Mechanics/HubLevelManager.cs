using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the HUB tutorial level. Can build the tile layout either at runtime
/// or in the Editor via the context menu (right-click component → Build Hub Layout).
/// Tiles match MainScene setup exactly: scale (1,0.2,1), BoxCollider center=(0,0,0) size=(1,1,1).
/// Also bootstraps the PauseMenu UI if its references are null (HubScene ships with an empty PauseMenu).
///
/// To edit the Hub in the Scene view without Play mode:
///   1. Right-click this component in the Inspector
///   2. Choose "Build Hub Layout" to generate all tiles
///   3. Edit tiles freely (move, delete, change materials)
///   4. Use "Clear Hub Layout" to remove generated tiles and rebuild fresh
/// </summary>
[ExecuteAlways]
public class HubLevelManager : MonoBehaviour
{
    private const string FIRST_VISIT_KEY = "Hub_FirstVisit";
    private const float TILE_TOP = 0.1f;

    [Header("References (auto-found if null)")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private HowToPlayController howToPlayScreen;

    [Header("Real Game Materials (loaded at runtime)")]
    [SerializeField] private Material basePlatformMat;
    [SerializeField] private Material goldGlowMat;
    [SerializeField] private Material blueGlowMat;
    [SerializeField] private Material purpleGlowMat;
    [SerializeField] private Material goalTileMat;
    [SerializeField] private Material darkPlatformMat;

    [Header("Real Game Mesh")]
    [SerializeField] private Mesh tileMesh;

    [Header("Trial Scene")]
    [SerializeField] private string mainTrialScene = "MainScene";

    /// <summary>True when the How To Play screen is showing.</summary>
    public bool IsTutorialOpen => howToPlayScreen != null && howToPlayScreen.IsOpen;

    private static readonly Color ACCENT_GOLD    = new Color(0.961f, 0.784f, 0.259f);
    private static readonly Color ACCENT_BLUE    = new Color(0.353f, 0.706f, 0.941f);
    private static readonly Color ACCENT_PURPLE  = new Color(0.608f, 0.365f, 0.898f);
    private static readonly Color GOAL_COLOR     = new Color(1f, 0.843f, 0f);
    private static readonly Color TILE_EDGE_COL  = new Color(0.14f, 0.18f, 0.28f);
    private static readonly Color COSMIC_AMBIENT = new Color(0.04f, 0.05f, 0.08f);

    /// <summary>Singleton for easy access.</summary>
    public static HubLevelManager Instance { get; private set; }

    void Awake()
    {
        if (!Application.isPlaying) return;
        Instance = this;
    }

    void Start()
    {
        if (!Application.isPlaying) return;

        LoadRealAssets();

        // Only build tiles at runtime if they don't already exist in the scene
        // (they were pre-built in edit mode and saved with the scene).
        Transform existingTiles = transform.Find("HubTiles");
        if (existingTiles == null || existingTiles.childCount == 0)
            BuildVisualLayout();

        // Always attach runtime components (gameplay scripts) to existing tiles
        existingTiles = transform.Find("HubTiles");
        if (existingTiles != null)
            AttachRuntimeComponents(existingTiles);

        SetupEnvironment();
        BootstrapPauseMenu();
        EnsureAudioListener();
        EnsureStickCursor();
        EnsurePlayerModel();
        EnsureTimeScaleMeter();

        if (IsFirstVisit())
            StartCoroutine(ShowTutorialDelayed(1.5f));
    }

    /// <summary>
    /// Attaches runtime-only components (tutorial triggers, goal tile, moving platform,
    /// floating orb animation, modal trigger) to pre-built tiles that already exist.
    /// Called every time the scene enters play mode.
    /// </summary>
    private void AttachRuntimeComponents(Transform root)
    {
        foreach (Transform child in root)
        {
            string name = child.name;

            // Parse tile position from name: "Tile_X_Z"
            if (name.StartsWith("Tile_"))
            {
                string[] parts = name.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
                {
                    // Tutorial triggers — color state section
                    if (x == 0 && z == 9)  AddTutorialTrigger(child.gameObject, TutorialTilePopup.TileType.Reverse);
                    if (x == 0 && z == 10) AddTutorialTrigger(child.gameObject, TutorialTilePopup.TileType.Frozen);
                    if (x == 0 && z == 12) AddTutorialTrigger(child.gameObject, TutorialTilePopup.TileType.Forward);

                    // Goal tile
                    if (x == 0 && z == 36 && child.GetComponent<GoalTile>() == null)
                    {
                        child.gameObject.name = "GoalTile";
                        child.gameObject.AddComponent<GoalTile>();
                    }
                }
            }

            // Moving platform
            if (name == "DemoPlatform" && child.GetComponent<MovingTile>() == null)
            {
                MovingTile mt = child.gameObject.AddComponent<MovingTile>();
                mt.moveDirection = Vector3.forward;
                mt.moveDistance = 2f;
                mt.tickInterval = 1f;
                mt.minTime = -2f;
                mt.maxTime = 2f;
                mt.smoothMovement = true;
            }

            // Floating orbs — add animation component at runtime
            if (name == "CosmicOrb" && child.GetComponent<HubFloatingOrb>() == null)
                child.gameObject.AddComponent<HubFloatingOrb>();
        }

        // Time Scale intro modal trigger
        Transform existingTrigger = root.Find("TimeScaleIntroTrigger");
        if (existingTrigger == null)
            CreateTimeScaleModalTrigger(root);

        // Player spawn
        if (playerTransform == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        if (playerTransform != null)
        {
            playerTransform.position = new Vector3(0f, TILE_TOP + 1.0f, 0f);
            playerTransform.rotation = Quaternion.identity;
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null) pm.orientation = PlayerMovement.Orientation.Standing;
        }
    }

#if UNITY_EDITOR
    /// <summary>Builds (or rebuilds) the Hub tile layout in the Editor. Tiles persist in the scene.</summary>
    [ContextMenu("Build Hub Layout")]
    private void EditorBuildLayout()
    {
        Transform existing = transform.Find("HubTiles");
        if (existing != null)
            DestroyImmediate(existing.gameObject);

        LoadRealAssets();
        BuildVisualLayout();

        UnityEditor.Undo.RegisterCreatedObjectUndo(transform.Find("HubTiles").gameObject, "Build Hub Layout");
        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[HubLevelManager] Hub layout built. Save the scene to persist changes.");
    }

    /// <summary>Removes all generated hub tiles so you can rebuild fresh.</summary>
    [ContextMenu("Clear Hub Layout")]
    private void EditorClearLayout()
    {
        Transform existing = transform.Find("HubTiles");
        if (existing != null)
        {
            UnityEditor.Undo.DestroyObjectImmediate(existing.gameObject);
            Debug.Log("[HubLevelManager] Hub layout cleared.");
        }
    }
#endif

    /// <summary>Shows the How To Play screen.</summary>
    public void ShowHowToPlay()
    {
        if (howToPlayScreen == null)
            howToPlayScreen = FindObjectOfType<HowToPlayController>(true);
        if (howToPlayScreen != null)
            howToPlayScreen.Show();
    }

    /// <summary>Loads the main trial scene.</summary>
    public void EnterTrial()
    {
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(mainTrialScene);
        else
            SceneManager.LoadScene(mainTrialScene);
    }

    /// <summary>Checks first visit via PlayerPrefs.</summary>
    public bool IsFirstVisit() => PlayerPrefs.GetInt(FIRST_VISIT_KEY, 1) == 1;

    /// <summary>Marks the hub as visited.</summary>
    public void MarkVisited()
    {
        PlayerPrefs.SetInt(FIRST_VISIT_KEY, 0);
        PlayerPrefs.Save();
    }

    private IEnumerator ShowTutorialDelayed(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowHowToPlay();
        MarkVisited();
    }

    // ── Asset Loading ────────────────────────────────────────────────────

    private void LoadRealAssets()
    {
        if (basePlatformMat == null)
            basePlatformMat = LoadMat("Assets/Zutzuy's Assets/black/clockPlatform.mat");
        if (goldGlowMat == null)
            goldGlowMat = LoadMat("Assets/Zutzuy's Assets/orange/orangeGlow.mat");
        if (purpleGlowMat == null)
            purpleGlowMat = LoadMat("Assets/Zutzuy's Assets/purple/purpleGlow.mat");
        if (goalTileMat == null)
            goalTileMat = LoadMat("Assets/Zutzuy's Assets/orange/goalTile.mat");
        if (darkPlatformMat == null)
            darkPlatformMat = LoadMat("Assets/Zutzuy's Assets/black purple/citadelPlatform.mat");

        // BLUE: Do NOT load from asset — the Zutzuy blue material is too dark/subtle.
        // Create a guaranteed bright, clearly-blue material for the tutorial tile.
        if (blueGlowMat == null)
            blueGlowMat = MakeTutorialMat(ACCENT_BLUE);

        // Load the REAL tile mesh — TileNew.fbx, same mesh used in MainScene
        if (tileMesh == null)
        {
            GameObject fbx = LoadAsset<GameObject>("Assets/Zutzuy's Assets/TileNew.fbx");
            if (fbx != null)
            {
                MeshFilter mf = fbx.GetComponentInChildren<MeshFilter>();
                if (mf != null) tileMesh = mf.sharedMesh;
            }
        }

        // Fallback materials if loading fails
        if (basePlatformMat == null) basePlatformMat = MakeFallbackMat(new Color(0.08f, 0.10f, 0.16f));
        if (goldGlowMat == null)    goldGlowMat = MakeTutorialMat(ACCENT_GOLD);
        if (purpleGlowMat == null)  purpleGlowMat = MakeTutorialMat(ACCENT_PURPLE);
        if (goalTileMat == null)    goalTileMat = MakeTutorialMat(GOAL_COLOR);
        if (darkPlatformMat == null) darkPlatformMat = basePlatformMat;
    }

    private Material LoadMat(string path)
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
#else
        return null;
#endif
    }

    private T LoadAsset<T>(string path) where T : Object
    {
#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
        return null;
#endif
    }

    private Material MakeFallbackMat(Color c)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material m = new Material(shader);
        m.color = c;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.4f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.6f);
        return m;
    }

    private Material MakeEmissiveMat(Color c)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material m = new Material(shader);
        m.color = c * 0.5f;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.5f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.7f);
        if (shader != null && shader.name == "Standard")
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.3f);
        }
        return m;
    }

    /// <summary>
    /// Creates a bright, clearly-visible tutorial tile material.
    /// Uses full color with strong emission so the tile is unmistakably the intended color.
    /// </summary>
    private Material MakeTutorialMat(Color c)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material m = new Material(shader);
        m.color = c;
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.3f);
        if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.75f);
        if (shader.name == "Standard")
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 0.6f);
        }
        return m;
    }

    // ── Level Building ───────────────────────────────────────────────────

    /// <summary>
    /// Builds the visual layout of the hub: tiles, pillars, orbs.
    /// Does NOT add runtime gameplay components (those are added by AttachRuntimeComponents).
    /// Works in both edit mode and play mode.
    /// </summary>
    private void BuildVisualLayout()
    {
        Transform root = new GameObject("HubTiles").transform;
        root.SetParent(transform);

        // Use a dictionary so each (x, z) position gets exactly ONE tile.
        var tiles = new System.Collections.Generic.Dictionary<(int, int), Material>();

        // ── Spawn Platform (5x5) ──
        for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
                tiles[(x, z)] = basePlatformMat;

        // Gold corners (override base)
        tiles[(-2, -2)] = goldGlowMat;
        tiles[( 2, -2)] = goldGlowMat;
        tiles[(-2,  2)] = goldGlowMat;
        tiles[( 2,  2)] = goldGlowMat;

        // ── Corridor (z=3..8) ──
        for (int z = 3; z <= 8; z++)
        {
            tiles[(0, z)] = basePlatformMat;
            Material edgeMat = z % 2 == 0 ? darkPlatformMat : basePlatformMat;
            tiles[(-1, z)] = edgeMat;
            tiles[( 1, z)] = edgeMat;
        }

        // ── Time States Arena (z=9..16) ──
        for (int x = -3; x <= 3; x++)
            for (int z = 9; z <= 16; z++)
                tiles[(x, z)] = basePlatformMat;

        // Tutorial tiles
        Material tutGold   = MakeTutorialMat(ACCENT_GOLD);
        Material tutBlue   = MakeTutorialMat(ACCENT_BLUE);
        Material tutPurple = MakeTutorialMat(ACCENT_PURPLE);

        tiles[(0, 9)]  = tutPurple;
        tiles[(0, 10)] = tutBlue;
        tiles[(0, 11)] = tutBlue;
        tiles[(0, 12)] = tutGold;

        // ── Path to Time Scale section (z=17..19) ──
        for (int z = 17; z <= 19; z++)
            for (int x = -1; x <= 1; x++)
                tiles[(x, z)] = basePlatformMat;

        // ── TIME SCALE TUTORIAL SECTION (z=20..32) ──
        for (int x = -3; x <= 3; x++)
            for (int z = 20; z <= 23; z++)
                tiles[(x, z)] = basePlatformMat;

        for (int z = 24; z <= 26; z++)
        {
            tiles[(-1, z)] = basePlatformMat;
            tiles[( 1, z)] = basePlatformMat;
        }

        for (int x = -3; x <= 3; x++)
            for (int z = 27; z <= 29; z++)
                tiles[(x, z)] = basePlatformMat;

        for (int z = 30; z <= 32; z++)
            for (int x = -1; x <= 1; x++)
                tiles[(x, z)] = basePlatformMat;

        // ── Path to goal (z=33..35) ──
        for (int z = 33; z <= 35; z++)
            for (int x = -1; x <= 1; x++)
                tiles[(x, z)] = basePlatformMat;

        // ── Goal tile ──
        tiles[(0, 36)] = goalTileMat;

        // ── Create all tiles (exactly one per position) ──
        foreach (var kvp in tiles)
        {
            int x = kvp.Key.Item1;
            int z = kvp.Key.Item2;
            CreateTile(root, x, z, kvp.Value);
        }

        // ── Moving demo platform (bridges the gap at z=25, center) ──
        CreateMovingDemoPlatformVisual(root);

        // ── Decorative pillars ──
        MakePillar(root, new Vector3(-4f, 0f, 0f), 0.4f, 4f);
        MakePillar(root, new Vector3(4f, 0f, 0f), 0.4f, 4f);
        MakePillar(root, new Vector3(-5f, 0f, 12.5f), 0.5f, 6f);
        MakePillar(root, new Vector3(5f, 0f, 12.5f), 0.5f, 6f);
        MakePillar(root, new Vector3(-5f, 0f, 25f), 0.5f, 6f);
        MakePillar(root, new Vector3(5f, 0f, 25f), 0.5f, 6f);

        // ── Floating orbs (visual only — HubFloatingOrb added at runtime) ──
        MakeOrb(root, new Vector3(-3f, 5f, 6f), 0.3f, ACCENT_GOLD);
        MakeOrb(root, new Vector3(3f, 7f, 10f), 0.25f, ACCENT_BLUE);
        MakeOrb(root, new Vector3(-4f, 6f, 15f), 0.2f, ACCENT_PURPLE);
        MakeOrb(root, new Vector3(4f, 5f, 22f), 0.25f, ACCENT_GOLD);
        MakeOrb(root, new Vector3(-3f, 7f, 29f), 0.2f, ACCENT_PURPLE);
        MakeOrb(root, new Vector3(0f, 8f, 36f), 0.4f, GOAL_COLOR);
    }

    /// <summary>
    /// Creates a tile matching MainScene setup exactly:
    /// TileNew.fbx mesh, scale (1,0.2,1), BoxCollider center=(0,0,0) size=(1,1,1), tag=Tile.
    /// </summary>
    private GameObject CreateTile(Transform parent, int x, int z, Material mat)
    {
        GameObject tile;

        if (tileMesh != null)
        {
            tile = new GameObject($"Tile_{x}_{z}");
            MeshFilter mf = tile.AddComponent<MeshFilter>();
            mf.sharedMesh = tileMesh;
            MeshRenderer mr = tile.AddComponent<MeshRenderer>();
            mr.material = mat;
        }
        else
        {
            tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile.name = $"Tile_{x}_{z}";
            Renderer rend = tile.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.material = mat;
            // Cube already has BoxCollider — remove so we add our own consistently
            BoxCollider existing = tile.GetComponent<BoxCollider>();
            if (existing != null) SafeDestroy(existing);
        }

        // Match MainScene tile transform: scale (1,0.2,1), position at integer coords
        tile.transform.SetParent(parent);
        tile.transform.localPosition = new Vector3(x, 0f, z);
        tile.transform.localScale = new Vector3(1f, 0.2f, 1f);
        tile.transform.localRotation = Quaternion.identity;

        // BoxCollider matching MainScene exactly: center=(0,0,0), size=(1,1,1)
        BoxCollider col = tile.AddComponent<BoxCollider>();
        col.center = Vector3.zero;
        col.size = Vector3.one;

        tile.tag = "Tile";
        return tile;
    }

    private void AddTutorialTrigger(GameObject tile, TutorialTilePopup.TileType type)
    {
        TutorialTilePopup popup = tile.AddComponent<TutorialTilePopup>();
        var field = typeof(TutorialTilePopup).GetField("tileType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(popup, type);
    }

    /// <summary>
    /// Creates the visual demo platform tile at z=24. The MovingTile component
    /// is added at runtime by AttachRuntimeComponents.
    /// </summary>
    private void CreateMovingDemoPlatformVisual(Transform root)
    {
        Material demoMat = MakeTutorialMat(ACCENT_GOLD);
        GameObject platform = CreateTile(root, 0, 24, demoMat);
        platform.name = "DemoPlatform";
    }

    /// <summary>
    /// Creates an invisible trigger at z=16 that shows the TimeScaleIntroModal
    /// once the player walks past the color-state tutorial tiles.
    /// Uses proximity detection (no Rigidbody needed).
    /// </summary>
    private void CreateTimeScaleModalTrigger(Transform root)
    {
        GameObject trigger = new GameObject("TimeScaleIntroTrigger");
        trigger.transform.SetParent(root);
        trigger.transform.localPosition = new Vector3(0f, TILE_TOP + 1.0f, 16f);

        trigger.AddComponent<TimeScaleIntroModal>();
    }

    private void MakePillar(Transform parent, Vector3 pos, float radius, float height)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = "Pillar";
        p.transform.SetParent(parent);
        p.transform.position = pos + Vector3.up * (height * 0.5f);
        p.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        Renderer r = p.GetComponent<Renderer>();
        if (r != null) r.material = darkPlatformMat != null ? darkPlatformMat : MakeFallbackMat(TILE_EDGE_COL);
        Collider c = p.GetComponent<Collider>();
        if (c != null) SafeDestroy(c);
    }

    private void MakeOrb(Transform parent, Vector3 pos, float radius, Color color)
    {
        GameObject o = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        o.name = "CosmicOrb";
        o.transform.SetParent(parent);
        o.transform.position = pos;
        o.transform.localScale = Vector3.one * radius * 2f;
        Renderer r = o.GetComponent<Renderer>();
        if (r != null)
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            Material m = new Material(shader);
            m.color = color;
            if (shader != null && shader.name == "Standard")
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 0.8f);
            }
            r.material = m;
        }
        // HubFloatingOrb animation is added at runtime by AttachRuntimeComponents
        Collider c = o.GetComponent<Collider>();
        if (c != null) SafeDestroy(c);
    }

    /// <summary>Destroys an object safely in both edit mode and play mode.</summary>
    private static void SafeDestroy(Object obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

    // ── Environment ──────────────────────────────────────────────────────

    private void SetupEnvironment()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = COSMIC_AMBIENT;
        RenderSettings.fog = false;

        Light dirLight = FindObjectOfType<Light>();
        if (dirLight == null)
        {
            GameObject go = new GameObject("DirectionalLight");
            go.transform.SetParent(transform);
            dirLight = go.AddComponent<Light>();
            dirLight.type = LightType.Directional;
        }
        dirLight.color = new Color(0.85f, 0.88f, 1f);
        dirLight.intensity = 0.6f;
        dirLight.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        dirLight.shadows = LightShadows.Soft;
    }

    // ── PauseMenu Bootstrap ──────────────────────────────────────────────

    /// <summary>Ensures an AudioListener exists in the scene.</summary>
    private void EnsureAudioListener()
    {
        if (FindObjectOfType<AudioListener>() == null)
        {
            Camera cam = Camera.main;
            if (cam != null)
                cam.gameObject.AddComponent<AudioListener>();
            else
                gameObject.AddComponent<AudioListener>();
        }
    }

    /// <summary>Ensures a UIStickCursor exists in the scene (HubScene doesn't have one by default).</summary>
    private void EnsureStickCursor()
    {
        if (FindObjectOfType<UIStickCursor>() != null) return;

        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
            es.gameObject.AddComponent<UIStickCursor>();
    }

    /// <summary>
    /// Creates a TimeScaleMeter in HubScene, matching MainScene's setup.
    /// Starts HIDDEN — the TimeScaleIntroModal reveals it with a glow
    /// when the player reaches that part of the tutorial.
    /// </summary>
    private void EnsureTimeScaleMeter()
    {
        if (FindObjectOfType<TimeScaleMeter>(true) != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

        // Create HUD parent if needed
        Transform hud = canvas.transform.Find("HUD");
        if (hud == null)
        {
            GameObject hudGO = new GameObject("HUD");
            hudGO.transform.SetParent(canvas.transform, false);
            RectTransform hudRT = hudGO.AddComponent<RectTransform>();
            hudRT.anchorMin = Vector2.zero;
            hudRT.anchorMax = Vector2.one;
            hudRT.offsetMin = Vector2.zero;
            hudRT.offsetMax = Vector2.zero;
            hud = hudGO.transform;
        }

        GameObject meterGO = new GameObject("TimeScaleMeter");
        meterGO.transform.SetParent(hud, false);
        RectTransform rt = meterGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -8f);
        rt.sizeDelta = new Vector2(440f, 52f);

        TimeScaleMeter meter = meterGO.AddComponent<TimeScaleMeter>();

        // Wire into TimeScaleLogic
        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.meter = meter;

        // Start hidden — TimeScaleIntroModal will reveal it
        meterGO.SetActive(false);
    }

    /// <summary>
    /// Replaces the player's mesh with the real hourglass model from Citadel
    /// if the player is currently using a placeholder (cube/capsule).
    /// </summary>
    private void EnsurePlayerModel()
    {
        if (playerTransform == null) return;

        MeshFilter mf = playerTransform.GetComponent<MeshFilter>();
        if (mf == null) return;

        // Load the hourglass mesh used in Citadel
        Mesh hourglassMesh = null;
        Material hourglassMat = null;

#if UNITY_EDITOR
        GameObject fbx = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Chanai's Assets/placeholder hourglass.fbx");
        if (fbx != null)
        {
            MeshFilter fbxMF = fbx.GetComponentInChildren<MeshFilter>();
            if (fbxMF != null) hourglassMesh = fbxMF.sharedMesh;
        }
        hourglassMat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Kayla's Assets/pocket watch/pocketWatchGlass.mat");
#endif

        if (hourglassMesh != null)
            mf.sharedMesh = hourglassMesh;

        if (hourglassMat != null)
        {
            MeshRenderer mr = playerTransform.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = hourglassMat;
        }
    }

    /// <summary>
    /// If the PauseMenu in this scene has null references (empty shell),
    /// build the full pause menu UI at runtime matching MainScene.
    /// </summary>
    private void BootstrapPauseMenu()
    {
        PauseMenuController pmc = FindObjectOfType<PauseMenuController>(true);
        if (pmc == null) return;

        // Check if pausePanel is already wired up
        var field = typeof(PauseMenuController).GetField("pausePanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && field.GetValue(pmc) != null) return;

        // Build the full PauseMenu UI
        HubPauseMenuBuilder.Build(pmc);
    }
}
