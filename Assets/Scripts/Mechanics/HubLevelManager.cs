using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public bool IsTutorialOpen => howToPlayScreen != null && howToPlayScreen.IsOpen;

    private static readonly Color ACCENT_GOLD    = new Color(0.961f, 0.784f, 0.259f);
    private static readonly Color ACCENT_BLUE    = new Color(0.353f, 0.706f, 0.941f);
    private static readonly Color ACCENT_PURPLE  = new Color(0.608f, 0.365f, 0.898f);
    private static readonly Color GOAL_COLOR     = new Color(1f, 0.843f, 0f);
    private static readonly Color TILE_EDGE_COL  = new Color(0.14f, 0.18f, 0.28f);
    private static readonly Color COSMIC_AMBIENT = new Color(0.04f, 0.05f, 0.08f);

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

        Transform existingTiles = transform.Find("HubTiles");
        if (existingTiles == null || existingTiles.childCount == 0)
            BuildVisualLayout();

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

    private void AttachRuntimeComponents(Transform root)
    {
        foreach (Transform child in root)
        {
            string name = child.name;

            if (name.StartsWith("Tile_"))
            {
                string[] parts = name.Split('_');
                if (parts.Length >= 3 && int.TryParse(parts[1], out int x) && int.TryParse(parts[2], out int z))
                {
                    if (x == 0 && z == 9)  AddTutorialTrigger(child.gameObject, TutorialTilePopup.TileType.Reverse);
                    if (x == 0 && z == 10) AddTutorialTrigger(child.gameObject, TutorialTilePopup.TileType.Frozen);
                    if (x == 0 && z == 12) AddTutorialTrigger(child.gameObject, TutorialTilePopup.TileType.Forward);

                    if (x == 0 && z == 35 && child.GetComponent<GoalTile>() == null)
                    {
                        child.gameObject.name = "GoalTile";
                        child.gameObject.AddComponent<GoalTile>();
                    }
                }
            }

            if (name == "DemoPlatform" && child.GetComponent<MovingTile>() == null)
            {
                MovingTile mt = child.gameObject.AddComponent<MovingTile>();
                mt.moveDirection = Vector3.forward;
                mt.moveDistance = 2f;
                mt.tickInterval = 1f;
                mt.minTime = -2f;
                mt.maxTime = 2f;
            }

            if (name == "CosmicOrb" && child.GetComponent<HubFloatingOrb>() == null)
                child.gameObject.AddComponent<HubFloatingOrb>();
        }

        Transform existingTrigger = root.Find("TimeScaleIntroTrigger");
        if (existingTrigger == null)
            CreateTimeScaleModalTrigger(root);

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

    public void ShowHowToPlay()
    {
        if (howToPlayScreen == null)
            howToPlayScreen = FindObjectOfType<HowToPlayController>(true);
        if (howToPlayScreen != null)
            howToPlayScreen.Show();
    }

    public void EnterTrial()
    {
        if (ScreenTransitionManager.Instance != null)
            ScreenTransitionManager.Instance.FadeToScene(mainTrialScene);
        else
            SceneManager.LoadScene(mainTrialScene);
    }

    public bool IsFirstVisit() => PlayerPrefs.GetInt(FIRST_VISIT_KEY, 1) == 1;

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

        if (blueGlowMat == null)
            blueGlowMat = MakeTutorialMat(ACCENT_BLUE);

        if (tileMesh == null)
        {
            GameObject fbx = LoadAsset<GameObject>("Assets/Zutzuy's Assets/TileNew.fbx");
            if (fbx != null)
            {
                MeshFilter mf = fbx.GetComponentInChildren<MeshFilter>();
                if (mf != null) tileMesh = mf.sharedMesh;
            }
        }

        if (basePlatformMat == null)  basePlatformMat = MakeFallbackMat(new Color(0.08f, 0.10f, 0.16f));
        if (goldGlowMat == null)      goldGlowMat = MakeTutorialMat(ACCENT_GOLD);
        if (purpleGlowMat == null)    purpleGlowMat = MakeTutorialMat(ACCENT_PURPLE);
        if (goalTileMat == null)      goalTileMat = MakeTutorialMat(GOAL_COLOR);
        if (darkPlatformMat == null)  darkPlatformMat = basePlatformMat;
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

    private void BuildVisualLayout()
    {
        Transform root = new GameObject("HubTiles").transform;
        root.SetParent(transform);

        var tiles = new System.Collections.Generic.Dictionary<(int, int), Material>();

        for (int x = -2; x <= 2; x++)
            for (int z = -2; z <= 2; z++)
                tiles[(x, z)] = basePlatformMat;

        tiles[(-2, -2)] = goldGlowMat;
        tiles[( 2, -2)] = goldGlowMat;
        tiles[(-2,  2)] = goldGlowMat;
        tiles[( 2,  2)] = goldGlowMat;

        for (int z = 3; z <= 8; z++)
        {
            tiles[(0, z)] = basePlatformMat;
            Material edgeMat = z % 2 == 0 ? darkPlatformMat : basePlatformMat;
            tiles[(-1, z)] = edgeMat;
            tiles[( 1, z)] = edgeMat;
        }

        for (int x = -3; x <= 3; x++)
            for (int z = 9; z <= 16; z++)
                tiles[(x, z)] = basePlatformMat;

        Material tutGold   = MakeTutorialMat(ACCENT_GOLD);
        Material tutBlue   = MakeTutorialMat(ACCENT_BLUE);
        Material tutPurple = MakeTutorialMat(ACCENT_PURPLE);

        tiles[(0, 9)]  = tutPurple;
        tiles[(0, 10)] = tutBlue;
        tiles[(0, 11)] = tutBlue;
        tiles[(0, 12)] = tutGold;

        for (int z = 17; z <= 19; z++)
            for (int x = -1; x <= 1; x++)
                tiles[(x, z)] = basePlatformMat;

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

        for (int z = 33; z <= 34; z++)
            for (int x = -1; x <= 1; x++)
                tiles[(x, z)] = basePlatformMat;

        tiles[(0, 35)] = goalTileMat;

        foreach (var kvp in tiles)
        {
            int x = kvp.Key.Item1;
            int z = kvp.Key.Item2;
            CreateTile(root, x, z, kvp.Value);
        }

        CreateMovingDemoPlatformVisual(root);

        MakePillar(root, new Vector3(-4f, 0f, 0f), 0.4f, 4f);
        MakePillar(root, new Vector3(4f, 0f, 0f), 0.4f, 4f);
        MakePillar(root, new Vector3(-5f, 0f, 12.5f), 0.5f, 6f);
        MakePillar(root, new Vector3(5f, 0f, 12.5f), 0.5f, 6f);
        MakePillar(root, new Vector3(-5f, 0f, 25f), 0.5f, 6f);
        MakePillar(root, new Vector3(5f, 0f, 25f), 0.5f, 6f);

        MakeOrb(root, new Vector3(-3f, 5f, 6f), 0.3f, ACCENT_GOLD);
        MakeOrb(root, new Vector3(3f, 7f, 10f), 0.25f, ACCENT_BLUE);
        MakeOrb(root, new Vector3(-4f, 6f, 15f), 0.2f, ACCENT_PURPLE);
        MakeOrb(root, new Vector3(4f, 5f, 22f), 0.25f, ACCENT_GOLD);
        MakeOrb(root, new Vector3(-3f, 7f, 29f), 0.2f, ACCENT_PURPLE);
        MakeOrb(root, new Vector3(0f, 8f, 36f), 0.4f, GOAL_COLOR);
    }

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
            BoxCollider existing = tile.GetComponent<BoxCollider>();
            if (existing != null) SafeDestroy(existing);
        }

        tile.transform.SetParent(parent);
        tile.transform.localPosition = new Vector3(x, 0f, z);
        tile.transform.localScale = new Vector3(1f, 0.2f, 1f);
        tile.transform.localRotation = Quaternion.identity;

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

    private void CreateMovingDemoPlatformVisual(Transform root)
    {
        Material demoMat = MakeTutorialMat(ACCENT_GOLD);
        GameObject platform = CreateTile(root, 0, 24, demoMat);
        platform.name = "DemoPlatform";
    }

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
        Collider c = o.GetComponent<Collider>();
        if (c != null) SafeDestroy(c);
    }

    private static void SafeDestroy(Object obj)
    {
        if (Application.isPlaying)
            Destroy(obj);
        else
            DestroyImmediate(obj);
    }

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

    private void EnsureStickCursor()
    {
        if (FindObjectOfType<UIStickCursor>() != null) return;

        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es != null)
            es.gameObject.AddComponent<UIStickCursor>();
    }

    private void EnsureTimeScaleMeter()
    {
        if (FindObjectOfType<TimeScaleMeter>(true) != null) return;

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return;

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

        if (TimeScaleLogic.Instance != null)
            TimeScaleLogic.Instance.meter = meter;

        meterGO.SetActive(false);
    }

    private void EnsurePlayerModel()
    {
        if (playerTransform == null) return;

        MeshFilter mf = playerTransform.GetComponent<MeshFilter>();
        if (mf == null) return;

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

    private void BootstrapPauseMenu()
    {
        PauseMenuController pmc = FindObjectOfType<PauseMenuController>(true);
        if (pmc == null) return;

        var field = typeof(PauseMenuController).GetField("pausePanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null && field.GetValue(pmc) != null) return;

        HubPauseMenuBuilder.Build(pmc);
    }
}