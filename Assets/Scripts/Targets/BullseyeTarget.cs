using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.Rocket;

namespace OrbitStrike.Targets
{
    /// <summary>
    /// Concentric 3-tier Bullseye Target:
    /// - Inner Bullseye (radius <= innerRadius): 100 points
    /// - Middle Ring (radius <= middleRadius): 50 points
    /// - Outer Ring (radius <= outerRadius): 25 points
    /// </summary>
    public class BullseyeTarget : TargetBase
    {
        [Header("Concentric Ring Thresholds")]
        [SerializeField] private Transform targetCenter;
        [SerializeField] private float innerRadius = 0.35f;
        [SerializeField] private float middleRadius = 0.75f;
        [SerializeField] private float outerRadius = 1.25f;

        public override void OnRocketHit(RocketController rocket, Vector3 hitPoint, Vector3 normal)
        {
            if (IsDestroyed) return;

            Vector3 centerPos = targetCenter != null ? targetCenter.position : transform.position;
            float distanceToCenter = Vector3.Distance(hitPoint, centerPos);

            int points;
            string message;
            Color color;

            if (distanceToCenter <= innerRadius)
            {
                tier = TargetTier.Bullseye100;
                points = 100;
                message = "★ PERFECT BULLSEYE! +100";
                color = new Color(0.0f, 1.0f, 0.4f); // Neon Green
            }
            else if (distanceToCenter <= middleRadius)
            {
                tier = TargetTier.Middle50;
                points = 50;
                message = "GREAT HIT! +50";
                color = new Color(0.0f, 0.85f, 1.0f); // Cyan
            }
            else if (distanceToCenter <= outerRadius)
            {
                tier = TargetTier.Outer25;
                points = 25;
                message = "GRAZE +25";
                color = new Color(1.0f, 0.8f, 0.1f); // Yellow
            }
            else
            {
                // Hit outside outer radius is a graze miss
                EventBus.TriggerRocketMiss(hitPoint, normal);
                return;
            }

            TriggerDestruction(hitPoint, normal, points, message, color);
        }
    }
}

