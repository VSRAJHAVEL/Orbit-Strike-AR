using System.Collections.Generic;
using UnityEngine;
using OrbitStrike.AR;
using OrbitStrike.Core;
using OrbitStrike.Data;

namespace OrbitStrike.Rocket
{
    /// <summary>
    /// Spawns and manages active rockets on the launch pad.
    /// Handles rocket switching, build-up materialization, and re-arming for retries.
    /// </summary>
    public class RocketSpawner : MonoBehaviour
    {
        public static RocketSpawner Instance { get; private set; }

        [Header("Archetype Configurations")]
        [SerializeField] private List<RocketData> availableRockets = new List<RocketData>();
        [SerializeField] private RocketController rocketPrefab;

        private RocketController _currentRocketInstance;
        private RocketData _selectedRocketData;

        public RocketController CurrentRocket => _currentRocketInstance;

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
            EventBus.OnRocketSelected += HandleRocketSelected;
            EventBus.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnRocketSelected -= HandleRocketSelected;
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Start()
        {
            if (availableRockets.Count > 0)
            {
                _selectedRocketData = availableRockets[0];
            }
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            // When transitioning to Aiming (either after pad placement or after a retry), spawn a rocket if needed
            if (newState == GameState.Aiming)
            {
                if (_currentRocketInstance == null || _currentRocketInstance.IsLaunched)
                {
                    SpawnRocketOnPad();
                }
            }
        }

        private void HandleRocketSelected(RocketType type)
        {
            RocketData match = availableRockets.Find(r => r.rocketType == type);
            if (match != null)
            {
                _selectedRocketData = match;
                if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Aiming)
                {
                    SpawnRocketOnPad();
                }
            }
        }

        public void SpawnRocketOnPad()
        {
            LaunchPadController pad = ARPlacementManager.Instance?.ActiveLaunchPad;
            if (pad == null || pad.RocketMountPoint == null) return;

            // Clean up existing unlaunched rocket
            if (_currentRocketInstance != null)
            {
                Destroy(_currentRocketInstance.gameObject);
            }

            Transform mount = pad.RocketMountPoint;
            if (rocketPrefab != null)
            {
                _currentRocketInstance = Instantiate(rocketPrefab, mount.position, mount.rotation, mount);
            }
            else
            {
                Color theme = _selectedRocketData != null ? _selectedRocketData.themeColor : Color.cyan;
                GameObject rocketGo = ProceduralAssetFactory.CreateProceduralRocket("Rocket_Procedural", theme);
                rocketGo.transform.SetParent(mount, false);
                rocketGo.transform.localPosition = new Vector3(0, 0.45f, 0);
                rocketGo.transform.localRotation = Quaternion.identity;
                _currentRocketInstance = rocketGo.AddComponent<RocketController>();
            }
            
            if (_selectedRocketData != null)
            {
                _currentRocketInstance.Initialize(_selectedRocketData);
            }

            _currentRocketInstance.PlayBuildUpAnimation();
        }
    }
}

