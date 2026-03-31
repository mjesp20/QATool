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
        private static List<List<QAToolTelemetryClass.Entry>> entriesByFile = new List<List<QAToolTelemetryClass.Entry>>();
        private static List<QAToolTelemetryClass.Entry> cachedEntries = new List<QAToolTelemetryClass.Entry>();

        // Shared palette so trails and events always match
        private static readonly Color[] playerPalette = { Color.red, Color.cyan, Color.green, Color.yellow, Color.magenta };

        // ──────────────────────────────────────────────
        //  Candidate caches
        //  Rebuilt on data load, trail change, or
        //  feedbackPreviewLength change — not every frame
        // ──────────────────────────────────────────────

        private static List<(QAToolTelemetryClass.Entry entry, int fileIndex)> _eventCandidates
            = new List<(QAToolTelemetryClass.Entry, int)>();

        private static List<(QAToolTelemetryClass.Entry entry, string preview, string fullText)> _feedbackCandidates
            = new List<(QAToolTelemetryClass.Entry, string, string)>();

        // ──────────────────────────────────────────────
        //  Line texture (sharp DrawAAPolyLine rendering)
        // ──────────────────────────────────────────────

        private static Texture2D _lineTex;

        private static Texture2D LineTex
        {
            get
            {
                if (_lineTex == null)
                {
                    _lineTex = new Texture2D(1, 1);
                    _lineTex.SetPixel(0, 0, Color.white);
                    _lineTex.Apply();
                }
                return _lineTex;
            }
        }

        // ──────────────────────────────────────────────
        //  Cached GUI styles
        // ──────────────────────────────────────────────

        private static GUIStyle _feedbackLabelStyle;
        private static GUIStyle _feedbackOutlineStyle;
        private static GUIStyle _eventLabelStyle;
        private static GUIStyle _eventLabelOutlineStyle;
        private static GUIStyle _eventAbbrStyle;
        private static GUIStyle _eventAbbrOutlineStyle;
        private static GUIStyle _transparentButtonStyle;

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

                if (EditorGUI.EndChangeCheck())
                {
                    RebuildCandidateCaches();
                    QAToolSceneValidator.ForceValidate();
                    RepaintScene();
                }

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

            Color prevBg = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0f, 1f, 0.35f, 1f);

            if (GUILayout.Button("↺  Refresh Data", reloadStyle))
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
        
        private void ResetAllSettings()
        {
            QAToolGlobals.ResetToDefaults();

            RebuildCandidateCaches();
            QAToolSceneValidator.ForceValidate();
            RepaintScene();
        }

        private void DrawVisualisationToggles()
        {
            GUILayout.Label("Toggles", EditorStyles.miniBoldLabel);
            QAToolGlobals.showGhostTrails   = EditorGUILayout.Toggle(new GUIContent("Show Ghost Trails",   "Draws a trail for each player file loaded."),                                              QAToolGlobals.showGhostTrails);
            QAToolGlobals.showHeatMap       = EditorGUILayout.Toggle(new GUIContent("Show Heat Map",       "Overlays a heatmap showing where players spent the most time."),                          QAToolGlobals.showHeatMap);
            QAToolGlobals.showFeedbackNotes = EditorGUILayout.Toggle(new GUIContent("Show Feedback Notes", "Displays in-world labels for any feedback notes recorded."),                              QAToolGlobals.showFeedbackNotes);
            QAToolGlobals.showEvents        = EditorGUILayout.Toggle(new GUIContent("Show Events",         "Renders clickable events, each showing individual event data."),                          QAToolGlobals.showEvents);

            GUILayout.Space(4);
            GUILayout.Label("Settings", EditorStyles.miniBoldLabel);
            QAToolGlobals.feedbackKeyCode        = EditorGUILayout.TextField(  new GUIContent("Feedback Key",           "The key players press in-game to submit a feedback note."),                QAToolGlobals.feedbackKeyCode);
            QAToolGlobals.feedbackPreviewLength  = EditorGUILayout.IntSlider(  new GUIContent("Feedback Preview Chars", "How many characters of a feedback note are shown in the scene view before truncating."), QAToolGlobals.feedbackPreviewLength, 1, 20);
            QAToolGlobals.renderRadius = EditorGUILayout.Slider(new GUIContent("Render Radius", "Only show events and feedback within this distance from the Scene camera."), QAToolGlobals.renderRadius, 1f, 200f);
            QAToolGlobals.dataPointsPerSecond    = EditorGUILayout.FloatField( new GUIContent("Data Points / Sec",      "How many data points are recorded per second during a session."),          QAToolGlobals.dataPointsPerSecond);
            QAToolGlobals.ghostTrailThickness    = EditorGUILayout.Slider(     new GUIContent("Trail Thickness",        "Controls the thickness of all ghost trail lines in the scene view."),      QAToolGlobals.ghostTrailThickness, 1f, 10f);
            GUILayout.Space(6);

            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.6f, 0.2f); // orange-ish

            if (GUILayout.Button("Reset to Defaults", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog(
                        "Reset Settings",
                        "Are you sure you want to reset all settings to default values?",
                        "Yes",
                        "Cancel"))
                {
                    ResetAllSettings();
                }
            }

            GUI.backgroundColor = prev;
        }

        private void DrawHeatmapControls()
        {
            GUILayout.Space(6);
            GUILayout.Label("Heatmap", EditorStyles.miniBoldLabel);

            QAToolGlobals.heatmapCellSize = EditorGUILayout.Slider(new GUIContent("Cell Size", "The size of each heatmap grid cell. Larger cells are broader but less precise."), QAToolGlobals.heatmapCellSize, 0.2f, 5f);
            QAToolGlobals.heatmapOpacity  = EditorGUILayout.Slider(new GUIContent("Opacity",   "Overall transparency of the heatmap overlay."),                                   QAToolGlobals.heatmapOpacity,  0f,   1f);
            QAToolGlobals.heatmapContrast = EditorGUILayout.Slider(new GUIContent("Contrast",  "Increases the difference between low and high density areas."),                   QAToolGlobals.heatmapContrast, 0f,   3f);

            GUILayout.Space(2);
            GUILayout.Label("Percentile Range", EditorStyles.miniLabel);

            float min = QAToolGlobals.heatmapMinPercentile;
            float max = QAToolGlobals.heatmapMaxPercentile;

            EditorGUILayout.MinMaxSlider(new GUIContent("", "Clamps which density percentile range is shown. Use to filter out extreme outliers."), ref min, ref max, 0f, 1f);
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
        
        private static bool IsInFrontOfCamera(Vector3 worldPos, Camera cam)
        {
            Vector3 toPoint = worldPos - cam.transform.position;
            return Vector3.Dot(cam.transform.forward, toPoint) > 0.1f;
        }
        
        private static bool IsWithinRadius(Vector3 worldPos, Camera cam, float radius)
        {
            return Vector3.Distance(cam.transform.position, worldPos) <= radius;
        }

        private static void DrawGhostTrails()
        {
            if (!QAToolGlobals.showGhostTrails || trailsByFile.Count == 0) return;
            if (Event.current.type != EventType.Repaint) return;

            for (int i = 0; i < trailsByFile.Count; i++)
            {
                if (!isPreview && i != activeFileIndex) continue;

                List<Vector3> trail = trailsByFile[i];
                if (trail == null || trail.Count == 0) continue;

                Handles.color = playerPalette[i % playerPalette.Length];
                Handles.DrawAAPolyLine(LineTex, QAToolGlobals.ghostTrailThickness, trail.ToArray());
            }
        }

        private static void DrawFeedbackNotes()
        {
            if (!QAToolGlobals.showFeedbackNotes || _feedbackCandidates == null) return;

            Camera cam = SceneView.currentDrawingSceneView?.camera;
            if (cam == null) return;

            EnsureStyles();

            Handles.BeginGUI();

            foreach (var (entry, preview, fullText) in _feedbackCandidates)
            {
                Vector3 worldPos = entry.position.ToVector3();

                // 🚀 Visibility checks
                if (!IsInFrontOfCamera(worldPos, cam) ||
                    !IsWithinRadius(worldPos, cam, QAToolGlobals.renderRadius))
                    continue;

                Vector3 labelWorld = worldPos + Vector3.up * 0.4f;
                Vector2 screenPos  = HandleUtility.WorldToGUIPoint(labelWorld);

                GUIContent content = new GUIContent(preview);
                Vector2 size       = _feedbackLabelStyle.CalcSize(content);

                Rect rect = new Rect(
                    screenPos.x - size.x * 0.5f,
                    screenPos.y - size.y,
                    size.x,
                    size.y
                );

                DrawGUIOutlinedLabel(rect, content, _feedbackLabelStyle, _feedbackOutlineStyle);

                Event e = Event.current;

                if (e.type == EventType.MouseDown &&
                    e.button == 0 &&
                    rect.Contains(e.mousePosition))
                {
                    QAToolFeedbackInspectorWindow.Show(entry, fullText);
                    e.Use();
                }
            }

            Handles.EndGUI();

            // ── 3D cubes ──
            if (Event.current.type != EventType.Repaint) return;

            Handles.color = Color.white;

            foreach (var (entry, _, _) in _feedbackCandidates)
            {
                Vector3 worldPos = entry.position.ToVector3();

                if (!IsInFrontOfCamera(worldPos, cam) ||
                    !IsWithinRadius(worldPos, cam, QAToolGlobals.renderRadius))
                    continue;

                Handles.DrawWireCube(worldPos, Vector3.one * 0.4f);
            }
        }

      private static void DrawEvents()
{
    if (!QAToolGlobals.showEvents || _eventCandidates == null) return;

    Camera sceneCamera = SceneView.currentDrawingSceneView?.camera;
    if (sceneCamera == null) return;

    EnsureStyles();

    Vector3 camPos    = sceneCamera.transform.position;
    bool    isRepaint = Event.current.type == EventType.Repaint;

    // ── 3D geometry ──
    if (isRepaint)
    {
        var sorted = _eventCandidates
            .Where(t =>
            {
                Vector3 pos = t.entry.position.ToVector3();
                return IsInFrontOfCamera(pos, sceneCamera) &&
                       IsWithinRadius(pos, sceneCamera, QAToolGlobals.renderRadius);
            })
            .OrderByDescending(t => Vector3.Distance(camPos, t.entry.position.ToVector3()))
            .ToList();

        foreach (var (entry, fileIndex) in sorted)
        {
            Vector3 pos   = entry.position.ToVector3();
            float   size  = 0.5f;
            Color   color = playerPalette[fileIndex % playerPalette.Length];

            Handles.color = color;
            Handles.DrawWireCube(pos, Vector3.one * size);
            Handles.SphereHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);
        }
    }

    // ── Labels + click handling ──
    Handles.BeginGUI();

    foreach (var (entry, fileIndex) in _eventCandidates)
    {
        Vector3 pos = entry.position.ToVector3();

        // 🚀 Visibility checks
        if (!IsInFrontOfCamera(pos, sceneCamera) ||
            !IsWithinRadius(pos, sceneCamera, QAToolGlobals.renderRadius))
            continue;

        if (!entry.args.TryGetValue("event", out object evt) || evt == null) continue;

        string evtName = evt.ToString();
        string abbr    = evtName.Length > 2 ? evtName.Substring(0, 2).ToUpper() : evtName.ToUpper();

        float size  = 0.5f;
        Color color = playerPalette[fileIndex % playerPalette.Length];

        Vector2 screenTop = HandleUtility.WorldToGUIPoint(pos + Vector3.up * size);
        Vector2 screenMid = HandleUtility.WorldToGUIPoint(pos);

        // Name label
        _eventLabelStyle.normal.textColor        = color;
        _eventLabelOutlineStyle.normal.textColor = Color.black;

        GUIContent nameContent = new GUIContent(evtName);
        Vector2 nameSize       = _eventLabelStyle.CalcSize(nameContent);

        Rect nameRect = new Rect(
            screenTop.x - nameSize.x * 0.5f,
            screenTop.y - nameSize.y,
            nameSize.x,
            nameSize.y
        );

        DrawGUIOutlinedLabel(nameRect, nameContent, _eventLabelStyle, _eventLabelOutlineStyle);

        // Abbreviation label
        GUIContent abbrContent = new GUIContent(abbr);
        Vector2 abbrSize       = _eventAbbrStyle.CalcSize(abbrContent);

        Rect abbrRect = new Rect(
            screenMid.x - abbrSize.x * 0.5f,
            screenMid.y - abbrSize.y * 0.5f,
            abbrSize.x,
            abbrSize.y
        );

        _eventAbbrOutlineStyle.normal.textColor = color;
        DrawGUIOutlinedLabel(abbrRect, abbrContent, _eventAbbrStyle, _eventAbbrOutlineStyle);

        // Click area
        Rect clickRect = Rect.MinMaxRect(
            Mathf.Min(nameRect.xMin, abbrRect.xMin),
            nameRect.yMin,
            Mathf.Max(nameRect.xMax, abbrRect.xMax),
            abbrRect.yMax
        );

        Event e = Event.current;

        if (e.type == EventType.MouseDown &&
            e.button == 0 &&
            clickRect.Contains(e.mousePosition))
        {
            QAToolEventInspectorWindow.Show(entry, fileIndex, color);
            e.Use();
        }
    }

    Handles.EndGUI();

}

        private static void DrawTemporalTrail()
        {
            if (temporalTrail.Count == 0) return;
            if (Event.current.type != EventType.Repaint) return;

            int   drawUpTo  = isPreview ? temporalTrail.Count - 1 : scrubIndex;
            float thickness = QAToolGlobals.ghostTrailThickness + 3f;

            Handles.color = Color.white;

            if (isPreview)
                Handles.DrawAAPolyLine(thickness, temporalTrail.Take(drawUpTo + 1).ToArray());
            else
                Handles.DrawAAPolyLine(LineTex, thickness, temporalTrail.Take(drawUpTo + 1).ToArray());

            Handles.DrawSolidDisc(temporalTrail[scrubIndex], Vector3.up, 0.2f);
        }

        // ──────────────────────────────────────────────
        //  GUI helpers
        // ──────────────────────────────────────────────

        /// <summary>
        /// Draws a screen-space label with a 1px outline by rendering offset copies behind it.
        /// Both styles must already have their textColor set before calling.
        /// </summary>
        private static void DrawGUIOutlinedLabel(Rect rect, GUIContent content, GUIStyle style, GUIStyle outlineStyle)
        {
     //       GUI.Label(new Rect(rect.x - 1, rect.y -1,     rect.width, rect.height), content, outlineStyle);
            GUI.Label(new Rect(rect.x + 1, rect.y +1,     rect.width, rect.height), content, outlineStyle);
            //GUI.Label(new Rect(rect.x,     rect.y - 1, rect.width, rect.height), content, outlineStyle);
           // GUI.Label(new Rect(rect.x,     rect.y + 1, rect.width, rect.height), content, outlineStyle);
            GUI.Label(rect,                                                        content, style);
        }

        /// <summary>Initialises all cached GUIStyles if they have not been built yet.</summary>
        private static void EnsureStyles()
        {
            if (_transparentButtonStyle == null)
            {
                _transparentButtonStyle = new GUIStyle(GUIStyle.none);
            }

            if (_feedbackLabelStyle == null)
            {
                _feedbackLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Italic,
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 11
                };
                _feedbackLabelStyle.normal.textColor = Color.white;
                _feedbackLabelStyle.hover.textColor  = Color.white;
            }

            if (_feedbackOutlineStyle == null)
            {
                _feedbackOutlineStyle = new GUIStyle(_feedbackLabelStyle);
                _feedbackOutlineStyle.normal.textColor = Color.black;
            }

            if (_eventLabelStyle == null)
            {
                _eventLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize  = 11
                };
            }

            if (_eventLabelOutlineStyle == null)
            {
                _eventLabelOutlineStyle = new GUIStyle(_eventLabelStyle);
            }

            if (_eventAbbrStyle == null)
            {
                _eventAbbrStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize  = 10
                };
                _eventAbbrStyle.normal.textColor = Color.black;
            }

            if (_eventAbbrOutlineStyle == null)
            {
                _eventAbbrOutlineStyle = new GUIStyle(_eventAbbrStyle);
            }
        }

        // ──────────────────────────────────────────────
        //  Candidate cache builder
        // ──────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the filtered entry lists used by DrawEvents and DrawFeedbackNotes.
        /// Call whenever data changes, the trail changes, or feedbackPreviewLength changes.
        /// </summary>
        private static void RebuildCandidateCaches()
        {
            bool trailActive = temporalTrail.Count > 0;

            _eventCandidates = entriesByFile
                .SelectMany((file, fileIndex) => file
                    .Where(e => e != null
                             && e.type == QAToolJSONTypes.Event.ToString()
                             && e.args != null
                             && e.args.ContainsKey("event")
                             && (!trailActive || fileIndex == activeFileIndex))
                    .Select(e => (entry: e, fileIndex)))
                .ToList();

            _feedbackCandidates = cachedEntries
                .Where(e => e.args != null
                         && e.args.TryGetValue("note", out object n)
                         && n != null)
                .Select(e =>
                {
                    string full    = e.args["note"].ToString();
                    string preview = full.Length > QAToolGlobals.feedbackPreviewLength
                                      ? full.Substring(0, QAToolGlobals.feedbackPreviewLength) + "…"
                                      : full;
                    return (entry: e, preview, fullText: full);
                })
                .ToList();
        }

        // ──────────────────────────────────────────────
        //  Data loading
        // ──────────────────────────────────────────────

        public void ReloadData()
        {
            List<List<QAToolTelemetryClass.Entry>> data = QAToolTelemetryLoader.LoadFromFolder();

            QAToolSceneValidator.ForceValidate();

            cachedEntries = FlattenEntries(data);
            entriesByFile = data;

            trailsByFile = data
                .Select(file => file
                    .Where(e => e != null && e.type == "Movement")
                    .Select(e => e.position.ToVector3())
                    .ToList())
                .ToList();

            // Invalidate cached styles so they rebuild cleanly
            _feedbackLabelStyle     = null;
            _feedbackOutlineStyle   = null;
            _eventLabelStyle        = null;
            _eventLabelOutlineStyle = null;
            _eventAbbrStyle         = null;
            _eventAbbrOutlineStyle  = null;
            _transparentButtonStyle = null;

            RebuildCandidateCaches();
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

            RebuildCandidateCaches();
            RepaintScene();
        }

        private static void UnloadTemporalTrail()
        {
            temporalTrail   = new List<Vector3>();
            activeFileIndex = 0;
            scrubIndex      = 0;
            isPreview       = true;

            RebuildCandidateCaches();
            RepaintScene();
        }

        // Called by QAToolTemporalFileWindow
        public static void SelectFile(int index) => LoadFileAtIndex(index);

        private static void RepaintScene() => SceneView.RepaintAll();
    }
}