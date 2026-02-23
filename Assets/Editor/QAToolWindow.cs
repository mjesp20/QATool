using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class QAToolWindow : EditorWindow
{
    private static List<List<Vector3>> allTrails = new List<List<Vector3>>();
    private string filterCriteria = "";

    void OnEnable()
    {
        // Subscribe to the SceneView so we can draw gizmos
        SceneView.duringSceneGui += OnSceneGUI;
        //ytDrawPlayerTrails();
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
            DrawPlayerTrails();
        }

        EditorGUI.BeginChangeCheck();
        QAToolGlobals.showGhostTrails = EditorGUILayout.Toggle("Show Ghost Trails", QAToolGlobals.showGhostTrails);
        QAToolGlobals.showHeatMap = EditorGUILayout.Toggle("Show Heat Map", QAToolGlobals.showHeatMap);
        QAToolGlobals.showFeedbackNotes = EditorGUILayout.Toggle("Show Feedback Notes", QAToolGlobals.showFeedbackNotes);

        QAToolGlobals.feedbackKeyCode = EditorGUILayout.TextArea(QAToolGlobals.feedbackKeyCode);


        if (EditorGUI.EndChangeCheck())
        {
            DrawPlayerTrails();
            SceneView.RepaintAll();
        }
    }
    private static void OnSceneGUI(SceneView sceneView)
    {
        DrawPlayerTrails();
        DrawFeedbackNotes();
    }

    public static void DrawFeedbackNotes()
    {
        if (!QAToolGlobals.showFeedbackNotes)
        {
            return;   
        }
        Handles.Label(Vector3.one,new GUIContent("waow"));
    }

    public static void DrawPlayerTrails()
    {
        allTrails.Clear();

        if (!QAToolGlobals.showGhostTrails)
        {
            SceneView.RepaintAll();
            return;
        }

        var entriesByFile = QAToolTelemetryLoader.GetAllPositionsByFile();

        foreach (var fileEntries in entriesByFile)
        {
            var positions = fileEntries.Select(entry => entry.PlayerPosition.ToVector3()).ToList();

            if (positions.Count > 0)
            {
                allTrails.Add(positions);
            }
        }

        if (allTrails.Count == 0) return;

        Color[] trailColors =
        {
            Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta
        };

        for (int t = 0; t < allTrails.Count; t++)
        {
            var trail = allTrails[t];
            if (trail == null || trail.Count == 0) continue;

            Handles.color = trailColors[t % trailColors.Length];

            for (int i = 0; i < trail.Count; i++)
            {
                Handles.SphereHandleCap(0, trail[i], Quaternion.identity, 0.2f, EventType.Repaint);
            }

            for (int i = 0; i < trail.Count - 1; i++)
            {
                Handles.DrawLine(trail[i], trail[i + 1]);
            }
        }
    }
}
