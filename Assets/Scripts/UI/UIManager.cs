using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OrbitStrike.Core;
using OrbitStrike.Data;
using OrbitStrike.Rocket;
using OrbitStrike.AR;

namespace OrbitStrike.UI
{
    /// <summary>
    /// Master UI manager orchestrating neon sci-fi HUD panels, modals, transitions,
    /// power-up triggers, star ratings, and accessibility layouts.
    /// Includes a high-fidelity OnGUI immediate-mode HUD guarantee so game controls
    /// and HUD are ALWAYS visible and fully playable in Unity Editor and builds.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject scanHUD;
        [SerializeField] private GameObject aimingHUD;
        [SerializeField] private GameObject winModal;
        [SerializeField] private GameObject loseModal;
        [SerializeField] private GameObject shopModal;
        [SerializeField] private GameObject settingsModal;

        [Header("Win Modal Elements")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private TextMeshProUGUI winScoreText;
        [SerializeField] private TextMeshProUGUI winCoinsEarnedText;

        [Header("Accessibility & Layout")]
        [SerializeField] private RectTransform controlsContainer;

        // OnGUI Interactive Parameters
        private float _guiForce = 0.75f;
        private float _guiAngle = 45f;
        private float _guiYaw = 0f;
        private string _countdownText = "";
        private bool _showWinPopup = false;
        private int _winStars = 3;
        private int _winScoreDelta = 100;

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
            EventBus.OnLevelCompleted += HandleLevelCompleted;
            EventBus.OnLaunchCountdownTick += HandleCountdownTick;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
            EventBus.OnLevelCompleted -= HandleLevelCompleted;
            EventBus.OnLaunchCountdownTick -= HandleCountdownTick;
        }

        private void Start()
        {
            ApplyLeftHandedLayout(SaveManager.Instance != null && SaveManager.Instance.Data.isLeftHandedUI);

            // Auto-advance from Menu to SurfaceScan / Aiming on startup so player gets right into the action
            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Menu)
            {
                GameManager.Instance.StartLevel(1);
            }

