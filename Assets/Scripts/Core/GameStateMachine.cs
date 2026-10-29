using UnityEngine;

namespace OrbitStrike.Core
{
    /// <summary>
    /// Explicit State Machine enforcing valid state transitions for Orbit Strike AR.
    /// </summary>
    public class GameStateMachine
    {
        public GameState CurrentState { get; private set; } = GameState.None;

        public bool CanTransitionTo(GameState next)
        {
            if (CurrentState == next) return false;

            return (CurrentState, next) switch
            {
                (GameState.None, GameState.Menu) => true,
                (GameState.None, GameState.SurfaceScan) => true,
                (GameState.None, GameState.Aiming) => true,
                (GameState.Menu, GameState.SurfaceScan) => true,
                (GameState.Menu, GameState.Aiming) => true,
                (GameState.Menu, GameState.Shop) => true,
                (GameState.Menu, GameState.Settings) => true,
                (GameState.Shop, GameState.Menu) => true,
                (GameState.Settings, GameState.Menu) => true,

                // AR Placement Flow
                (GameState.SurfaceScan, GameState.PadPlacement) => true,
                (GameState.PadPlacement, GameState.Aiming) => true,
                (GameState.PadPlacement, GameState.SurfaceScan) => true, // Relocate pad

                // Gameplay Loop
                (GameState.Aiming, GameState.Countdown) => true,
                (GameState.Countdown, GameState.Launching) => true,
                (GameState.Countdown, GameState.Aiming) => true, // Cancel
                (GameState.Launching, GameState.FlightTracking) => true,
                (GameState.FlightTracking, GameState.TargetHit) => true,
                (GameState.FlightTracking, GameState.Miss) => true,

                // Post-flight resolution
                (GameState.TargetHit, GameState.LevelComplete) => true,
                (GameState.TargetHit, GameState.Aiming) => true, // Multi-target levels
                (GameState.Miss, GameState.Aiming) => true, // Retry with next rocket
                (GameState.Miss, GameState.GameOver) => true, // Out of rockets

                // Level transitions
                (GameState.LevelComplete, GameState.Aiming) => true,
                (GameState.LevelComplete, GameState.Menu) => true,
                (GameState.GameOver, GameState.Aiming) => true,
                (GameState.GameOver, GameState.Menu) => true,

                // Fallbacks to Menu
                (_, GameState.Menu) => true,
                _ => false
            };
        }

        public bool TransitionTo(GameState nextState)
        {
            if (!CanTransitionTo(nextState))
            {
                Debug.LogWarning($"[GameStateMachine] Invalid transition requested: {CurrentState} -> {nextState}");
                return false;
            }

            GameState oldState = CurrentState;
            CurrentState = nextState;
            Debug.Log($"[GameStateMachine] State Transition: {oldState} -> {CurrentState}");
            EventBus.TriggerGameStateChanged(nextState, oldState);
            return true;
        }
    }
}

