using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEditor.IMGUI.Controls;

public class QAToolFilterWindow : EditorWindow
{
    private TreeViewState treeViewState;
    private PlayerTreeView treeView;

    [MenuItem("Window/QA Tool Filters")]
    public static void ShowWindow()
    {
        GetWindow<QAToolFilterWindow>("QA Tool Filters").Show();
    }

    private void OnEnable()
    {
        if (treeViewState == null)
            treeViewState = new TreeViewState();

        var headerState = PlayerTreeView.CreateDefaultMultiColumnHeaderState(position.width);
        var multiColumnHeader = new MultiColumnHeader(headerState);
        multiColumnHeader.canSort = true;

        treeView = new PlayerTreeView(treeViewState, multiColumnHeader);
    }

    private void OnGUI()
    {
        treeView.OnGUI(new Rect(0, 0, position.width, position.height - 30));

        // Print Table Values button
        Rect buttonRect = new Rect(10, position.height - 28, 220, 24);
        if (GUI.Button(buttonRect, "Print Table Values"))
        {
            treeView.PrintTableValues();
        }
    }
}

public class PlayerTreeView : TreeView
{
    private const int triStateColumnCount = 4;
    private int[] headerTriStates;
    public List<PlayerRowData> data = new List<PlayerRowData>();

    // JSON columns to display
    private string[] jsonColumns = new string[] { "Type", "TotalTime", "PositionsCount", "ArgsCount" };

    public PlayerTreeView(TreeViewState state, MultiColumnHeader header) : base(state, header)
{
    rowHeight = 20;
    showAlternatingRowBackgrounds = true;
    headerTriStates = new int[triStateColumnCount];

    string folderPath = QAToolGlobals.folderPath;

    if (Directory.Exists(folderPath))
    {
        // 🔹 Still get file list exactly like original system
        string[] filePaths = Directory.GetFiles(folderPath);

        for (int i = 0; i < filePaths.Length; i++)
        {
            string playerName = Path.GetFileNameWithoutExtension(filePaths[i]);

            List<QAToolTelemetryClass.Entry> entries = new List<QAToolTelemetryClass.Entry>();

            // 🔥 Re-parse manually using allowed ParseLine()
            foreach (string line in File.ReadLines(filePaths[i]))
            {
                var parsed = QAToolTelemetryLoader.ParseLine(line);
                if (parsed != null)
                    entries.Add(parsed);
            }

            // ---- Compute summary values ----
            float totalTime = 0f;
            List<string> types = new List<string>();
            int positionsCount = 0;
            int argsCount = 0;

            foreach (var entry in entries)
            {
                if (entry == null) continue;

                totalTime += entry.time;

                if (!string.IsNullOrEmpty(entry.type) &&
                    !types.Contains(entry.type) &&
                    types.Count < 5)
                {
                    types.Add(entry.type);
                }

                if (entry.PlayerPosition != null)
                    positionsCount++;

                argsCount += entry.args?.Count ?? 0;
            }

            var row = new PlayerRowData(playerName, entries)
            {
                jsonValues = new object[]
                {
                    string.Join(", ", types),
                    totalTime,
                    positionsCount,
                    argsCount
                }
            };

            data.Add(row);
        }
    }

    Reload();
}

    protected override TreeViewItem BuildRoot()
    {
        var root = new TreeViewItem { id = 0, depth = -1 };

        var items = new List<TreeViewItem> { new TreeViewItem { id = 1, depth = 0, displayName = "HeaderRow" } };
        for (int i = 0; i < data.Count; i++)
            items.Add(new TreeViewItem { id = i + 2, depth = 0 });

        SetupParentsAndChildrenFromDepths(root, items);
        return root;
    }

