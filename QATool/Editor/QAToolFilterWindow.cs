using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

// ─────────────────────────────────────────────
// Editor Window
// ─────────────────────────────────────────────
namespace QATool
{

    public class QAToolFilterWindow : EditorWindow
    {
        private TreeViewState treeViewState;
        private PlayerTreeView treeView;
        private List<string> argKeys;

        // Local UI state — operator selection and raw string input per arg
        private Dictionary<string, QAToolGlobals.FilterOperator> filterOps = new();
        private Dictionary<string, string> filterValueStrings = new();

        public static void ShowWindow() => GetWindow<QAToolFilterWindow>("QA Tool Filters").Show();

        private void OnEnable()
        {
            if (treeViewState == null)
                treeViewState = new TreeViewState();

            var loadedData = QAToolTelemetryLoader.LoadFromFolder();
            argKeys = CollectUniqueArgKeys(loadedData);

            InitFilterState();

            var headerState = PlayerTreeView.CreateHeaderState(position.width, argKeys);
            treeView = new PlayerTreeView(treeViewState, new MultiColumnHeader(headerState), loadedData, argKeys);
        }

        // Seeds local UI state from whatever is already in QAToolGlobals, or defaults to Ignore
        private void InitFilterState()
        {
            filterOps.Clear();
            filterValueStrings.Clear();
            foreach (var key in argKeys)
            {
                if (QAToolGlobals.FlagFilters != null &&
                    QAToolGlobals.FlagFilters.TryGetValue(key, out var existing) &&
                    existing.enabled)
                {
                    filterOps[key] = existing.op;
                    filterValueStrings[key] = existing.value?.ToString() ?? "";
                }
                else
                {
                    filterOps[key] = QAToolGlobals.FilterOperator.Ignore;
                    filterValueStrings[key] = "";
                }
            }
        }

        private void OnGUI()
        {
            const float filterStripHeight = 26f;
            const float buttonHeight = 30f;

            // Filter strip must be drawn before the tree view to stay outside its clip scope
            DrawFilterStrip(new Rect(0, 0, position.width, filterStripHeight));
            treeView.OnGUI(new Rect(0, filterStripHeight, position.width, position.height - buttonHeight - filterStripHeight));

            if (GUI.Button(new Rect(10, position.height - 28, 140, 24), "Clear All Filters"))
            {
                foreach (var key in argKeys)
                {
                    filterOps[key] = QAToolGlobals.FilterOperator.Ignore;
                    filterValueStrings[key] = "";
                }
                CommitFilters();
            }
        }

        // Draws one filter control per arg column, horizontally aligned with tree view columns
        private void DrawFilterStrip(Rect stripRect)
        {
            float scrollX = treeViewState.scrollPos.x;
            var columns = treeView.multiColumnHeader.state.columns;
            int[] visibleColumns = treeView.multiColumnHeader.state.visibleColumns;

            float xPos = -scrollX;
            foreach (int colIdx in visibleColumns)
            {
                float colWidth = columns[colIdx].width;

                if (colIdx >= 1 && colIdx <= argKeys.Count)
                {
                    string argName = argKeys[colIdx - 1];
                    DrawArgFilter(new Rect(xPos + 1, stripRect.y + 2, colWidth - 3, stripRect.height - 4), argName);
                }

                xPos += colWidth;
            }
        }

        private void DrawArgFilter(Rect rect, string argName)
        {
            Type argType = GetArgType(argName);

            var currentOp = filterOps.TryGetValue(argName, out var op) ? op : QAToolGlobals.FilterOperator.Ignore;
            var currentValStr = filterValueStrings.TryGetValue(argName, out var vs) ? vs : "";

            // Bool and string only support equality; numerics (and unknown) get the full set
            bool isNumeric = argType == typeof(int) || argType == typeof(float) || argType == null;
            bool isBool = argType == typeof(bool);

            QAToolGlobals.FilterOperator[] allowedOperators = isNumeric
                ? new[]
                {
                QAToolGlobals.FilterOperator.Ignore,
                QAToolGlobals.FilterOperator.Equal,
                QAToolGlobals.FilterOperator.NotEqual,
                QAToolGlobals.FilterOperator.GreaterThan,
                QAToolGlobals.FilterOperator.GreaterThanOrEqual,
                QAToolGlobals.FilterOperator.LessThan,
                QAToolGlobals.FilterOperator.LessThanOrEqual
                }
                : new[]
                {
                QAToolGlobals.FilterOperator.Ignore,
                QAToolGlobals.FilterOperator.Equal,
                QAToolGlobals.FilterOperator.NotEqual
                };

            var opValues = allowedOperators;
            var opLabels = allowedOperators
                .Select(op => QAToolGlobals.FilterOperatorToString[op])
                .ToArray();

            int opIdx = Mathf.Max(0, System.Array.IndexOf(opValues, currentOp));

            const float opWidth = 28f;
            Rect opRect = new Rect(rect.x, rect.y, opWidth, rect.height);
            Rect valRect = new Rect(rect.x + opWidth + 2, rect.y, rect.width - opWidth - 2, rect.height);

            EditorGUI.BeginChangeCheck();

            int newOpIdx = EditorGUI.Popup(opRect, opIdx, opLabels);
            var newOp = opValues[newOpIdx];

            // Use currentOp (saved state) to decide visibility so the field persists across frames.
            // If the dropdown was just changed to Ignore, the field disappears next frame.
            string newValStr = currentValStr;
            if (currentOp != QAToolGlobals.FilterOperator.Ignore || newOp != QAToolGlobals.FilterOperator.Ignore)
            {
                newValStr = isBool
                    ? (EditorGUI.Toggle(valRect, currentValStr == "true") ? "true" : "false")
                    : EditorGUI.TextField(valRect, currentValStr);
            }

            if (EditorGUI.EndChangeCheck())
            {
                filterOps[argName] = newOp;
                filterValueStrings[argName] = newValStr;
                CommitFilters();
            }
        }

