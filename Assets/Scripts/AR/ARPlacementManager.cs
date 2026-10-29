using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using OrbitStrike.Core;

namespace OrbitStrike.AR
{
    /// <summary>
    /// Manages AR plane detection, surface raycasting, and placement of the launch pad & environment playspace.
    /// Includes Unity Editor simulation so developers can test without requiring an active mobile AR device.
    /// </summary>
    public class ARPlacementManager : MonoBehaviour
    {
        public static ARPlacementManager Instance { get; private set; }

        [Header("AR Foundation Components")]
        [SerializeField] private ARRaycastManager raycastManager;
        [SerializeField] private ARPlaneManager planeManager;
        [SerializeField] private Camera arCamera;

        [Header("Reticle & Prefabs")]
        [SerializeField] private SurfaceReticle reticlePrefab;
        [SerializeField] private GameObject playspaceContainerPrefab;
        [SerializeField] private LaunchPadController launchPadPrefab;

        [Header("Runtime References")]
        private SurfaceReticle _activeReticle;
        private GameObject _spawnedPlayspace;
        private LaunchPadController _activeLaunchPad;
        private ARScaleRotateGestures _gestureController;

        private Pose _currentPlacementPose;
        private bool _isPlacementPoseValid = false;
        private bool _isPadPlaced = false;

        private static readonly List<ARRaycastHit> Hits = new List<ARRaycastHit>();

        public LaunchPadController ActiveLaunchPad => _activeLaunchPad;
        public bool IsPadPlaced => _isPadPlaced;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (arCamera == null)
            {
                arCamera = Camera.main;
            }

            _gestureController = gameObject.AddComponent<ARScaleRotateGestures>();
        }

        private void Start()
        {
            if (reticlePrefab != null)
            {
                _activeReticle = Instantiate(reticlePrefab);
                _activeReticle.gameObject.SetActive(false);
            }
            else
            {
                GameObject retGo = new GameObject("ProceduralReticle");
                GameObject ring1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring1.name = "OuterRing";
                ring1.transform.SetParent(retGo.transform, false);
                ring1.transform.localScale = new Vector3(0.8f, 0.01f, 0.8f);
                var mr1 = ring1.GetComponent<MeshRenderer>();
                if (mr1 != null) mr1.material = ProceduralAssetFactory.CreateHolographicGlowMaterial(Color.cyan, 2.5f);

                GameObject ring2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring2.name = "InnerRing";
                ring2.transform.SetParent(retGo.transform, false);
                ring2.transform.localScale = new Vector3(0.4f, 0.02f, 0.4f);
                var mr2 = ring2.GetComponent<MeshRenderer>();
                if (mr2 != null) mr2.material = ProceduralAssetFactory.CreateHolographicGlowMaterial(Color.cyan, 3.5f);

                _activeReticle = retGo.AddComponent<SurfaceReticle>();
                _activeReticle.gameObject.SetActive(false);
            }

#if UNITY_EDITOR
            // Automatically deploy in Unity Editor so the game is immediately playable
            StartCoroutine(AutoDeployEditorRoutine());
#endif
        }

#if UNITY_EDITOR
        private IEnumerator AutoDeployEditorRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            if (!_isPadPlaced)
            {
                PlaceLaunchPadAndPlayspace(Vector3.zero, Quaternion.identity);
            }
        }
