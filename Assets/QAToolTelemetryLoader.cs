using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public static List<QAToolTelemetryClass.Entry> GetFirstEntryFromAllFiles()
    {
        List<QAToolTelemetryClass.Entry> entries = new List<QAToolTelemetryClass.Entry>();

        foreach (var filePath in Directory.GetFiles(QAToolGlobals.folderPath))
        {
            string firstLine = File.ReadLines(filePath).First();

            QAToolTelemetryClass.Entry parsedLine = ParseLine(firstLine);
            entries.Add(parsedLine);
        }
        
        return entries;
    }
    
    #endregion

    /// <summary>
    /// Gets a list of all Entries by specified JSONType in the folder. Summarizes from ALL files
    /// </summary>
    public static List<QAToolTelemetryClass.Entry> GetAllEntries(QAToolJSONTypes type = QAToolJSONTypes.None)
    {
        List<QAToolTelemetryClass.Entry> entries  = new List<QAToolTelemetryClass.Entry>();
        foreach (List<QAToolTelemetryClass.Entry> entryList in LoadFromFolder())
        {
            foreach (QAToolTelemetryClass.Entry entry in entryList)
            {
                if (entry == null) continue;

                QAToolJSONTypes entryType = (QAToolJSONTypes)Enum.Parse(typeof(QAToolJSONTypes), entry.type);

                if (type == QAToolJSONTypes.None || entryType == type)
                {
                    entries.Add(entry);
                }
            }
        }
        return entries;
    }

    public static List<List<QAToolTelemetryClass.Entry>> GetAllPositionsByFile(QAToolJSONTypes type = QAToolJSONTypes.None)
    {
        List<List<QAToolTelemetryClass.Entry>> entriesByFile = new List<List<QAToolTelemetryClass.Entry>>();
        foreach (List<QAToolTelemetryClass.Entry> entryList in LoadFromFolder())
        {
            List<QAToolTelemetryClass.Entry> fileEntries = new List<QAToolTelemetryClass.Entry>();
            foreach (QAToolTelemetryClass.Entry entry in entryList)
            {
                if (entry == null) continue;
                QAToolJSONTypes entryType = (QAToolJSONTypes)Enum.Parse(typeof(QAToolJSONTypes), entry.type);
                if (type == QAToolJSONTypes.None || entryType == type)
                {
                    fileEntries.Add(entry);
                }
            }
            if (fileEntries.Count > 0)
                entriesByFile.Add(fileEntries);
        }
        return entriesByFile;
    }

}
