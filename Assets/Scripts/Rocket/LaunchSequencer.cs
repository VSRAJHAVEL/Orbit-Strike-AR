using System.Collections;
using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.AR;
using OrbitStrike.Systems;
using OrbitStrike.VFX;

namespace OrbitStrike.Rocket
{
    /// <summary>
    /// Executes the high-tension launch sequence:
    /// - 3, 2, 1, Liftoff countdown with audio beeps and pad beacon flashing
    /// - Engine pre-ignition flame burst and screen shake
    /// - Ground shockwave release and physical liftoff into flight state
    /// </summary>
    public class LaunchSequencer : MonoBehaviour
    {
        public static LaunchSequencer Instance { get; private set; }

        [Header("Countdown")]
        [SerializeField] private float countdownDuration = 3.0f;
        [SerializeField] private LaunchShockwave shockwaveEffect;

        private float _cachedForcePercent = 0.8f;
        private float _cachedPitchAngle = 45f;
        private float _cachedYawAngle = 0f;
        private Coroutine _activeCountdownRoutine;

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
            EventBus.OnAimParametersChanged += HandleAimParametersChanged;
        }

        private void OnDisable()
        {
            EventBus.OnAimParametersChanged -= HandleAimParametersChanged;
        }

        private void HandleAimParametersChanged(float force, float pitch, float yaw)
        {
            _cachedForcePercent = force;
            _cachedPitchAngle = pitch;
            _cachedYawAngle = yaw;
        }

        public void InitiateLaunch()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Aiming) return;

            if (_activeCountdownRoutine != null) StopCoroutine(_activeCountdownRoutine);
            _activeCountdownRoutine = StartCoroutine(LaunchCountdownRoutine());
        }

        public void AbortCountdown()
        {
            if (_activeCountdownRoutine != null)
            {
                StopCoroutine(_activeCountdownRoutine);
                _activeCountdownRoutine = null;
            }
            GameManager.Instance?.SetState(GameState.Aiming);
        }

        private IEnumerator LaunchCountdownRoutine()
        {
            GameManager.Instance.SetState(GameState.Countdown);

            int seconds = Mathf.CeilToInt(countdownDuration);
            while (seconds > 0)
            {
                EventBus.TriggerLaunchCountdownTick(seconds);
                HapticsManager.Instance?.TriggerLightTick();
                CameraShake.Instance?.Shake(0.2f, 0.05f);

                yield return new WaitForSeconds(1.0f);
                seconds--;
            }

            // "0" / LIFTOFF!
            EventBus.TriggerLaunchCountdownTick(0);
            HapticsManager.Instance?.TriggerMediumImpact();
            CameraShake.Instance?.Shake(0.5f, 0.18f);

            // Trigger ground shockwave
            if (shockwaveEffect != null)
            {
                shockwaveEffect.PlayShockwave();
            }

            // Calculate 3D launch direction
            Quaternion launchRot = Quaternion.Euler(-_cachedPitchAngle, _cachedYawAngle, 0f);
            Vector3 launchDir = launchRot * Vector3.forward;

            // Fire rocket
            RocketController activeRocket = RocketSpawner.Instance?.CurrentRocket;
            if (activeRocket != null)
            {
                GameManager.Instance.ConsumeRocket();
                GameManager.Instance.SetState(GameState.Launching);

                activeRocket.Launch(_cachedForcePercent, launchDir);

                yield return new WaitForSeconds(0.2f);
                GameManager.Instance.SetState(GameState.FlightTracking);
            }
            else
            {
                Debug.LogWarning("[LaunchSequencer] No active rocket found on launch pad!");
                GameManager.Instance.SetState(GameState.Aiming);
            }

            _activeCountdownRoutine = null;
        }
    }
}

