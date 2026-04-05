using UnityEngine;

namespace TrialsOfKairos.UI
{
    /// <summary>ScriptableObject that holds metadata for a single trial level.</summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "Trials of Kairos/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int    levelIndex;
        public string trialName;
        public string chapterName;

        [Header("Mechanics")]
        public string timeMechanic;
        public string hazardDescription;

        [Header("Progress")]
        public bool   isUnlocked      = false;
        public bool   isCompleted     = false;
        public bool   isBossLevel     = false;
        public int    completionStars = 0;
        public float  bestTimeSeconds = 0f;

        [Header("Boss")]
        public BossVariant bossVariant = BossVariant.A;
        public string      bossName;
    }
}
