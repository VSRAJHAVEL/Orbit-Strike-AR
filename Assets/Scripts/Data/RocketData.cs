using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Data
{
    /// <summary>
    /// Deep aeronautical specifications for Orbit Strike AR rocket archetypes.
    /// Employs real aerospace physics models: variable propellant mass depletion,
    /// dynamic pressure drag, and aerodynamic restoring moments.
    /// </summary>
    [CreateAssetMenu(fileName = "NewRocketData", menuName = "OrbitStrike/Rocket Data")]
    public class RocketData : ScriptableObject
    {
        [Header("Identity & Lore")]
        public RocketType rocketType;
        public string rocketName;
        [TextArea(2, 4)]
        public string description;
        public Color themeColor = Color.cyan;
        public int unlockCost = 0;
        public bool isUnlockedByDefault = false;

        [Header("Mass & Propulsion")]
        [Tooltip("Structural dry mass (empty airframe) in kg")]
        public float dryMass = 1.2f;

        [Tooltip("Usable propellant mass in kg")]
        public float propellantMass = 1.8f;

        [Tooltip("Total mass at launch (Dry + Propellant)")]
        public float TotalLaunchMass => dryMass + propellantMass;

        [Tooltip("Peak engine thrust force in Newtons")]
        public float peakThrust = 55f;

        [Tooltip("Continuous burn duration in seconds")]
        public float burnDuration = 2.4f;

        [Tooltip("Booster impulse spike at ignition (multiplier on thrust)")]
        public float initialBoosterSpike = 1.35f;

        [Tooltip("Thrust curve profile over normalized burn time (0 to 1)")]
        public AnimationCurve thrustCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.8f);

        [Header("Aerodynamics & Fin Stability")]
        [Tooltip("Atmospheric drag coefficient Cd")]
        public float dragCoefficient = 0.28f;

        [Tooltip("Frontal cross-sectional aerodynamic area in m²")]
        public float crossSectionalArea = 0.035f;

        [Tooltip("Fin restoring moment coefficient aligning nose to relative wind")]
        public float finStabilityTorque = 18f;

        [Tooltip("Gimbaled engine steering authority in degrees")]
        public float maxGimbalAngle = 8f;

        [Header("Warhead & Destruction")]
        public float blastRadius = 2.5f;
        public int directHitDamage = 120;
        public float explosionImpulse = 400f;
        public bool hasArmorPiercing = false;
        public bool hasEMPDisruption = false;
    }
}
