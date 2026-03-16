using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace QATool
{
    public class QAToolWindow : EditorWindow
    {
        // ──────────────────────────────────────────────
        //  Scene data
        // ──────────────────────────────────────────────

        private static List<List<Vector3>> trailsByFile = new List<List<Vector3>>();
        private static List<QAToolTelemetryClass.Entry> cachedEntries = new List<QAToolTelemetryClass.Entry>();

        // ──────────────────────────────────────────────
        //  Temporal-trail state
        // ──────────────────────────────────────────────

        private static List<Vector3> temporalTrail = new List<Vector3>();
        public static int activeFileIndex = 0;
        private static int scrubIndex = 0;
        private static bool isPreview = true;

        // ──────────────────────────────────────────────
        //  Misc editor state
        // ──────────────────────────────────────────────

        private Rect popupButtonRect;
        private int _lastHotControl;

        // ──────────────────────────────────────────────
        //  Lifecycle
        // ──────────────────────────────────────────────

        [MenuItem("Window/QA Tool")]
        public static void ShowWindow() => GetWindow<QAToolWindow>("QA Tool");

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            RepaintScene();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ──────────────────────────────────────────────
        //  GUI entry point
        // ──────────────────────────────────────────────

        void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("QA Tool", EditorStyles.boldLabel);

            DrawToolbarButtons();

            // Update values on every change, but don't validate yet
            EditorGUI.BeginChangeCheck();
            DrawVisualisationToggles();
            DrawHeatmapControls();
            EditorGUI.EndChangeCheck(); // <-- drop the if-block here

            // Only validate + repaint when a slider (or any control) is released
            bool controlJustReleased = _lastHotControl != 0 && GUIUtility.hotControl == 0;
            if (controlJustReleased)
            {
                QAToolSceneValidator.ForceValidate();
                RepaintScene();
            }
            _lastHotControl = GUIUtility.hotControl;

            GUILayout.Space(10);
            DrawTemporalTrailSection();
        }

        // ──────────────────────────────────────────────
        //  OnGUI sections
        // ──────────────────────────────────────────────

        private void DrawToolbarButtons()
        {
            if (GUILayout.Button("Filters"))
                QAToolFilterWindow.ShowWindow();

            if (GUILayout.Button("Flags"))
                QAToolFlagWindow.ShowWindow();

            if (Event.current.type == EventType.Repaint)
                popupButtonRect = GUILayoutUtility.GetLastRect();

            if (GUILayout.Button("Reload Data"))
                ReloadData();
        }

        private void DrawVisualisationToggles()
        {
            QAToolGlobals.showGhostTrails = EditorGUILayout.Toggle("Show Ghost Trails", QAToolGlobals.showGhostTrails);
            QAToolGlobals.showHeatMap = EditorGUILayout.Toggle("Show Heat Map", QAToolGlobals.showHeatMap);
            QAToolGlobals.showFeedbackNotes = EditorGUILayout.Toggle("Show Feedback Notes", QAToolGlobals.showFeedbackNotes);
            QAToolGlobals.feedbackKeyCode = EditorGUILayout.TextArea(QAToolGlobals.feedbackKeyCode);
            QAToolGlobals.dataPointsPerSecond = EditorGUILayout.FloatField("Data Points Per Second", QAToolGlobals.dataPointsPerSecond);
        }

        private void DrawHeatmapControls()
        {
            QAToolGlobals.heatmapCellSize = EditorGUILayout.Slider("Cell Size", QAToolGlobals.heatmapCellSize, 0.2f, 5f);
            QAToolGlobals.heatmapOpacity = EditorGUILayout.Slider("Opacity", QAToolGlobals.heatmapOpacity, 0f, 1f);
            QAToolGlobals.heatmapContrast = EditorGUILayout.Slider("Contrast", QAToolGlobals.heatmapContrast, 0f, 3f);

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
        }

        private void DrawTemporalTrailSection()
        {
            GUILayout.Label("Temporal Trail", EditorStyles.boldLabel);

            DrawTemporalTrailLoadButtons();

            if (trailsByFile.Count == 0) return;

            DrawFilePicker();

            if (temporalTrail.Count == 0) return;

            DrawScrubber();
        }

        private void DrawTemporalTrailLoadButtons()
        {
            if (GUILayout.Button("Load Temporal Trail"))
                LoadFileAtIndex(0);

            if (GUILayout.Button("Unload Temporal Trail"))
                UnloadTemporalTrail();
        }

        private void DrawFilePicker()
        {
            if (GUILayout.Button("Browse Files"))
                QAToolTemporalFileWindow.ShowWindow();

            GUILayout.Space(4);
            GUILayout.Label($"Player File: {activeFileIndex + 1} / {trailsByFile.Count}");

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(activeFileIndex <= 0);
            if (GUILayout.Button("◀ Prev File"))
                LoadFileAtIndex(activeFileIndex - 1);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(activeFileIndex >= trailsByFile.Count - 1);
            if (GUILayout.Button("Next File ▶"))
                LoadFileAtIndex(activeFileIndex + 1);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawScrubber()
        {
            GUILayout.Space(4);
            GUILayout.Label($"Point: {scrubIndex} / {temporalTrail.Count - 1}");

            int newIndex = (int)EditorGUILayout.Slider("Scrub", scrubIndex, 0, temporalTrail.Count - 1);
            if (newIndex != scrubIndex)
            {
                scrubIndex = newIndex;
                isPreview = false;
                RepaintScene();
            }
        }

        // ──────────────────────────────────────────────
        //  Scene-view rendering
        // ──────────────────────────────────────────────

        static void OnSceneGUI(SceneView sceneView)
        {
            DrawGhostTrails();
            DrawFeedbackNotes();
            DrawTemporalTrail();
        }

        private static void DrawGhostTrails()
        {
            if (!QAToolGlobals.showGhostTrails || trailsByFile.Count == 0) return;

            Color[] palette = { Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta };

            for (int i = 0; i < trailsByFile.Count; i++)
            {
                List<Vector3> trail = trailsByFile[i];
                if (trail == null || trail.Count == 0) continue;

                Handles.color = palette[i % palette.Length];
                for (int p = 0; p < trail.Count - 1; p++)
                    Handles.DrawLine(trail[p], trail[p + 1]);
            }
        }

        private static void DrawFeedbackNotes()
        {
            if (!QAToolGlobals.showFeedbackNotes || cachedEntries == null) return;

            foreach (QAToolTelemetryClass.Entry entry in cachedEntries)
            {
                if (entry.args.TryGetValue("note", out object note))
                    Handles.Label(entry.PlayerPosition.ToVector3(), note.ToString());
            }
        }



        private static void DrawTemporalTrail()
        {
            if (temporalTrail.Count == 0) return;

            int drawUpTo = isPreview ? temporalTrail.Count - 1 : scrubIndex;
            float thickness = isPreview ? 6f : 4f;

            Handles.color = Color.white;
            Handles.DrawAAPolyLine(thickness, temporalTrail.Take(drawUpTo + 1).ToArray());
            Handles.DrawSolidDisc(temporalTrail[scrubIndex], Vector3.up, 0.2f);
        }

        // ──────────────────────────────────────────────
        //  Data loading
        // ──────────────────────────────────────────────

        public void ReloadData()
        {
            List<List<QAToolTelemetryClass.Entry>> data = QAToolTelemetryLoader.LoadFromFolder();

            QAToolSceneValidator.ForceValidate();

            cachedEntries = FlattenEntries(data);

            trailsByFile = data
                .Select(file => file.Select(e => e.PlayerPosition.ToVector3()).ToList())
                .ToList();

            RepaintScene();
        }

        private static List<QAToolTelemetryClass.Entry> FlattenEntries(List<List<QAToolTelemetryClass.Entry>> data)
        {
            return data.SelectMany(file => file).ToList();
        }

        // ──────────────────────────────────────────────
        //  Temporal-trail helpers
        // ──────────────────────────────────────────────

        private static void LoadFileAtIndex(int index)
        {
            if (trailsByFile.Count == 0 || index < 0 || index >= trailsByFile.Count) return;

            activeFileIndex = index;
            temporalTrail = trailsByFile[index];
            scrubIndex = 0;
            isPreview = true;
            RepaintScene();
        }

        private static void UnloadTemporalTrail()
        {
            temporalTrail = new List<Vector3>();
            activeFileIndex = 0;
            scrubIndex = 0;
            isPreview = true;
            RepaintScene();
        }

        // Called by QAToolTemporalFileWindow
        public static void SelectFile(int index) => LoadFileAtIndex(index);

        private static void RepaintScene() => SceneView.RepaintAll();
    }
}