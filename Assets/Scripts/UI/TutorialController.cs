using UnityEngine;
using TMPro;
using OrbitStrike.Core;

namespace OrbitStrike.UI
{
    /// <summary>
    /// Interactive tutorial guide walking new players through the 5 core game loop steps.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        [Header("Tutorial Overlay")]
        [SerializeField] private GameObject tutorialContainer;
        [SerializeField] private TextMeshProUGUI stepTitleText;
        [SerializeField] private TextMeshProUGUI stepDescriptionText;

        private int _currentStep = 1;

        private void OnEnable()
        {
            EventBus.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void Start()
        {
            ShowStep(1);
        }

        private void HandleGameStateChanged(GameState newState, GameState oldState)
        {
            switch (newState)
            {
                case GameState.SurfaceScan:
                    ShowStep(1);
                    break;
                case GameState.PadPlacement:
                    ShowStep(2);
                    break;
                case GameState.Aiming:
                    ShowStep(3);
                    break;
                case GameState.Countdown:
                case GameState.Launching:
                    ShowStep(4);
                    break;
                case GameState.TargetHit:
                case GameState.LevelComplete:
                    ShowStep(5);
                    break;
                default:
                    if (tutorialContainer != null) tutorialContainer.SetActive(false);
                    break;
            }
        }

        public void ShowStep(int step)
        {
            _currentStep = step;
            if (tutorialContainer != null) tutorialContainer.SetActive(true);

            switch (step)
            {
                case 1:
                    SetText("STEP 1: SCAN SURFACE", "Point device camera at a flat floor or table until the cyan holographic reticle locks on.");
                    break;
                case 2:
                    SetText("STEP 2: PLACE LAUNCH PAD", "Tap to deploy the launch pad! Pinch with two fingers to scale, twist to rotate.");
                    break;
                case 3:
                    SetText("STEP 3: TUNE TRAJECTORY", "Adjust the Angle and Force sliders. Swipe across the screen to aim your heading.");
                    break;
                case 4:
                    SetText("STEP 4: LAUNCH ROCKET!", "Countdown underway! Engine igniting... Watch the aerodynamic flight arc!");
                    break;
                case 5:
                    SetText("STEP 5: TARGET IMPACT", "Bullseye direct hit! Fewer rockets used awards up to 3 stars and bonus coins.");
                    break;
            }
        }

        private void SetText(string title, string desc)
        {
            if (stepTitleText != null) stepTitleText.text = title;
            if (stepDescriptionText != null) stepDescriptionText.text = desc;
        }

        public void Dismiss()
        {
            if (tutorialContainer != null) tutorialContainer.SetActive(false);
        }
    }
}

