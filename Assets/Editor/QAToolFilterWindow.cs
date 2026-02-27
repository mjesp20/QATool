using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.IMGUI.Controls;

public class QAToolFilterWindow : EditorWindow
{
    private TreeViewState treeViewState;
    private PlayerTreeView treeView;


    public static void ShowWindow()
    {
        GetWindow<QAToolFilterWindow>("QA Tool Filters").Show();
    }

    private void OnEnable()
    {
        if (treeViewState == null)
            treeViewState = new TreeViewState();
        
        List<List<QAToolTelemetryClass.Entry>> loadedData = QAToolTelemetryLoader.LoadFromFolder();
        List<string> argKeys = CollectArgKeys(loadedData);

        var headerState = PlayerTreeView.CreateDefaultMultiColumnHeaderState(position.width);
        var multiColumnHeader = new TriStateMultiColumnHeader(headerState, argKeys.Count);

        treeView = new PlayerTreeView(treeViewState, multiColumnHeader, loadedData, argKeys);
    }
    
    private List<string> CollectArgKeys(List<List<QAToolTelemetryClass.Entry>> loadedData)
    {
        var keys = new List<string>();
        foreach (var entries in loadedData)
        {
            if (entries == null) continue;
            foreach (var entry in entries)
            {
                if (entry?.args == null) continue;
                foreach (var key in entry.args.Keys)
                {
                    if (!keys.Contains(key))
                        keys.Add(key);
                }
            }
        }
        return keys;
    }

    private void OnGUI()
    {
        treeView.OnGUI(new Rect(0, 0, position.width, position.height - 30));

        Rect buttonRect = new Rect(10, position.height - 28, 220, 24);
        if (GUI.Button(buttonRect, "Print Table Values"))
            treeView.PrintTableValues();
    }
}

public class PlayerTreeView : TreeView
{
    private List<string> argKeys;
    private int triStateColumnCount => argKeys.Count;
    private int[] headerTriStates;
    public List<PlayerRowData> data = new List<PlayerRowData>();

    private string[] jsonColumns = new string[] { "Type", "TotalTime", "PositionsCount", "ArgsCount" };

