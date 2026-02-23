using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

public static class QAToolTelemetryLoader
{
    static string folderPath = QAToolGlobals.folderPath;


    #region Generic Functions
    public static List<List<QAToolTelemetryClass.Entry>> LoadFromFolder()
    {
        if (!Directory.Exists(folderPath))
        {
            throw new Exception("No Folder Found");
        }
        string[] filePaths = Directory.GetFiles(folderPath);
        if (filePaths.Length == 0)
        {
            throw new Exception("No files in folder");
        }

        List<List<QAToolTelemetryClass.Entry>> parsedFiles = new List<List<QAToolTelemetryClass.Entry>>();
        foreach (string filePath in filePaths)
        {
            parsedFiles.Add(LoadFromFile(filePath));
        }
        return parsedFiles;
    }

    public static List<QAToolTelemetryClass.Entry> LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new Exception("File not found");
        }
        List<QAToolTelemetryClass.Entry> parsedLines = new List<QAToolTelemetryClass.Entry>();
        foreach (string entry in File.ReadLines(filePath))
        {
            parsedLines.Add(ParseLine(entry));
        }
        return parsedLines;
    }

    public static QAToolTelemetryClass.Entry ParseLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<QAToolTelemetryClass.Entry>(line);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse line: {line}\nError: {e.Message}");
            return null;
        }
    }
    #endregion

    /// <summary>
    /// Gets a list of all positions by specified JSONType in the folder. Summarizes from ALL files
    /// </summary>
    public static List<Vector3> GetAllPositions(QAToolJSONTypes type = QAToolJSONTypes.None)
    {
        List<Vector3> positions = new List<Vector3>();
        foreach (List<QAToolTelemetryClass.Entry> entryList in LoadFromFolder())
        {
            foreach (QAToolTelemetryClass.Entry entry in entryList)
            {
                if (entry == null) continue;

                QAToolJSONTypes entryType = (QAToolJSONTypes)Enum.Parse(typeof(QAToolJSONTypes), entry.type);

                if (type == QAToolJSONTypes.None || entryType == type)
                {
                    positions.Add(entry.PlayerPosition.ToVector3());
                }
            }
        }
        return positions;
    }

}
