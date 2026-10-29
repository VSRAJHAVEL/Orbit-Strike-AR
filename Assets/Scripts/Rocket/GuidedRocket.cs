using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Rocket
{
    /// <summary>
    /// Guided missile power-up: actively steers towards the primary target center.
    /// </summary>
    public class GuidedRocket : MonoBehaviour
    {
        [Header("Guidance Parameters")]
        [SerializeField] private float guidanceStrength = 12f;
        [SerializeField] private float activationDelay = 0.5f;

        private RocketController _rocket;
        private Transform _targetTransform;
        private float _flightTimer = 0f;
        private bool _isGuidanceActive = false;

        private void Awake()
        {
            _rocket = GetComponent<RocketController>();
        }

        private void OnEnable()
        {
            EventBus.OnPowerUpActivated += HandlePowerUpActivated;
        }

        private void OnDisable()
        {
            EventBus.OnPowerUpActivated -= HandlePowerUpActivated;
        }

        private void HandlePowerUpActivated(PowerUpType type)
        {
            if (type == PowerUpType.GuidedMissile)
            {
                _isGuidanceActive = true;
                FindNearestTarget();
            }
        }

        private void FindNearestTarget()
        {
#if UNITY_2023_1_OR_NEWER
            var target = Object.FindAnyObjectByType<Targets.TargetBase>();
#else
            var target = Object.FindObjectOfType<Targets.TargetBase>();
#endif
            if (target != null)
            {
                _targetTransform = target.transform;
            }
        }

        private void FixedUpdate()
        {
            if (!_isGuidanceActive || _rocket == null || !_rocket.IsLaunched || _targetTransform == null) return;

            _flightTimer += Time.fixedDeltaTime;
            if (_flightTimer < activationDelay) return;

            Vector3 toTarget = (_targetTransform.position - transform.position).normalized;
            Vector3 cross = Vector3.Cross(transform.forward, toTarget);

            if (_rocket.AttachedRigidbody != null)
            {
                _rocket.AttachedRigidbody.AddTorque(cross * guidanceStrength, ForceMode.Acceleration);
            }
        }
    }
}

