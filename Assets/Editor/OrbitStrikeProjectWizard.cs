#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OrbitStrike.AR;
using OrbitStrike.Core;
using OrbitStrike.Data;
using OrbitStrike.Environment;
using OrbitStrike.Rocket;
using OrbitStrike.Systems;
using OrbitStrike.Targets;
using OrbitStrike.UI;
using OrbitStrike.VFX;

namespace OrbitStrike.Editor
{
    /// <summary>
    /// Master Project Setup Wizard implementing Unity Editor Tool Developer principles.
    /// Generates materials, prefabs, ScriptableObjects, and scene assets in 1 click.
    /// </summary>
    public class OrbitStrikeProjectWizard : EditorWindow
    {
        [MenuItem("OrbitStrike/1. Project Setup Wizard", false, 1)]
        public static void ShowWindow()
        {
            var win = GetWindow<OrbitStrikeProjectWizard>("Orbit Strike Setup");
            win.minSize = new Vector2(480, 520);
        }

        private Vector2 _scroll;

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("ORBIT STRIKE AR — PROJECT SETUP WIZARD", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Automates generation of all URP materials, complete prefabs, ScriptableObject data assets (4 Rockets, 5 Worlds, 30 Levels), and scene wiring in compliance with Unity Architect standards.",
                MessageType.Info);

            GUILayout.Space(15);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            if (GUILayout.Button("★ EXECUTE COMPLETE 1-CLICK PROJECT SETUP", GUILayout.Height(45)))
            {
                ExecuteCompleteSetup();
            }

            GUILayout.Space(15);
            GUILayout.Label("Individual Pipeline Steps", EditorStyles.boldLabel);

            if (GUILayout.Button("Step 1: Generate All URP Materials"))
            {
                GenerateMaterials();
            }

            if (GUILayout.Button("Step 2: Generate ScriptableObjects (Rockets, Worlds, 30 Levels)"))
            {
                GenerateScriptableObjects();
            }

            if (GUILayout.Button("Step 3: Build & Save Complete Prefabs"))
            {
                BuildPrefabs();
            }

            EditorGUILayout.EndScrollView();
        }

        public static void ExecuteCompleteSetup()
        {
            EditorUtility.DisplayProgressBar("Orbit Strike Setup", "Generating Materials...", 0.2f);
            GenerateMaterials();

            EditorUtility.DisplayProgressBar("Orbit Strike Setup", "Generating ScriptableObjects...", 0.5f);
            GenerateScriptableObjects();

            EditorUtility.DisplayProgressBar("Orbit Strike Setup", "Building Complete Prefabs...", 0.8f);
            BuildPrefabs();

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Setup Complete", "All Orbit Strike AR materials, ScriptableObjects, and prefabs have been successfully built!", "OK");
        }

        public static void GenerateMaterials()
        {
            EnsureFolder("Assets/Materials");
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            CreateMaterial(shader, "Mat_NeonCyan", new Color(0f, 0.95f, 1f), true, 2.8f);
            CreateMaterial(shader, "Mat_WarningOrange", new Color(1f, 0.45f, 0f), true, 2.5f);
            CreateMaterial(shader, "Mat_TargetGreen", new Color(0f, 1f, 0.35f), true, 2.5f);
            CreateMaterial(shader, "Mat_TargetYellow", new Color(1f, 0.85f, 0.1f), true, 1.8f);
            CreateMaterial(shader, "Mat_DarkMetal", new Color(0.12f, 0.14f, 0.18f), false, 0f, 0.85f, 0.25f);
            CreateMaterial(shader, "Mat_RocketHull", new Color(0.92f, 0.94f, 0.96f), false, 0f, 0.5f, 0.3f);
            CreateMaterial(shader, "Mat_ShieldFresnel", new Color(0f, 0.6f, 1f, 0.4f), true, 1.5f);
            CreateMaterial(shader, "Mat_CraterDecal", new Color(0.08f, 0.08f, 0.08f, 0.9f), false, 0f);

            AssetDatabase.SaveAssets();
        }

