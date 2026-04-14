using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Session-only best time tracker. Stores per-level best completion times in memory.
/// All data resets when the game is restarted — no PlayerPrefs, no file I/O.
/// </summary>
public static class BestTimeTracker
{
    private static readonly Dictionary<string, float> _bestTimes = new Dictionary<string, float>();

    /// <summary>Level keys matching the scene mapping.</summary>
    public const string KEY_CITADEL = "Citadel";
    public const string KEY_GARDEN  = "Garden";
    public const string KEY_CLOCK   = "Clock";

    /// <summary>Records a completion time. Only stores it if it beats the current best.</summary>
    public static void Record(string levelKey, float timeSeconds)
    {
        if (string.IsNullOrEmpty(levelKey) || timeSeconds <= 0f) return;

        if (_bestTimes.TryGetValue(levelKey, out float existing))
        {
            if (timeSeconds < existing)
                _bestTimes[levelKey] = timeSeconds;
        }
        else
        {
            _bestTimes[levelKey] = timeSeconds;
        }
    }

    /// <summary>Returns the best time for a level, or -1 if none recorded.</summary>
    public static float Get(string levelKey)
    {
        if (!string.IsNullOrEmpty(levelKey) && _bestTimes.TryGetValue(levelKey, out float t))
            return t;
        return -1f;
    }

    /// <summary>Returns true if any time has been recorded for this level.</summary>
    public static bool Has(string levelKey)
    {
        return !string.IsNullOrEmpty(levelKey) && _bestTimes.ContainsKey(levelKey);
    }

    /// <summary>Marks a level as completed (records a sentinel time if no time tracked).</summary>
    public static void MarkComplete(string levelKey)
    {
        if (!Has(levelKey))
            _bestTimes[levelKey] = float.MaxValue;
    }

    /// <summary>Returns the level key for a given scene name.</summary>
    public static string KeyForScene(string sceneName)
    {
        switch (sceneName)
        {
            case "MainScene":   return KEY_CITADEL;
            case "GardenScene": return KEY_GARDEN;
            case "ClockScene":  return KEY_CLOCK;
            default:            return null;
        }
    }

    /// <summary>Formats a time in seconds as M:SS.mm.</summary>
    public static string Format(float seconds)
    {
        if (seconds <= 0f || seconds >= float.MaxValue) return "--:--.--";
        int mins = Mathf.FloorToInt(seconds / 60f);
        float secs = seconds - mins * 60f;
        return $"{mins}:{secs:00.00}";
    }

    /// <summary>Clears all recorded times (called on fresh game start if needed).</summary>
    public static void Reset()
    {
        _bestTimes.Clear();
    }
}