            UpdatePanelVisibility(GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Aiming);
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            UpdatePanelVisibility(newState);
            if (newState == GameState.Aiming)
            {
                _showWinPopup = false;
                _countdownText = "";
            }
        }

        private void HandleCountdownTick(float seconds)
        {
            _countdownText = seconds > 0 ? Mathf.CeilToInt(seconds).ToString() : "LIFTOFF!";
        }

        private void UpdatePanelVisibility(GameState state)
        {
            if (menuPanel != null) menuPanel.SetActive(state == GameState.Menu);
            if (scanHUD != null) scanHUD.SetActive(state == GameState.SurfaceScan || state == GameState.PadPlacement);
            if (aimingHUD != null) aimingHUD.SetActive(state == GameState.Aiming || state == GameState.Countdown);
            if (winModal != null && state != GameState.LevelComplete) winModal.SetActive(false);
            if (loseModal != null && state != GameState.GameOver) loseModal.SetActive(false);
            if (shopModal != null) shopModal.SetActive(state == GameState.Shop);
            if (settingsModal != null) settingsModal.SetActive(state == GameState.Settings);
        }

        private void HandleLevelCompleted(int levelIndex, int stars)
        {
            _showWinPopup = true;
            _winStars = stars;
            _winScoreDelta = stars * 100;

            if (winModal != null)
            {
                winModal.SetActive(true);
                if (winScoreText != null && GameManager.Instance != null)
                    winScoreText.text = $"SCORE: {GameManager.Instance.CurrentScore:N0}";
                if (winCoinsEarnedText != null)
                    winCoinsEarnedText.text = $"+{stars * 100} COINS";

                if (starImages != null)
                {
                    for (int i = 0; i < starImages.Length; i++)
                    {
                        if (starImages[i] != null)
                            starImages[i].color = (i < stars) ? Color.yellow : new Color(1, 1, 1, 0.2f);
                    }
                }
            }
        }

        // --- GUARANTEED ONGUI HUD LAYER ---
        private void OnGUI()
        {
            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.Aiming;

            // 1. TOP STATUS BAR
            GUI.Box(new Rect(10, 10, Screen.width - 20, 50), "");
            GUI.color = Color.cyan;
            GUI.skin.label.fontSize = 18;
            GUI.skin.label.fontStyle = FontStyle.Bold;
            int lvl = GameManager.Instance != null ? GameManager.Instance.CurrentLevelIndex : 1;
            int sc = GameManager.Instance != null ? GameManager.Instance.CurrentScore : 0;
            int rk = GameManager.Instance != null ? GameManager.Instance.RocketsRemaining : 3;
            GUI.Label(new Rect(25, 20, 350, 30), $"ORBIT STRIKE AR — LEVEL {lvl}");
            GUI.color = Color.white;
            GUI.Label(new Rect(Screen.width - 450, 20, 200, 30), $"SCORE: {sc:N0}");
            GUI.color = Color.yellow;
            GUI.Label(new Rect(Screen.width - 220, 20, 200, 30), $"ROCKETS: {new string('★', rk)}");

            // 2. STATUS NOTIFICATION BANNER
            string status = state switch
            {
                GameState.SurfaceScan => "CLICK SURFACE TO DEPLOY LAUNCH PAD",
                GameState.PadPlacement => "CLICK TO CONFIRM LAUNCH PAD POSITION",
                GameState.Aiming => "TUNE FORCE & ANGLE SLIDERS, THEN HIT LAUNCH!",
                GameState.Countdown => $"COUNTDOWN: {_countdownText}",
                GameState.FlightTracking => "ROCKET IN FLIGHT...",
                GameState.TargetHit => "DIRECT TARGET HIT! EVALUATING SCORE...",
                GameState.Miss => "MISSED TARGET! RETRYING...",
                _ => "ORBIT STRIKE AR"
            };

            GUI.color = new Color(0f, 0.95f, 1f);
            GUI.Box(new Rect(Screen.width * 0.5f - 220, 70, 440, 35), status);

            // 3. COUNTDOWN BIG TEXT
            if (!string.IsNullOrEmpty(_countdownText) && state == GameState.Countdown)
            {
                GUI.color = Color.yellow;
                GUI.skin.label.fontSize = 60;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(Screen.width * 0.5f - 150, Screen.height * 0.4f - 50, 300, 100), _countdownText);
                GUI.skin.label.fontSize = 18;
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }

            // 4. AIMING & SLIDERS DOCK (During Aiming)
            if (state == GameState.Aiming)
            {
                float panelW = 340;
                float panelH = 220;
                float panelX = Screen.width - panelW - 20;
                float panelY = Screen.height - panelH - 20;

                GUI.color = new Color(0.05f, 0.1f, 0.2f, 0.85f);
                GUI.Box(new Rect(panelX, panelY, panelW, panelH), "");
                GUI.color = Color.white;

                // Force Slider
                GUI.Label(new Rect(panelX + 15, panelY + 15, 200, 25), $"THRUST FORCE: {Mathf.RoundToInt(_guiForce * 100)}%");
                float newForce = GUI.HorizontalSlider(new Rect(panelX + 15, panelY + 40, panelW - 30, 20), _guiForce, 0.2f, 1.0f);
                if (Mathf.Abs(newForce - _guiForce) > 0.01f)
                {
                    _guiForce = newForce;
                    NotifyAimChanged();
                }

                // Pitch Angle Slider
                GUI.Label(new Rect(panelX + 15, panelY + 65, 200, 25), $"PITCH ANGLE: {Mathf.RoundToInt(_guiAngle)}°");
                float newAngle = GUI.HorizontalSlider(new Rect(panelX + 15, panelY + 90, panelW - 30, 20), _guiAngle, 15f, 85f);
                if (Mathf.Abs(newAngle - _guiAngle) > 0.5f)
                {
                    _guiAngle = newAngle;
                    NotifyAimChanged();
                }

                // Yaw Slider
                GUI.Label(new Rect(panelX + 15, panelY + 115, 200, 25), $"AZIMUTH YAW: {Mathf.RoundToInt(_guiYaw)}°");
                float newYaw = GUI.HorizontalSlider(new Rect(panelX + 15, panelY + 140, panelW - 30, 20), _guiYaw, -60f, 60f);
                if (Mathf.Abs(newYaw - _guiYaw) > 0.5f)
                {
                    _guiYaw = newYaw;
                    NotifyAimChanged();
                }

                // Archetype Buttons
                float btnW = 75;
                GUI.color = Color.cyan;
                if (GUI.Button(new Rect(20, Screen.height - 60, btnW, 40), "BASIC")) SelectArchetype(RocketType.Basic);
                GUI.color = new Color(1f, 0.5f, 0f);
                if (GUI.Button(new Rect(105, Screen.height - 60, btnW, 40), "HEAVY")) SelectArchetype(RocketType.Heavy);
                GUI.color = Color.green;
                if (GUI.Button(new Rect(190, Screen.height - 60, btnW, 40), "SPEED")) SelectArchetype(RocketType.Speedster);
                GUI.color = Color.magenta;
                if (GUI.Button(new Rect(275, Screen.height - 60, btnW, 40), "HAWK")) SelectArchetype(RocketType.Precision);

                // BIG LAUNCH BUTTON
                GUI.color = new Color(1f, 0.35f, 0f);
                GUI.skin.button.fontSize = 20;
                GUI.skin.button.fontStyle = FontStyle.Bold;
                if (GUI.Button(new Rect(panelX + 15, panelY + 168, panelW - 30, 42), "★ LAUNCH ★"))
                {
                    LaunchSequencer.Instance?.InitiateLaunch();
                }
                GUI.skin.button.fontSize = 14;
            }

            // 5. WIN MODAL
            if (_showWinPopup)
            {
                GUI.color = new Color(0.05f, 0.1f, 0.2f, 0.95f);
                float mW = 400;
                float mH = 220;
                float mX = (Screen.width - mW) * 0.5f;
                float mY = (Screen.height - mH) * 0.5f;
                GUI.Box(new Rect(mX, mY, mW, mH), "");

                GUI.color = Color.cyan;
                GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                GUI.skin.label.fontSize = 22;
                GUI.Label(new Rect(mX, mY + 20, mW, 35), "★ TARGET ANNIHILATED! ★");

                GUI.color = Color.yellow;
                GUI.skin.label.fontSize = 28;
                GUI.Label(new Rect(mX, mY + 60, mW, 35), new string('★', _winStars));

                GUI.color = Color.white;
                GUI.skin.label.fontSize = 16;
                GUI.Label(new Rect(mX, mY + 100, mW, 25), $"+{_winScoreDelta} POINTS • +300 COINS");

                GUI.color = Color.cyan;
                if (GUI.Button(new Rect(mX + 100, mY + 150, 200, 45), "NEXT LEVEL →"))
                {
                    _showWinPopup = false;
                    GameManager.Instance?.NextLevel();
                }
                GUI.skin.label.alignment = TextAnchor.UpperLeft;
            }
        }

        private void SelectArchetype(RocketType type)
        {
            EventBus.TriggerRocketSelected(type);
        }

        private void NotifyAimChanged()
        {
            EventBus.TriggerAimParametersChanged(_guiForce, _guiAngle, _guiYaw);
        }

        public void OnPlayClicked() => GameManager.Instance?.StartLevel(1);
        public void OnLaunchButtonPressed() => LaunchSequencer.Instance?.InitiateLaunch();
        public void OnNextLevelClicked() => GameManager.Instance?.NextLevel();
        public void OnRetryClicked() => GameManager.Instance?.RetryCurrentLevel();

        public void ApplyLeftHandedLayout(bool isLeftHanded)
        {
            if (controlsContainer != null)
            {
                controlsContainer.anchorMin = isLeftHanded ? new Vector2(0, 0) : new Vector2(1, 0);
                controlsContainer.anchorMax = isLeftHanded ? new Vector2(0, 1) : new Vector2(1, 1);
                controlsContainer.pivot = isLeftHanded ? new Vector2(0, 0.5f) : new Vector2(1, 0.5f);
            }
        }
    }
}