        // Writes current UI state to QAToolGlobals.FlagFilters and refreshes the tree view.
        // A filter is only marked enabled when the value string successfully parses for its type —
        // this prevents QAToolGlobals from receiving half-typed strings like "5." or "".
        private void CommitFilters()
        {
            var filters = new Dictionary<string, QAToolGlobals.FlagFilter>();
            foreach (var key in argKeys)
            {
                var op = filterOps.TryGetValue(key, out var o) ? o : QAToolGlobals.FilterOperator.Ignore;
                var valStr = filterValueStrings.TryGetValue(key, out var v) ? v : "";

                bool parsed = TryParseFilterValue(valStr, GetArgType(key), out object parsedValue);

                filters[key] = new QAToolGlobals.FlagFilter
                {
                    enabled = op != QAToolGlobals.FilterOperator.Ignore && parsed,
                    op = op,
                    value = parsedValue
                };
            }
            QAToolGlobals.FlagFilters = filters;
            treeView.Reload();
        }

        // ── Helpers ──────────────────────────────

        private static Type GetArgType(string argName)
        {
            if (QAToolGlobals.flagTypes != null && QAToolGlobals.flagTypes.TryGetValue(argName, out var t))
                return t;
            return null;
        }

        // Returns true only when valStr is a complete, valid value for the given type.
        // Strings always succeed. Numerics require a full successful parse (no partial input).
        private static bool TryParseFilterValue(string valStr, Type type, out object result)
        {
            if (type == typeof(int))
            {
                if (int.TryParse(valStr, out int i)) { result = i; return true; }
                result = null; return false;
            }
            if (type == typeof(float))
            {
                if (float.TryParse(valStr,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float f))
                { result = f; return true; }
                result = null; return false;
            }
            if (type == typeof(bool))
            {
                result = valStr == "true";
                return true;
            }
            // String or unknown type — always valid, even if empty
            result = valStr;
            return !string.IsNullOrEmpty(valStr);
        }

        private static List<string> CollectUniqueArgKeys(List<List<QAToolTelemetryClass.Entry>> loadedData)
        {
            var keys = new HashSet<string>();
            foreach (var session in loadedData)
            {
                if (session == null) continue;
                foreach (var entry in session)
                {
                    if (entry?.args == null) continue;
                    foreach (var key in entry.args.Keys)
                    {
                        if (key != "note" && key != "event")
                            keys.Add(key);
                    }
                }
            }
            var sorted = keys.ToList();
            sorted.Sort();
            return sorted;
        }
    }

    // ─────────────────────────────────────────────
    // Tree View
    // ─────────────────────────────────────────────

    public class PlayerTreeView : TreeView
    {
        private readonly List<string> argKeys;
        private readonly int argColumnCount;

        public List<PlayerRowData> data = new List<PlayerRowData>();

        public PlayerTreeView(TreeViewState state, MultiColumnHeader header,
                              List<List<QAToolTelemetryClass.Entry>> loadedData,
                              List<string> argKeys) : base(state, header)
        {
            this.argKeys = argKeys;
            this.argColumnCount = argKeys.Count;

            rowHeight = 20;
            showAlternatingRowBackgrounds = true;

            foreach (var session in loadedData)
            {
                if (session == null || session.Count == 0) continue;
                data.Add(BuildRowData(session));
            }

            Reload();
        }

        // Collects the last recorded value of each arg across all entries in the session
        private PlayerRowData BuildRowData(List<QAToolTelemetryClass.Entry> session)
        {
            var argValues = new Dictionary<string, object>();

            foreach (var entry in session)
            {
                if (entry?.args == null) continue;
                foreach (var kvp in entry.args)
                {
                    if (argKeys.Contains(kvp.Key))
                        argValues[kvp.Key] = kvp.Value; // last value wins
                }
            }

            string playerName = session[0]?.playerID.ToString() ?? "Unknown";
            return new PlayerRowData(playerName, session, argValues);
        }

