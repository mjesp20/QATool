using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;


public class QAToolWindow : EditorWindow
{
    private List<PlayerData> playerDataList = new List<PlayerData>();
    private string filterCriteria = "";
    private GameObject heatmapPrefab;

    void Awake()
    {
        LoadPlayerTelemetryData();
    }

   

    [MenuItem("Window/QA Tool")]
    public static void ShowWindow()
    {
        GetWindow<QAToolWindow>("QA Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("QA Tool", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Reload Player Path Data"))
        {
            LoadPlayerTelemetryData();
        }

        if (GUILayout.Button("Show Player Trails"))
        {
            ShowPlayerTrails();
        }
    }

    public void LoadPlayerTelemetryData()
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string folderPath = Path.Combine(documentsPath, "QATool");
        
        foreach (var file in Directory.GetFiles(folderPath))
        {
            List<Vector3> positions = QAToolTelemetryLoader.LoadPositions(file);
            QAToolTelemetryLoader.CreateGizmos(positions);
            
            
                
        }
        
        
    }
    private void ShowPlayerTrails()
    {
        LoadPlayerTelemetryData();
        Debug.Log("Showing player trails.");
    }
}

[System.Serializable]
public class PlayerData
{
    public Vector3 position;
    // Add more fields as necessary (e.g., actions, timestamps)
}