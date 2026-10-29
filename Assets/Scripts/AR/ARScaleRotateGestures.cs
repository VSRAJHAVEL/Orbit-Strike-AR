using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.AR
{
    /// <summary>
    /// Handles pinch-to-scale and two-finger twist-to-rotate gestures for the AR playspace.
    /// Also includes Unity Editor input fallback (Scroll to zoom, RMB drag to rotate).
    /// </summary>
    public class ARScaleRotateGestures : MonoBehaviour
    {
        [Header("Target Transform")]
        [SerializeField] private Transform targetPlayspace;

        [Header("Scale Limits")]
        [SerializeField] private float minScale = 0.2f;
        [SerializeField] private float maxScale = 2.0f;
        [SerializeField] private float scaleSensitivity = 0.005f;

        [Header("Rotation Limits")]
        [SerializeField] private float rotationSensitivity = 0.4f;

        private float _currentScale = 1.0f;

        public void SetTarget(Transform target)
        {
            targetPlayspace = target;
            if (target != null)
            {
                _currentScale = target.localScale.x;
            }
        }

        private void Update()
        {
            if (targetPlayspace == null) return;

            // Only allow scaling/rotating during placement and aiming states
            if (GameManager.Instance != null)
            {
                var state = GameManager.Instance.CurrentState;
                if (state != GameState.PadPlacement && state != GameState.Aiming && state != GameState.SurfaceScan)
                {
                    return;
                }
            }

            HandleTouchGestures();
            HandleEditorFallbacks();
        }

        private void HandleTouchGestures()
        {
            if (Input.touchCount != 2) return;

            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            // Previous positions
            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            // 1. Pinch to Scale
            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;
            float deltaMagnitudeDiff = touchDeltaMag - prevTouchDeltaMag;

            if (Mathf.Abs(deltaMagnitudeDiff) > 1.5f)
            {
                _currentScale += deltaMagnitudeDiff * scaleSensitivity * 0.05f;
                _currentScale = Mathf.Clamp(_currentScale, minScale, maxScale);
                targetPlayspace.localScale = Vector3.one * _currentScale;
                EventBus.TriggerPlayspaceScaled(_currentScale);
            }

            // 2. Two-finger twist to Rotate
            Vector2 prevVector = touchOnePrevPos - touchZeroPrevPos;
            Vector2 currentVector = touchOne.position - touchZero.position;
            float angleDelta = Vector2.SignedAngle(prevVector, currentVector);

            if (Mathf.Abs(angleDelta) > 0.2f)
            {
                targetPlayspace.Rotate(Vector3.up, -angleDelta * rotationSensitivity, Space.World);
            }
        }

        private void HandleEditorFallbacks()
        {
#if UNITY_EDITOR
            // Mouse scroll to scale
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentScale += scroll * 0.5f;
                _currentScale = Mathf.Clamp(_currentScale, minScale, maxScale);
                targetPlayspace.localScale = Vector3.one * _currentScale;
                EventBus.TriggerPlayspaceScaled(_currentScale);
            }

            // Right-click drag to rotate
            if (Input.GetMouseButton(1))
            {
                float rotX = Input.GetAxis("Mouse X") * 5.0f;
                targetPlayspace.Rotate(Vector3.up, -rotX, Space.World);
            }
#endif
        }
    }
}

