using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class QAToolGlobals
{
    public static string name = "QATool";
    public static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public static string folderPath = Path.Combine(documentsPath, name);
    public static bool showGhostTrails
    {
        get
        {
            return EditorPrefs.GetBool("showGhostTrails");
        }
        set
        {
            EditorPrefs.SetBool("showGhostTrails", value);
        }
    }

    public static bool showHeatMap
    {
        get
        {
            return EditorPrefs.GetBool("showHeatMap");
        }
        set
        {
            EditorPrefs.SetBool("showHeatMap", value);
        }
    }

    public static bool showFeedbackNotes
    {
        get
        {
            return EditorPrefs.GetBool("showFeedbackNotes");
        }
        set
        {
            EditorPrefs.SetBool("showFeedbackNotes", value);
        }
    }

    public static string feedbackKeyCode
    {
        get
        {
            return EditorPrefs.GetString("feedbackKeyCode");
        }
        set
        {
            EditorPrefs.SetString("feedbackKeyCode", value);
        }
    }
    public static float dataPointsPerSecond
    {

        get
        {
            return EditorPrefs.GetFloat("dataPointsPerSecond");
        }

        set
        {
            EditorPrefs.SetFloat("dataPointsPerSecond", value);
        }


    }

    public static float heatmapCellSize
    {

        get
        {
            return EditorPrefs.GetFloat("cellSize");
        }

        set
        {
            EditorPrefs.SetFloat("cellSize", value);
        }


    }

    public static float heatmapOpacity
    {

        get
        {
            return EditorPrefs.GetFloat("Opacity");
        }

        set
        {
            EditorPrefs.SetFloat("Opacity", value);
        }


    }

    public static float heatmapContrast
    {

        get
        {
            return EditorPrefs.GetFloat("Contrast");
        }

        set
        {
            EditorPrefs.SetFloat("Contrast", value);
        }


    }

    public static float heatmapHeightOffset
    {

        get
        {
            return EditorPrefs.GetFloat("Height Offset");
        }

        set
        {
            EditorPrefs.SetFloat("Height Offset", value);
        }


    }


    public static float heatmapMinPercentile
    {

        get
        {
            return EditorPrefs.GetFloat("Min Percentile Draw");
        }

        set
        {
            EditorPrefs.SetFloat("Min Percentile Draw", value);
        }


    }

    public static float heatmapMaxPercentile
    {

        get
        {
            return EditorPrefs.GetFloat("Max Percentile Draw");
        }

        set
        {
            EditorPrefs.SetFloat("Max Percentile Draw", value);
        }


    }


    public static Dictionary<string, Type> flagTypes
    {
        get
        {
            Dictionary<string, Type> dict = new Dictionary<string, Type>();
            string raw = EditorPrefs.GetString("QAToolFlags", null); //input or null
            if (string.IsNullOrEmpty(raw)) return null;
            foreach (string section in raw.Split("|"))
            {
                string key = section.Split(":")[0];
                string value = section.Split(":")[1];
                dict[key] = Type.GetType(value);
            }
            return dict;
        }
        set {
            List<string> parts = new List<string>();
            foreach (KeyValuePair<string, Type> keyValuePair in value)
            {
                parts.Add($"{keyValuePair.Key}:{keyValuePair.Value}");
            }
            EditorPrefs.SetString("QAToolFlags", string.Join("|", parts));
        }
    }

    private static Dictionary<string, object> _flagValues = new Dictionary<string, object>();
    public static Dictionary<string,object> flagValues{
        get
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (KeyValuePair<string,Type> keyValuePair in flagTypes)
            {
                if (_flagValues.ContainsKey(keyValuePair.Key))
                {
                    dict[keyValuePair.Key] = _flagValues[keyValuePair.Key];
                }
                else
                {
                    dict[keyValuePair.Key] = null;
                }
            }
            return dict;
        }
    }
    public static object getValue(string key)
    {
        return _flagValues[key];
    }
    public static void setValue(string key, object value)
    {
        _flagValues[key]=value;
    }
        

    public static readonly Dictionary<string, string> typeNameToString = new()
    {
        {"Int32","int"},
        {"Single","float"},
        {"Boolean","bool"},
        {"String","string"},
       };

}
