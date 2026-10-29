using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using OrbitStrike.AR;
using OrbitStrike.Audio;
using OrbitStrike.Data;
using OrbitStrike.Environment;
using OrbitStrike.Rocket;
using OrbitStrike.Systems;
using OrbitStrike.UI;

namespace OrbitStrike.Core
{
    /// <summary>
    /// Automatic scene orchestrator: Ensures all managers, AR subsystems, audio sources,
    /// pools, and UI hierarchies are initialized seamlessly on startup.
    /// Includes Editor simulation camera mode so the screen is NEVER black in Editor.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class OrbitStrikeSceneBootstrapper : MonoBehaviour
    {
        private void Awake()
        {
            BootstrapCoreSystems();
        }

        public static void BootstrapCoreSystems()
        {
            // 1. Core Systems & Persistence
            EnsureManager<GameManager>("GameManager");
            EnsureManager<SaveManager>("SaveManager");
            EnsureManager<AudioManager>("AudioManager");
            EnsureManager<ObjectPooler>("ObjectPooler");
            EnsureManager<CameraShake>("CameraShake");
            EnsureManager<HapticsManager>("HapticsManager");

            // 2. AR Foundation Components & Editor Safeguards
            EnsureARFoundationRig();

            // 3. Gameplay & Physics Systems
            EnsureManager<ARPlacementManager>("ARPlacementManager");
            EnsureManager<RocketSpawner>("RocketSpawner");
            EnsureManager<TrajectoryPredictor>("TrajectoryPredictor");
            EnsureManager<LaunchSequencer>("LaunchSequencer");
            EnsureManager<WindSystem>("WindSystem");
            EnsureManager<GravityController>("GravityController");
            EnsureManager<WorldDioramaManager>("WorldDioramaManager");

            // 4. Editor Visual Floor
            EnsureEditorGroundGrid();

            // 5. Ensure EventSystem
            EnsureEventSystem();

            Debug.Log("[OrbitStrikeSceneBootstrapper] All Orbit Strike AR core systems successfully initialized.");
        }

        private static void EnsureEventSystem()
        {
            if (FindAny<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        private static T EnsureManager<T>(string gameObjectName) where T : Component
        {
            T existing = FindAny<T>();
            if (existing != null) return existing;

            GameObject go = new GameObject($"[{gameObjectName}]");
            T comp = go.AddComponent<T>();
            return comp;
        }

        public static T FindAny<T>() where T : Component
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindAnyObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        private static void EnsureARFoundationRig()
        {
            if (FindAny<ARSession>() == null)
            {
                GameObject sessionGo = new GameObject("AR Session");
                sessionGo.AddComponent<ARSession>();
                sessionGo.AddComponent<ARInputManager>();
            }

            if (FindAny<XROrigin>() == null)
            {
                GameObject originGo = new GameObject("XR Origin");
                var origin = originGo.AddComponent<XROrigin>();
                originGo.AddComponent<ARRaycastManager>();
                originGo.AddComponent<ARPlaneManager>();

                // Setup Camera
                Camera mainCam = Camera.main;
                if (mainCam == null)
                {
                    GameObject camGo = new GameObject("AR Camera");
                    camGo.transform.SetParent(originGo.transform);
                    camGo.tag = "MainCamera";
                    mainCam = camGo.AddComponent<Camera>();
                }
                else
                {
                    mainCam.transform.SetParent(originGo.transform);
                }

                // Optimal angled perspective
                mainCam.transform.localPosition = new Vector3(0, 2.2f, -4.5f);
                mainCam.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

                origin.Camera = mainCam;

                if (mainCam.GetComponent<ARCameraManager>() == null)
                {
                    mainCam.gameObject.AddComponent<ARCameraManager>();
                }

                var bg = mainCam.GetComponent<ARCameraBackground>();
                if (bg == null)
                {
                    bg = mainCam.gameObject.AddComponent<ARCameraBackground>();
                }

#if UNITY_EDITOR
                // CRITICAL: Disable ARCameraBackground in Editor so screen is not black!
                bg.enabled = false;
                mainCam.clearFlags = CameraClearFlags.SolidColor;
                mainCam.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
#endif
            }
            else
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
#if UNITY_EDITOR
                    var bg = mainCam.GetComponent<ARCameraBackground>();
                    if (bg != null) bg.enabled = false;
                    mainCam.clearFlags = CameraClearFlags.SolidColor;
                    mainCam.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
#endif
                }
            }
        }

        private static void EnsureEditorGroundGrid()
        {
#if UNITY_EDITOR
            if (GameObject.Find("AR_Simulation_Floor") == null)
            {
                GameObject grid = GameObject.CreatePrimitive(PrimitiveType.Plane);
                grid.name = "AR_Simulation_Floor";
                grid.transform.position = Vector3.zero;
                grid.transform.localScale = new Vector3(2.5f, 1f, 2.5f);

                var mr = grid.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.material = ProceduralAssetFactory.CreateHolographicGlowMaterial(new Color(0f, 0.4f, 0.8f, 0.35f), 1.2f);
                }
            }
#endif
        }
    }
}
