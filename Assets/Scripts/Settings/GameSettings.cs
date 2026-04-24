using System;
using UnityEngine;

/// <summary>
/// Centralized game settings persisted via PlayerPrefs.
/// Each property auto-saves on write. Call <see cref="ApplyAll"/>
/// on startup to push saved values into Unity systems.
/// </summary>
public static class GameSettings
{
    /// <summary>Applies video settings on game startup.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnStartup()
    {
        ApplyVideo();
    }

    // ── PlayerPrefs keys ────────────────────────────────────────────────
    private const string KEY_MASTER_VOL         = "opt_masterVol";
    private const string KEY_MUSIC_VOL          = "opt_musicVol";
    private const string KEY_SFX_VOL            = "opt_sfxVol";
    private const string KEY_MOUSE_SENS         = "opt_mouseSens";
    private const string KEY_STICK_SENS         = "opt_stickSens";
    private const string KEY_LEFT_STICK_DEAD    = "opt_leftStickDead";
    private const string KEY_RIGHT_STICK_DEAD   = "opt_rightStickDead";
    private const string KEY_INVERT_Y           = "opt_invertY";
    private const string KEY_RESOLUTION_IDX     = "opt_resIdx";
    private const string KEY_DISPLAY_MODE       = "opt_dispMode";
    private const string KEY_VSYNC              = "opt_vsync";
    private const string KEY_QUALITY            = "opt_quality";

    // ── Defaults ────────────────────────────────────────────────────────
    public const float DEFAULT_MASTER_VOL          = 1f;
    public const float DEFAULT_MUSIC_VOL           = 0.5f;
    public const float DEFAULT_SFX_VOL             = 1f;
    public const float DEFAULT_MOUSE_SENS          = 2f;
    public const float DEFAULT_STICK_SENS          = 120f;
    public const float DEFAULT_LEFT_STICK_DEADZONE = 0.15f;
    public const float DEFAULT_RIGHT_STICK_DEADZONE = 0.15f;
    public const bool  DEFAULT_INVERT_Y            = false;
    public const int   DEFAULT_DISPLAY_MODE        = 1; // 0=Windowed, 1=Fullscreen, 2=Borderless
    public const int   DEFAULT_VSYNC               = 1;

    // ── Ranges ──────────────────────────────────────────────────────────
    public const float MOUSE_SENS_MIN = 0.1f;
    public const float MOUSE_SENS_MAX = 10f;
    public const float STICK_SENS_MIN = 20f;
    public const float STICK_SENS_MAX = 300f;
    public const float STICK_DEAD_MIN = 0.05f;
    public const float STICK_DEAD_MAX = 0.5f;

    // ── Events ──────────────────────────────────────────────────────────

    /// <summary>Fires when any audio volume setting changes.</summary>
    public static event Action OnAudioChanged;

    /// <summary>Fires when any camera/input setting changes.</summary>
    public static event Action OnInputChanged;

    // ── Audio ────────────────────────────────────────────────────────────

