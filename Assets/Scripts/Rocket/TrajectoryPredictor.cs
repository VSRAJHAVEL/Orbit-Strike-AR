using System.Collections.Generic;
using UnityEngine;
using OrbitStrike.AR;
using OrbitStrike.Core;
using OrbitStrike.Data;
using OrbitStrike.Environment;

namespace OrbitStrike.Rocket
{
    /// <summary>
    /// Simulates ballistic and powered rocket flight trajectories in real time:
    /// - Accounts for staging mass depletion m(t) = m_dry + m_fuel(t)
    /// - Aerodynamic dynamic pressure drag (0.5 * rho * v^2 * Cd * A)
    /// - Wind shear acceleration and custom planetary gravity
    /// - Gated prediction dot counts governed by difficulty tier
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class TrajectoryPredictor : MonoBehaviour
    {
        public static TrajectoryPredictor Instance { get; private set; }

        [Header("Rendering")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private LayerMask collisionLayers;

        [Header("Fidelity & Limits")]
        [SerializeField] private float simulationStep = 0.04f;
        [SerializeField] private int maxSimulationSteps = 140;

        [Header("Difficulty Limits")]
        [SerializeField] private int casualMaxDots = 50;
        [SerializeField] private int standardMaxDots = 22;
        [SerializeField] private int expertMaxDots = 8;

        private float _currentThrottle = 0.75f;
        private float _currentPitchAngle = 45f;
        private float _currentYawAngle = 0f;
        private const float AirDensity = 1.225f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (lineRenderer == null) lineRenderer = GetComponent<LineRenderer>();
        }

        private void OnEnable()
        {
            EventBus.OnAimParametersChanged += HandleAimChanged;
            EventBus.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            EventBus.OnAimParametersChanged -= HandleAimChanged;
            EventBus.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleAimChanged(float throttle, float pitch, float yaw)
        {
            _currentThrottle = throttle;
            _currentPitchAngle = pitch;
            _currentYawAngle = yaw;

            if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Aiming)
            {
                UpdateTrajectory();
            }
        }

        private void HandleStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Aiming)
            {
                lineRenderer.enabled = true;
                UpdateTrajectory();
            }
            else
            {
                lineRenderer.enabled = false;
            }
        }

        public void UpdateTrajectory()
        {
            LaunchPadController pad = ARPlacementManager.Instance?.ActiveLaunchPad;
            if (pad == null || pad.RocketMountPoint == null)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            RocketController activeRocket = RocketSpawner.Instance?.CurrentRocket;
            RocketData config = activeRocket != null ? activeRocket.Config : null;

            float dryMass = config != null ? config.dryMass : 1.2f;
            float fuelMass = config != null ? config.propellantMass : 1.8f;
            float totalMass = dryMass + fuelMass;
            float peakThrust = config != null ? config.peakThrust : 55f;
            float burnDuration = config != null ? config.burnDuration : 2.4f;
            float cd = config != null ? config.dragCoefficient : 0.28f;
            float area = config != null ? config.crossSectionalArea : 0.035f;
            float spike = config != null ? config.initialBoosterSpike : 1.35f;

            Quaternion launchRot = Quaternion.Euler(-_currentPitchAngle, _currentYawAngle, 0f);
            Vector3 launchDir = launchRot * Vector3.forward;

            Vector3 simPos = pad.RocketMountPoint.position;
            float initialBooster = (peakThrust * _currentThrottle * spike * 0.25f) / totalMass;
            Vector3 simVel = launchDir * initialBooster;

            Vector3 gravity = GravityController.Instance != null 
                ? GravityController.Instance.CurrentGravityVector 
                : Physics.gravity;

            Vector3 wind = WindSystem.Instance != null 
                ? WindSystem.Instance.CurrentWindVector * (WindSystem.Instance.CurrentWindSpeed * 1.8f)
                : Vector3.zero;

            int dotLimit = GetDotLimit();
            List<Vector3> points = new List<Vector3> { simPos };

            float simTime = 0f;
            float remainingFuel = fuelMass;

            for (int i = 0; i < maxSimulationSteps && points.Count < dotLimit; i++)
            {
                simTime += simulationStep;

                // Propellant depletion
                if (remainingFuel > 0f)
                {
                    float burnRate = fuelMass / burnDuration;
                    remainingFuel = Mathf.Max(0f, remainingFuel - burnRate * simulationStep);
                }
                float currentMass = dryMass + remainingFuel;

                // Thrust
                Vector3 thrustAccel = Vector3.zero;
                if (simTime <= burnDuration && remainingFuel > 0f)
                {
                    float normT = Mathf.Clamp01(simTime / burnDuration);
                    float curve = config != null ? config.thrustCurve.Evaluate(normT) : 1f;
                    thrustAccel = launchDir * ((peakThrust * _currentThrottle * curve) / currentMass);
                }

                // Aerodynamic drag
                float speedSqr = simVel.sqrMagnitude;
                Vector3 dragAccel = Vector3.zero;
                if (speedSqr > 0.01f)
                {
                    float dynPressure = 0.5f * AirDensity * speedSqr;
                    float dragMag = dynPressure * cd * area;
                    dragAccel = -simVel.normalized * (dragMag / currentMass);
                }

                Vector3 totalAccel = thrustAccel + gravity + dragAccel + (wind / currentMass);
                Vector3 nextVel = simVel + totalAccel * simulationStep;
                Vector3 nextPos = simPos + simVel * simulationStep + 0.5f * totalAccel * (simulationStep * simulationStep);

                // Raycast collision against targets/obstacles/ground
                Vector3 stepVector = nextPos - simPos;
                if (Physics.Raycast(simPos, stepVector.normalized, out RaycastHit hit, stepVector.magnitude, collisionLayers))
                {
                    points.Add(hit.point);
                    break;
                }

                points.Add(nextPos);
                simPos = nextPos;
                simVel = nextVel;

                if (simPos.y < -3.0f) break;
            }

            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPositions(points.ToArray());
        }

        private int GetDotLimit()
        {
            DifficultyLevel diff = GameManager.Instance != null ? GameManager.Instance.ActiveDifficulty : DifficultyLevel.Standard;
            return diff switch
            {
                DifficultyLevel.Casual => casualMaxDots,
                DifficultyLevel.Standard => standardMaxDots,
                DifficultyLevel.Expert => expertMaxDots,
                _ => standardMaxDots
            };
        }
    }
}
