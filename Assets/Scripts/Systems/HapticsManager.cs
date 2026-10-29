using UnityEngine;

namespace OrbitStrike.Systems
{
    /// <summary>
    /// Delivers tactile haptic feedback for UI sliders, countdown ticks, engine ignition, and detonations.
    /// Safely handles mobile Android/iOS vibration API calls.
    /// </summary>
    public class HapticsManager : MonoBehaviour
    {
        public static HapticsManager Instance { get; private set; }

        public bool HapticsEnabled { get; set; } = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void TriggerLightTick()
        {
            if (!HapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public void TriggerMediumImpact()
        {
            if (!HapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        public void TriggerHeavyExplosion()
        {
            if (!HapticsEnabled) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}

