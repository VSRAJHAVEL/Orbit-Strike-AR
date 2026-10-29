using System.Collections;
using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.Data;
using OrbitStrike.Environment;

namespace OrbitStrike.Rocket
{
    /// <summary>
    /// High-fidelity aerospace flight controller:
    /// - Continuous propellant mass expulsion: m(t) = m_dry + m_fuel(t)
    /// - Non-linear dynamic pressure drag: F_drag = 0.5 * rho * v^2 * Cd * A
    /// - Fin aerodynamic restoring torque aligning heading with velocity vector
    /// - Physical engine gimbal deflection
    /// - Impact dispatch to Targets, Shields, Drone hulls, and Boss weak points
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class RocketController : MonoBehaviour
    {
        [Header("Aerospace Specifications")]
        [SerializeField] private RocketData rocketData;

        [Header("Engine & Gimbal Assembly")]
        [SerializeField] private Transform engineGimbalPivot;
        [SerializeField] private ParticleSystem mainEnginePlume;
        [SerializeField] private ParticleSystem smokeTrail;
        [SerializeField] private TrailRenderer ribbonTrail;
        [SerializeField] private Light engineLight;

        [Header("Flight State")]
        private Rigidbody _rb;
        private bool _isLaunched = false;
        private bool _isEngineFiring = false;
        private float _flightElapsed = 0f;
        private float _remainingPropellant = 0f;
        private float _thrustInputThrottle = 1.0f;
        private bool _hasDetonated = false;
        private const float AirDensitySeaLevel = 1.225f; // kg/m^3

        public Rigidbody AttachedRigidbody => _rb;
        public RocketData Config => rocketData;
        public bool IsLaunched => _isLaunched;
        public float RemainingPropellantRatio => rocketData != null && rocketData.propellantMass > 0 ? _remainingPropellant / rocketData.propellantMass : 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            ConfigurePhysics();
        }

        public void Initialize(RocketData data)
        {
            rocketData = data;
            ConfigurePhysics();
        }

        private void ConfigurePhysics()
        {
            if (_rb == null || rocketData == null) return;

            _remainingPropellant = rocketData.propellantMass;
            _rb.mass = rocketData.TotalLaunchMass;
            _rb.linearDamping = 0.02f; // Handled dynamically via aerodynamic pressure equation
            _rb.angularDamping = 0.8f;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.isKinematic = true;
        }

        public void PlayBuildUpAnimation()
        {
            StartCoroutine(BuildUpRoutine());
        }

