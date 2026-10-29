#pragma warning disable 0618
using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.Rocket;
using OrbitStrike.AR;

namespace OrbitStrike.Systems
{
    /// <summary>
    /// Coordinates camera framing across game states:
    /// - Overview mode during AR placement and Aiming
    /// - Dynamic chase cam tracking the rocket during ballistic flight
    /// - Smooth slow-motion transition upon target impact
    /// </summary>
    public class CinemachineRocketFollower : MonoBehaviour
    {
        public static CinemachineRocketFollower Instance { get; private set; }

        [Header("Cinemachine Cameras")]
        [SerializeField] private Unity.Cinemachine.CinemachineVirtualCamera padOverviewCam;
        [SerializeField] private Unity.Cinemachine.CinemachineVirtualCamera rocketFollowCam;

        [Header("Follow Settings")]
        [SerializeField] private Vector3 followOffset = new Vector3(0, 1.2f, -3.5f);

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
            EventBus.OnGameStateChanged += HandleGameStateChanged;
            EventBus.OnRocketLiftoff += HandleRocketLiftoff;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
            EventBus.OnRocketLiftoff -= HandleRocketLiftoff;
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            switch (newState)
            {
                case GameState.PadPlacement:
                case GameState.Aiming:
                case GameState.Countdown:
                    SwitchToPadView();
                    break;
                case GameState.FlightTracking:
                    SwitchToFollowView();
                    break;
            }
        }

        private void HandleRocketLiftoff(Vector3 launchPos, Vector3 velocity)
        {
            RocketController rocket = RocketSpawner.Instance?.CurrentRocket;
            if (rocket != null && rocketFollowCam != null)
            {
                rocketFollowCam.Follow = rocket.transform;
                rocketFollowCam.LookAt = rocket.transform;
            }

            SwitchToFollowView();
        }

        public void SwitchToPadView()
        {
            if (padOverviewCam != null) padOverviewCam.Priority = 20;
            if (rocketFollowCam != null) rocketFollowCam.Priority = 10;
        }

        public void SwitchToFollowView()
        {
            if (rocketFollowCam != null) rocketFollowCam.Priority = 20;
            if (padOverviewCam != null) padOverviewCam.Priority = 10;
        }
    }
}

