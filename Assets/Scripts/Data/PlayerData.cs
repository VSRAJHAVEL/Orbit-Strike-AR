using System;
using System.Collections.Generic;
using UnityEngine;

namespace OrbitStrike.Data
{
    [Serializable]
    public struct LevelStarEntry
    {
        public int levelIndex;
        public int stars;

        public LevelStarEntry(int level, int starCount)
        {
            levelIndex = level;
            stars = starCount;
        }
    }

    [Serializable]
    public class PlayerData
    {
        public int coins = 250;
        public int highestUnlockedLevel = 1;
        [SerializeField] public List<LevelStarEntry> levelStarsList = new List<LevelStarEntry>();
        [SerializeField] public List<string> unlockedRockets = new List<string> { "Basic" };

        // Upgrades
        public int thrustUpgradeLevel = 0;
        public int blastUpgradeLevel = 0;
        public int fuelUpgradeLevel = 0;

        // Settings
        public float masterVolume = 1.0f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 1.0f;
        public bool hapticsEnabled = true;
        public bool isLeftHandedUI = false;
        public int graphicsQualityIndex = 2; // High

        public int GetLevelStars(int level)
        {
            for (int i = 0; i < levelStarsList.Count; i++)
            {
                if (levelStarsList[i].levelIndex == level) return levelStarsList[i].stars;
            }
            return 0;
        }

        public void SetLevelStars(int level, int stars)
        {
            for (int i = 0; i < levelStarsList.Count; i++)
            {
                if (levelStarsList[i].levelIndex == level)
                {
                    if (stars > levelStarsList[i].stars)
                    {
                        levelStarsList[i] = new LevelStarEntry(level, stars);
                    }
                    return;
                }
            }
            levelStarsList.Add(new LevelStarEntry(level, stars));
        }
    }
}