        // ── TreeView overrides ────────────────────

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1 };
            var items = new List<TreeViewItem>();
            for (int i = 0; i < data.Count; i++)
                items.Add(new TreeViewItem { id = i + 1, depth = 0 });
            SetupParentsAndChildrenFromDepths(root, items);
            return root;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            int dataIndex = args.item.id - 1;
            if (dataIndex < 0 || dataIndex >= data.Count) return;

            var row = data[dataIndex];

            // Dim rows that don't satisfy the active filters
            if (!PassesFilters(row))
                EditorGUI.DrawRect(args.rowRect, new Color(0f, 0f, 0f, 0.3f));

            for (int visibleCol = 0; visibleCol < args.GetNumVisibleColumns(); visibleCol++)
            {
                Rect cellRect = args.GetCellRect(visibleCol);
                int colIndex = args.GetColumn(visibleCol);

                if (colIndex == 0)
                {
                    EditorGUI.LabelField(cellRect, row.playerName);
                }
                else if (colIndex <= argColumnCount)
                {
                    string argName = argKeys[colIndex - 1];
                    string display = row.argValues.TryGetValue(argName, out var val) ? val?.ToString() ?? "" : "—";
                    EditorGUI.LabelField(cellRect, display);
                }
            }
        }

        // ── Filter evaluation ─────────────────────

        private static bool PassesFilters(PlayerRowData row)
        {
            if (QAToolGlobals.FlagFilters == null) return true;

            foreach (var kvp in QAToolGlobals.FlagFilters)
            {
                var filter = kvp.Value;
                if (!filter.enabled || filter.op == QAToolGlobals.FilterOperator.Ignore) continue;

                row.argValues.TryGetValue(kvp.Key, out var rowValue);
                if (!EvaluateFilter(rowValue, filter))
                    return false;
            }
            return true;
        }

        private static bool EvaluateFilter(object rowValue, QAToolGlobals.FlagFilter filter)
        {
            if (filter.value == null) return true;

            // Prefer numeric comparison when both sides are convertible
            if (TryToDouble(rowValue, out double dRow) && TryToDouble(filter.value, out double dFilter))
            {
                return filter.op switch
                {
                    QAToolGlobals.FilterOperator.Equal => dRow == dFilter,
                    QAToolGlobals.FilterOperator.NotEqual => dRow != dFilter,
                    QAToolGlobals.FilterOperator.GreaterThan => dRow > dFilter,
                    QAToolGlobals.FilterOperator.GreaterThanOrEqual => dRow >= dFilter,
                    QAToolGlobals.FilterOperator.LessThan => dRow < dFilter,
                    QAToolGlobals.FilterOperator.LessThanOrEqual => dRow <= dFilter,
                    _ => true
                };
            }

            // Fall back to string comparison (only Equal / NotEqual are meaningful here)
            string sRow = rowValue?.ToString() ?? "";
            string sFilter = filter.value.ToString();
            return filter.op switch
            {
                QAToolGlobals.FilterOperator.Equal => sRow == sFilter,
                QAToolGlobals.FilterOperator.NotEqual => sRow != sFilter,
                _ => true
            };
        }

        private static bool TryToDouble(object val, out double result)
        {
            result = 0;
            if (val == null) return false;
            try { result = System.Convert.ToDouble(val); return true; }
            catch { return false; }
        }

        // ── Debug helper ─────────────────────────

        public void PrintTableValues()
        {
            Debug.Log("=== Table Values ===");
            foreach (var row in data)
            {
                string rowStr = row.playerName + ": ";
                foreach (var key in argKeys)
                    rowStr += $"{key}={(row.argValues.TryGetValue(key, out var v) ? v?.ToString() ?? "—" : "—")} ";
                Debug.Log(rowStr);
            }
            Debug.Log("===================");
        }

        // ── Static factory ───────────────────────

        public static MultiColumnHeaderState CreateHeaderState(float width, List<string> argKeys)
        {
            var columns = new MultiColumnHeaderState.Column[1 + argKeys.Count];

            columns[0] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Player Name"),
                width = 150,
                minWidth = 80,
                autoResize = true
            };

            for (int i = 0; i < argKeys.Count; i++)
            {
                columns[i + 1] = new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent(argKeys[i]),
                    width = 100,
                    minWidth = 60,
                    autoResize = true
                };
            }

            return new MultiColumnHeaderState(columns);
        }
    }

    // ─────────────────────────────────────────────
    // Data Model
    // ─────────────────────────────────────────────

    [System.Serializable]
    public class PlayerRowData
    {
        public string playerName;
        public List<QAToolTelemetryClass.Entry> entries;
        public Dictionary<string, object> argValues; // last recorded value per arg

        public PlayerRowData(string name, List<QAToolTelemetryClass.Entry> entries, Dictionary<string, object> argValues)
        {
            playerName = name;
            this.entries = entries;
            this.argValues = argValues ?? new Dictionary<string, object>();
        }
    }
}