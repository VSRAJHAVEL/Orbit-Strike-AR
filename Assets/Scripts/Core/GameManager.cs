using System.Collections;
using UnityEngine;
using OrbitStrike.Data;

namespace OrbitStrike.Core
{
    /// <summary>
    /// Central manager controlling game cycle, state transitions, rocket inventory, and level completion.
    /// Follows strict singleton pattern.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State")]
        private GameStateMachine _stateMachine;
        public GameState CurrentState => _stateMachine?.CurrentState ?? GameState.None;

        [Header("Active Level & Session Stats")]
        public int CurrentLevelIndex { get; private set; } = 1;
        public int CurrentScore { get; private set; } = 0;
        public int RocketsRemaining { get; private set; } = 3;
        public int TotalRocketsUsedInLevel { get; private set; } = 0;
        public RocketType ActiveRocketType { get; private set; } = RocketType.Basic;
        public DifficultyLevel ActiveDifficulty { get; private set; } = DifficultyLevel.Standard;

        [Header("Slow-Motion Settings")]
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 1.2f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _stateMachine = new GameStateMachine();
        }

        private void Start()
        {
            _stateMachine.TransitionTo(GameState.Menu);
        }

        private void OnEnable()
        {
            EventBus.OnTargetHit += HandleTargetHit;
            EventBus.OnRocketMiss += HandleRocketMiss;
            EventBus.OnRocketSelected += HandleRocketSelected;
        }

        private void OnDisable()
        {
            EventBus.OnTargetHit -= HandleTargetHit;
            EventBus.OnRocketMiss -= HandleRocketMiss;
            EventBus.OnRocketSelected -= HandleRocketSelected;
        }

        public bool SetState(GameState nextState)
        {
            return _stateMachine.TransitionTo(nextState);
        }

        public void StartSurfaceScan()
        {
            SetState(GameState.SurfaceScan);
        }

        public void StartLevel(int levelIndex)
        {
            CurrentLevelIndex = levelIndex;
            CurrentScore = 0;
            TotalRocketsUsedInLevel = 0;
            RocketsRemaining = 3; // Default 3 rockets per level
            EventBus.TriggerScoreUpdated(0, 0);

            if (_stateMachine.CurrentState != GameState.Aiming && _stateMachine.CurrentState != GameState.PadPlacement)
            {
                SetState(GameState.SurfaceScan);
            }
            else
            {
                SetState(GameState.Aiming);
            }
        }

        public void ConsumeRocket()
        {
            RocketsRemaining = Mathf.Max(0, RocketsRemaining - 1);
            TotalRocketsUsedInLevel++;
        }

        private void HandleRocketSelected(RocketType type)
        {
            ActiveRocketType = type;
        }

        private void HandleTargetHit(TargetTier tier, int points, Vector3 hitPoint, Vector3 normal)
        {
            CurrentScore += points;
            EventBus.TriggerScoreUpdated(CurrentScore, points);
            StartCoroutine(TriggerSlowMotionRoutine());

            SetState(GameState.TargetHit);

            // Compute stars: 3 stars if hit with 1 rocket, 2 stars with 2 rockets, 1 star with 3+
            int stars = TotalRocketsUsedInLevel switch
            {
                <= 1 => 3,
                2 => 2,
                _ => 1
            };

            // Award coins (100 coins per star + score bonus)
            int earnedCoins = (stars * 100) + (points / 2);
            SaveManager.Instance?.AddCoins(earnedCoins);
            SaveManager.Instance?.SaveLevelStars(CurrentLevelIndex, stars);

            StartCoroutine(DelayedLevelCompleteRoutine(stars));
        }

        private void HandleRocketMiss(Vector3 impactPoint, Vector3 normal)
        {
            SetState(GameState.Miss);

            if (RocketsRemaining > 0)
            {
                StartCoroutine(DelayedAimingResetRoutine(1.8f));
            }
            else
            {
                StartCoroutine(DelayedGameOverRoutine(1.8f));
            }
        }

        private IEnumerator TriggerSlowMotionRoutine()
        {
            Time.timeScale = slowMoScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f;
        }

        private IEnumerator DelayedLevelCompleteRoutine(int stars)
        {
            yield return new WaitForSeconds(1.5f);
            SetState(GameState.LevelComplete);
            EventBus.TriggerLevelCompleted(CurrentLevelIndex, stars);
        }

        private IEnumerator DelayedAimingResetRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetState(GameState.Aiming);
        }

        private IEnumerator DelayedGameOverRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetState(GameState.GameOver);
        }

        public void NextLevel()
        {
            StartLevel(CurrentLevelIndex + 1);
        }

        public void RetryCurrentLevel()
        {
            StartLevel(CurrentLevelIndex);
        }

        public void SetDifficulty(DifficultyLevel difficulty)
        {
            ActiveDifficulty = difficulty;
        }
    }
}

