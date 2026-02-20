using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteAlways]
public class EditorHeatmap : MonoBehaviour
{
    string folderPath = QAToolGlobals.folderPath;
    [Range(0f, 2f)]
    public float cellSize = 1f;
    public float heightOffset = 0.02f;

    [Range(0f, 1f)]
    public float opacity = 0.6f;
    [Range(1, 5)]
    public int drawThreshold = 1;
    public float contrast;

    [System.NonSerialized]
    Dictionary<Vector3Int, int> heatmap = new Dictionary<Vector3Int, int>();

    [System.NonSerialized]
    bool loaded = false;

    [System.NonSerialized]
    float lastCellSize = -1f;

    void OnValidate()
    {
        if (!loaded || cellSize != lastCellSize)
        {
            LoadHeatmap();
            loaded = true;
            lastCellSize = cellSize;
        }

#if UNITY_EDITOR
        UnityEditor.SceneView.RepaintAll();
#endif
    }

    void LoadHeatmap()
    {
        if (heatmap == null)
            heatmap = new Dictionary<Vector3Int, int>();

        heatmap.Clear();

        if (!Directory.Exists(folderPath))
        {
            //Debug.LogWarning("Heatmap folder not found: " + folderPath);
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.jsonl");
        Debug.Log($"Found {files.Length} jsonl files in {folderPath}");

        foreach (string file in files)
        {
            List<Vector3> positions = QAToolTelemetryLoader.LoadPositions(file);
            //Debug.Log($"{Path.GetFileName(file)}: {positions.Count} positions");

            foreach (var position in positions)
            {
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(position.x / cellSize),
                    Mathf.FloorToInt(position.y / cellSize),
                    Mathf.FloorToInt(position.z / cellSize)
                );

                if (!heatmap.ContainsKey(cell))
                    heatmap[cell] = 0;
                heatmap[cell]++;
            }
        }

        //Debug.Log($"Heatmap loaded: {heatmap.Count} cells");
    }

    void OnDrawGizmos()
{
    if (heatmap == null || heatmap.Count == 0)
        return;

#if UNITY_EDITOR
    Camera cam = UnityEditor.SceneView.lastActiveSceneView?.camera;
#else
    Camera cam = Camera.current;
#endif

    if (cam == null)
        return;

    int minCount = int.MaxValue;
    int maxCount = 0;

    foreach (var kvp in heatmap)
    {
        if (kvp.Value < drawThreshold) continue;
        minCount = Mathf.Min(minCount, kvp.Value);
        maxCount = Mathf.Max(maxCount, kvp.Value);
    }

    if (minCount == int.MaxValue) return;
    if (minCount == maxCount) maxCount = minCount + 1;

    int logMin = Mathf.Max(1, minCount);
    int logMax = Mathf.Max(logMin + 1, maxCount);

    // 🔥 Create sortable list
    var cells = new List<KeyValuePair<Vector3Int, int>>(heatmap);

    // 🔥 Sort by distance from camera (farthest first!)
    cells.Sort((a, b) =>
    {
        Vector3 centerA = new Vector3(
            a.Key.x * cellSize + cellSize / 2f,
            a.Key.y * cellSize + cellSize / 2f,
            a.Key.z * cellSize + cellSize / 2f
        );

        Vector3 centerB = new Vector3(
            b.Key.x * cellSize + cellSize / 2f,
            b.Key.y * cellSize + cellSize / 2f,
            b.Key.z * cellSize + cellSize / 2f
        );

        float distA = Vector3.Distance(cam.transform.position, centerA);
        float distB = Vector3.Distance(cam.transform.position, centerB);

        Color color = Color.Lerp(Color.blue, Color.red, normalized);
        return distB.CompareTo(distA); // Farthest first
    });

    foreach (var kvp in cells)
    {
        if (kvp.Value < drawThreshold) continue;

        float normalized = Mathf.Log(kvp.Value - logMin + 2f) / Mathf.Log(logMax - logMin + 2f);
        normalized = Mathf.Pow(normalized, contrast);

        Color color = Color.Lerp(Color.green, Color.red, normalized);

        Vector3 center = new Vector3(
            kvp.Key.x * cellSize + cellSize / 2f,
            kvp.Key.y * cellSize + cellSize / 2f,
            kvp.Key.z * cellSize + cellSize / 2f
        );

        Gizmos.color = new Color(color.r, color.g, color.b, opacity);
        Gizmos.DrawCube(center, Vector3.one * cellSize);
    }
}
}
