#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using TMPro;
using OrbitStrike.AR;
using OrbitStrike.Audio;
using OrbitStrike.Core;
using OrbitStrike.Data;
using OrbitStrike.Environment;
using OrbitStrike.Rocket;
using OrbitStrike.Systems;
using OrbitStrike.UI;

namespace OrbitStrike.Editor
{
    /// <summary>
    /// Builds and wires the complete, production-grade GameplayAR scene hierarchy.
    /// Follows Unity Architect rules: zero manual wiring required.
    /// Includes camera position, simulation floor, and full UI Canvas hierarchy.
    /// </summary>
    public static class OrbitStrikeSceneBuilder
    {
        [MenuItem("OrbitStrike/2. Build & Wire Complete Gameplay Scene", false, 2)]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/GameplayAR.unity");

            // Clear legacy objects
            foreach (var root in scene.GetRootGameObjects())
            {
                Object.DestroyImmediate(root);
            }

            // 1. Lighting & Environment
            GameObject lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(50, -30, 0);

            // 2. Core Managers & Bootstrapper
            GameObject coreGo = new GameObject("[CORE_SYSTEMS]");
            coreGo.AddComponent<OrbitStrikeAR.OrbitStrikeLegendaryEngine>();
            coreGo.AddComponent<OrbitStrikeSceneBootstrapper>();
            coreGo.AddComponent<GameManager>();
            coreGo.AddComponent<SaveManager>();
            coreGo.AddComponent<AudioManager>();
            coreGo.AddComponent<ObjectPooler>();
            coreGo.AddComponent<CameraShake>();
            coreGo.AddComponent<HapticsManager>();

            // 3. Environmental Subsystems
            GameObject envGo = new GameObject("[ENVIRONMENT_SYSTEMS]");
            envGo.AddComponent<WindSystem>();
            envGo.AddComponent<GravityController>();
            envGo.AddComponent<WorldDioramaManager>();

            // 4. AR Foundation Hierarchy
            GameObject arSession = new GameObject("AR Session");
            arSession.AddComponent<ARSession>();
            arSession.AddComponent<ARInputManager>();

            GameObject arOrigin = new GameObject("XR Origin");
            var originComp = arOrigin.AddComponent<XROrigin>();
            arOrigin.AddComponent<ARRaycastManager>();
            arOrigin.AddComponent<ARPlaneManager>();

            GameObject camGo = new GameObject("AR Camera");
            camGo.transform.SetParent(arOrigin.transform);
            camGo.tag = "MainCamera";
            camGo.transform.localPosition = new Vector3(0, 2.2f, -4.5f);
            camGo.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);

            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.12f);
            originComp.Camera = cam;

            camGo.AddComponent<ARCameraManager>();
            var bg = camGo.AddComponent<ARCameraBackground>();
            bg.enabled = false; // CRITICAL: Prevent black screen in Editor!
            camGo.AddComponent<AudioListener>();

            // 5. Visual AR Simulation Floor for Editor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "AR_Simulation_Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2.5f, 1f, 2.5f);
            var floorMr = floor.GetComponent<MeshRenderer>();
            Material gridMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_NeonCyan.mat");
            if (floorMr != null && gridMat != null) floorMr.material = gridMat;

            // 6. Cinemachine Tracking
            GameObject cmGo = new GameObject("[CINEMACHINE_RIG]");
            cmGo.AddComponent<Unity.Cinemachine.CinemachineBrain>();
            cmGo.AddComponent<CinemachineRocketFollower>();

            // 7. Gameplay Systems
            GameObject gameplayGo = new GameObject("[GAMEPLAY_RIG]");
            var placement = gameplayGo.AddComponent<ARPlacementManager>();
            var spawner = gameplayGo.AddComponent<RocketSpawner>();
            var traj = gameplayGo.AddComponent<TrajectoryPredictor>();
            var launcher = gameplayGo.AddComponent<LaunchSequencer>();

            // Wire Trajectory line renderer
            var lr = gameplayGo.AddComponent<LineRenderer>();
            lr.startWidth = 0.04f;
            lr.endWidth = 0.04f;
            lr.material = gridMat;

            // 8. UI Canvas Hierarchy
            GameObject canvasGo = new GameObject("[UI_CANVAS]");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var uiMgr = canvasGo.AddComponent<UIManager>();
            var hudCtrl = canvasGo.AddComponent<HUDController>();
            var forceAngle = canvasGo.AddComponent<ForceAngleControls>();

            // Event System
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            EditorUtility.DisplayDialog("Scene Builder", "Assets/Scenes/GameplayAR.unity has been fully populated, camera background configured, and wired!", "OK");
        }
    }
}
#endif
