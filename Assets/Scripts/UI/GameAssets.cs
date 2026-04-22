using UnityEngine;
using TMPro;

/// <summary>
/// ScriptableObject that holds references to all runtime UI assets (fonts, icons).
/// Lives in Assets/Resources/ so it can be loaded via Resources.Load in builds.
/// This ensures all referenced assets are included in the build.
/// </summary>
[CreateAssetMenu(fileName = "GameAssets", menuName = "Trials of Kairos/Game Assets")]
public class GameAssets : ScriptableObject
{
    private static GameAssets _instance;

    [Header("Cinzel Fonts")]
    public TMP_FontAsset cinzelRegular;
    public TMP_FontAsset cinzelBold;
    public TMP_FontAsset cinzelBlack;

    [Header("Controller Icons")]
    public Sprite iconCtrlA;
    public Sprite iconCtrlB;
    public Sprite iconCtrlL;
    public Sprite iconCtrlR;
    public Sprite iconCtrlPause;

    [Header("Keyboard Icons")]
    public Sprite iconKeyW;
    public Sprite iconKeyA;
    public Sprite iconKeyS;
    public Sprite iconKeyD;
    public Sprite iconKeyEsc;
    public Sprite iconMouse;
    public Sprite iconMouseLeft;

    /// <summary>
    /// Loads and caches the GameAssets instance from Resources.
    /// </summary>
    public static GameAssets Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<GameAssets>("GameAssets");
                if (_instance == null)
                    Debug.LogError("[GameAssets] GameAssets.asset not found in Resources/ folder.");
            }
            return _instance;
        }
    }
}