    /// <summary>Master volume multiplier (0-1).</summary>
    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MASTER_VOL, DEFAULT_MASTER_VOL);
        set { PlayerPrefs.SetFloat(KEY_MASTER_VOL, Mathf.Clamp01(value)); OnAudioChanged?.Invoke(); }
    }

    /// <summary>Music volume multiplier (0-1).</summary>
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(KEY_MUSIC_VOL, DEFAULT_MUSIC_VOL);
        set { PlayerPrefs.SetFloat(KEY_MUSIC_VOL, Mathf.Clamp01(value)); OnAudioChanged?.Invoke(); }
    }

    /// <summary>SFX volume multiplier (0-1).</summary>
    public static float SFXVolume
    {
        get => PlayerPrefs.GetFloat(KEY_SFX_VOL, DEFAULT_SFX_VOL);
        set { PlayerPrefs.SetFloat(KEY_SFX_VOL, Mathf.Clamp01(value)); OnAudioChanged?.Invoke(); }
    }

    // ── Camera / Input ──────────────────────────────────────────────────

    /// <summary>Mouse look sensitivity.</summary>
    public static float MouseSensitivity
    {
        get => PlayerPrefs.GetFloat(KEY_MOUSE_SENS, DEFAULT_MOUSE_SENS);
        set { PlayerPrefs.SetFloat(KEY_MOUSE_SENS, Mathf.Clamp(value, MOUSE_SENS_MIN, MOUSE_SENS_MAX)); OnInputChanged?.Invoke(); }
    }

    /// <summary>Controller right stick look sensitivity (degrees/sec).</summary>
    public static float StickSensitivity
    {
        get => PlayerPrefs.GetFloat(KEY_STICK_SENS, DEFAULT_STICK_SENS);
        set { PlayerPrefs.SetFloat(KEY_STICK_SENS, Mathf.Clamp(value, STICK_SENS_MIN, STICK_SENS_MAX)); OnInputChanged?.Invoke(); }
    }

    /// <summary>Left stick deadzone radius (movement).</summary>
    public static float LeftStickDeadzone
    {
        get => PlayerPrefs.GetFloat(KEY_LEFT_STICK_DEAD, DEFAULT_LEFT_STICK_DEADZONE);
        set { PlayerPrefs.SetFloat(KEY_LEFT_STICK_DEAD, Mathf.Clamp(value, STICK_DEAD_MIN, STICK_DEAD_MAX)); OnInputChanged?.Invoke(); }
    }

    /// <summary>Right stick deadzone radius (camera).</summary>
    public static float RightStickDeadzone
    {
        get => PlayerPrefs.GetFloat(KEY_RIGHT_STICK_DEAD, DEFAULT_RIGHT_STICK_DEADZONE);
        set { PlayerPrefs.SetFloat(KEY_RIGHT_STICK_DEAD, Mathf.Clamp(value, STICK_DEAD_MIN, STICK_DEAD_MAX)); OnInputChanged?.Invoke(); }
    }

    /// <summary>Backward-compatible alias for right stick deadzone (used by CameraFollow).</summary>
    public static float StickDeadzone
    {
        get => RightStickDeadzone;
        set => RightStickDeadzone = value;
    }

    /// <summary>Invert vertical camera look.</summary>
    public static bool InvertYAxis
    {
        get => PlayerPrefs.GetInt(KEY_INVERT_Y, DEFAULT_INVERT_Y ? 1 : 0) == 1;
        set { PlayerPrefs.SetInt(KEY_INVERT_Y, value ? 1 : 0); OnInputChanged?.Invoke(); }
    }

    // ── Video ────────────────────────────────────────────────────────────

    /// <summary>Index into the filtered resolution list.</summary>
    public static int ResolutionIndex
    {
        get => PlayerPrefs.GetInt(KEY_RESOLUTION_IDX, -1);
        set => PlayerPrefs.SetInt(KEY_RESOLUTION_IDX, value);
    }

    /// <summary>Display mode: 0=Windowed, 1=Fullscreen, 2=Borderless.</summary>
    public static int DisplayMode
    {
        get => PlayerPrefs.GetInt(KEY_DISPLAY_MODE, DEFAULT_DISPLAY_MODE);
        set => PlayerPrefs.SetInt(KEY_DISPLAY_MODE, Mathf.Clamp(value, 0, 2));
    }

    /// <summary>VSync count (0=off, 1=on).</summary>
    public static int VSync
    {
        get => PlayerPrefs.GetInt(KEY_VSYNC, DEFAULT_VSYNC);
        set => PlayerPrefs.SetInt(KEY_VSYNC, Mathf.Clamp(value, 0, 1));
    }

    /// <summary>Quality level index.</summary>
    public static int QualityLevel
    {
        get => PlayerPrefs.GetInt(KEY_QUALITY, QualitySettings.GetQualityLevel());
        set => PlayerPrefs.SetInt(KEY_QUALITY, Mathf.Clamp(value, 0, QualitySettings.names.Length - 1));
    }

    // ── Display mode labels ─────────────────────────────────────────────

    private static readonly string[] DISPLAY_MODE_LABELS = { "WINDOWED", "FULLSCREEN", "BORDERLESS" };

    /// <summary>Returns the display name for a display mode index.</summary>
    public static string GetDisplayModeLabel(int mode)
    {
        if (mode >= 0 && mode < DISPLAY_MODE_LABELS.Length) return DISPLAY_MODE_LABELS[mode];
        return "UNKNOWN";
    }

    /// <summary>Number of available display modes.</summary>
    public static int DisplayModeCount => DISPLAY_MODE_LABELS.Length;

    // ── Apply ────────────────────────────────────────────────────────────

    /// <summary>Pushes all saved settings into Unity systems. Call on startup.</summary>
    public static void ApplyAll()
    {
        ApplyAudio();
        ApplyVideo();
    }

    /// <summary>Applies audio volume settings to SoundManager.</summary>
    public static void ApplyAudio()
    {
        if (SoundManager.Instance == null) return;
        SoundManager.Instance.SetVolumes(MasterVolume, MusicVolume, SFXVolume);
    }

    /// <summary>Applies video settings (resolution, fullscreen, vsync, quality).</summary>
    public static void ApplyVideo()
    {
        QualitySettings.vSyncCount = VSync;

        int q = QualityLevel;
        if (q >= 0 && q < QualitySettings.names.Length)
            QualitySettings.SetQualityLevel(q, true);

        ApplyResolution();
    }

    /// <summary>Applies the saved resolution and display mode.</summary>
    public static void ApplyResolution()
    {
        Resolution[] available = GetFilteredResolutions();
        int idx = ResolutionIndex;
        if (idx < 0 || idx >= available.Length)
            idx = available.Length - 1;

        if (idx < 0) return;

        Resolution res = available[idx];
        FullScreenMode fsMode;
        switch (DisplayMode)
        {
            case 0:  fsMode = FullScreenMode.Windowed; break;
            case 2:  fsMode = FullScreenMode.FullScreenWindow; break;
            default: fsMode = FullScreenMode.ExclusiveFullScreen; break;
        }

        Screen.SetResolution(res.width, res.height, fsMode);
    }

    /// <summary>Returns deduplicated resolutions sorted ascending.</summary>
    public static Resolution[] GetFilteredResolutions()
    {
        Resolution[] all = Screen.resolutions;
        var unique = new System.Collections.Generic.List<Resolution>();
        var seen = new System.Collections.Generic.HashSet<string>();

        foreach (Resolution r in all)
        {
            string key = $"{r.width}x{r.height}";
            if (seen.Add(key))
                unique.Add(r);
        }

        return unique.ToArray();
    }

    /// <summary>Saves all pending PlayerPrefs to disk.</summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    /// <summary>Resets all settings to defaults.</summary>
    public static void ResetToDefaults()
    {
        MasterVolume       = DEFAULT_MASTER_VOL;
        MusicVolume        = DEFAULT_MUSIC_VOL;
        SFXVolume          = DEFAULT_SFX_VOL;
        MouseSensitivity   = DEFAULT_MOUSE_SENS;
        StickSensitivity   = DEFAULT_STICK_SENS;
        LeftStickDeadzone  = DEFAULT_LEFT_STICK_DEADZONE;
        RightStickDeadzone = DEFAULT_RIGHT_STICK_DEADZONE;
        InvertYAxis        = DEFAULT_INVERT_Y;
        DisplayMode        = DEFAULT_DISPLAY_MODE;
        VSync              = DEFAULT_VSYNC;
        QualityLevel       = QualitySettings.GetQualityLevel();
        ResolutionIndex    = -1;
        Save();
        ApplyAll();
    }
}
