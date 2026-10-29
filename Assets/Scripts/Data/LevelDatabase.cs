using System.Collections.Generic;
using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Data
{
    /// <summary>
    /// Master repository defining the complete 30-level campaign across all 5 worlds.
    /// Provides programmatic fallbacks if custom ScriptableObject assets are generated at runtime.
    /// </summary>
    public static class LevelDatabase
    {
        private static readonly Dictionary<int, LevelData> CachedLevels = new Dictionary<int, LevelData>();

        public static LevelData GetLevel(int levelNumber)
        {
            levelNumber = Mathf.Clamp(levelNumber, 1, 30);

            if (CachedLevels.TryGetValue(levelNumber, out LevelData cached))
            {
                return cached;
            }

            LevelData data = ScriptableObject.CreateInstance<LevelData>();
            data.levelNumber = levelNumber;

            // Generate progressive parameters across 5 Worlds (6 levels per world)
            int worldIndex = (levelNumber - 1) / 6; // 0 to 4
            int levelInWorld = ((levelNumber - 1) % 6) + 1; // 1 to 6

            switch (worldIndex)
            {
                case 0: // World 1: Mojave Range (Desert) - Earth Gravity, Calm winds
                    data.levelTitle = $"Desert Range {levelInWorld}";
                    data.customGravity = -9.81f;
                    data.windDirection = Vector3.right;
                    data.windSpeed = levelInWorld * 0.5f;
                    data.targetLocalPosition = new Vector3(0, 0, 3.5f + levelInWorld * 0.6f);
                    data.targetTier = TargetTier.Bullseye100;
                    data.hasObstacle = levelInWorld >= 4;
                    data.obstacleLocalPosition = new Vector3(0, 0, 3.0f);
                    break;

                case 1: // World 2: Frostpeak Glacier (Arctic) - Strong Crosswinds & Shields
                    data.levelTitle = $"Frostpeak Pass {levelInWorld}";
                    data.customGravity = -9.81f;
                    data.windDirection = (levelInWorld % 2 == 0) ? Vector3.left : Vector3.right;
                    data.windSpeed = 2.5f + levelInWorld * 0.8f;
                    data.targetLocalPosition = new Vector3((levelInWorld % 2 == 0 ? -1f : 1f) * 0.8f, 0.2f, 4.0f + levelInWorld * 0.5f);
                    data.targetTier = levelInWorld >= 3 ? TargetTier.Shield : TargetTier.Bullseye100;
                    data.hasObstacle = true;
                    data.obstacleLocalPosition = new Vector3(0, 0, 2.8f);
                    break;

                case 2: // World 3: Neo-Metropolis (City) - Moving Drone Targets & High Sky Obstacles
                    data.levelTitle = $"Skyline Corridor {levelInWorld}";
                    data.customGravity = -9.81f;
                    data.windDirection = Vector3.forward + Vector3.right * 0.5f;
                    data.windSpeed = 2.0f + levelInWorld * 0.5f;
                    data.targetLocalPosition = new Vector3(0, 0.5f, 4.5f + levelInWorld * 0.6f);
                    data.targetTier = levelInWorld >= 2 ? TargetTier.MovingDrone : TargetTier.Bullseye100;
                    data.hasObstacle = true;
                    data.obstacleLocalPosition = new Vector3(0, 0, 3.2f);
                    data.obstacleScale = new Vector3(2.0f, 1.4f, 0.5f);
                    break;

                case 3: // World 4: Canopy Deep (Jungle) - Mars Gravity (-3.71), Mountain Obstacles
                    data.levelTitle = $"Canopy Canyon {levelInWorld}";
                    data.customGravity = -3.71f;
                    data.windDirection = Vector3.right + Vector3.back * 0.2f;
                    data.windSpeed = 3.0f + levelInWorld * 0.7f;
                    data.targetLocalPosition = new Vector3(0.5f, 0, 5.0f + levelInWorld * 0.7f);
                    data.targetTier = (levelInWorld % 2 == 0) ? TargetTier.Shield : TargetTier.MovingDrone;
                    data.hasObstacle = true;
                    data.obstacleLocalPosition = new Vector3(0, 0, 3.5f);
                    data.obstacleScale = new Vector3(2.5f, 1.8f, 0.6f);
                    break;

                case 4: // World 5: Lunar Base Alpha (Moon) - Moon Gravity (-1.62), Boss on Stage 30!
                    data.levelTitle = levelInWorld == 6 ? "★ BOSS: Dreadnought Core" : $"Lunar Crater {levelInWorld}";
                    data.customGravity = -1.62f;
                    data.windDirection = Vector3.zero; // Vacuum on moon!
                    data.windSpeed = 0f;
                    data.targetLocalPosition = new Vector3(0, 0.2f, 6.0f + levelInWorld * 0.8f);
                    data.targetTier = levelInWorld == 6 ? TargetTier.BossCore : TargetTier.MovingDrone;
                    data.hasObstacle = levelInWorld >= 2;
                    data.obstacleLocalPosition = new Vector3(0, 0, 4.0f);
                    break;
            }

            CachedLevels[levelNumber] = data;
            return data;
        }
    }
}

