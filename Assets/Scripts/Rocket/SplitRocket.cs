using System.Collections;
using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Rocket
{
    /// <summary>
    /// Split rocket power-up: divides the main rocket into 3 sub-munitions mid-flight.
    /// </summary>
    public class SplitRocket : MonoBehaviour
    {
        [Header("Sub-munition Prefab")]
        [SerializeField] private GameObject subMunitionPrefab;
        [SerializeField] private float splitDelay = 1.0f;
        [SerializeField] private float spreadAngle = 15f;

        private RocketController _rocket;
        private bool _hasSplit = false;
        private bool _isSplitPowerUpArmed = false;
        private float _flightTime = 0f;

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
            if (type == PowerUpType.SplitRocket)
            {
                _isSplitPowerUpArmed = true;
            }
        }

        private void Update()
        {
            if (!_isSplitPowerUpArmed || _hasSplit || _rocket == null || !_rocket.IsLaunched) return;

            _flightTime += Time.deltaTime;

#pragma warning disable 0618
            bool reachedApogee = _rocket.AttachedRigidbody != null && _rocket.AttachedRigidbody.velocity.y < 0.5f;
#pragma warning restore 0618
            if (_flightTime >= splitDelay || reachedApogee)
            {
                ExecuteSplit();
            }
        }

        public void ExecuteSplit()
        {
            if (_hasSplit) return;
            _hasSplit = true;

            if (subMunitionPrefab != null)
            {
#pragma warning disable 0618
            Vector3 currentVel = _rocket.AttachedRigidbody != null ? _rocket.AttachedRigidbody.velocity : Vector3.forward;
#pragma warning restore 0618

                // Spawn left and right flanking sub-munitions
                Quaternion leftRot = Quaternion.Euler(0, -spreadAngle, 0);
                Quaternion rightRot = Quaternion.Euler(0, spreadAngle, 0);

                SpawnSubMunition(transform.position, leftRot * currentVel);
                SpawnSubMunition(transform.position, rightRot * currentVel);
            }
        }

        private void SpawnSubMunition(Vector3 spawnPos, Vector3 velocity)
        {
            GameObject sub = Instantiate(subMunitionPrefab, spawnPos, Quaternion.LookRotation(velocity));
            Rigidbody rb = sub.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = velocity;
            }
        }
    }
}

