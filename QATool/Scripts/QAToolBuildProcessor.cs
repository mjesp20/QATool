#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace QATool
{
    /// <summary>
    /// Hooks into the Unity build pipeline to:
    ///   1. (Pre-build)  Read QATool settings from EditorPrefs and write them to
    ///                   Assets/StreamingAssets/QATool/config.json so the built
    ///                   player can access them via File.ReadAllText at runtime.
    ///   2. (Post-build) Delete that file (and its .meta) so it never permanently
    ///                   lives inside the project's Assets folder.
    /// </summary>
    public class QAToolBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuild
    {
        // Lower numbers run earlier; 0 is fine for our purposes
        public int callbackOrder => 0;

        // Paths are relative to the project root (same convention as AssetDatabase)
        private const string StreamingAssetsDir  = "Assets/StreamingAssets";
        private const string QAToolDir           = "Assets/StreamingAssets/QATool";
        private const string ConfigAssetPath     = "Assets/StreamingAssets/QATool/config.json";

        // ----------------------------------------------------------------
        // PRE-BUILD: write config.json into StreamingAssets
        // ----------------------------------------------------------------
        public void OnPreprocessBuild(BuildReport report)
        {
            var config = BuildConfigFromPrefs();
            WriteConfigFile(config);
            Debug.Log("[QATool] config.json written to StreamingAssets.");
        }

        // ----------------------------------------------------------------
        // POST-BUILD: remove config.json so it doesn't pollute the project
        // ----------------------------------------------------------------
        public void OnPostprocessBuild(BuildTarget target, string path)
        {
            DeleteConfigFile();
            Debug.Log("[QATool] config.json removed from StreamingAssets.");
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------

        private static QAToolConfigData BuildConfigFromPrefs()
        {
            // Read flag definitions stored as JSON-wrapped list in EditorPrefs
            List<QAToolConfigData.FlagDefinition> flagDefs = QAToolConfig.FlagDefinitions;

            return new QAToolConfigData
            {
                dataPointsPerSecond = EditorPrefs.GetFloat(QAToolConfig.PrefDataPointsPerSecond, 10f),
                feedbackKeyCode     = EditorPrefs.GetString(QAToolConfig.PrefFeedbackKeyCode, "F1"),
                flagDefinitions     = flagDefs
            };
        }

        private static void WriteConfigFile(QAToolConfigData config)
        {
            // Ensure StreamingAssets/QATool/ exists
            if (!Directory.Exists(StreamingAssetsDir))
                Directory.CreateDirectory(StreamingAssetsDir);

            if (!Directory.Exists(QAToolDir))
                Directory.CreateDirectory(QAToolDir);

            string json = JsonUtility.ToJson(config, prettyPrint: true);
            File.WriteAllText(ConfigAssetPath, json);

            // Tell Unity about the new file so it gets included in the build
            AssetDatabase.Refresh();
        }

        private static void DeleteConfigFile()
        {
            if (File.Exists(ConfigAssetPath))
            {
                File.Delete(ConfigAssetPath);
                string meta = ConfigAssetPath + ".meta";
                if (File.Exists(meta)) File.Delete(meta);
            }

            // Remove the QATool subfolder if now empty
            if (Directory.Exists(QAToolDir) && Directory.GetFileSystemEntries(QAToolDir).Length == 0)
            {
                Directory.Delete(QAToolDir);
                string dirMeta = QAToolDir + ".meta";
                if (File.Exists(dirMeta)) File.Delete(dirMeta);
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif
