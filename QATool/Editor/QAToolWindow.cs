using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QATool
{
    public class QAToolWindow : EditorWindow
    {
        private Rect popupButtonRect;

        private static List<Vector3> temporalTrail = new List<Vector3>();
        private static int currentPointIndex = 0;

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

            if (GUILayout.Button("Reload Data"))
                UpdateScene();

            EditorGUI.BeginChangeCheck();
            QAToolGlobals.showGhostTrails    = EditorGUILayout.Toggle("Show Ghost Trails",    QAToolGlobals.showGhostTrails);
            QAToolGlobals.showHeatMap        = EditorGUILayout.Toggle("Show Heat Map",        QAToolGlobals.showHeatMap);
            QAToolGlobals.showFeedbackNotes  = EditorGUILayout.Toggle("Show Feedback Notes",  QAToolGlobals.showFeedbackNotes);

            QAToolGlobals.feedbackKeyCode = EditorGUILayout.TextArea(QAToolGlobals.feedbackKeyCode);

            QAToolGlobals.dataPointsPerSecond = EditorGUILayout.FloatField("Data Points Per Second", QAToolGlobals.dataPointsPerSecond);
            QAToolGlobals.heatmapCellSize     = EditorGUILayout.Slider("Cell Size",  QAToolGlobals.heatmapCellSize,  0.2f, 5f);
            QAToolGlobals.heatmapOpacity      = EditorGUILayout.Slider("Opacity",    QAToolGlobals.heatmapOpacity,   0f,   1f);
            QAToolGlobals.heatmapContrast     = EditorGUILayout.Slider("Contrast",   QAToolGlobals.heatmapContrast,  0f,   3f);

            EditorGUILayout.LabelField("Percentile Range");
            float min = QAToolGlobals.heatmapMinPercentile;
            float max = QAToolGlobals.heatmapMaxPercentile;

            EditorGUILayout.MinMaxSlider(ref min, ref max, 0f, 1f);

            EditorGUILayout.BeginHorizontal();
            min = EditorGUILayout.FloatField(min, GUILayout.MaxWidth(50));
            max = EditorGUILayout.FloatField(max, GUILayout.MaxWidth(50));
            EditorGUILayout.EndHorizontal();
            QAToolGlobals.heatmapMinPercentile = Mathf.Clamp01(min);
            QAToolGlobals.heatmapMaxPercentile = Mathf.Clamp01(max);

            if (EditorGUI.EndChangeCheck())
                UpdateScene();

            DrawTemporalTrail();
        }
        private static List<List<Vector3>> PositionsByFile = new List<List<Vector3>>();
        private static List<QAToolTelemetryClass.Entry> cachedEntries = new List<QAToolTelemetryClass.Entry>();

        public void UpdateScene()
        {
            List<List<QAToolTelemetryClass.Entry>> data = QAToolTelemetryLoader.LoadFromFolder();

            QAToolSceneValidator.ForceValidate();

            cachedEntries = DataToUnsortedList(data);

            PositionsByFile = data
                .Select(entryList => entryList
                    .Select(entry => entry.PlayerPosition.ToVector3())
                    .ToList())
                .ToList();

            DrawPlayerTrails();
            SceneView.RepaintAll();
        }

        List<QAToolTelemetryClass.Entry> DataToUnsortedList(List<List<QAToolTelemetryClass.Entry>> data)
        {
            List<QAToolTelemetryClass.Entry> list = new List<QAToolTelemetryClass.Entry>();
            foreach (List<QAToolTelemetryClass.Entry> entryList in data)
            {
                foreach (QAToolTelemetryClass.Entry entry in entryList)
                {
                    list.Add(entry);
                }
            }
            return list;
        }

        public static void DrawFeedbackNotes(List<QAToolTelemetryClass.Entry> list)
        {
            if (!QAToolGlobals.showFeedbackNotes) return;

            foreach (QAToolTelemetryClass.Entry entry in list)
            {
                if (entry.args.ContainsKey("note"))
                {
                    Handles.Label(entry.PlayerPosition.ToVector3(), entry.args["note"].ToString());
                }
            }   
        }

        static void OnSceneGUI(SceneView sceneView)
        {
            // Ghost trails
            if (QAToolGlobals.showGhostTrails && PositionsByFile.Count > 0)
            {
                Color[] trailColors =
                {
            Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta
        };

                for (int t = 0; t < PositionsByFile.Count; t++)
                {
                    var trail = PositionsByFile[t];
                    if (trail == null || trail.Count == 0) continue;

                    Handles.color = trailColors[t % trailColors.Length];
                    for (int i = 0; i < trail.Count - 1; i++)
                        Handles.DrawLine(trail[i], trail[i + 1]);
                }
            }

            // Feedback notes — must be called from here, not UpdateScene()
            if (QAToolGlobals.showFeedbackNotes && cachedEntries != null)
                DrawFeedbackNotes(cachedEntries);

            // Temporal trail
            if (temporalTrail.Count > 0)
            {
                Handles.color = Color.white;

                int drawUpTo = isPreview ? temporalTrail.Count - 1 : currentPointIndex;
                float thickness = isPreview ? 6f : 4f;
                Handles.DrawAAPolyLine(thickness, temporalTrail.Take(drawUpTo + 1).ToArray());
                Handles.DrawSolidDisc(temporalTrail[currentPointIndex], Vector3.up, 0.2f);
            }
        }

        public static void DrawPlayerTrails()
        {
            SceneView.RepaintAll();
        }

        private void DrawTemporalTrail()
        {
            GUILayout.Space(10);
            GUILayout.Label("Temporal Trail", EditorStyles.boldLabel);

            if (GUILayout.Button("Load Temporal Trail"))
            {
                currentFileIndex = 0;
                LoadFileAtIndex(0);
            }

            if (GUILayout.Button("Unload Temporal Trail"))
            {
                temporalTrail.Clear();
                //PositionsByFile.Clear();
                currentFileIndex  = 0;
                currentPointIndex = 0;
                isPreview         = true;
                SceneView.RepaintAll();
            }

            if (PositionsByFile.Count == 0) return;

            if (GUILayout.Button("Browse Files"))
                QAToolTemporalFileWindow.ShowWindow();

            GUILayout.Space(4);
            GUILayout.Label($"Player File: {currentFileIndex + 1} / {PositionsByFile.Count}");

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginDisabledGroup(currentFileIndex <= 0);
            if (GUILayout.Button("◀ Prev File"))
            {
                currentFileIndex--;
                LoadFileAtIndex(currentFileIndex);
            }
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(currentFileIndex >= PositionsByFile.Count - 1);
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
            EditorGUILayout.EndHorizontal();

            int newIndex = (int)EditorGUILayout.Slider("Scrub", currentPointIndex, 0, temporalTrail.Count - 1);
            if (newIndex != currentPointIndex)
            {
                currentPointIndex = newIndex;
                isPreview         = false;
                SceneView.RepaintAll();
            }
        }

        private static void LoadFileAtIndex(int index)
        {
            if (PositionsByFile.Count == 0 || index < 0 || index >= PositionsByFile.Count) return;

            temporalTrail = PositionsByFile[index];
            currentPointIndex = 0;
            isPreview         = true;
            SceneView.RepaintAll();
        }

        public static void SelectFile(int index)
        {
            currentFileIndex = index;
            LoadFileAtIndex(index);
        }
    }
}
