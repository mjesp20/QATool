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
        //  Temporal-trail state
        // ──────────────────────────────────────────────

        private static List<Vector3> temporalTrail = new List<Vector3>();
        public static int activeFileIndex = 0;
        private static int scrubIndex = 0;
        private static bool isPreview = true;

        // ──────────────────────────────────────────────
        //  Heatmap state
        // ──────────────────────────────────────────────

        private static Dictionary<Vector3Int, int> _heatmap = new Dictionary<Vector3Int, int>();
        private static float _lastHeatmapCellSize = -1f;

        // ──────────────────────────────────────────────
        //  Misc editor state
        // ──────────────────────────────────────────────

        private Rect popupButtonRect;
        private int _lastHotControl;

        // ──────────────────────────────────────────────
        //  Foldout state
        // ──────────────────────────────────────────────

        private bool _foldWindows = true;
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
                bool changed = EditorGUI.EndChangeCheck();

                bool controlJustReleased = _lastHotControl != 0 && GUIUtility.hotControl == 0;
                if (controlJustReleased)
                {
                    // Rebuild heatmap grid if cell size changed or heatmap was just toggled on
                    if (QAToolGlobals.heatmapCellSize != _lastHeatmapCellSize)
                        LoadHeatmap();

                    QAToolSceneValidator.ForceValidate();
                    RepaintScene();
                }
                else if (changed)
                {
                    // Cheap path: only repaint for opacity / contrast / percentile changes
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
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                fixedHeight = 46f,
                alignment = TextAnchor.MiddleCenter,
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

        private void DrawVisualisationToggles()
        {
            GUILayout.Label("Toggles", EditorStyles.miniBoldLabel);
            QAToolGlobals.showGhostTrails = EditorGUILayout.Toggle(new GUIContent("Show Ghost Trails", "Draws a trail for each player file loaded."), QAToolGlobals.showGhostTrails);
            QAToolGlobals.showHeatMap = EditorGUILayout.Toggle(new GUIContent("Show Heat Map", "Overlays a heatmap showing where players spent the most time."), QAToolGlobals.showHeatMap);
            QAToolGlobals.showFeedbackNotes = EditorGUILayout.Toggle(new GUIContent("Show Feedback Notes", "Displays in-world labels for any feedback notes recorded."), QAToolGlobals.showFeedbackNotes);
            QAToolGlobals.showEvents = EditorGUILayout.Toggle(new GUIContent("Show Events", "Renders clickable events, each showing individual event data."), QAToolGlobals.showEvents);

            GUILayout.Space(4);
            GUILayout.Label("Settings", EditorStyles.miniBoldLabel);
            QAToolGlobals.feedbackKeyCode = EditorGUILayout.TextField(new GUIContent("Feedback Key", "The key players press in-game to submit a feedback note."), QAToolGlobals.feedbackKeyCode);
            QAToolGlobals.dataPointsPerSecond = EditorGUILayout.FloatField(new GUIContent("Data Points / Sec", "How many data points are recorded per second during a session."), QAToolGlobals.dataPointsPerSecond);
            QAToolGlobals.ghostTrailThickness = EditorGUILayout.Slider(new GUIContent("Trail Thickness", "Controls the thickness of all ghost trail lines in the scene view."), QAToolGlobals.ghostTrailThickness, 1f, 10f);
        }

        private void DrawHeatmapControls()
        {
            GUILayout.Space(6);
            GUILayout.Label("Heatmap", EditorStyles.miniBoldLabel);

            QAToolGlobals.heatmapCellSize = EditorGUILayout.Slider(new GUIContent("Cell Size", "The size of each heatmap grid cell. Larger cells are broader but less precise."), QAToolGlobals.heatmapCellSize, 0.2f, 5f);
            QAToolGlobals.heatmapOpacity = EditorGUILayout.Slider(new GUIContent("Opacity", "Overall transparency of the heatmap overlay."), QAToolGlobals.heatmapOpacity, 0f, 1f);
            QAToolGlobals.heatmapContrast = EditorGUILayout.Slider(new GUIContent("Contrast", "Increases the difference between low and high density areas."), QAToolGlobals.heatmapContrast, 0f, 3f);

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
            DrawHeatmap();
            DrawFeedbackNotes();
            DrawEvents();
            DrawTemporalTrail();
        }

        private static void DrawGhostTrails()
        {
            if (!QAToolGlobals.showGhostTrails || trailsByFile.Count == 0) return;

            for (int i = 0; i < trailsByFile.Count; i++)
            {
                if (!isPreview && i != activeFileIndex) continue;

                List<Vector3> trail = trailsByFile[i];
                if (trail == null || trail.Count == 0) continue;

                Handles.color = playerPalette[i % playerPalette.Length];
                Handles.DrawAAPolyLine(LineTex, QAToolGlobals.ghostTrailThickness, trail.ToArray());
            }
        }

        private static void DrawHeatmap()
        {
            if (!QAToolGlobals.showHeatMap || _heatmap == null || _heatmap.Count == 0) return;

            Camera cam = SceneView.currentDrawingSceneView?.camera;
            if (cam == null) return;

            float cellSize = QAToolGlobals.heatmapCellSize;
            Vector3 camPos = cam.transform.position;

            // Build sorted list back-to-front for correct transparency blending
            List<KeyValuePair<Vector3Int, int>> cells = _heatmap
                .OrderByDescending(kvp =>
                {
                    Vector3 c = CellCenter(kvp.Key, cellSize);
                    return Vector3.Distance(camPos, c);
                })
                .ToList();

            // Compute percentile thresholds from sorted value list
            List<int> sorted = _heatmap.Values.OrderBy(v => v).ToList();
            int total = sorted.Count;
            int minIndex = Mathf.Clamp(Mathf.FloorToInt(QAToolGlobals.heatmapMinPercentile * (total - 1)), 0, total - 1);
            int maxIndex = Mathf.Clamp(Mathf.FloorToInt(QAToolGlobals.heatmapMaxPercentile * (total - 1)), 0, total - 1);

            int minThreshold = sorted[minIndex];
            int maxThreshold = sorted[maxIndex];

            float logMin = Mathf.Max(1, minThreshold);
            float logMax = Mathf.Max(logMin + 1, maxThreshold);

            foreach (KeyValuePair<Vector3Int, int> kvp in cells)
            {
                if (kvp.Value < minThreshold || kvp.Value > maxThreshold) continue;

                float normalized = Mathf.Log(kvp.Value - logMin + 2f) / Mathf.Log(logMax - logMin + 2f);
                normalized = Mathf.Pow(normalized, QAToolGlobals.heatmapContrast);

                Color col = Color.Lerp(Color.green, Color.red, normalized);
                col.a = QAToolGlobals.heatmapOpacity;

                DrawSolidCube(CellCenter(kvp.Key, cellSize), cellSize, col);
            }
        }

        /// <summary>
        /// Draws a solid, axis-aligned cube using Handles.DrawAAConvexPolygon for each face.
        /// Handles has no solid-cube primitive, so we render all six quads manually.
        /// </summary>
        private static void DrawSolidCube(Vector3 center, float size, Color color)
        {
            float h = size * 0.5f;
            Handles.color = color;

            // Pre-compute the 8 corners
            Vector3 p000 = center + new Vector3(-h, -h, -h);
            Vector3 p001 = center + new Vector3(-h, -h, h);
            Vector3 p010 = center + new Vector3(-h, h, -h);
            Vector3 p011 = center + new Vector3(-h, h, h);
            Vector3 p100 = center + new Vector3(h, -h, -h);
            Vector3 p101 = center + new Vector3(h, -h, h);
            Vector3 p110 = center + new Vector3(h, h, -h);
            Vector3 p111 = center + new Vector3(h, h, h);

            Handles.DrawAAConvexPolygon(p010, p110, p111, p011); // Top
            Handles.DrawAAConvexPolygon(p000, p001, p101, p100); // Bottom
            Handles.DrawAAConvexPolygon(p001, p011, p111, p101); // Front  (+Z)
            Handles.DrawAAConvexPolygon(p000, p100, p110, p010); // Back   (-Z)
            Handles.DrawAAConvexPolygon(p000, p010, p011, p001); // Left   (-X)
            Handles.DrawAAConvexPolygon(p100, p101, p111, p110); // Right  (+X)
        }

        private static Vector3 CellCenter(Vector3Int cell, float cellSize) =>
            new Vector3(
                cell.x * cellSize + cellSize * 0.5f,
                cell.y * cellSize + cellSize * 0.5f,
                cell.z * cellSize + cellSize * 0.5f);

        private static void DrawFeedbackNotes()
        {
            if (!QAToolGlobals.showFeedbackNotes || cachedEntries == null) return;

            foreach (QAToolTelemetryClass.Entry entry in cachedEntries)
            {
                if (entry.args.TryGetValue("note", out object note))
                    Handles.Label(entry.position.ToVector3(), note.ToString());
            }
        }

        /// <summary>Draws a label with a solid outline by rendering it multiple times offset in each direction.</summary>
        private static void DrawOutlinedLabel(Vector3 pos, string text, GUIStyle style, Color outlineColor)
        {
            GUIStyle outlineStyle = new GUIStyle(style) { normal = { textColor = outlineColor } };

            Camera cam = SceneView.currentDrawingSceneView.camera;

            // Keep the offset a fixed pixel size regardless of distance
            float dist = Vector3.Distance(cam.transform.position, pos);
            float worldOffset = 1.5f * dist / cam.pixelHeight * 2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);

            Vector3 r = cam.transform.right * worldOffset;
            Vector3 u = cam.transform.up * worldOffset;

            Handles.Label(pos + r, text, outlineStyle);
            Handles.Label(pos - r, text, outlineStyle);
            Handles.Label(pos + u, text, outlineStyle);
            Handles.Label(pos - u, text, outlineStyle);

            Handles.Label(pos, text, style);
        }

        private static void DrawEvents()
        {
            if (!QAToolGlobals.showEvents || entriesByFile == null) return;

            Camera sceneCamera = SceneView.currentDrawingSceneView?.camera;
            if (sceneCamera == null) return;

            Vector3 camPos = sceneCamera.transform.position;

            bool trailActive = temporalTrail.Count > 0;

            var candidates = entriesByFile
                .SelectMany((file, fileIndex) => file
                    .Where(e => e != null
                             && e.type == QAToolJSONTypes.Event.ToString()
                             && e.args != null
                             && e.args.ContainsKey("event")
                             && (!trailActive || fileIndex == activeFileIndex))
                    .Select(e => (entry: e, fileIndex)))
                .OrderByDescending(t => Vector3.Distance(camPos, t.entry.position.ToVector3()))
                .ToList();

            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            GUIStyle abbrStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };

            foreach (var (entry, fileIndex) in candidates)
            {
                if (!entry.args.TryGetValue("event", out object evt) || evt == null) continue;

                string evtName = evt.ToString();
                string abbr = evtName.Length > 2 ? evtName.Substring(0, 2).ToUpper() : evtName.ToUpper();
                Vector3 pos = entry.position.ToVector3();
                float size = 0.5f;
                float dist = Vector3.Distance(camPos, pos);
                Color color = playerPalette[fileIndex % playerPalette.Length];

                // Register a control ID for this sphere so Unity can track hover/click
                int controlId = GUIUtility.GetControlID(FocusType.Passive);
                HandleUtility.AddControl(controlId, HandleUtility.DistanceToCube(pos, Quaternion.identity, size));

                // Handle click
                if (Event.current.type == EventType.MouseDown
                    && Event.current.button == 0
                    && HandleUtility.nearestControl == controlId)
                {
                    QAToolEventInspectorWindow.Show(entry, fileIndex, color);
                    Event.current.Use();
                }

                if (Event.current.type != EventType.Repaint) continue;

                labelStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(300f / dist), 8, 64);
                labelStyle.normal.textColor = color;
                labelStyle.hover.textColor = color;
                labelStyle.active.textColor = color;
                labelStyle.focused.textColor = color;
                abbrStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(200f / dist), 6, 48);
                abbrStyle.normal.textColor = Color.black;

                Handles.color = color;
                Handles.DrawWireCube(pos, Vector3.one * size);
                Handles.SphereHandleCap(0, pos, Quaternion.identity, size, EventType.Repaint);

                DrawOutlinedLabel(pos + Vector3.up * size, evtName, labelStyle, Color.black);
                DrawOutlinedLabel(pos, abbr, abbrStyle, color);
            }
        }

        private static void DrawTemporalTrail()
        {
            if (temporalTrail.Count == 0) return;

            int drawUpTo = isPreview ? temporalTrail.Count - 1 : scrubIndex;
            float thickness = QAToolGlobals.ghostTrailThickness + 3f;

            Handles.color = Color.white;

            if (isPreview)
                Handles.DrawAAPolyLine(thickness, temporalTrail.Take(drawUpTo + 1).ToArray());
            else
                Handles.DrawAAPolyLine(LineTex, thickness, temporalTrail.Take(drawUpTo + 1).ToArray());

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
            entriesByFile = data;

            trailsByFile = data
                .Select(file => file
                    .Where(e => e != null && e.type == "Movement")
                    .Select(e => e.position.ToVector3())
                    .ToList())
                .ToList();

            LoadHeatmap();
            RepaintScene();
        }

        private static List<QAToolTelemetryClass.Entry> FlattenEntries(List<List<QAToolTelemetryClass.Entry>> data)
        {
            return data.SelectMany(file => file).ToList();
        }

        // ──────────────────────────────────────────────
        //  Heatmap loading
        // ──────────────────────────────────────────────

        /// <summary>
        /// Rebuilds the heatmap dictionary from all loaded telemetry entries.
        /// Called on data reload and whenever the cell size slider is released.
        /// </summary>
        private static void LoadHeatmap()
        {
            _heatmap.Clear();
            _lastHeatmapCellSize = QAToolGlobals.heatmapCellSize;

            float cellSize = QAToolGlobals.heatmapCellSize;

            IEnumerable<Vector3> positions = QAToolTelemetryLoader.GetAllEntries()
                .Select(entry => entry.position.ToVector3());

            foreach (Vector3 pos in positions)
            {
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(pos.x / cellSize),
                    Mathf.FloorToInt(pos.y / cellSize),
                    Mathf.FloorToInt(pos.z / cellSize));

                if (!_heatmap.ContainsKey(cell))
                    _heatmap[cell] = 0;

                _heatmap[cell]++;
            }
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