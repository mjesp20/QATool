using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QATool
{
    // -----------------------------------------------------------------------
    // Data shape — matches the JSON written by QAToolBuildProcessor
    // -----------------------------------------------------------------------
    [Serializable]
    public class QAToolConfigData
    {
        public float dataPointsPerSecond = 10f;
        public string feedbackKeyCode    = "F1";
        public List<FlagDefinition> flagDefinitions = new List<FlagDefinition>();

        [Serializable]
        public class FlagDefinition
        {
            public string key;
            public string typeName; // "int" | "float" | "bool" | "string"
        }
    }

    // -----------------------------------------------------------------------
    // QAToolConfig
    //   • In the editor  → reads / writes EditorPrefs directly (no asset file)
    //   • In a build     → reads the JSON baked into StreamingAssets by
    //                       QAToolBuildProcessor at build time
    // -----------------------------------------------------------------------
    public static class QAToolConfig
    {
        // Path inside StreamingAssets that the build processor writes to
        public const string StreamingAssetsRelativePath = "QATool/config.json";

        // EditorPrefs keys (kept internal so the build processor can reuse them)
        internal const string PrefDataPointsPerSecond = "QATool_dataPointsPerSecond";
        internal const string PrefFeedbackKeyCode     = "QATool_feedbackKeyCode";
        internal const string PrefFlagDefinitions     = "QATool_flagDefinitions";

#if UNITY_EDITOR
        // ----------------------------------------------------------------
        // Editor path — live EditorPrefs, no file on disk
        // ----------------------------------------------------------------

        public static float DataPointsPerSecond
        {
            get => EditorPrefs.GetFloat(PrefDataPointsPerSecond, 10f);
            set => EditorPrefs.SetFloat(PrefDataPointsPerSecond, value);
        }

        public static string FeedbackKeyCode
        {
            get => EditorPrefs.GetString(PrefFeedbackKeyCode, "F1");
            set => EditorPrefs.SetString(PrefFeedbackKeyCode, value);
        }

        public static List<QAToolConfigData.FlagDefinition> FlagDefinitions
        {
            get
            {
                string json = EditorPrefs.GetString(PrefFlagDefinitions, null);
                if (string.IsNullOrEmpty(json)) return new List<QAToolConfigData.FlagDefinition>();
                return JsonUtility.FromJson<FlagDefListWrapper>(json)?.items
                       ?? new List<QAToolConfigData.FlagDefinition>();
            }
            set
            {
                var wrapper = new FlagDefListWrapper { items = value ?? new List<QAToolConfigData.FlagDefinition>() };
                EditorPrefs.SetString(PrefFlagDefinitions, JsonUtility.ToJson(wrapper));
            }
        }

        public static Dictionary<string, Type> GetFlagTypes()
            => ParseFlagTypes(FlagDefinitions);

#else
        // ----------------------------------------------------------------
        // Build / runtime path — reads StreamingAssets/QATool/config.json
        // ----------------------------------------------------------------

        private static QAToolConfigData _data;

        private static QAToolConfigData Data
        {
            get
            {
                if (_data == null) Load();
                return _data ?? (_data = new QAToolConfigData());
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            string path = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativePath);
            if (!File.Exists(path))
            {
                Debug.LogWarning("[QATool] config.json not found in StreamingAssets. Using default values."); //this wont be visible most likely
                _data = new QAToolConfigData();
                return;
            }

            try
            {
                _data = JsonUtility.FromJson<QAToolConfigData>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[QATool] Failed to parse config.json: {e.Message}. Using default values.");
                _data = new QAToolConfigData();
            }
        }

        public static float  DataPointsPerSecond => Data.dataPointsPerSecond;
        public static string FeedbackKeyCode     => Data.feedbackKeyCode;

        public static Dictionary<string, Type> GetFlagTypes()
            => ParseFlagTypes(Data.flagDefinitions);

#endif

        // ----------------------------------------------------------------
        // Shared helpers
        // ----------------------------------------------------------------

        private static Dictionary<string, Type> ParseFlagTypes(List<QAToolConfigData.FlagDefinition> defs)
        {
            var dict = new Dictionary<string, Type>();
            foreach (var def in defs)
            {
                Type t = def.typeName switch
                {
                    "int"    => typeof(int),
                    "float"  => typeof(float),
                    "bool"   => typeof(bool),
                    "string" => typeof(string),
                    _        => null
                };
                if (t != null) dict[def.key] = t;
            }
            return dict;
        }

        // JsonUtility can't serialise a bare List<T> — wrap it
        [Serializable]
        private class FlagDefListWrapper
        {
            public List<QAToolConfigData.FlagDefinition> items = new List<QAToolConfigData.FlagDefinition>();
        }
    }
}
