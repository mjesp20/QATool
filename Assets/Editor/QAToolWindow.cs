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

    private static List<Vector3> temporalTrail = new List<Vector3>();
    private static int currentPointIndex = 0;

    private static List<List<QAToolTelemetryClass.Entry>> allFiles = new List<List<QAToolTelemetryClass.Entry>>();
    public static int currentFileIndex = 0;
    private static bool isPreview = true;

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        DrawPlayerTrails();
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
        GUILayout.Space(10);
        GUILayout.Label("QA Tool", EditorStyles.boldLabel);

        if (GUILayout.Button("Filters"))
            QAToolFilterWindow.ShowWindow();

        if (GUILayout.Button("Flags"))
            QAToolFlagWindow.ShowWindow();
        
        
        if (Event.current.type == EventType.Repaint)
            popupButtonRect = GUILayoutUtility.GetLastRect();

        if (GUILayout.Button("Reload Player Path Data"))
            DrawPlayerTrails();

        EditorGUI.BeginChangeCheck();
        QAToolGlobals.showGhostTrails    = EditorGUILayout.Toggle("Show Ghost Trails",    QAToolGlobals.showGhostTrails);
        QAToolGlobals.showHeatMap        = EditorGUILayout.Toggle("Show Heat Map",        QAToolGlobals.showHeatMap);
        QAToolGlobals.showFeedbackNotes  = EditorGUILayout.Toggle("Show Feedback Notes",  QAToolGlobals.showFeedbackNotes);

        QAToolGlobals.feedbackKeyCode    = EditorGUILayout.TextArea(QAToolGlobals.feedbackKeyCode);
        
        //---------------Enable/Disable-----------------------------
        //---------------Float field Øverst-------------------------
        //---------------Slider Nederst-----------------------------
        QAToolGlobals.dataPointsPerSecond    = EditorGUILayout.FloatField("Data Points Per Second", QAToolGlobals.dataPointsPerSecond);
        //QAToolGlobals.dataPointsPerSecond  = EditorGUILayout.Slider("Data Points logged Per Second", QAToolGlobals.dataPointsPerSecond, 0.1f, 20
        QAToolGlobals.heatmapCellSize        = EditorGUILayout.Slider("Cell Size",  QAToolGlobals.heatmapCellSize,  0.2f, 5f);
        QAToolGlobals.heatmapOpacity         = EditorGUILayout.Slider("Opacity",    QAToolGlobals.heatmapOpacity,   0f,   1f);
        QAToolGlobals.heatmapContrast        = EditorGUILayout.Slider("Contrast",   QAToolGlobals.heatmapContrast,  0f,   3f);

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
            UpdateScene();
        }
        
        DrawTemporalTrail();

        
    }
    public void UpdateScene()
    {
        QAToolSceneValidator.ForceValidate();
        DrawPlayerTrails();
        SceneView.RepaintAll();
        

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
                    
                    Handles.DrawLine(trail[i], trail[i + 1]);
            }
        }
        
        if (temporalTrail.Count > 0)
        {
            Handles.color = Color.white;
            
            int drawUpTo = isPreview ? temporalTrail.Count - 1 : currentPointIndex;

            float thickness = isPreview ? 6f : 4f; //Hurtig Hardcode
            Handles.DrawAAPolyLine(thickness, temporalTrail.Take(drawUpTo + 1).ToArray());

            Handles.DrawSolidDisc(temporalTrail[currentPointIndex], Vector3.up, 0.2f);
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
                allTrails.Add(positions);
        }

        trailsDirty = false;
        SceneView.RepaintAll();
    }
    
    private void DrawTemporalTrail()
    {
        
        
        GUILayout.Space(10);
        GUILayout.Label("Temporal Trail", EditorStyles.boldLabel);

        if (GUILayout.Button("Load Temporal Trail"))
        {
            LoadAllFiles();
            LoadFileAtIndex(0);
        }
        
        
        if (allFiles.Count == 0) return;
        
        if (GUILayout.Button("Browse Files"))
            QAToolTemporalFileWindow.ShowWindow();

        GUILayout.Space(4);
        GUILayout.Label($"Player File: {currentFileIndex + 1} / {allFiles.Count}");

        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginDisabledGroup(currentFileIndex <= 0);
        if (GUILayout.Button("◀ Prev File"))
        {
            currentFileIndex--;
            LoadFileAtIndex(currentFileIndex);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(currentFileIndex >= allFiles.Count - 1);
        if (GUILayout.Button("Next File ▶"))
        {
            currentFileIndex++;
            LoadFileAtIndex(currentFileIndex);
        }
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.EndHorizontal();

        if (temporalTrail.Count == 0) return;

        GUILayout.Space(4);
        GUILayout.Label($"Point: {currentPointIndex} / {temporalTrail.Count - 1}");

        
        EditorGUILayout.BeginHorizontal();
        /*
        EditorGUI.BeginDisabledGroup(currentPointIndex <= 0);
        if (GUILayout.Button("◀ Prev"))
        {
            currentPointIndex--;
            isPreview = false;
            SceneView.RepaintAll();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUI.BeginDisabledGroup(currentPointIndex >= temporalTrail.Count - 1);
        if (GUILayout.Button("Next ▶"))
        {
            currentPointIndex++;
            isPreview = false;
            SceneView.RepaintAll();
        }
        
        EditorGUI.EndDisabledGroup();
        */
        EditorGUILayout.EndHorizontal();

        int newIndex = (int)EditorGUILayout.Slider("Scrub", currentPointIndex, 0, temporalTrail.Count - 1);
        if (newIndex != currentPointIndex)
        {
            currentPointIndex = newIndex;
            isPreview = false;
            SceneView.RepaintAll();
        }
    }
    
    private static void LoadAllFiles()
    {
        allFiles = QAToolTelemetryLoader.GetAllPositionsByFile().ToList();
        currentFileIndex = 0;
    }
    
    private static void LoadFileAtIndex(int index)
    {
        if (allFiles.Count == 0 || index < 0 || index >= allFiles.Count) return;

        temporalTrail = allFiles[index].Select(e => e.PlayerPosition.ToVector3()).ToList();
        currentPointIndex = 0;
        isPreview = true;
        SceneView.RepaintAll();
    }
    
    public static void SelectFile(int index)
    {
        currentFileIndex = index;
        LoadFileAtIndex(index);
    }
    
    
    
    
}