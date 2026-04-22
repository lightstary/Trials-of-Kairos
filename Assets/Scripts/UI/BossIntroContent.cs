/// <summary>
/// Static content for each boss intro modal.
/// Each boss gets its own set of pages explaining the mechanic,
/// objective, and fail conditions.
/// </summary>
public static class BossIntroContent
{
    /// <summary>Intro pages for The Citadel (Boss A) — tile-fall survival.</summary>
    public static readonly string[] Citadel = new string[]
    {
        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "The arena ahead is your <b>first boss fight</b>.\n\n" +
        "Tiles will light up <color=#00FF4D>green</color> \u2014\n" +
        "those are <b>safe</b>.\n\n" +
        "Every other tile will <color=#FF3300>blink red</color>,\n" +
        "then <b>fall away</b>.\n\n" +
        "Get to a safe tile before the rest drop.",

        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "You must survive <b>3 rounds</b>.\n\n" +
        "Each round, new safe tiles are chosen.\n" +
        "The fall pattern changes every time.\n\n" +
        "While you fight, <b>time keeps moving</b> \u2014\n" +
        "your time scale meter is still active.\n\n" +
        "If the meter hits <b>+10</b> or <b>\u201310</b>,\n" +
        "you will <b>fail immediately</b>.",

        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "<b>Objective:</b> Survive all 3 rounds.\n\n" +
        "<b>Fail conditions:</b>\n" +
        "\u2022  Fall off the arena\n" +
        "\u2022  Time scale hits +10 or \u201310\n\n" +
        "Control your orientation carefully.\n" +
        "<b>Good luck.</b>"
    };

    /// <summary>Returns the intro pages for a given boss name.</summary>
    public static string[] GetPages(string bossName)
    {
        switch (bossName)
        {
            case "THE CITADEL": return Citadel;
            default:            return Citadel; // fallback
        }
    }
}
