using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using OrbitStrike.Core;
using OrbitStrike.Rocket;
using OrbitStrike.Systems;

namespace OrbitStrike.Targets
{
    /// <summary>
    /// Multi-Phase Level 30 Boss Dreadnought:
    /// - Phase 1: Orbital Defense Drones orbiting the perimeter, shielding core
    /// - Phase 2: Harmonic Energy Shield Barrier (150 HP)
    /// - Phase 3: Armored Command Core with Critical Weakpoint Hitbox (2x damage)
    /// </summary>
    public class BossTarget : TargetBase
    {
        public enum BossPhase
        {
            Phase1_EscortDrones = 1,
            Phase2_EnergyShield = 2,
            Phase3_CoreExposed = 3
        }

        [Header("Boss Boss Phase & Health")]
        [SerializeField] private BossPhase currentPhase = BossPhase.Phase1_EscortDrones;
        [SerializeField] private int maxShieldHP = 150;
        [SerializeField] private int maxCoreHP = 350;
        [SerializeField] private Slider bossHealthBar;

        [Header("Escort Drones")]
        [SerializeField] private List<Transform> orbitalDrones = new List<Transform>();
        [SerializeField] private float droneOrbitSpeed = 45f;
        [SerializeField] private float orbitRadius = 2.2f;

        [Header("Shield & Core Visuals")]
        [SerializeField] private GameObject energyShieldDome;
        [SerializeField] private Transform criticalWeakPoint;
        [SerializeField] private ParticleSystem damageSmokeVFX;
        [SerializeField] private ParticleSystem criticalFlareVFX;

        private int _currentShieldHP;
        private int _currentCoreHP;
        private float _orbitAngle = 0f;

        private void Start()
        {
            _currentShieldHP = maxShieldHP;
            _currentCoreHP = maxCoreHP;

            if (bossHealthBar != null)
            {
                bossHealthBar.maxValue = maxShieldHP + maxCoreHP;
                bossHealthBar.value = _currentShieldHP + _currentCoreHP;
            }
        }

        private void Update()
        {
            if (IsDestroyed) return;

            // Animate orbiting escort drones in Phase 1
            if (currentPhase == BossPhase.Phase1_EscortDrones && orbitalDrones.Count > 0)
            {
                _orbitAngle += droneOrbitSpeed * Time.deltaTime;
                for (int i = 0; i < orbitalDrones.Count; i++)
                {
                    if (orbitalDrones[i] != null)
                    {
                        float angle = _orbitAngle + (i * (360f / orbitalDrones.Count));
                        float rad = angle * Mathf.Deg2Rad;
                        Vector3 offset = new Vector3(Mathf.Cos(rad) * orbitRadius, Mathf.Sin(Time.time * 2f + i) * 0.2f, Mathf.Sin(rad) * orbitRadius);
                        orbitalDrones[i].position = transform.position + offset;
                    }
                }
            }
        }

        public override void OnRocketHit(RocketController rocket, Vector3 hitPoint, Vector3 normal)
        {
            if (IsDestroyed) return;

            int damage = rocket != null && rocket.Config != null ? rocket.Config.directHitDamage : 100;
            bool isWeakpointHit = criticalWeakPoint != null && Vector3.Distance(hitPoint, criticalWeakPoint.position) < 0.6f;

            if (isWeakpointHit)
            {
                damage *= 2; // Critical 2x weak point damage!
                if (criticalFlareVFX != null) criticalFlareVFX.Play();
            }

            switch (currentPhase)
            {
                case BossPhase.Phase1_EscortDrones:
                    // Destroy an escort drone
                    if (orbitalDrones.Count > 0)
                    {
                        Transform drone = orbitalDrones[0];
                        orbitalDrones.RemoveAt(0);
                        if (drone != null)
                        {
                            ObjectPooler.Instance?.Spawn("ExplosionVFX", drone.position, Quaternion.identity);
                            Destroy(drone.gameObject);
                        }

                        CameraShake.Instance?.Shake(0.4f, 0.15f);
                        SpawnScorePopup(hitPoint, "DRONE DESTROYED! +50", Color.cyan);

                        if (orbitalDrones.Count == 0)
                        {
                            currentPhase = BossPhase.Phase2_EnergyShield;
                            SpawnScorePopup(transform.position + Vector3.up * 1f, "PHASE 2: SHIELD ENGAGED!", Color.yellow);
                        }
                    }
                    break;

                case BossPhase.Phase2_EnergyShield:
                    _currentShieldHP = Mathf.Max(0, _currentShieldHP - damage);
                    UpdateHealthBar();
                    CameraShake.Instance?.Shake(0.35f, 0.15f);

                    if (_currentShieldHP <= 0)
                    {
                        currentPhase = BossPhase.Phase3_CoreExposed;
                        if (energyShieldDome != null) energyShieldDome.SetActive(false);
                        SpawnScorePopup(hitPoint, "SHIELD COLLAPSED! CORE EXPOSED!", Color.yellow);
                    }
                    else
                    {
                        SpawnScorePopup(hitPoint, $"-{damage} SHIELD HP", new Color(0.2f, 0.7f, 1f));
                    }
                    break;

                case BossPhase.Phase3_CoreExposed:
                    _currentCoreHP = Mathf.Max(0, _currentCoreHP - damage);
                    UpdateHealthBar();
                    CameraShake.Instance?.Shake(0.6f, 0.28f);

                    if (_currentCoreHP <= maxCoreHP * 0.4f && damageSmokeVFX != null && !damageSmokeVFX.isPlaying)
                    {
                        damageSmokeVFX.Play();
                    }

                    if (isWeakpointHit)
                    {
                        SpawnScorePopup(hitPoint, $"★ CRITICAL CORE HIT! -{damage} HP", Color.red);
                    }
                    else
                    {
                        SpawnScorePopup(hitPoint, $"-{damage} HP", Color.yellow);
                    }

                    if (_currentCoreHP <= 0)
                    {
                        TriggerDestruction(hitPoint, normal, 1000, "★ DREADNOUGHT ANNIHILATED! +1000 PTS", new Color(1f, 0.85f, 0f));
                        return;
                    }
                    break;
            }

            EventBus.TriggerScoreUpdated(GameManager.Instance.CurrentScore + damage, damage);
            EventBus.TriggerRocketMiss(hitPoint, normal); // Ready next shot
        }

        private void UpdateHealthBar()
        {
            if (bossHealthBar != null)
            {
                bossHealthBar.value = _currentShieldHP + _currentCoreHP;
            }
        }

        private void SpawnScorePopup(Vector3 pos, string text, Color color)
        {
            GameObject popup = ObjectPooler.Instance?.Spawn("ScoreText", pos + Vector3.up * 0.5f, Quaternion.identity);
            if (popup != null)
            {
                popup.GetComponent<UI.FloatingScoreText>()?.Display(text, color);
            }
        }
    }
}
