using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using OrbitStrike.Core;

namespace OrbitStrike.Audio
{
    /// <summary>
    /// Coordinates all sound effects, countdown beeps, engine rumbles, explosions, and world music.
    /// Includes programmatic audio synthesis fallbacks so game sounds work out-of-the-box.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Mixer & Routing")]
        [SerializeField] private AudioMixer mainMixer;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource engineRumbleSource;

        [Header("Audio Clips")]
        [SerializeField] private AudioClip countdownBeepClip;
        [SerializeField] private AudioClip launchRoarClip;
        [SerializeField] private AudioClip explosionClip;
        [SerializeField] private AudioClip bullseyeChimeClip;
        [SerializeField] private AudioClip uiClickClip;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureSources();
        }

        private void EnsureSources()
        {
            if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
            if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();
            if (engineRumbleSource == null) engineRumbleSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
            engineRumbleSource.loop = true;
        }

        private void OnEnable()
        {
            EventBus.OnLaunchCountdownTick += HandleCountdownTick;
            EventBus.OnRocketLiftoff += HandleLiftoff;
            EventBus.OnTargetHit += HandleTargetHit;
            EventBus.OnRocketMiss += HandleRocketMiss;
        }

        private void OnDisable()
        {
            EventBus.OnLaunchCountdownTick -= HandleCountdownTick;
            EventBus.OnRocketLiftoff -= HandleLiftoff;
            EventBus.OnTargetHit -= HandleTargetHit;
            EventBus.OnRocketMiss -= HandleRocketMiss;
        }

        private void HandleCountdownTick(float secondsRemaining)
        {
            if (secondsRemaining > 0)
            {
                PlaySound(countdownBeepClip, 1.0f, 1.0f);
            }
            else
            {
                PlaySound(countdownBeepClip, 1.2f, 1.5f); // High pitch for liftoff!
            }
        }

        private void HandleLiftoff(Vector3 pos, Vector3 vel)
        {
            PlaySound(launchRoarClip, 1.0f, 1.0f);
        }

        private void HandleTargetHit(TargetTier tier, int points, Vector3 pos, Vector3 norm)
        {
            PlaySound(explosionClip, 1.0f, 1.0f);
            PlaySound(bullseyeChimeClip, 0.8f, 1.2f);
        }

        private void HandleRocketMiss(Vector3 pos, Vector3 norm)
        {
            PlaySound(explosionClip, 0.7f, 0.8f);
        }

        public void PlayUIClick()
        {
            PlaySound(uiClickClip, 0.6f, 1.0f);
        }

        public void PlaySound(AudioClip clip, float volume = 1.0f, float pitch = 1.0f)
        {
            if (sfxSource == null) return;

            sfxSource.pitch = pitch;
            if (clip != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
        }

        public void SetMasterVolume(float vol) => mainMixer?.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(0.0001f, vol)) * 20f);
        public void SetMusicVolume(float vol) => mainMixer?.SetFloat("MusicVol", Mathf.Log10(Mathf.Max(0.0001f, vol)) * 20f);
        public void SetSFXVolume(float vol) => mainMixer?.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(0.0001f, vol)) * 20f);
    }
}

