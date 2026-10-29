using System.Collections;
using UnityEngine;

namespace OrbitStrike.Systems
{
    /// <summary>
    /// Procedural camera shake for launch rumble and target detonation impacts.
    /// Works with standard camera and Cinemachine virtual cameras.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        private Vector3 _originalLocalPos;
        private Coroutine _activeShakeRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _originalLocalPos = transform.localPosition;
        }

        public void Shake(float duration, float intensity)
        {
            if (_activeShakeRoutine != null)
            {
                StopCoroutine(_activeShakeRoutine);
                transform.localPosition = _originalLocalPos;
            }

            _activeShakeRoutine = StartCoroutine(ShakeRoutine(duration, intensity));
        }

        private IEnumerator ShakeRoutine(float duration, float intensity)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1.0f - Mathf.Clamp01(elapsed / duration);

                float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) * 2f - 1f) * intensity * damp;
                float offsetY = (Mathf.PerlinNoise(0f, Time.time * 25f) * 2f - 1f) * intensity * damp;

                transform.localPosition = _originalLocalPos + new Vector3(offsetX, offsetY, 0f);
                yield return null;
            }

            transform.localPosition = _originalLocalPos;
            _activeShakeRoutine = null;
        }
    }
}

