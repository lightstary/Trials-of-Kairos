using UnityEngine;
using System.Collections.Generic;

public static class BestTimeTracker
{
    private static readonly Dictionary<string, float> _bestTimes = new Dictionary<string, float>();

    public const string KEY_CITADEL = "Citadel";
    public const string KEY_GARDEN  = "Garden";
    public const string KEY_CLOCK   = "Clock";

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

    public static float Get(string levelKey)
    {
        if (!string.IsNullOrEmpty(levelKey) && _bestTimes.TryGetValue(levelKey, out float t))
            return t;
        return -1f;
    }

    public static bool Has(string levelKey)
    {
        return !string.IsNullOrEmpty(levelKey) && _bestTimes.ContainsKey(levelKey);
    }

    public static void MarkComplete(string levelKey)
    {
        if (!Has(levelKey))
            _bestTimes[levelKey] = float.MaxValue;
    }

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

    public static string Format(float seconds)
    {
        if (seconds <= 0f || seconds >= float.MaxValue) return "--:--.--";
        int mins = Mathf.FloorToInt(seconds / 60f);
        float secs = seconds - mins * 60f;
        return $"{mins}:{secs:00.00}";
    }

    public static void Reset()
    {
        _bestTimes.Clear();
    }
}