    protected override void RowGUI(RowGUIArgs args)
    {
        int rowIndex = args.item.id;

        for (int i = 0; i < args.GetNumVisibleColumns(); i++)
        {
            Rect cellRect = args.GetCellRect(i);

            if (rowIndex == 1)
            {
                // Header row
                if (i == 0)
                    EditorGUI.LabelField(cellRect, "Player Name");
                else if (i <= triStateColumnCount)
                    if (GUI.Button(cellRect, $"Tri-State {i}"))
                        OverrideAllRowsInColumn(i - 1);
                    else
                    {
                        int jsonColIndex = i - 1 - triStateColumnCount;
                        if (jsonColIndex >= 0 && jsonColIndex < jsonColumns.Length)
                            EditorGUI.LabelField(cellRect, jsonColumns[jsonColIndex]);
                    }
            }
            else
            {
                int dataIndex = rowIndex - 2;

                if (dataIndex < 0 || dataIndex >= data.Count)
                    continue; // skip invalid rows

                var row = data[dataIndex];

                if (i == 0)
                    EditorGUI.LabelField(cellRect, row.playerName);
                else if (i <= triStateColumnCount)
                {
                    int triIndex = i - 1;
                    if (triIndex >= 0 && triIndex < row.triStates.Length)
                        if (GUI.Button(cellRect, GetTriStateSymbol(row.triStates[triIndex])))
                            row.triStates[triIndex] = (row.triStates[triIndex] + 1) % 3;
                }
                else
                {
                    int jsonIndex = i - 1 - triStateColumnCount;
                    if (row.jsonValues != null && jsonIndex >= 0 && jsonIndex < row.jsonValues.Length)
                        EditorGUI.LabelField(cellRect, row.jsonValues[jsonIndex]?.ToString());
                }
            }
        }
    }

    private void OverrideAllRowsInColumn(int triIndex)
    {
        headerTriStates[triIndex] = (headerTriStates[triIndex] + 1) % 3;
        foreach (var row in data)
            row.triStates[triIndex] = headerTriStates[triIndex];
        Reload();
    }

    private string GetTriStateSymbol(int state)
    {
        return state == 0 ? "☐" : state == 1 ? "◩" : "☑";
    }

    public void PrintTableValues()
    {
        Debug.Log("=== Table Values ===");
        foreach (var row in data)
        {
            string rowStr = row.playerName + ": ";
            for (int i = 0; i < row.triStates.Length; i++)
                rowStr += GetTriStateSymbol(row.triStates[i]) + " ";
            for (int i = 0; i < row.jsonValues.Length; i++)
                rowStr += row.jsonValues[i] + " ";
            Debug.Log(rowStr);
        }
        Debug.Log("===================");
    }

    public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float width)
    {
        int totalColumns = 1 + triStateColumnCount + 4; // Name + Tri-State + 4 JSON columns
        var columns = new MultiColumnHeaderState.Column[totalColumns];

        columns[0] = new MultiColumnHeaderState.Column
        {
            headerContent = new GUIContent("Player Name"),
            width = 150,
            minWidth = 80,
            autoResize = true
        };

        for (int i = 0; i < triStateColumnCount; i++)
        {
            columns[i + 1] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent($"Tri-State {i + 1}"),
                width = 100,
                minWidth = 60,
                autoResize = true
            };
        }

        string[] jsonCols = new string[] { "Type", "TotalTime", "PositionsCount", "ArgsCount" };
        for (int i = 0; i < jsonCols.Length; i++)
        {
            columns[i + 1 + triStateColumnCount] = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent(jsonCols[i]),
                width = 150,
                minWidth = 60,
                autoResize = true
            };
        }

        return new MultiColumnHeaderState(columns);
    }
}

[System.Serializable]
public class PlayerRowData
{
    public string playerName;
    public int[] triStates;
    public List<QAToolTelemetryClass.Entry> entries;
    public object[] jsonValues; // dynamically stored JSON info

    public PlayerRowData(string name, List<QAToolTelemetryClass.Entry> parsedEntries)
    {
        playerName = name;
        entries = parsedEntries;
        triStates = new int[4];
        jsonValues = new object[4]; // match JSON columns: Type, TotalTime, PositionsCount, ArgsCount
    }
}