using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.AR
{
    /// <summary>
    /// Controls the physical and visual behaviors of the AR Launch Pad:
    /// - Hydraulic deployment sequence
    /// - Blinking warning beacons and glowing runway lights
    /// - Barrel / gantry elevation tilt controlled by UI angle slider
    /// - Rocket mounting bracket
    /// </summary>
    public class LaunchPadController : MonoBehaviour
    {
        [Header("Mount & Gimbal Hierarchy")]
        [SerializeField] private Transform gantryYawPivot;
        [SerializeField] private Transform launchRailPitchPivot;
        [SerializeField] private Transform rocketSpawnPoint;

        [Header("Hydraulic Deployment Anim")]
        [SerializeField] private Transform[] supportLegs;
        [SerializeField] private Transform blastDeflector;
        [SerializeField] private float deployDuration = 1.2f;

        [Header("Lighting & Glow")]
        [SerializeField] private Light[] beaconLights;
        [SerializeField] private MeshRenderer[] runwayLightRenderers;
        [SerializeField] private Color activeGlowColor = new Color(0.0f, 0.85f, 1.0f);
        [SerializeField] private Color warningGlowColor = new Color(1.0f, 0.5f, 0.0f);
        [SerializeField] private float beaconBlinkRate = 2.0f;

        public Transform RocketMountPoint => rocketSpawnPoint != null ? rocketSpawnPoint : transform;

        private bool _isDeployed = false;
        private float _currentPitchAngle = 45f;
        private float _currentYawAngle = 0f;

        private void OnEnable()
        {
            EventBus.OnAimParametersChanged += HandleAimParametersChanged;
        }

        private void OnDisable()
        {
            EventBus.OnAimParametersChanged -= HandleAimParametersChanged;
        }

        private void Start()
        {
            // Initial stow configuration before deploy
            if (!_isDeployed)
            {
                TriggerDeploy();
            }
        }

        private void Update()
        {
            AnimateLights();
            UpdateGimbalOrientation();
        }

        public void TriggerDeploy()
        {
            StartCoroutine(DeploySequenceRoutine());
        }

        private IEnumerator DeploySequenceRoutine()
        {
            _isDeployed = false;
            float elapsed = 0f;

            // Initial folded poses
            Vector3 legStartScale = new Vector3(1f, 0.1f, 1f);
            Vector3 legEndScale = Vector3.one;

            while (elapsed < deployDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / deployDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (supportLegs != null)
                {
                    foreach (var leg in supportLegs)
                    {
                        if (leg != null) leg.localScale = Vector3.Lerp(legStartScale, legEndScale, smoothT);
                    }
                }

                if (blastDeflector != null)
                {
                    blastDeflector.localRotation = Quaternion.Euler(Mathf.Lerp(0f, 45f, smoothT), 0f, 0f);
                }

                yield return null;
            }

            _isDeployed = true;
        }

        private void HandleAimParametersChanged(float force, float angleDegrees, float yawDegrees)
        {
            _currentPitchAngle = angleDegrees;
            _currentYawAngle = yawDegrees;
        }

        private void UpdateGimbalOrientation()
        {
            if (launchRailPitchPivot != null)
            {
                // In local space, tilt pitch angle upwards from horizon
                launchRailPitchPivot.localRotation = Quaternion.Slerp(
                    launchRailPitchPivot.localRotation,
                    Quaternion.Euler(-_currentPitchAngle, 0f, 0f),
                    Time.deltaTime * 12f
                );
            }

            if (gantryYawPivot != null)
            {
                gantryYawPivot.localRotation = Quaternion.Slerp(
                    gantryYawPivot.localRotation,
                    Quaternion.Euler(0f, _currentYawAngle, 0f),
                    Time.deltaTime * 12f
                );
            }
        }

        private void AnimateLights()
        {
            float blink = Mathf.PingPong(Time.time * beaconBlinkRate, 1.0f);
            bool isWarning = GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Countdown;

            Color targetColor = isWarning ? warningGlowColor : activeGlowColor;

            if (beaconLights != null)
            {
                foreach (var light in beaconLights)
                {
                    if (light != null)
                    {
                        light.color = targetColor;
                        light.intensity = Mathf.Lerp(0.5f, 2.5f, blink);
                    }
                }
            }

            if (runwayLightRenderers != null)
            {
                foreach (var mr in runwayLightRenderers)
                {
                    if (mr != null && mr.material != null)
                    {
                        mr.material.SetColor("_EmissionColor", targetColor * (0.8f + 0.4f * blink));
                    }
                }
            }
        }
    }
}

