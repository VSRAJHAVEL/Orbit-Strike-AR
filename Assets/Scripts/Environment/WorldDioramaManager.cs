using System.Collections.Generic;
using UnityEngine;
using OrbitStrike.AR;
using OrbitStrike.Core;
using OrbitStrike.Data;
using OrbitStrike.Targets;

namespace OrbitStrike.Environment
{
    /// <summary>
    /// Spawns and arranges the AR tabletop diorama, targets, obstacles, and environmental conditions
    /// for each of the 30 campaign levels across the 5 worlds.
    /// </summary>
    public class WorldDioramaManager : MonoBehaviour
    {
        public static WorldDioramaManager Instance { get; private set; }

        [Header("Target Prefabs")]
        [SerializeField] private BullseyeTarget bullseyePrefab;
        [SerializeField] private ShieldedTarget shieldedPrefab;
        [SerializeField] private MovingTarget movingPrefab;
        [SerializeField] private BossTarget bossPrefab;

        [Header("Obstacle & Scenery Prefabs")]
        [SerializeField] private GameObject defaultObstaclePrefab;
        [SerializeField] private GameObject[] worldGroundPlates; // Desert, Arctic, City, Jungle, Moon

        [Header("Runtime Spawned Content")]
        private GameObject _activeDioramaContainer;
        private TargetBase _activeTarget;
        private GameObject _activeObstacle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            EventBus.OnPadPlaced += HandlePadPlaced;
            EventBus.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnPadPlaced -= HandlePadPlaced;
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandlePadPlaced(Vector3 pos, Quaternion rot)
        {
            LoadCurrentLevelDiorama();
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Aiming && _activeTarget == null)
            {
                LoadCurrentLevelDiorama();
            }
        }

        public void LoadLevel(LevelData levelData)
        {
            if (levelData == null) return;

            // 1. Setup Environmental Wind & Gravity
            WindSystem.Instance?.SetWind(levelData.windDirection, levelData.windSpeed);
            GravityController.Instance?.SetGravity(levelData.customGravity);

            // 2. Clear old scenery
            ClearDiorama();

            LaunchPadController pad = ARPlacementManager.Instance?.ActiveLaunchPad;
            Transform parentAnchor = pad != null ? pad.transform.parent : transform;

            _activeDioramaContainer = new GameObject($"Diorama_Level_{levelData.levelNumber}");
            _activeDioramaContainer.transform.SetParent(parentAnchor, false);
            _activeDioramaContainer.transform.localPosition = Vector3.zero;
            _activeDioramaContainer.transform.localRotation = Quaternion.identity;

            // 3. Spawn Target based on Tier
            TargetBase targetPrefabToSpawn = levelData.targetTier switch
            {
                TargetTier.Shield => shieldedPrefab != null ? shieldedPrefab : bullseyePrefab,
                TargetTier.MovingDrone => movingPrefab != null ? movingPrefab : bullseyePrefab,
                TargetTier.BossCore => bossPrefab != null ? bossPrefab : bullseyePrefab,
                _ => bullseyePrefab
            };

            if (targetPrefabToSpawn != null)
            {
                _activeTarget = Instantiate(targetPrefabToSpawn, _activeDioramaContainer.transform);
                _activeTarget.transform.localPosition = levelData.targetLocalPosition;
                _activeTarget.transform.localRotation = Quaternion.identity;
            }
            else
            {
                GameObject targetGo = ProceduralAssetFactory.CreateProceduralBullseyeTarget();
                targetGo.transform.SetParent(_activeDioramaContainer.transform, false);
                targetGo.transform.localPosition = levelData.targetLocalPosition;
                _activeTarget = targetGo.AddComponent<BullseyeTarget>();
            }

            // 4. Spawn Obstacle if required
            if (levelData.hasObstacle && defaultObstaclePrefab != null)
            {
                _activeObstacle = Instantiate(defaultObstaclePrefab, _activeDioramaContainer.transform);
                _activeObstacle.transform.localPosition = levelData.obstacleLocalPosition;
                _activeObstacle.transform.localScale = levelData.obstacleScale;
            }
        }

        public void LoadCurrentLevelDiorama()
        {
            int levelIndex = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 1;
            LevelData level = LevelDatabase.GetLevel(levelIndex);
            LoadLevel(level);
        }

        public void ClearDiorama()
        {
            if (_activeDioramaContainer != null)
            {
                Destroy(_activeDioramaContainer);
                _activeDioramaContainer = null;
                _activeTarget = null;
                _activeObstacle = null;
            }
        }
    }
}

