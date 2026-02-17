using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class QAToolWindow : EditorWindow
{
    private List<PlayerData> playerDataList = new List<PlayerData>();
    private static List<List<Vector3>> allTrails = new List<List<Vector3>>();
    private string filterCriteria = "";

    void OnEnable()
    {
        // Subscribe to the SceneView so we can draw gizmos
        SceneView.duringSceneGui += OnSceneGUI;
        LoadPlayerTelemetryData();
    }

    void OnDisable()
    {
        // Always unsubscribe to avoid memory leaks / ghost delegates
        SceneView.duringSceneGui -= OnSceneGUI;
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

    private void OnSceneGUI(SceneView sceneView)
    {
        if (allTrails == null || allTrails.Count == 0) return;

        // Assign a different color per trail
        Color[] trailColors = { Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta };

        for (int t = 0; t < allTrails.Count; t++)
        {
            List<Vector3> trail = allTrails[t];
            if (trail == null || trail.Count == 0) continue;

            Handles.color = trailColors[t % trailColors.Length];

            // Draw a sphere at each position
            foreach (Vector3 pos in trail)
            {
                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);
            }

            // Draw lines connecting the positions
            for (int i = 0; i < trail.Count - 1; i++)
            {
                Handles.DrawLine(trail[i], trail[i + 1]);
            }
        }
    }

    public void LoadPlayerTelemetryData()
    {
        allTrails.Clear();

        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string folderPath = Path.Combine(documentsPath, "QATool");

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"QATool folder not found at: {folderPath}");
            return;
        }

        foreach (var file in Directory.GetFiles(folderPath))
        {
            List<Vector3> positions = QAToolTelemetryLoader.LoadPositions(file);
            if (positions != null && positions.Count > 0)
            {
                allTrails.Add(positions);
            }
        }

        Debug.Log($"Loaded {allTrails.Count} trails.");

        // Repaint the scene to reflect changes immediately
        SceneView.RepaintAll();
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
}