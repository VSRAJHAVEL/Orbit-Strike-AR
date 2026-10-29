using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Data
{
    [CreateAssetMenu(fileName = "NewLevelData", menuName = "OrbitStrike/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Level Identity")]
        public int levelNumber = 1;
        public string levelTitle;
        public WorldConfig worldConfig;

        [Header("Target & Layout")]
        public TargetTier targetTier = TargetTier.Bullseye100;
        public Vector3 targetLocalPosition = new Vector3(0, 0, 5.0f);
        public bool hasObstacle = false;
        public Vector3 obstacleLocalPosition = new Vector3(0, 0, 2.5f);
        public Vector3 obstacleScale = new Vector3(1.5f, 1.0f, 0.4f);

        [Header("Environmental Hazards")]
        public Vector3 windDirection = Vector3.right;
        public float windSpeed = 0f;
        public float customGravity = -9.81f;

        [Header("Star Thresholds")]
        public int parRocketsFor3Stars = 1;
        public int parRocketsFor2Stars = 2;
    }
}

