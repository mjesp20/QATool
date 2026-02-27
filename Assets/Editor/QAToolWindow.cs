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
    private Rect popupButtonRect;
    
    

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        //DrawPlayerTrails();
    }

    void OnDisable()
    {
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

        GUILayout.Space(10);
        GUILayout.Label("QA Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Filters"))
        {
            QAToolFilterWindow.ShowWindow();
        }
        if (GUILayout.Button("Flags"))
        {
            QAToolFlagWindow.ShowWindow();
        }
        if (GUILayout.Button("addFilter"))
        {
            var filters = new Dictionary<string, QAToolGlobals.FlagFilter>();   // get (deserialize)
            filters["jumps"] = new QAToolGlobals.FlagFilter()
            {
                enabled = true,
                op = QAToolGlobals.FilterOperator.GreaterThanOrEqual,
                value = 2

            };
            QAToolGlobals.FlagFilters = filters;
            QAToolSceneValidator.ForceValidate();
        }
        if (Event.current.type == EventType.Repaint)
            popupButtonRect = GUILayoutUtility.GetLastRect();

        if (GUILayout.Button("Reload Player Path Data"))
        {
            DrawPlayerTrails();
        }

        EditorGUI.BeginChangeCheck();
        QAToolGlobals.showGhostTrails = EditorGUILayout.Toggle("Show Ghost Trails", QAToolGlobals.showGhostTrails);
        QAToolGlobals.showHeatMap = EditorGUILayout.Toggle("Show Heat Map", QAToolGlobals.showHeatMap);
        QAToolGlobals.showFeedbackNotes = EditorGUILayout.Toggle("Show Feedback Notes", QAToolGlobals.showFeedbackNotes);

        QAToolGlobals.feedbackKeyCode = EditorGUILayout.TextArea(QAToolGlobals.feedbackKeyCode);
        
        //---------------Enable/Disable-----------------------------
        //---------------Float field Øverst-------------------------
        //---------------Slider Nederst-----------------------------
        QAToolGlobals.dataPointsPerSecond = EditorGUILayout.FloatField("Data Points Per Second", QAToolGlobals.dataPointsPerSecond);
        //QAToolGlobals.dataPointsPerSecond = EditorGUILayout.Slider("Data Points logged Per Second", QAToolGlobals.dataPointsPerSecond, 0.1f, 20f);

        QAToolGlobals.heatmapCellSize = EditorGUILayout.Slider("Cell Size", QAToolGlobals.heatmapCellSize, 0.2f, 5f);
        
        QAToolGlobals.heatmapOpacity = EditorGUILayout.Slider("Opacity", QAToolGlobals.heatmapOpacity, 0f, 1f);
        
        QAToolGlobals.heatmapContrast = EditorGUILayout.Slider("Contrast", QAToolGlobals.heatmapContrast, 0f, 3f);
        
        EditorGUILayout.LabelField("Percentile Range");

        float min = QAToolGlobals.heatmapMinPercentile;
        float max = QAToolGlobals.heatmapMaxPercentile;

        EditorGUILayout.MinMaxSlider(
            ref min,
            ref max,
            0f,
            1f
        );
        
        EditorGUILayout.BeginHorizontal();
        min = EditorGUILayout.FloatField(min, GUILayout.MaxWidth(50));
        max = EditorGUILayout.FloatField(max, GUILayout.MaxWidth(50));
        EditorGUILayout.EndHorizontal();

        QAToolGlobals.heatmapMinPercentile = Mathf.Clamp01(min);
        QAToolGlobals.heatmapMaxPercentile = Mathf.Clamp01(max);
        
        
        if (EditorGUI.EndChangeCheck())
        {
            QAToolSceneValidator.ForceValidate();
            DrawPlayerTrails();
            SceneView.RepaintAll();
        }
    }


    public static void DrawFeedbackNotes()
    {
        if (!QAToolGlobals.showFeedbackNotes) return;

        List<QAToolTelemetryClass.Entry> notes = QAToolTelemetryLoader.GetAllEntries(QAToolJSONTypes.FeedbackNote);
        foreach (QAToolTelemetryClass.Entry note in notes)
        {
            Handles.Label(note.PlayerPosition.ToVector3(), note.args["note"].ToString());
        }
    }

    static bool trailsDirty = true;

    static void OnSceneGUI(SceneView sceneView)
    {
        // do not rebuild every frame
        if (allTrails.Count > 0)
        {
            Color[] trailColors =
            {
            Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta
        };

            for (int t = 0; t < allTrails.Count; t++)
            {
                var trail = allTrails[t];
                if (trail == null || trail.Count == 0) continue;

                Handles.color = trailColors[t % trailColors.Length];

                for (int i = 0; i < trail.Count - 1; i++)
                {
                    Handles.DrawLine(trail[i], trail[i + 1]);
                }
            }
        }

        DrawFeedbackNotes();
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

        trailsDirty = false; 
        SceneView.RepaintAll();
    }
}

