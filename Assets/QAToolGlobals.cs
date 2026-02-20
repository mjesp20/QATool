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



}
