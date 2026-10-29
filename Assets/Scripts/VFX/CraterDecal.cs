using System.Collections;
using UnityEngine;
using OrbitStrike.Systems;

namespace OrbitStrike.VFX
{
    /// <summary>
    /// Displays a charred scorch crater on surfaces upon missed rocket detonations.
    /// Fades out after a set duration and returns to the pool.
    /// </summary>
    public class CraterDecal : MonoBehaviour
    {
        [SerializeField] private MeshRenderer decalRenderer;
        [SerializeField] private float displayDuration = 6.0f;
        [SerializeField] private float fadeDuration = 1.5f;

        private Material _decalMat;

        private void Awake()
        {
            if (decalRenderer != null) _decalMat = decalRenderer.material;
        }

        private void OnEnable()
        {
            StartCoroutine(LifeCycleRoutine());
        }

        private IEnumerator LifeCycleRoutine()
        {
            if (_decalMat != null)
            {
                Color c = _decalMat.color;
                c.a = 0.9f;
                _decalMat.color = c;
            }

            yield return new WaitForSeconds(displayDuration);

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;

                if (_decalMat != null)
                {
                    Color c = _decalMat.color;
                    c.a = Mathf.Lerp(0.9f, 0.0f, t);
                    _decalMat.color = c;
                }

                yield return null;
            }

            ObjectPooler.Instance?.Recycle("CraterDecal", gameObject);
        }
    }
}

