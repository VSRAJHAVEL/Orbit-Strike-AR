using System.Collections;
using UnityEngine;

namespace OrbitStrike.VFX
{
    /// <summary>
    /// Renders an expanding radial shockwave ring and dust cloud at the launch pad during liftoff.
    /// </summary>
    public class LaunchShockwave : MonoBehaviour
    {
        [Header("Shockwave Visuals")]
        [SerializeField] private Transform ringMesh;
        [SerializeField] private ParticleSystem launchSmoke;
        [SerializeField] private ParticleSystem launchSparks;
        [SerializeField] private float targetRadius = 3.5f;
        [SerializeField] private float duration = 0.65f;

        private Material _ringMat;

        private void Awake()
        {
            if (ringMesh != null)
            {
                var mr = ringMesh.GetComponent<MeshRenderer>();
                if (mr != null) _ringMat = mr.material;
            }
        }

        public void PlayShockwave()
        {
            gameObject.SetActive(true);
            if (launchSmoke != null) launchSmoke.Play();
            if (launchSparks != null) launchSparks.Play();

            StartCoroutine(ExpandAndFadeRoutine());
        }

        private IEnumerator ExpandAndFadeRoutine()
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = new Vector3(targetRadius, 0.05f, targetRadius);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (ringMesh != null)
                {
                    ringMesh.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                }

                if (_ringMat != null)
                {
                    Color col = _ringMat.color;
                    col.a = Mathf.Lerp(1.0f, 0.0f, smoothT);
                    _ringMat.color = col;
                }

                yield return null;
            }

            gameObject.SetActive(false);
        }
    }
}

