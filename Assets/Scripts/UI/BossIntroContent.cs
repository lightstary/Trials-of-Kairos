/// <summary>
/// Static content for each boss intro modal.
/// Each boss gets its own set of pages explaining the mechanic,
/// objective, and fail conditions.
/// </summary>
public static class BossIntroContent
{
    /// <summary>Page index where the boss pointer glow should appear during Garden tutorial.</summary>
    public const int GARDEN_POINTER_GLOW_PAGE = 1;

    /// <summary>Intro pages for The Citadel (Boss A) -- tile-fall survival.</summary>
    public static readonly string[] Citadel = new string[]
    {
        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "The arena ahead is your <b>first boss fight</b>.\n\n" +
        "Tiles will light up <color=#00FF4D>green</color> --\n" +
        "those are <b>safe</b>.\n\n" +
        "Every other tile will <color=#FF3300>blink red</color>,\n" +
        "<b>shake</b>, then <b>fall away</b>.\n\n" +
        "Get to a safe tile before the rest drop.",

        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "The tile beneath you will <b>glow</b>\n" +
        "to show if you're safe:\n\n" +
        "<color=#00FF4D>Green glow</color> = you're on a <b>safe tile</b>.\n" +
        "<color=#FF3300>Red glow</color> = you're on a <b>danger tile</b>.\n\n" +
        "Watch for <b>shaking</b> -- it means a tile\n" +
        "is about to fall.",

        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "You must survive <b>3 rounds</b>.\n\n" +
        "Each round, new safe tiles are chosen.\n" +
        "The fall pattern changes every time.\n\n" +
        "While you fight, <b>time keeps moving</b> --\n" +
        "your time scale meter is still active.\n\n" +
        "If the meter hits <b>+10</b> or <b>-10</b>,\n" +
        "you will <b>fail immediately</b>.",

        "<color=#F5C842><size=32>THE CITADEL</size></color>\n\n" +
        "<b>Objective:</b> Survive all 3 rounds.\n\n" +
        "<b>Fail conditions:</b>\n" +
        "  - Fall off the arena\n" +
        "  - Time scale hits +10 or -10\n\n" +
        "Control your orientation carefully.\n" +
        "<b>Good luck.</b>"
    };

    /// <summary>Intro pages for The Garden (Boss B) -- shared time scale with boss pointer.</summary>
    public static readonly string[] Garden = new string[]
    {
        // PAGE 1: What's happening
        "<color=#F5C842><size=32>THE GARDEN</size></color>\n\n" +
        "A <color=#FF2690>pink marker</color> will appear on your Time Scale.\n" +
        "That is the <b>Death Pointer</b>.\n\n" +
        "It moves on its own, pushing toward\n" +
        "<b>+10</b> or <b>-10</b> to kill you.",

        // PAGE 2: How to survive (boss pointer glow active on this page)
        "<color=#F5C842><size=32>THE GARDEN</size></color>\n\n" +
        "To block it, roll the <b>opposite direction</b>.\n\n" +
        "Boss pushes right? Roll <b>upside-down</b> (reverse).\n" +
        "Boss pushes left? Roll <b>upright</b> (forward).\n\n" +
        "Going the <b>same</b> direction makes it <b>faster</b>.\n" +
        "<color=#5BB4F0>Frozen</color> pauses it briefly, then it resumes.",

        // PAGE 3: Win condition
        "<color=#F5C842><size=32>THE GARDEN</size></color>\n\n" +
        "The boss <b>swaps direction</b> every few seconds\n" +
        "and gets <b>faster</b> each time.\n\n" +
        "<b>Survive long enough and you win.</b>\n\n" +
        "Don't let the meter hit the edges.\n" +
        "<b>Good luck.</b>"
    };

    /// <summary>Intro pages for The Clock (Boss C) -- objective-based clock alignment.</summary>
    public static readonly string[] Clock = new string[]
    {
        "<color=#F5C842><size=32>THE CLOCK</size></color>\n\n" +
        "A great clock stands before you.\n" +
        "Its hand has stopped -- you must <b>set it right</b>.\n\n" +
        "<color=#F5C842>Forward</color> (upright) moves the clock hand.\n" +
        "<color=#5BB4F0>Frozen</color> pauses the clock hand.\n" +
        "<color=#9B5DE5>Reverse</color> does nothing.",

        "<color=#F5C842><size=32>THE CLOCK</size></color>\n\n" +
        "You will be given a <color=#33FF66>target time</color>.\n" +
        "Move the clock hand to match it.\n\n" +
        "When you reach the target, a <b>new target</b>\n" +
        "will appear -- each one more demanding.\n\n" +
        "Complete all targets to win.",

        "<color=#F5C842><size=32>THE CLOCK</size></color>\n\n" +
        "The tiles fall -- watch for <b>shaking</b>.\n\n" +
        "The tile beneath you will <b>glow</b>:\n" +
        "<color=#00FF4D>Green</color> = safe, <color=#FF3300>Red</color> = danger.\n\n" +
        "Your <b>time scale meter</b> is still active.\n" +
        "If it hits <b>+10</b> or <b>-10</b>,\n" +
        "you will <b>fail immediately</b>.",

        "<color=#F5C842><size=32>THE CLOCK</size></color>\n\n" +
        "<b>Objective:</b> Rotate the clock hand\nto each <color=#33FF66>target time</color>.\n\n" +
        "<b>Fail conditions:</b>\n" +
        "  - Fall off the arena\n" +
        "  - Time scale hits +10 or -10\n\n" +
        "<b>Good luck.</b>"
    };

    /// <summary>Returns the intro pages for a given boss name.</summary>
    public static string[] GetPages(string bossName)
    {
        switch (bossName)
        {
            case "THE CITADEL": return Citadel;
            case "THE GARDEN":  return Garden;
            case "THE CLOCK":   return Clock;
            default:            return Citadel;
        }
    }
}
