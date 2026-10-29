using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OrbitStrike.Core;
using OrbitStrike.Systems;

namespace OrbitStrike.UI
{
    /// <summary>
    /// Dual Slider and Drag-to-Aim interface:
    /// - Slider 1: Force power bar and percentage readout
    /// - Slider 2: Launch angle slider (degrees)
    /// - Drag-to-Aim: Touch drag on screen rotates azimuth (yaw)
    /// </summary>
    public class ForceAngleControls : MonoBehaviour
    {
        [Header("Force Control")]
        [SerializeField] private Slider forceSlider;
        [SerializeField] private Image forceFillBar;
        [SerializeField] private TextMeshProUGUI forceText;
        [SerializeField] private Gradient forceColorGradient;

        [Header("Angle Control")]
        [SerializeField] private Slider angleSlider;
        [SerializeField] private TextMeshProUGUI angleText;

        [Header("Aiming Dynamics")]
        [SerializeField] private float yawSensitivity = 0.25f;
        [SerializeField] private float minPitchAngle = 10f;
        [SerializeField] private float maxPitchAngle = 85f;

        private float _currentForce = 0.75f;
        private float _currentPitch = 45f;
        private float _currentYaw = 0f;

        private Vector2 _touchStartPos;
        private bool _isDraggingAim = false;

        private void Start()
        {
            if (forceSlider != null)
            {
                forceSlider.minValue = 0.2f;
                forceSlider.maxValue = 1.0f;
                forceSlider.value = _currentForce;
                forceSlider.onValueChanged.AddListener(OnForceSliderChanged);
            }

            if (angleSlider != null)
            {
                angleSlider.minValue = minPitchAngle;
                angleSlider.maxValue = maxPitchAngle;
                angleSlider.value = _currentPitch;
                angleSlider.onValueChanged.AddListener(OnAngleSliderChanged);
            }

            UpdateDisplay();
            NotifyAimChanged();
        }

        private void Update()
        {
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameState.Aiming) return;

            HandleDragToAim();
        }

        private void OnForceSliderChanged(float val)
        {
            _currentForce = val;
            UpdateDisplay();
            NotifyAimChanged();
            HapticsManager.Instance?.TriggerLightTick();
        }

        private void OnAngleSliderChanged(float val)
        {
            _currentPitch = val;
            UpdateDisplay();
            NotifyAimChanged();
            HapticsManager.Instance?.TriggerLightTick();
        }

        private void HandleDragToAim()
        {
            // Touch screen swipe
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);

                // Prevent dragging if touch originated over UI sliders
                if (touch.phase == TouchPhase.Began)
                {
                    bool overUI = UnityEngine.EventSystems.EventSystem.current != null &&
                                  UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touch.fingerId);

                    if (!overUI)
                    {
                        _isDraggingAim = true;
                        _touchStartPos = touch.position;
                    }
                }
                else if (touch.phase == TouchPhase.Moved && _isDraggingAim)
                {
                    float deltaX = touch.deltaPosition.x;
                    _currentYaw += deltaX * yawSensitivity;
                    NotifyAimChanged();
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    _isDraggingAim = false;
                }
            }

#if UNITY_EDITOR
            // Mouse drag in editor
            bool mouseOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (Input.GetMouseButtonDown(0) && !mouseOverUI)
            {
                _isDraggingAim = true;
            }
            else if (Input.GetMouseButton(0) && _isDraggingAim)
            {
                float deltaX = Input.GetAxis("Mouse X") * 12f;
                _currentYaw += deltaX * yawSensitivity;
                NotifyAimChanged();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDraggingAim = false;
            }
#endif
        }

        private void UpdateDisplay()
        {
            if (forceText != null)
            {
                forceText.text = $"{Mathf.RoundToInt(_currentForce * 100f)}%";
            }

            if (forceFillBar != null && forceColorGradient != null)
            {
                forceFillBar.color = forceColorGradient.Evaluate(_currentForce);
            }

            if (angleText != null)
            {
                angleText.text = $"{Mathf.RoundToInt(_currentPitch)}°";
            }
        }

        private void NotifyAimChanged()
        {
            EventBus.TriggerAimParametersChanged(_currentForce, _currentPitch, _currentYaw);
        }
    }
}

