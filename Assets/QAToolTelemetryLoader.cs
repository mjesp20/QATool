using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

public static class QAToolTelemetryLoader
{
    public static List<Vector3> LoadPositions(string filePath)
    {
        List<Vector3> positions = new List<Vector3>();
        try
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    QAToolTelemetryClass.Root entry = JsonUtility.FromJson<QAToolTelemetryClass.Root>(line);
                    positions.Add(entry.PlayerPosition.ToVector3());
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to parse line: {line}\nError: {e.Message}");
                }
            }

            Debug.Log($"Loaded {positions.Count} positions from {filePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read telemetry file: {e.Message}");
        }

        return positions;
    }

    public static void CreateGizmos(List<Vector3> positions)
    {
        
        for (int i = 0; i < positions.Count; i++)
        {
            Gizmos.DrawSphere(positions[i], 1);
            if (i != 0)
            {
                Gizmos.DrawLine(positions[i-1], positions[i]);
            }
        }
        
    }
}