        private IEnumerator BuildUpRoutine()
        {
            transform.localScale = Vector3.zero;
            float elapsed = 0f;
            float duration = 0.75f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float s = Mathf.Sin(t * Mathf.PI * 0.5f);
                transform.localScale = Vector3.one * s;
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        public void Launch(float throttlePercent, Vector3 initialDirection)
        {
            if (_isLaunched) return;

            _isLaunched = true;
            _thrustInputThrottle = Mathf.Clamp(throttlePercent, 0.2f, 1.0f);
            _remainingPropellant = rocketData.propellantMass;
            _flightElapsed = 0f;

            transform.parent = null;
            _rb.isKinematic = false;
            transform.forward = initialDirection.normalized;

            // Initial launch ramp booster burst
            float boosterImpulse = (rocketData.peakThrust * _thrustInputThrottle * rocketData.initialBoosterSpike) * 0.25f;
            _rb.AddForce(transform.forward * boosterImpulse, ForceMode.Impulse);

            IgniteRocketEngine();
            EventBus.TriggerRocketLiftoff(transform.position, _rb.linearVelocity);
        }

        private void IgniteRocketEngine()
        {
            _isEngineFiring = true;
            if (mainEnginePlume != null) mainEnginePlume.Play();
            if (smokeTrail != null) smokeTrail.Play();
            if (ribbonTrail != null) ribbonTrail.emitting = true;
            if (engineLight != null) engineLight.enabled = true;
        }

        private void CutRocketEngine()
        {
            _isEngineFiring = false;
            if (mainEnginePlume != null) mainEnginePlume.Stop();
            if (engineLight != null) engineLight.enabled = false;
        }

        private void FixedUpdate()
        {
            if (!_isLaunched || _hasDetonated) return;

            _flightElapsed += Time.fixedDeltaTime;

            // 1. Staged Propellant Mass Depletion
            if (_isEngineFiring && _remainingPropellant > 0f)
            {
                float burnRate = rocketData.propellantMass / rocketData.burnDuration;
                float burnedAmount = burnRate * Time.fixedDeltaTime;
                _remainingPropellant = Mathf.Max(0f, _remainingPropellant - burnedAmount);
                _rb.mass = rocketData.dryMass + _remainingPropellant;

                if (_remainingPropellant <= 0f || _flightElapsed >= rocketData.burnDuration)
                {
                    CutRocketEngine();
                }
                else
                {
                    // Compute thrust along engine nozzle vector using thrust curve
                    float normTime = Mathf.Clamp01(_flightElapsed / rocketData.burnDuration);
                    float curveMult = rocketData.thrustCurve.Evaluate(normTime);
                    float thrustMagnitude = rocketData.peakThrust * _thrustInputThrottle * curveMult;

                    Vector3 thrustDir = engineGimbalPivot != null ? engineGimbalPivot.forward : transform.forward;
                    _rb.AddForce(thrustDir * thrustMagnitude, ForceMode.Force);
                }
            }

            // 2. Realistic Dynamic Pressure Aerodynamic Drag: F_d = 0.5 * rho * v^2 * Cd * A
            Vector3 vel = _rb.linearVelocity;
            float speedSqr = vel.sqrMagnitude;
            if (speedSqr > 0.01f)
            {
                float dynamicPressure = 0.5f * AirDensitySeaLevel * speedSqr;
                float dragForceMagnitude = dynamicPressure * rocketData.dragCoefficient * rocketData.crossSectionalArea;
                Vector3 dragVector = -vel.normalized * dragForceMagnitude;
                _rb.AddForce(dragVector, ForceMode.Force);

                // 3. Fin Restoring Aerodynamic Torque
                Vector3 crossFin = Vector3.Cross(transform.forward, vel.normalized);
                _rb.AddTorque(crossFin * rocketData.finStabilityTorque, ForceMode.Acceleration);
            }

            // 4. Wind System Crosswind Force
            if (WindSystem.Instance != null && WindSystem.Instance.CurrentWindSpeed > 0f)
            {
                Vector3 windForce = WindSystem.Instance.CurrentWindVector * (WindSystem.Instance.CurrentWindSpeed * 1.8f);
                _rb.AddForce(windForce, ForceMode.Acceleration);
            }

            // 5. Custom World Planetary Gravity
            if (GravityController.Instance != null)
            {
                _rb.AddForce(GravityController.Instance.CurrentGravityVector, ForceMode.Acceleration);
            }
        }

        public void SetGimbalDeflection(Vector2 deflectionPitchYaw)
        {
            if (engineGimbalPivot == null || rocketData == null) return;
            float maxG = rocketData.maxGimbalAngle;
            float pitch = Mathf.Clamp(deflectionPitchYaw.x, -maxG, maxG);
            float yaw = Mathf.Clamp(deflectionPitchYaw.y, -maxG, maxG);
            engineGimbalPivot.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasDetonated) return;
            Detonate(collision.gameObject, collision.contacts[0].point, collision.contacts[0].normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hasDetonated) return;
            Detonate(other.gameObject, transform.position, -transform.forward);
        }

        private void Detonate(GameObject hitTarget, Vector3 hitPoint, Vector3 normal)
        {
            _hasDetonated = true;
            CutRocketEngine();

            if (smokeTrail != null) smokeTrail.Stop();
            if (ribbonTrail != null) ribbonTrail.emitting = false;

            _rb.isKinematic = true;

            // Route collision to TargetBase hierarchy
            var target = hitTarget.GetComponentInParent<Targets.TargetBase>();
            if (target != null)
            {
                target.OnRocketHit(this, hitPoint, normal);
            }
            else
            {
                EventBus.TriggerRocketMiss(hitPoint, normal);
            }

            StartCoroutine(RecycleRoutine());
        }

        private IEnumerator RecycleRoutine()
        {
            yield return new WaitForSeconds(0.08f);
            gameObject.SetActive(false);
        }
    }
}
