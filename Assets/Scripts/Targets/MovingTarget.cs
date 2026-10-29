using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.Rocket;

namespace OrbitStrike.Targets
{
    /// <summary>
    /// Moving Drone Target: Patrols between waypoints with sinusoidal bobbing.
    /// Provides dynamic moving challenge in advanced city and moon worlds.
    /// </summary>
    public class MovingTarget : TargetBase
    {
        [Header("Movement Path")]
        [SerializeField] private Vector3 movementOffset = new Vector3(2.5f, 0f, 0f);
        [SerializeField] private float speed = 1.5f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2.0f;

        private Vector3 _startPos;
        private Vector3 _endPos;

        private void Start()
        {
            _startPos = transform.position - movementOffset * 0.5f;
            _endPos = transform.position + movementOffset * 0.5f;
        }

        private void Update()
        {
            if (IsDestroyed) return;

            // Ping-pong between waypoints
            float t = Mathf.PingPong(Time.time * speed, 1.0f);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            Vector3 currentPathPos = Vector3.Lerp(_startPos, _endPos, smoothT);
            float bobY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;

            transform.position = currentPathPos + Vector3.up * bobY;
        }

        public override void OnRocketHit(RocketController rocket, Vector3 hitPoint, Vector3 normal)
        {
            if (IsDestroyed) return;
            TriggerDestruction(hitPoint, normal, 120, "★ DRONE INTERCEPT! +120", new Color(0.9f, 0.2f, 1.0f));
        }
    }
}

