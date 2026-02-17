using System.Collections.Generic;
using System.IO;
using UnityEngine;

[ExecuteAlways]
public class EditorHeatmap : MonoBehaviour
{
    public string folderPath = @"C:\Users\rasmu\Documents\QATool";
    public float cellSize = 1f;
    public float heightOffset = 0.02f;
    
    [Range(0f, 1f)]
    public float opacity = 0.6f;

    Dictionary<Vector2Int, int> heatmap = new Dictionary<Vector2Int, int>();
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
            string[] lines = File.ReadAllLines(file);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                PlayerLogEntry entry =
                    JsonUtility.FromJson<PlayerLogEntry>(line);

                if (entry == null || entry.PlayerPosition == null)
                    continue;

                Vector2Int cell = new Vector2Int(
                    Mathf.FloorToInt(entry.PlayerPosition.x / cellSize),
                    Mathf.FloorToInt(entry.PlayerPosition.z / cellSize)
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

        int maxCount = 0;
        foreach (var kvp in heatmap)
            maxCount = Mathf.Max(maxCount, kvp.Value);

        foreach (var kvp in heatmap)
        {
            float normalized = (float)kvp.Value / maxCount;
            Color color = Color.Lerp(Color.green, Color.red, normalized);

            Vector3 center = new Vector3(
                kvp.Key.x * cellSize + cellSize / 2f,
                heightOffset,
                kvp.Key.y * cellSize + cellSize / 2f
            );

            Gizmos.color = new Color(color.r, color.g, color.b, opacity);
            Gizmos.DrawCube(center, new Vector3(cellSize, 0.02f, cellSize));
        }
    }
}

[System.Serializable]
public class PositionData
{
    public float x;
    public float y;
    public float z;
}

[System.Serializable]
public class PlayerLogEntry
{
    public string time;
    public string playerID;
    public PositionData PlayerPosition;
}
