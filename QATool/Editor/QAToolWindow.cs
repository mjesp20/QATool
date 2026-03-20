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
        //  Foldout state
        // ──────────────────────────────────────────────

        private bool _foldWindows       = true;
        private bool _foldVisualisation = true;
        private bool _foldTemporalTrail = true;

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
            // ── Title ──────────────────────────────────
            GUILayout.Space(8);
            GUILayout.Label("QA Tool", EditorStyles.boldLabel);
            DrawHorizontalLine();

            // ── Reload Data (prominent) ────────────────
            GUILayout.Space(6);
            DrawReloadButton();
            GUILayout.Space(6);
            DrawHorizontalLine();

            // ── Windows / Filters ─────────────────────
            GUILayout.Space(4);
            DrawSection("Data Windows", ref _foldWindows, DrawToolbarButtons);
            DrawHorizontalLine();

            // ── Visualisation Toggles ─────────────────
            GUILayout.Space(4);
            DrawSection("Visualisation", ref _foldVisualisation, () =>
            {
                EditorGUI.BeginChangeCheck();
                DrawVisualisationToggles();
                DrawHeatmapControls();
                EditorGUI.EndChangeCheck();

                bool controlJustReleased = _lastHotControl != 0 && GUIUtility.hotControl == 0;
                if (controlJustReleased)
                {
                    QAToolSceneValidator.ForceValidate();
                    RepaintScene();
                }
                _lastHotControl = GUIUtility.hotControl;
            });
            DrawHorizontalLine();

            // ── Temporal Trail ────────────────────────
            GUILayout.Space(4);
            DrawTemporalTrailSection();
        }

        // ──────────────────────────────────────────────
        //  Layout helpers
        // ──────────────────────────────────────────────

        /// <summary>Draws a collapsible, titled, boxed section.</summary>
        private void DrawSection(string title, ref bool foldout, System.Action drawContent)
        {
            // Foldout arrow + title as the section header
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);

            if (!foldout) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(2);
            drawContent();
            GUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }

        /// <summary>Draws a full-width 1px horizontal divider.</summary>
        private void DrawHorizontalLine(float topSpacing = 4f, float bottomSpacing = 4f)
        {
            GUILayout.Space(topSpacing);
            Rect r = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(r, new Color(0.35f, 0.35f, 0.35f, 1f));
            GUILayout.Space(bottomSpacing);
        }

        /// <summary>Draws the tall, prominent Reload Data button.</summary>
        private void DrawReloadButton()
        {
            GUIStyle reloadStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize    = 13,
                fontStyle   = FontStyle.Bold,
                fixedHeight = 46f,
                alignment   = TextAnchor.MiddleCenter,
            };

            // Tint the button so it stands out
            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 1f, 0.35f, 1f); // green accent

            if (GUILayout.Button("↺  Refresh Data  ↺", reloadStyle))
                ReloadData();

            GUI.backgroundColor = prevBg;
        }

        // ──────────────────────────────────────────────
        //  OnGUI sections
        // ──────────────────────────────────────────────

        private void DrawToolbarButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Filters"))
                QAToolFilterWindow.ShowWindow();

            if (GUILayout.Button("Flags"))
                QAToolFlagWindow.ShowWindow();

            EditorGUILayout.EndHorizontal();

            if (Event.current.type == EventType.Repaint)
                popupButtonRect = GUILayoutUtility.GetLastRect();
        }

        private void DrawVisualisationToggles()
        {
            GUILayout.Label("Toggles", EditorStyles.miniBoldLabel);
            QAToolGlobals.showGhostTrails    = EditorGUILayout.Toggle("Show Ghost Trails",    QAToolGlobals.showGhostTrails);
            QAToolGlobals.showHeatMap        = EditorGUILayout.Toggle("Show Heat Map",        QAToolGlobals.showHeatMap);
            QAToolGlobals.showFeedbackNotes  = EditorGUILayout.Toggle("Show Feedback Notes",  QAToolGlobals.showFeedbackNotes);
            QAToolGlobals.showEvents         = EditorGUILayout.Toggle("Show Events",          QAToolGlobals.showEvents);

            GUILayout.Space(4);
            GUILayout.Label("Settings", EditorStyles.miniBoldLabel);
            QAToolGlobals.feedbackKeyCode      = EditorGUILayout.TextField("Feedback Key",         QAToolGlobals.feedbackKeyCode);
            QAToolGlobals.dataPointsPerSecond  = EditorGUILayout.FloatField("Data Points / Sec",   QAToolGlobals.dataPointsPerSecond);
        }

        private void DrawHeatmapControls()
        {
            GUILayout.Space(6);
            GUILayout.Label("Heatmap", EditorStyles.miniBoldLabel);

            QAToolGlobals.heatmapCellSize  = EditorGUILayout.Slider("Cell Size",  QAToolGlobals.heatmapCellSize,  0.2f, 5f);
            QAToolGlobals.heatmapOpacity   = EditorGUILayout.Slider("Opacity",    QAToolGlobals.heatmapOpacity,   0f,   1f);
            QAToolGlobals.heatmapContrast  = EditorGUILayout.Slider("Contrast",   QAToolGlobals.heatmapContrast,  0f,   3f);

            GUILayout.Space(2);
            GUILayout.Label("Percentile Range", EditorStyles.miniLabel);

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
            DrawSection("Temporal Trail", ref _foldTemporalTrail, () =>
            {
                DrawTemporalTrailLoadButtons();

                if (trailsByFile.Count == 0) return;

                GUILayout.Space(4);
                DrawFilePicker();

                if (temporalTrail.Count == 0) return;

                GUILayout.Space(4);
                DrawScrubber();
            });
        }

        private void DrawTemporalTrailLoadButtons()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Load Trail"))
                LoadFileAtIndex(0);

            if (GUILayout.Button("Unload Trail"))
                UnloadTemporalTrail();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilePicker()
        {
            GUILayout.Label("File Browser", EditorStyles.miniBoldLabel);

            if (GUILayout.Button("Browse Files"))
                QAToolTemporalFileWindow.ShowWindow();

            GUILayout.Space(2);
            GUILayout.Label($"Player File: {activeFileIndex + 1} / {trailsByFile.Count}", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginDisabledGroup(activeFileIndex <= 0);
            if (GUILayout.Button("◀ Prev"))
                LoadFileAtIndex(activeFileIndex - 1);
            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginDisabledGroup(activeFileIndex >= trailsByFile.Count - 1);
            if (GUILayout.Button("Next ▶"))
                LoadFileAtIndex(activeFileIndex + 1);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawScrubber()
        {
            GUILayout.Label("Scrubber", EditorStyles.miniBoldLabel);
            GUILayout.Label($"Point: {scrubIndex} / {temporalTrail.Count - 1}", EditorStyles.miniLabel);

            //GUILayout.Label("Scrub", EditorStyles.miniLabel);
            int newIndex = (int)EditorGUILayout.Slider(scrubIndex, 0, temporalTrail.Count - 1);
            if (newIndex != scrubIndex)
            {
                scrubIndex = newIndex;
                isPreview  = false;
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
            DrawEvents();
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
                    Handles.Label(entry.position.ToVector3(), note.ToString());
            }
        }

        private static void DrawEvents()
        {
            if (!QAToolGlobals.showEvents || cachedEntries == null) return;
            if (Event.current.type != EventType.Repaint) return;

            foreach (QAToolTelemetryClass.Entry entry in cachedEntries)
            {
                if (entry == null) continue;
                if (entry.type != QAToolJSONTypes.Event.ToString()) continue;
                if (entry.args == null) continue;
                if (!entry.args.TryGetValue("event", out object evt)) continue;
                if (evt == null) continue;

                Vector3 pos  = entry.position.ToVector3();
                float   size = HandleUtility.GetHandleSize(pos) * 0.25f;

                Handles.DrawWireCube(pos, Vector3.one * size);
                Handles.SphereHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
                Handles.Label(pos + Vector3.up * size, evt.ToString());
            }
        }

        private static void DrawTemporalTrail()
        {
            if (temporalTrail.Count == 0) return;

            int   drawUpTo   = isPreview ? temporalTrail.Count - 1 : scrubIndex;
            float thickness  = isPreview ? 6f : 4f;

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
                .Select(file => file
                    .Where(e => e.type == "Movement")
                    .Select(e => e.position.ToVector3())
                    .ToList())
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
            temporalTrail   = trailsByFile[index];
            scrubIndex      = 0;
            isPreview       = true;
            RepaintScene();
        }

        private static void UnloadTemporalTrail()
        {
            temporalTrail   = new List<Vector3>();
            activeFileIndex = 0;
            scrubIndex      = 0;
            isPreview       = true;
            RepaintScene();
        }

        // Called by QAToolTemporalFileWindow
        public static void SelectFile(int index) => LoadFileAtIndex(index);

        private static void RepaintScene() => SceneView.RepaintAll();
    }
}