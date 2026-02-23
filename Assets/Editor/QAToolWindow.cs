using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class QAToolWindow : EditorWindow
{
    private static List<List<Vector3>> allTrails = new List<List<Vector3>>();
    private string filterCriteria = "";

    void OnEnable()
    {
        // Subscribe to the SceneView so we can draw gizmos
        SceneView.duringSceneGui += OnSceneGUI;
        //LoadPlayerTelemetryData();
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

    void OnGUI()
    {
        //GUILayout.Label("QA Tool", EditorStyles.boldLabel);


        if (GUILayout.Button("Reload Player Path Data"))
        {
            //LoadPlayerTelemetryData();
        }

        EditorGUI.BeginChangeCheck();
        QAToolGlobals.showGhostTrails = EditorGUILayout.Toggle("Show Ghost Trails", QAToolGlobals.showGhostTrails);
        QAToolGlobals.showHeatMap = EditorGUILayout.Toggle("Show Heat Map", QAToolGlobals.showHeatMap);
        QAToolGlobals.showFeedbackNotes = EditorGUILayout.Toggle("Show Feedback Notes", QAToolGlobals.showFeedbackNotes);

        QAToolGlobals.feedbackKeyCode = EditorGUILayout.TextArea(QAToolGlobals.feedbackKeyCode);


        if (EditorGUI.EndChangeCheck())
        {
            //LoadPlayerTelemetryData();
            SceneView.RepaintAll();
        }

    }


    private void OnSceneGUI(SceneView sceneView)
    {
        if (!QAToolGlobals.showGhostTrails) return;
        if (allTrails == null || allTrails.Count == 0) return;

        Color[] trailColors = { Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta };

        for (int t = 0; t < allTrails.Count; t++)
        {
            List<Vector3> trail = allTrails[t];
            if (trail == null || trail.Count == 0) continue;

            Handles.color = trailColors[t % trailColors.Length];

            foreach (Vector3 pos in trail)
            {
                Handles.SphereHandleCap(0, pos, Quaternion.identity, 0.2f, EventType.Repaint);
            }

            for (int i = 0; i < trail.Count - 1; i++)
            {
                Handles.DrawLine(trail[i], trail[i + 1]);
            }
        }
    }

    /*
    public void LoadPlayerTelemetryData()
    {
        allTrails.Clear();

        if (!QAToolGlobals.showGhostTrails)
        {
            SceneView.RepaintAll();
            return;
        }

        foreach (var file in Directory.GetFiles(QAToolGlobals.folderPath))
        {
            List<Vector3> positions = QAToolTelemetryLoader.ExtractPositions(QAToolTelemetryLoader.LoadFromFile(file));
            if (positions != null && positions.Count > 0)
            {
                allTrails.Add(positions);
            }
        }

        Debug.Log($"Loaded {allTrails.Count} trails.");
        SceneView.RepaintAll();
    }*/

}
