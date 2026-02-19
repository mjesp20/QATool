using System;
using System.IO;
using UnityEngine;

public static class QAToolGlobals
{
    public static string name = "QATool";
    public static string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    public static string folderPath = Path.Combine(documentsPath, name);
    public static bool showGhostTrails;
    public static bool showHeatMap;
    public static bool showFeedbackNotes;


}
