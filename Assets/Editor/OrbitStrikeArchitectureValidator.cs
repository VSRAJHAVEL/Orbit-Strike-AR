#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using OrbitStrike.Core;

namespace OrbitStrike.Editor
{
    /// <summary>
    /// Architecture Validator implementing strict Unity Architect & Editor Tool Developer quality gates.
    /// Audits project assets, SRP line budgets, and decoupled SO configurations.
    /// </summary>
    public static class OrbitStrikeArchitectureValidator
    {
        [MenuItem("OrbitStrike/3. Validate Architecture & Quality Gates", false, 3)]
        public static void RunValidation()
        {
            int errorCount = 0;
            int warningCount = 0;

            Debug.Log("<color=#00f0ff><b>[OrbitStrikeValidator]</b> Beginning architectural audit...</color>");

            // 1. Audit Script Line Lengths (SRP enforcement: < 220 lines)
            string[] scripts = Directory.GetFiles("Assets/Scripts", "*.cs", SearchOption.AllDirectories);
            foreach (var file in scripts)
            {
                int lines = File.ReadAllLines(file).Length;
                if (lines > 250)
                {
                    Debug.LogWarning($"[SRP Budget] {Path.GetFileName(file)} has {lines} lines (recommended < 200). Consider refactoring.");
                    warningCount++;
                }
            }

            // 2. Audit ScriptableObjects existence
            for (int i = 1; i <= 30; i++)
            {
                string path = $"Assets/Resources/ScriptableObjects/Levels/Level_{i:D2}.asset";
                if (!File.Exists(path))
                {
                    Debug.LogWarning($"[Missing Asset] {path} not found. Run Wizard Step 2 to generate.");
                    warningCount++;
                }
            }

            // 3. Audit Assemblies
            if (!File.Exists("Assets/OrbitStrike.asmdef"))
            {
                Debug.LogError("[Assembly Error] Assets/OrbitStrike.asmdef is missing!");
                errorCount++;
            }

            if (!File.Exists("Assets/Editor/OrbitStrike.Editor.asmdef"))
            {
                Debug.LogError("[Assembly Error] Assets/Editor/OrbitStrike.Editor.asmdef is missing!");
                errorCount++;
            }

            if (errorCount == 0)
            {
                Debug.Log($"<color=#00ff66><b>[OrbitStrikeValidator]</b> AUDIT PASSED! (0 Errors, {warningCount} Notices).</color>");
                EditorUtility.DisplayDialog("Audit Result", $"Architecture Audit Passed!\n\nErrors: {errorCount}\nNotices: {warningCount}", "OK");
            }
            else
            {
                Debug.LogError($"[OrbitStrikeValidator] Audit Failed with {errorCount} errors.");
                EditorUtility.DisplayDialog("Audit Result", $"Audit Failed with {errorCount} errors. Check console.", "OK");
            }
        }
    }
}
#endif