    public PlayerTreeView(TreeViewState state, MultiColumnHeader header,
                          List<List<QAToolTelemetryClass.Entry>> loadedData,
                          List<string> argKeys) : base(state, header)
    {
        this.argKeys = argKeys;
        rowHeight = 20;
        showAlternatingRowBackgrounds = true;
        headerTriStates = new int[triStateColumnCount];

        foreach (List<QAToolTelemetryClass.Entry> entries in loadedData)
        {
            if (entries == null || entries.Count == 0) continue;

            float totalTime = 0f;
            List<string> types = new List<string>();
            int positionsCount = 0;
            int argsCount = 0;
            
            bool[] presentArgs = new bool[argKeys.Count];

            foreach (QAToolTelemetryClass.Entry entry in entries)
            {
                if (entry == null) continue;

                totalTime = Mathf.Max(totalTime, entry.time);

                if (!string.IsNullOrEmpty(entry.type) &&
                    !types.Contains(entry.type) &&
                    types.Count < 5)
                {
                    types.Add(entry.type);
                }

                if (entry.PlayerPosition != null)
                    positionsCount++;

                argsCount += entry.args?.Count ?? 0;
                
                if (entry.args != null)
                {
                    for (int k = 0; k < argKeys.Count; k++)
                    {
                        if (entry.args.ContainsKey(argKeys[k]))
                            presentArgs[k] = true;
                    }
                }
            }

            string playerName = entries[0]?.playerID.ToString() ?? "Unknown";

            var row = new PlayerRowData(playerName, entries, triStateColumnCount)
            {
                presentArgs = presentArgs,
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

        (header as TriStateMultiColumnHeader).onTriStateColumnClicked = OverrideAllRowsInColumn;
        Reload();
    }

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

        for (int i = 0; i < args.GetNumVisibleColumns(); i++)
        {
            Rect cellRect = args.GetCellRect(i);

            if (i == 0)
            {
                EditorGUI.LabelField(cellRect, row.playerName);
            }
            else if (i <= triStateColumnCount)
            {
                int triIndex = i - 1;
                bool hasArg = row.presentArgs != null && row.presentArgs[triIndex];
                
                GUI.enabled = hasArg;
                if (GUI.Button(cellRect, GetTriStateSymbol(hasArg ? row.triStates[triIndex] : 0)))
                    row.triStates[triIndex] = (row.triStates[triIndex] + 1) % 3;
                GUI.enabled = true;
            }
            else
            {
                int jsonIndex = i - 1 - triStateColumnCount;
                if (row.jsonValues != null && jsonIndex >= 0 && jsonIndex < row.jsonValues.Length)
                    EditorGUI.LabelField(cellRect, row.jsonValues[jsonIndex]?.ToString());
            }
        }
    }

    private void OverrideAllRowsInColumn(int triIndex)
    {
        headerTriStates[triIndex] = (headerTriStates[triIndex] + 1) % 3;
        foreach (var row in data)
        {
            if (row.presentArgs != null && row.presentArgs[triIndex])
                row.triStates[triIndex] = headerTriStates[triIndex];
        }
        Reload();
    }

    private string GetTriStateSymbol(int state)
    {
        return state == 0 ? "☐" : state == 1 ? "☒" : "☑";
    }

    public void PrintTableValues()
    {
        Debug.Log("=== Table Values ===");
        foreach (var row in data)
        {
            string rowStr = row.playerName + ": ";
            for (int i = 0; i < argKeys.Count; i++)
                rowStr += $"{argKeys[i]}={GetTriStateSymbol(row.triStates[i])} ";
            for (int i = 0; i < row.jsonValues.Length; i++)
                rowStr += row.jsonValues[i] + " ";
            Debug.Log(rowStr);
        }
        Debug.Log("===================");
    }

    public void test()
    {
        HashSet<string> seenArguments = new HashSet<string>();
        
        foreach (QAToolTelemetryClass.Entry entry in QAToolTelemetryLoader.GetFirstEntryFromAllFiles())
        {
            foreach (KeyValuePair<string, object> keyValuePair in entry.args)
            {
                seenArguments.Add(keyValuePair.Key);
                
            }
        }
    }
    
    private static List<string> GetAllUniqueArgKeys()
    {
        HashSet<string> seenArguments = new HashSet<string>();

        foreach (QAToolTelemetryClass.Entry entry in QAToolTelemetryLoader.GetFirstEntryFromAllFiles())
        {
            foreach (KeyValuePair<string, object> keyValuePair in entry.args)
            {
                seenArguments.Add(keyValuePair.Key);
            }
        }
        
        List<string> argKeys = seenArguments.ToList();
        argKeys.Sort();

        return argKeys;
    }
    
    

    public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(float width)
    {
        List<string> argKeys = GetAllUniqueArgKeys();

        int totalColumns = 1 + argKeys.Count + 4;
        var columns = new MultiColumnHeaderState.Column[totalColumns];

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

        string[] jsonCols = { "Type", "TotalTime", "PositionsCount", "ArgsCount" };

        for (int i = 0; i < jsonCols.Length; i++)
        {
            columns[i + 1 + argKeys.Count] = new MultiColumnHeaderState.Column
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

public class TriStateMultiColumnHeader : MultiColumnHeader
{
    public System.Action<int> onTriStateColumnClicked;
    private int triStateColumnCount;

    public TriStateMultiColumnHeader(MultiColumnHeaderState state, int triStateColumnCount) : base(state)
    {
        this.triStateColumnCount = triStateColumnCount;
    }

    protected override void ColumnHeaderClicked(MultiColumnHeaderState.Column column, int columnIndex)
    {
        if (columnIndex >= 1 && columnIndex <= triStateColumnCount)
        {
            onTriStateColumnClicked?.Invoke(columnIndex - 1);
        }
        else
        {
            base.ColumnHeaderClicked(column, columnIndex);
        }
    }
}

[System.Serializable]
public class PlayerRowData
{
    public string playerName;
    public int[] triStates;
    public bool[] presentArgs;
    public List<QAToolTelemetryClass.Entry> entries;
    public object[] jsonValues;

    public PlayerRowData(string name, List<QAToolTelemetryClass.Entry> parsedEntries, int triStateCount)
    {
        playerName = name;
        entries = parsedEntries;
        triStates = new int[triStateCount];
        presentArgs = new bool[triStateCount];
        jsonValues = new object[4];
    }
}