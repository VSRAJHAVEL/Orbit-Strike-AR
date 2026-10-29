using System.Collections;
using UnityEngine;
using OrbitStrike.Core;
using OrbitStrike.Rocket;
using OrbitStrike.Systems;

namespace OrbitStrike.Targets
{
    /// <summary>
    /// Shielded Target: Protected by a glowing forcefield dome.
    /// Requires 2 hits: First hit overloads and collapses the shield; second hit destroys the core.
    /// </summary>
    public class ShieldedTarget : TargetBase
    {
        [Header("Shield Components")]
        [SerializeField] private GameObject shieldMesh;
        [SerializeField] private ParticleSystem shieldCollapseVFX;
        [SerializeField] private MeshRenderer shieldRenderer;

        private bool _isShieldActive = true;

        public override void OnRocketHit(RocketController rocket, Vector3 hitPoint, Vector3 normal)
        {
            if (IsDestroyed) return;

            if (_isShieldActive)
            {
                // First Hit: Collapse Shield
                _isShieldActive = false;

                if (shieldCollapseVFX != null) shieldCollapseVFX.Play();
                CameraShake.Instance?.Shake(0.35f, 0.15f);
                HapticsManager.Instance?.TriggerMediumImpact();

                // Floating popup
                GameObject popup = ObjectPooler.Instance?.Spawn("ScoreText", hitPoint + Vector3.up * 0.4f, Quaternion.identity);
                if (popup != null)
                {
                    popup.GetComponent<UI.FloatingScoreText>()?.Display("SHIELD DOWN! +20", new Color(0.2f, 0.6f, 1.0f));
                }

                if (shieldMesh != null)
                {
                    StartCoroutine(CollapseShieldRoutine());
                }

                EventBus.TriggerScoreUpdated(GameManager.Instance.CurrentScore + 20, 20);
                EventBus.TriggerRocketMiss(hitPoint, normal); // Prompt player for 2nd hit
            }
            else
            {
                // Second Hit: Obliterate Core
                TriggerDestruction(hitPoint, normal, 150, "★ CORE DESTROYED! +150", new Color(1.0f, 0.4f, 0.0f));
            }
        }

        private IEnumerator CollapseShieldRoutine()
        {
            float elapsed = 0f;
            float duration = 0.4f;
            Vector3 startScale = shieldMesh.transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                shieldMesh.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                yield return null;
            }

            shieldMesh.SetActive(false);
        }
    }
}

