using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Environment
{
    /// <summary>
    /// Controls planetary gravity vectors across different worlds:
    /// - Earth: -9.81 m/s²
    /// - Moon:  -1.62 m/s²
    /// - Mars:  -3.71 m/s²
    /// </summary>
    public class GravityController : MonoBehaviour
    {
        public static GravityController Instance { get; private set; }

        public const float EarthGravity = -9.81f;
        public const float MoonGravity = -1.62f;
        public const float MarsGravity = -3.71f;

        [SerializeField] private float activeGravity = EarthGravity;

        public Vector3 CurrentGravityVector => new Vector3(0, activeGravity, 0);
        public float ActiveGravityValue => activeGravity;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetGravity(float gravityValue)
        {
            activeGravity = gravityValue;
            EventBus.TriggerGravityChanged(activeGravity / EarthGravity);
        }
    }
}

