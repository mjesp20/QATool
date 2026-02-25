using System;
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
            EditorPrefs.SetBool("showGhostTrails",value);
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

    



}
