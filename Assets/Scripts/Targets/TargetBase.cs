using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.Rocket;
using OrbitStrike.Systems;

namespace OrbitStrike.Targets
{
    /// <summary>
    /// Abstract foundation for all Orbit Strike AR target structures:
    /// - Standard Bullseye, Shielded targets, Moving Drone targets, and Boss targets.
    /// Coordinates destruction VFX, screen shake, audio, and score dispatches.
    /// </summary>
    public abstract class TargetBase : MonoBehaviour
    {
        [Header("Target Properties")]
        [SerializeField] protected TargetTier tier = TargetTier.Bullseye100;
        [SerializeField] protected int basePoints = 100;
        [SerializeField] protected GameObject destructionVFXPrefab;
        [SerializeField] protected int debrisCount = 6;

        public TargetTier Tier => tier;
        public bool IsDestroyed { get; protected set; } = false;

        public abstract void OnRocketHit(RocketController rocket, Vector3 hitPoint, Vector3 normal);

        protected virtual void TriggerDestruction(Vector3 hitPoint, Vector3 normal, int pointsAwarded, string popupMessage, Color popupColor)
        {
            IsDestroyed = true;

            // 1. Screen Shake & Haptics
            CameraShake.Instance?.Shake(0.6f, 0.25f);
            HapticsManager.Instance?.TriggerHeavyExplosion();

            // 2. Spawn Explosion VFX from ObjectPooler
            ObjectPooler.Instance?.Spawn("ExplosionVFX", hitPoint, Quaternion.identity);

            // 3. Scatter Debris shards
            for (int i = 0; i < debrisCount; i++)
            {
                GameObject debris = ObjectPooler.Instance?.Spawn("Debris", hitPoint + Random.insideUnitSphere * 0.2f, Random.rotation);
                if (debris != null)
                {
                    debris.GetComponent<VFX.DebrisPhysics>()?.ExplodeOutward(hitPoint, 15f);
                }
            }

            // 4. Floating Score Popup
            GameObject popup = ObjectPooler.Instance?.Spawn("ScoreText", hitPoint + Vector3.up * 0.4f, Quaternion.identity);
            if (popup != null)
            {
                popup.GetComponent<UI.FloatingScoreText>()?.Display(popupMessage, popupColor);
            }

            // 5. Fire Global Event
            EventBus.TriggerTargetHit(tier, pointsAwarded, hitPoint, normal);

            // Hide or deactivate mesh
            gameObject.SetActive(false);
        }
    }
}

