using UnityEngine;

namespace OrbitStrike.AR
{
    /// <summary>
    /// Renders an animated holographic targeting reticle on detected real-world surfaces.
    /// Rotates concentric rings and pulses emissive intensity when aligned with a valid plane.
    /// </summary>
    public class SurfaceReticle : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private Transform outerRing;
        [SerializeField] private Transform innerRing;
        [SerializeField] private MeshRenderer reticleRenderer;

        [Header("Animation Parameters")]
        [SerializeField] private float outerRotationSpeed = 45f;
        [SerializeField] private float innerRotationSpeed = -75f;
        [SerializeField] private float pulseFrequency = 2.5f;

        [Header("Status Colors")]
        [SerializeField] private Color validColor = new Color(0.0f, 0.95f, 1.0f, 0.85f); // Neon Cyan
        [SerializeField] private Color invalidColor = new Color(1.0f, 0.2f, 0.2f, 0.4f); // Warning Red

        private Material _reticleMat;
        private bool _isValidPlacement = false;

        private void Awake()
        {
            if (reticleRenderer != null)
            {
                _reticleMat = reticleRenderer.material;
            }
        }

        private void Update()
        {
            // Rotate outer and inner rings in opposite directions
            if (outerRing != null)
            {
                outerRing.Rotate(Vector3.up, outerRotationSpeed * Time.deltaTime, Space.Self);
            }

            if (innerRing != null)
            {
                innerRing.Rotate(Vector3.up, innerRotationSpeed * Time.deltaTime, Space.Self);
            }

            // Pulsing emissive glow
            if (_reticleMat != null)
            {
                float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * pulseFrequency);
                Color baseCol = _isValidPlacement ? validColor : invalidColor;
                _reticleMat.color = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * pulse);
            }
        }

        public void SetValid(bool isValid)
        {
            _isValidPlacement = isValid;
        }

        public void SetPose(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}

