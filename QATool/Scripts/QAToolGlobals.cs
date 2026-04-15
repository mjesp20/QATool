using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace QATool
{
    public static class QAToolGlobals
    {
        public static string name          = "QATool";
        public static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public static string folderPath    = Path.Combine(documentsPath, name, Application.productName);
        public static float ghostTrailThickness = 2f;
        public static int feedbackPreviewLength = 4;
        public static float renderRadius = 200f;
        
        
        
        
        public static void ResetToDefaults()
        {
            showGhostTrails   = true;
            showHeatMap       = false;
            showFeedbackNotes = true;
            showEvents        = false;

            feedbackKeyCode       = "F";
            feedbackPreviewLength = 10;
            renderRadius          = 50f;
            dataPointsPerSecond   = 10f;
            ghostTrailThickness   = 2f;

            heatmapCellSize       = 1f;
            heatmapOpacity        = 0.5f;
            heatmapContrast       = 1f;
            heatmapMinPercentile  = 0f;
            heatmapMaxPercentile  = 1f;
        }

        // -------------------------------------------------------------------
        // Flag types — editor writes to EditorPrefs via QAToolConfig;
        //              builds read from the baked JSON via QAToolConfig
        // -------------------------------------------------------------------
        public static Dictionary<string, Type> flagTypes
        {
            get => QAToolConfig.GetFlagTypes();
#if UNITY_EDITOR
            set
            {
                QAToolConfig.FlagDefinitions = value?
                    .Select(kvp => new QAToolConfigData.FlagDefinition
                    {
                        key      = kvp.Key,
                        typeName = typeNameToString[kvp.Value.Name]
                    })
                    .ToList()
                    ?? new List<QAToolConfigData.FlagDefinition>();
            }
#endif
        }

        // -------------------------------------------------------------------
        // Flag values — runtime read / write used by gameplay scripts
        // -------------------------------------------------------------------
        private static readonly Dictionary<string, object> _flagValues = new Dictionary<string, object>();

        public static Dictionary<string, object> flagValues
        {
            get
            {
                var types = flagTypes;
                if (types == null || types.Count == 0)
                    return new Dictionary<string, object>(); // Never hand null to callers

                var dict = new Dictionary<string, object>();
                foreach (var kvp in types)
                    dict[kvp.Key] = _flagValues.GetValueOrDefault(kvp.Key);
                return dict;
            }
        }

        public static object GetFlagValue(string key)              => _flagValues.GetValueOrDefault(key);
        public static void SetFlagValue(string key, object value)
        {
            // Auto-register the flag type if it hasn't been defined yet
            if (value != null && !flagTypes.ContainsKey(key))
            {
                var inferredType = value.GetType(); //issue: Floats that are whole numbers (i.e 1 2 etc) will be defined as ints

#if UNITY_EDITOR
                // Persist the new flag definition so it survives domain reloads
                var updatedTypes = flagTypes ?? new Dictionary<string, Type>();
                updatedTypes[key] = inferredType;
                flagTypes = updatedTypes;
                Debug.LogWarning($"[QATool] Flag '{key}' was not defined. Auto-registered as '{inferredType.Name}'. " +
                                 $"Consider adding it explicitly in the QA Tool config.");
#endif
            }

            _flagValues[key] = value;
        }

        public static void Event(Dictionary<string, object> dict) =>
            QAToolPlayerTracker.Instance.QAToolEvent(dict);

        // -------------------------------------------------------------------
        // Config values — EditorPrefs in editor, JSON in builds
        // -------------------------------------------------------------------
        public static float dataPointsPerSecond
        {
            get => QAToolConfig.DataPointsPerSecond;
#if UNITY_EDITOR
            set => QAToolConfig.DataPointsPerSecond = value;
#endif
        }

        public static string feedbackKeyCode
        {
            get => QAToolConfig.FeedbackKeyCode;
#if UNITY_EDITOR
            set => QAToolConfig.FeedbackKeyCode = value;
#endif
        }

        // -------------------------------------------------------------------
        // Editor-only visualisation / filter prefs
        // -------------------------------------------------------------------
#if UNITY_EDITOR
        public static bool showGhostTrails
        {
            get => EditorPrefs.GetBool("showGhostTrails");
            set => EditorPrefs.SetBool("showGhostTrails", value);
        }

        public static bool showHeatMap
        {
            get => EditorPrefs.GetBool("showHeatMap");
            set => EditorPrefs.SetBool("showHeatMap", value);
        }

        public static bool showFeedbackNotes
        {
            get => EditorPrefs.GetBool("showFeedbackNotes");
            set => EditorPrefs.SetBool("showFeedbackNotes", value);
        }
        public static bool showEvents        {
            get => EditorPrefs.GetBool("showEvents");
            set => EditorPrefs.SetBool("showEvents", value);
        }

        public static float heatmapCellSize
        {
            get => EditorPrefs.GetFloat("cellSize");
            set => EditorPrefs.SetFloat("cellSize", value);
        }

        public static float heatmapOpacity
        {
            get => EditorPrefs.GetFloat("Opacity");
            set => EditorPrefs.SetFloat("Opacity", value);
        }

        public static float heatmapContrast
        {
            get => EditorPrefs.GetFloat("Contrast");
            set => EditorPrefs.SetFloat("Contrast", value);
        }

        public static float heatmapHeightOffset
        {
            get => EditorPrefs.GetFloat("Height Offset");
            set => EditorPrefs.SetFloat("Height Offset", value);
        }

        public static float heatmapMinPercentile
        {
            get => EditorPrefs.GetFloat("Min Percentile Draw");
            set => EditorPrefs.SetFloat("Min Percentile Draw", value);
        }

        public static float heatmapMaxPercentile
        {
            get => EditorPrefs.GetFloat("Max Percentile Draw");
            set => EditorPrefs.SetFloat("Max Percentile Draw", value);
        }

        public static Dictionary<string, FlagFilter> FlagFilters
        {
            get
            {
                var dict = new Dictionary<string, FlagFilter>();
                string raw = EditorPrefs.GetString("QAToolFlagFilters", null);
                if (string.IsNullOrEmpty(raw)) return dict;

                foreach (string section in raw.Split("|"))
                {
                    string[] parts = section.Split(":");
                    if (parts.Length != 4) continue;

                    string key         = parts[0];
                    bool enabled       = bool.Parse(parts[1]);
                    FilterOperator op  = (FilterOperator)Enum.Parse(typeof(FilterOperator), parts[2]);
                    string rawValue    = parts[3];

                    object value = null;
                    var types = flagTypes;
                    if (types != null && types.TryGetValue(key, out Type flagType) && rawValue != "null")
                        value = Convert.ChangeType(rawValue, flagType);

                    dict[key] = new FlagFilter { enabled = enabled, op = op, value = value };
                }

                return dict;
            }
            set
            {
                if (value == null) { EditorPrefs.DeleteKey("QAToolFlagFilters"); return; }

                var parts = new List<string>();
                foreach (var kvp in value)
                {
                    string serializedValue = kvp.Value.value?.ToString() ?? "null";
                    parts.Add($"{kvp.Key}:{kvp.Value.enabled}:{kvp.Value.op}:{serializedValue}");
                }

                EditorPrefs.SetString("QAToolFlagFilters", string.Join("|", parts));
            }
        }
#endif

        // -------------------------------------------------------------------
        // Shared types and utilities
        // -------------------------------------------------------------------
        public static readonly Dictionary<string, string> typeNameToString = new Dictionary<string, string>
        {
            { "Int32",   "int"    },
            { "Single",  "float"  },
            { "Boolean", "bool"   },
            { "String",  "string" },
        };

        public static object NormalizeType(object input) => input switch
        {
            long   l => (int)l,
            double d => (float)d,
            _        => input
        };

        public enum FilterOperator
        {
            Ignore, Equal, NotEqual,
            GreaterThan, GreaterThanOrEqual,
            LessThan, LessThanOrEqual
        }

        public static readonly Dictionary<FilterOperator, string> FilterOperatorToString = new Dictionary<FilterOperator, string>
        {
            { FilterOperator.Ignore,             "—" },
            { FilterOperator.Equal,              "=" },
            { FilterOperator.NotEqual,           "≠" },
            { FilterOperator.GreaterThan,        ">" },
            { FilterOperator.GreaterThanOrEqual, "≥" },
            { FilterOperator.LessThan,           "<" },
            { FilterOperator.LessThanOrEqual,    "≤" },
        };

        public class FlagFilter
        {
            public bool           enabled;
            public FilterOperator op;
            public object         value;
        }
    }
    
}
