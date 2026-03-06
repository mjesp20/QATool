using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
#endif


namespace QATool
{
    [CreateAssetMenu(fileName = "QAToolConfig", menuName = "QATool/Config")]
    public class QAToolConfig : ScriptableObject
    {
        [Serializable]
        public class FlagDefinition
        {
            public string key;
            public string typeName; // "int", "float", "bool", "string"
        }

        public float dataPointsPerSecond = 10f;
        public string feedbackKeyCode = "F1";

        public List<FlagDefinition> flagDefinitions = new();

        private static QAToolConfig _instance;
        public static QAToolConfig Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance == null)
                    _instance = GetOrCreate();
#endif
                return _instance;
            }
            set => _instance = value;
        }
        private void OnEnable()
        {
            _instance = this;
        }

        public Dictionary<string, Type> GetFlagTypes()
        {
            var dict = new Dictionary<string, Type>();
            foreach (var def in flagDefinitions)
            {
                Type t = def.typeName switch
                {
                    "int" => typeof(int),
                    "float" => typeof(float),
                    "bool" => typeof(bool),
                    "string" => typeof(string),
                    _ => null
                };
                if (t != null) dict[def.key] = t;
            }
            return dict;
        }

#if UNITY_EDITOR
        public const string AssetPath = "Assets/QATool/QAToolConfig.asset";

        public static QAToolConfig GetOrCreate()
        {
            var config = AssetDatabase.LoadAssetAtPath<QAToolConfig>(AssetPath);
            if (config != null) return config;

            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/QATool"))
                AssetDatabase.CreateFolder("Assets", "QATool");

            config = CreateInstance<QAToolConfig>();
            AssetDatabase.CreateAsset(config, AssetPath);
            AssetDatabase.SaveAssets();

            AddToPreloadedAssets(config);
            return config;
        }

        public static void AddToPreloadedAssets(QAToolConfig config)
        {
            var preloaded = PlayerSettings.GetPreloadedAssets().ToList();
            if (!preloaded.Contains(config))
            {
                preloaded.Add(config);
                PlayerSettings.SetPreloadedAssets(preloaded.ToArray());
            }
        }
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            // Instance is already set via the setter above from preloaded assets
            // This just ensures it's been loaded before anything tries to use it
            if (_instance == null)
                Debug.LogWarning("[QATool] QAToolConfig not found in Preloaded Assets. Run the QATool editor window to generate it.");
        }
    }
}