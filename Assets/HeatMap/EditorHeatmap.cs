using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteAlways]
public class EditorHeatmap : MonoBehaviour
{
    string folderPath = QAToolGlobals.folderPath;
    public float cellSize = 1f;
    public float heightOffset = 0.02f;
    
    [Range(0f, 1f)]
    public float opacity = 0.6f;

    public int drawThreshold = 1;

    Dictionary<Vector3Int, int> heatmap = new Dictionary<Vector3Int, int>();
    bool loaded = false;

    void Update()
    {
        if (!loaded)
        {
            LoadHeatmap();
            loaded = true;
        }
    }

    void LoadHeatmap()
    {
        heatmap.Clear();

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning("Heatmap folder not found: " + folderPath);
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.jsonl");

        foreach (string file in files)
        {
            
            List<Vector3> positions = QAToolTelemetryLoader.LoadPositions(file);

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

        Debug.Log("Editor Heatmap Loaded Cells: " + heatmap.Count);
    }

    void OnDrawGizmos()
    {
        if (heatmap == null || heatmap.Count == 0)
            return;

        int minCount = int.MaxValue;
        int maxCount = 0;

        // Find min & max of only values that pass threshold
        foreach (var kvp in heatmap)
        {
            if (kvp.Value < drawThreshold)
                continue;

            minCount = Mathf.Min(minCount, kvp.Value);
            maxCount = Mathf.Max(maxCount, kvp.Value);
        }

        // Prevent divide-by-zero if all values equal
        if (minCount == maxCount)
            maxCount = minCount + 1;

        foreach (var kvp in heatmap)
        {
            if (kvp.Value < drawThreshold)
                continue;

            float normalized =
                (float)(kvp.Value - minCount) /
                (float)(maxCount - minCount);

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