        private static Material CreateMaterial(Shader shader, string name, Color color, bool emissive, float emissionBoost, float metallic = 0.5f, float smoothness = 0.5f)
        {
            string path = $"Assets/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.color = color;
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);

            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * emissionBoost);
            }

            EditorUtility.SetDirty(mat);
            return mat;
        }

        public static void GenerateScriptableObjects()
        {
            // 1. Rockets
            EnsureFolder("Assets/Resources/ScriptableObjects/Rockets");
            CreateRocketSO(RocketType.Basic, "Strike-1", "Standard agile tactical rocket.", new Color(0f, 0.95f, 1f), 1.2f, 1.8f, 55f, 2.4f, 0.28f, 120, 0, true);
            CreateRocketSO(RocketType.Heavy, "Titan-IV", "Armored siege rocket with huge blast radius.", new Color(1f, 0.45f, 0f), 2.5f, 3.5f, 85f, 3.2f, 0.45f, 250, 300, false);
            CreateRocketSO(RocketType.Speedster, "Velocity-X", "Ultra-lightweight sprint rocket.", new Color(0f, 1f, 0.4f), 0.7f, 1.2f, 65f, 1.5f, 0.18f, 90, 450, false);
            CreateRocketSO(RocketType.Precision, "Hawk-Eye", "Advanced fin stabilization for pinpoint trajectory.", new Color(0.9f, 0.2f, 1f), 1.0f, 1.5f, 48f, 2.8f, 0.15f, 140, 600, false);

            // 2. Worlds
            EnsureFolder("Assets/Resources/ScriptableObjects/Worlds");
            CreateWorldSO(WorldType.MojaveDesert, "Mojave Range", "Earth gravity with dry, open canyons.", -9.81f, new Color(0.9f, 0.65f, 0.3f));
            CreateWorldSO(WorldType.FrostpeakArctic, "Frostpeak Glacier", "High-velocity glacial crosswinds.", -9.81f, new Color(0.6f, 0.85f, 1f));
            CreateWorldSO(WorldType.NeoCity, "Neo-Metropolis", "Rooftops with dense obstacles and moving drones.", -9.81f, new Color(0f, 0.9f, 1f));
            CreateWorldSO(WorldType.CanopyJungle, "Canopy Deep", "Low-gravity Martian plateau terrain.", -3.71f, new Color(0.2f, 0.8f, 0.4f));
            CreateWorldSO(WorldType.LunarBase, "Lunar Base Alpha", "Vacuum conditions with lunar orbital parabolas.", -1.62f, new Color(0.85f, 0.85f, 0.9f));

            // 3. 30 Levels
            EnsureFolder("Assets/Resources/ScriptableObjects/Levels");
            for (int i = 1; i <= 30; i++)
            {
                LevelData level = LevelDatabase.GetLevel(i);
                string path = $"Assets/Resources/ScriptableObjects/Levels/Level_{i:D2}.asset";
                LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (existing == null)
                {
                    AssetDatabase.CreateAsset(level, path);
                }
                else
                {
                    EditorUtility.CopySerialized(level, existing);
                    EditorUtility.SetDirty(existing);
                }
            }

            AssetDatabase.SaveAssets();
        }

        private static void CreateRocketSO(RocketType type, string name, string desc, Color themeColor, float dryMass, float propellantMass, float peakThrust, float burnDuration, float cd, int dmg, int cost, bool unlockedByDefault)
        {
            string path = $"Assets/Resources/ScriptableObjects/Rockets/Rocket_{type}.asset";
            RocketData so = AssetDatabase.LoadAssetAtPath<RocketData>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<RocketData>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.rocketType = type;
            so.rocketName = name;
            so.description = desc;
            so.themeColor = themeColor;
            so.dryMass = dryMass;
            so.propellantMass = propellantMass;
            so.peakThrust = peakThrust;
            so.burnDuration = burnDuration;
            so.dragCoefficient = cd;
            so.directHitDamage = dmg;
            so.unlockCost = cost;
            so.isUnlockedByDefault = unlockedByDefault;
            so.thrustCurve = AnimationCurve.EaseInOut(0f, 1.2f, 1f, 0.75f);

            EditorUtility.SetDirty(so);
        }

        private static void CreateWorldSO(WorldType type, string name, string desc, float gravity, Color color)
        {
            string path = $"Assets/Resources/ScriptableObjects/Worlds/World_{type}.asset";
            WorldConfig so = AssetDatabase.LoadAssetAtPath<WorldConfig>(path);
            if (so == null)
            {
                so = ScriptableObject.CreateInstance<WorldConfig>();
                AssetDatabase.CreateAsset(so, path);
            }

            so.worldType = type;
            so.worldName = name;
            so.worldDescription = desc;
            so.gravity = gravity;
            so.primaryColor = color;
            so.ambientLightingColor = color * 0.4f;

            EditorUtility.SetDirty(so);
        }

        public static void BuildPrefabs()
        {
            EnsureFolder("Assets/Prefabs/LaunchPad");
            EnsureFolder("Assets/Prefabs/Rockets");
            EnsureFolder("Assets/Prefabs/Targets");
            EnsureFolder("Assets/Prefabs/VFX");

            // Launch Pad Prefab
            GameObject padGo = ProceduralAssetFactory.CreateProceduralLaunchPad();
            padGo.AddComponent<LaunchPadController>();
            SaveAsPrefab(padGo, "Assets/Prefabs/LaunchPad/LaunchPad.prefab");
            DestroyImmediate(padGo);

            // 4 Rocket Prefabs
            BuildRocketPrefab(RocketType.Basic, new Color(0f, 0.95f, 1f));
            BuildRocketPrefab(RocketType.Heavy, new Color(1f, 0.45f, 0f));
            BuildRocketPrefab(RocketType.Speedster, new Color(0f, 1f, 0.4f));
            BuildRocketPrefab(RocketType.Precision, new Color(0.9f, 0.2f, 1f));

            // Bullseye Target Prefab
            GameObject targetGo = ProceduralAssetFactory.CreateProceduralBullseyeTarget();
            targetGo.AddComponent<BullseyeTarget>();
            SaveAsPrefab(targetGo, "Assets/Prefabs/Targets/Target_Bullseye.prefab");
            DestroyImmediate(targetGo);

            AssetDatabase.SaveAssets();
        }

        private static void BuildRocketPrefab(RocketType type, Color themeColor)
        {
            GameObject rocketGo = ProceduralAssetFactory.CreateProceduralRocket($"Rocket_{type}", themeColor);
            var rb = rocketGo.AddComponent<Rigidbody>();
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var ctrl = rocketGo.AddComponent<RocketController>();
            RocketData so = AssetDatabase.LoadAssetAtPath<RocketData>($"Assets/Resources/ScriptableObjects/Rockets/Rocket_{type}.asset");
            if (so != null) ctrl.Initialize(so);

            SaveAsPrefab(rocketGo, $"Assets/Prefabs/Rockets/Rocket_{type}.prefab");
            DestroyImmediate(rocketGo);
        }

        private static void SaveAsPrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        private static void EnsureFolder(string path)
        {
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }
    }
}
#endif

