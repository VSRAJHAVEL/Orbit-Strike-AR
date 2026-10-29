using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OrbitStrike.Core;

namespace OrbitStrike.UI
{
    /// <summary>
    /// Updates live in-game HUD indicators:
    /// - Current score and level title
    /// - Remaining rockets visual icons
    /// - Wind direction compass arrow and speed text
    /// - High-visibility countdown text overlay
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Header Info")]
        [SerializeField] private TextMeshProUGUI levelTitleText;
        [SerializeField] private TextMeshProUGUI scoreText;

        [Header("Rocket Inventory")]
        [SerializeField] private Image[] rocketIcons;

        [Header("Wind Compass")]
        [SerializeField] private RectTransform windArrowTransform;
        [SerializeField] private TextMeshProUGUI windSpeedText;

        [Header("Countdown")]
        [SerializeField] private GameObject countdownContainer;
        [SerializeField] private TextMeshProUGUI countdownText;

        private void OnEnable()
        {
            EventBus.OnScoreUpdated += HandleScoreUpdated;
            EventBus.OnWindChanged += HandleWindChanged;
            EventBus.OnLaunchCountdownTick += HandleCountdownTick;
            EventBus.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnScoreUpdated -= HandleScoreUpdated;
            EventBus.OnWindChanged -= HandleWindChanged;
            EventBus.OnLaunchCountdownTick -= HandleCountdownTick;
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Start()
        {
            UpdateRocketIcons();
            if (countdownContainer != null) countdownContainer.SetActive(false);
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            UpdateRocketIcons();

            if (levelTitleText != null && GameManager.Instance != null)
            {
                levelTitleText.text = $"LEVEL {GameManager.Instance.CurrentLevelIndex}";
            }

            if (newState == GameState.Aiming && countdownContainer != null)
            {
                countdownContainer.SetActive(false);
            }
        }

        private void HandleScoreUpdated(int currentScore, int delta)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {currentScore:N0}";
            }
        }

        private void HandleWindChanged(Vector3 direction, float speed)
        {
            if (windSpeedText != null)
            {
                windSpeedText.text = speed > 0f ? $"{speed:F1} m/s" : "CALM";
            }

            if (windArrowTransform != null)
            {
                if (speed > 0f)
                {
                    float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                    windArrowTransform.localRotation = Quaternion.Euler(0, 0, -angle);
                }
            }
        }

        private void HandleCountdownTick(float secondsRemaining)
        {
            if (countdownContainer == null || countdownText == null) return;

            countdownContainer.SetActive(true);

            if (secondsRemaining > 0)
            {
                countdownText.text = Mathf.CeilToInt(secondsRemaining).ToString();
                countdownText.color = Color.yellow;
            }
            else
            {
                countdownText.text = "LIFTOFF!";
                countdownText.color = Color.cyan;
            }
        }

        public void UpdateRocketIcons()
        {
            if (rocketIcons == null) return;

            int remaining = GameManager.Instance != null ? GameManager.Instance.RocketsRemaining : 3;
            for (int i = 0; i < rocketIcons.Length; i++)
            {
                if (rocketIcons[i] != null)
                {
                    rocketIcons[i].color = (i < remaining) ? Color.cyan : new Color(1, 1, 1, 0.2f);
                }
            }
        }
    }
}

