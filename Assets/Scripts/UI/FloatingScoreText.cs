using System.Collections;
using UnityEngine;
using TMPro;
using OrbitStrike.Systems;

namespace OrbitStrike.UI
{
    /// <summary>
    /// Animates a world-space floating score readout ("+100 BULLSEYE!", "+50") upon target collision.
    /// Faces the active camera, drifts upwards, and fades into the object pool.
    /// </summary>
    public class FloatingScoreText : MonoBehaviour
    {
        [SerializeField] private TextMeshPro textMesh;
        [SerializeField] private float floatSpeed = 1.2f;
        [SerializeField] private float lifetime = 1.5f;

        private Camera _mainCam;

        private void Awake()
        {
            if (textMesh == null) textMesh = GetComponent<TextMeshPro>();
            _mainCam = Camera.main;
        }

        public void Display(string text, Color color)
        {
            if (textMesh != null)
            {
                textMesh.text = text;
                textMesh.color = color;
            }

            StartCoroutine(FloatAndFadeRoutine());
        }

        private void LateUpdate()
        {
            if (_mainCam != null)
            {
                // Billboard facing camera
                transform.forward = _mainCam.transform.forward;
            }
        }

        private IEnumerator FloatAndFadeRoutine()
        {
            float elapsed = 0f;
            Color initialColor = textMesh != null ? textMesh.color : Color.yellow;
            Vector3 startPos = transform.position;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                transform.position = startPos + Vector3.up * (floatSpeed * elapsed);

                if (textMesh != null)
                {
                    Color c = initialColor;
                    c.a = Mathf.Lerp(1.0f, 0.0f, t * t);
                    textMesh.color = c;
                }

                yield return null;
            }

            ObjectPooler.Instance?.Recycle("ScoreText", gameObject);
        }
    }
}

