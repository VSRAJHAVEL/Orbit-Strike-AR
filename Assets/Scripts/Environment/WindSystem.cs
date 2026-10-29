using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Environment
{
    /// <summary>
    /// Manages atmospheric wind forces that push the rocket in flight and alter predicted trajectory.
    /// Includes sinusoidal gust turbulence and broadcasts updates to UI wind indicator.
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        public static WindSystem Instance { get; private set; }

        [Header("Wind State")]
        [SerializeField] private Vector3 baseWindDirection = Vector3.right;
        [SerializeField] private float baseWindSpeed = 0f; // m/s
        [SerializeField] private float gustStrength = 0.5f;

        public Vector3 CurrentWindVector { get; private set; }
        public float CurrentWindSpeed { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (baseWindSpeed > 0f)
            {
                float gust = Mathf.Sin(Time.time * 1.5f) * gustStrength;
                CurrentWindSpeed = Mathf.Max(0f, baseWindSpeed + gust);
                CurrentWindVector = baseWindDirection.normalized;
            }
            else
            {
                CurrentWindSpeed = 0f;
                CurrentWindVector = Vector3.zero;
            }
        }

        public void SetWind(Vector3 direction, float speed)
        {
            baseWindDirection = direction.normalized;
            baseWindSpeed = speed;
            CurrentWindVector = baseWindDirection;
            CurrentWindSpeed = speed;

            EventBus.TriggerWindChanged(CurrentWindVector, CurrentWindSpeed);
        }
    }
}

