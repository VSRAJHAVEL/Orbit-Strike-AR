using UnityEngine;

namespace OrbitStrike.Core
{
    /// <summary>
    /// Generates high-fidelity procedural sci-fi meshes and materials for Orbit Strike AR.
    /// Ensures a 100% placeholder-free visual presentation without external DCC asset dependencies.
    /// </summary>
    public static class ProceduralAssetFactory
    {
        public static Material CreateHolographicGlowMaterial(Color baseColor, float emissionMultiplier = 2.5f)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(shader)
            {
                name = $"Mat_Holo_{baseColor}"
            };

            mat.color = baseColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", baseColor * emissionMultiplier);
            return mat;
        }

        public static GameObject CreateProceduralRocket(string name, Color accentColor)
        {
            GameObject root = new GameObject(name);

            // Fuselage (Cylinder)
            GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            fuselage.name = "Fuselage";
            fuselage.transform.SetParent(root.transform, false);
            fuselage.transform.localScale = new Vector3(0.18f, 0.45f, 0.18f);
            fuselage.transform.localPosition = new Vector3(0, 0.45f, 0);

            // Nose Cone
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            nose.name = "NoseCone";
            nose.transform.SetParent(root.transform, false);
            nose.transform.localScale = new Vector3(0.18f, 0.35f, 0.18f);
            nose.transform.localPosition = new Vector3(0, 0.9f, 0);

            // 4 Stabilizing Aerodynamic Fins
            for (int i = 0; i < 4; i++)
            {
                GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fin.name = $"Fin_{i}";
                fin.transform.SetParent(root.transform, false);
                fin.transform.localScale = new Vector3(0.02f, 0.22f, 0.16f);

                float angle = i * 90f;
                fin.transform.localRotation = Quaternion.Euler(0, angle, 0);
                fin.transform.localPosition = Quaternion.Euler(0, angle, 0) * new Vector3(0, 0.12f, 0.16f);

                // Tint fins with accent color
                var finRenderer = fin.GetComponent<MeshRenderer>();
                if (finRenderer != null)
                {
                    finRenderer.material = CreateHolographicGlowMaterial(accentColor, 1.2f);
                }
            }

            // Engine Nozzle
            GameObject nozzle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nozzle.name = "EngineNozzle";
            nozzle.transform.SetParent(root.transform, false);
            nozzle.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
            nozzle.transform.localPosition = new Vector3(0, 0.02f, 0);

            return root;
        }

        public static GameObject CreateProceduralLaunchPad()
        {
            GameObject pad = new GameObject("LaunchPad_Assembly");

            // Base Platform (Hexagonal / Beveled Cylinder)
            GameObject basePlatform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePlatform.name = "BasePlatform";
            basePlatform.transform.SetParent(pad.transform, false);
            basePlatform.transform.localScale = new Vector3(1.2f, 0.06f, 1.2f);

            // Blast Deflector Ramp
            GameObject deflector = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deflector.name = "BlastDeflector";
            deflector.transform.SetParent(pad.transform, false);
            deflector.transform.localScale = new Vector3(0.5f, 0.04f, 0.5f);
            deflector.transform.localPosition = new Vector3(0, 0.08f, -0.2f);
            deflector.transform.localRotation = Quaternion.Euler(30, 0, 0);

            // Gantry Arm
            GameObject gantry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gantry.name = "GantrySupport";
            gantry.transform.SetParent(pad.transform, false);
            gantry.transform.localScale = new Vector3(0.08f, 1.0f, 0.08f);
            gantry.transform.localPosition = new Vector3(0.4f, 0.5f, 0);

            // Runway Indicator Lights (4 perimeter beacons)
            for (int i = 0; i < 4; i++)
            {
                GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                beacon.name = $"Beacon_{i}";
                beacon.transform.SetParent(pad.transform, false);
                beacon.transform.localScale = Vector3.one * 0.06f;

                float angle = i * 90f + 45f;
                beacon.transform.localPosition = Quaternion.Euler(0, angle, 0) * new Vector3(0.55f, 0.05f, 0);

                var beaconRenderer = beacon.GetComponent<MeshRenderer>();
                if (beaconRenderer != null)
                {
                    beaconRenderer.material = CreateHolographicGlowMaterial(Color.cyan, 3.0f);
                }
            }

            return pad;
        }

        public static GameObject CreateProceduralBullseyeTarget()
        {
            GameObject target = new GameObject("Target_Bullseye");

            // Outer Ring (25 pts) - Yellow
            GameObject outer = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            outer.name = "Ring_Outer_25";
            outer.transform.SetParent(target.transform, false);
            outer.transform.localScale = new Vector3(2.5f, 0.02f, 2.5f);
            outer.GetComponent<MeshRenderer>().material = CreateHolographicGlowMaterial(new Color(1f, 0.8f, 0.1f), 1.0f);

            // Middle Ring (50 pts) - Cyan
            GameObject middle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            middle.name = "Ring_Middle_50";
            middle.transform.SetParent(target.transform, false);
            middle.transform.localScale = new Vector3(1.5f, 0.03f, 1.5f);
            middle.transform.localPosition = new Vector3(0, 0.01f, 0);
            middle.GetComponent<MeshRenderer>().material = CreateHolographicGlowMaterial(new Color(0f, 0.85f, 1f), 1.5f);

            // Inner Bullseye (100 pts) - Neon Green
            GameObject bullseye = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bullseye.name = "Ring_Bullseye_100";
            bullseye.transform.SetParent(target.transform, false);
            bullseye.transform.localScale = new Vector3(0.7f, 0.04f, 0.7f);
            bullseye.transform.localPosition = new Vector3(0, 0.02f, 0);
            bullseye.GetComponent<MeshRenderer>().material = CreateHolographicGlowMaterial(new Color(0f, 1f, 0.35f), 2.5f);

            return target;
        }
    }
}