#endif

        private void Update()
        {
            GameState state = GameManager.Instance != null ? GameManager.Instance.CurrentState : GameState.SurfaceScan;

            if (state == GameState.SurfaceScan || state == GameState.PadPlacement)
            {
                UpdatePlacementPose();
                UpdateReticle();
                CheckPlacementInput();
            }
            else
            {
                if (_activeReticle != null && _activeReticle.gameObject.activeSelf)
                {
                    _activeReticle.gameObject.SetActive(false);
                }
            }
        }

        private void UpdatePlacementPose()
        {
            _isPlacementPoseValid = false;

            // 1. AR Foundation Raycast on Mobile Devices
            if (raycastManager != null)
            {
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                if (raycastManager.Raycast(screenCenter, Hits, TrackableType.PlaneWithinPolygon))
                {
                    _isPlacementPoseValid = true;
                    _currentPlacementPose = Hits[0].pose;
                    EventBus.TriggerPlaneDetected(true);
                    return;
                }
            }

            // 2. Editor / Desktop Raycast Fallback (Casts onto ground plane Y = 0)
            if (arCamera != null)
            {
                Ray ray = arCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
                Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

                if (groundPlane.Raycast(ray, out float enterDistance))
                {
                    _isPlacementPoseValid = true;
                    Vector3 hitPoint = ray.GetPoint(enterDistance);
                    Vector3 cameraForward = arCamera.transform.forward;
                    cameraForward.y = 0;
                    Quaternion lookRot = cameraForward != Vector3.zero ? Quaternion.LookRotation(cameraForward) : Quaternion.identity;

                    _currentPlacementPose = new Pose(hitPoint, lookRot);
                    EventBus.TriggerPlaneDetected(true);
                }
            }
        }

        private void UpdateReticle()
        {
            if (_activeReticle == null) return;

            if (_isPlacementPoseValid)
            {
                _activeReticle.gameObject.SetActive(true);
                _activeReticle.SetPose(_currentPlacementPose.position, _currentPlacementPose.rotation);
                _activeReticle.SetValid(true);
            }
            else
            {
                _activeReticle.SetValid(false);
            }
        }

        private void CheckPlacementInput()
        {
            bool tapDetected = false;

            // Touch screen tap
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began && !IsPointerOverUI(touch.position))
                {
                    tapDetected = true;
                }
            }

            // Mouse click for Editor testing
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI(Input.mousePosition))
            {
                tapDetected = true;
            }

            if (tapDetected && _isPlacementPoseValid)
            {
                PlaceLaunchPadAndPlayspace(_currentPlacementPose.position, _currentPlacementPose.rotation);
            }
        }

        public void PlaceLaunchPadAndPlayspace(Vector3 position, Quaternion rotation)
        {
            if (_spawnedPlayspace == null)
            {
                if (playspaceContainerPrefab != null)
                {
                    _spawnedPlayspace = Instantiate(playspaceContainerPrefab, position, rotation);
                }
                else
                {
                    _spawnedPlayspace = new GameObject("AR_Playspace_Anchor");
                    _spawnedPlayspace.transform.SetPositionAndRotation(position, rotation);
                }

                _gestureController.SetTarget(_spawnedPlayspace.transform);
            }
            else
            {
                _spawnedPlayspace.transform.SetPositionAndRotation(position, rotation);
            }

            if (_activeLaunchPad == null)
            {
                if (launchPadPrefab != null)
                {
                    _activeLaunchPad = Instantiate(launchPadPrefab, _spawnedPlayspace.transform);
                }
                else
                {
                    GameObject padGo = ProceduralAssetFactory.CreateProceduralLaunchPad();
                    padGo.transform.SetParent(_spawnedPlayspace.transform, false);
                    _activeLaunchPad = padGo.AddComponent<LaunchPadController>();
                }
                _activeLaunchPad.transform.localPosition = Vector3.zero;
                _activeLaunchPad.transform.localRotation = Quaternion.identity;
            }

            _isPadPlaced = true;
            if (_activeReticle != null)
            {
                _activeReticle.gameObject.SetActive(false);
            }

            EventBus.TriggerPadPlaced(position, rotation);

            // Transition from placement to Aiming
            GameManager.Instance?.SetState(GameState.Aiming);
        }

        private bool IsPointerOverUI(Vector2 screenPos)
        {
            return UnityEngine.EventSystems.EventSystem.current != null &&
                   UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        }

        public void ResetPlacement()
        {
            _isPadPlaced = false;
            if (_spawnedPlayspace != null)
            {
                Destroy(_spawnedPlayspace);
                _spawnedPlayspace = null;
                _activeLaunchPad = null;
            }
            GameManager.Instance?.SetState(GameState.SurfaceScan);
        }
    }
}

