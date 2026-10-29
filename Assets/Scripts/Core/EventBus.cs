using System;
using UnityEngine;

namespace OrbitStrike.Core
{
    /// <summary>
    /// Global decoupled event bus using C# Actions to maintain clean architectural boundaries.
    /// Eliminates tight couplings between AR, Physics, UI, Audio, and Meta systems.
    /// </summary>
    public static class EventBus
    {
        // Game State Events
        public static event Action<GameState, GameState> OnGameStateChanged;
        public static void TriggerGameStateChanged(GameState newState, GameState oldState) => OnGameStateChanged?.Invoke(newState, oldState);

        // AR Placement & Plane Events
        public static event Action<bool> OnPlaneDetected;
        public static void TriggerPlaneDetected(bool detected) => OnPlaneDetected?.Invoke(detected);

        public static event Action<Vector3, Quaternion> OnPadPlaced;
        public static void TriggerPadPlaced(Vector3 pos, Quaternion rot) => OnPadPlaced?.Invoke(pos, rot);

        public static event Action<float> OnPlayspaceScaled;
        public static void TriggerPlayspaceScaled(float scale) => OnPlayspaceScaled?.Invoke(scale);

        // Rocket & Aiming Events
        public static event Action<RocketType> OnRocketSelected;
        public static void TriggerRocketSelected(RocketType type) => OnRocketSelected?.Invoke(type);

        public static event Action<float, float, float> OnAimParametersChanged;
        public static void TriggerAimParametersChanged(float force, float angle, float yaw) => OnAimParametersChanged?.Invoke(force, angle, yaw);

        // Launch & Flight Events
        public static event Action<float> OnLaunchCountdownTick;
        public static void TriggerLaunchCountdownTick(float secondsRemaining) => OnLaunchCountdownTick?.Invoke(secondsRemaining);

        public static event Action<Vector3, Vector3> OnRocketLiftoff;
        public static void TriggerRocketLiftoff(Vector3 launchPos, Vector3 initialVelocity) => OnRocketLiftoff?.Invoke(launchPos, initialVelocity);

        // Collision & Scoring Events
        public static event Action<TargetTier, int, Vector3, Vector3> OnTargetHit;
        public static void TriggerTargetHit(TargetTier tier, int points, Vector3 hitPoint, Vector3 normal) => OnTargetHit?.Invoke(tier, points, hitPoint, normal);

        public static event Action<Vector3, Vector3> OnRocketMiss;
        public static void TriggerRocketMiss(Vector3 impactPoint, Vector3 normal) => OnRocketMiss?.Invoke(impactPoint, normal);

        // Environment Events
        public static event Action<Vector3, float> OnWindChanged;
        public static void TriggerWindChanged(Vector3 windDirection, float speed) => OnWindChanged?.Invoke(windDirection, speed);

        public static event Action<float> OnGravityChanged;
        public static void TriggerGravityChanged(float gravityMultiplier) => OnGravityChanged?.Invoke(gravityMultiplier);

        // Meta Economy & Stars Events
        public static event Action<int, int> OnCoinsChanged;
        public static void TriggerCoinsChanged(int currentCoins, int delta) => OnCoinsChanged?.Invoke(currentCoins, delta);

        public static event Action<int, int> OnScoreUpdated;
        public static void TriggerScoreUpdated(int currentScore, int delta) => OnScoreUpdated?.Invoke(currentScore, delta);

        public static event Action<int, int> OnLevelCompleted;
        public static void TriggerLevelCompleted(int levelIndex, int starsEarned) => OnLevelCompleted?.Invoke(levelIndex, starsEarned);

        public static event Action<PowerUpType> OnPowerUpActivated;
        public static void TriggerPowerUpActivated(PowerUpType powerUp) => OnPowerUpActivated?.Invoke(powerUp);
    }
}

