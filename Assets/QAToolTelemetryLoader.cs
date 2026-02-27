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
            QAToolTelemetryClass.Entry entry = JsonConvert.DeserializeObject<QAToolTelemetryClass.Entry>(line);
            return FilterEntry(entry);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse line: {line}\nError: {e.Message}");
            return null;
        }
    }

    private static QAToolTelemetryClass.Entry FilterEntry(QAToolTelemetryClass.Entry entry)
    {
        if (entry.args == null)
            return entry;


        foreach (string argName in entry.args.Keys)
        {
            if (QAToolGlobals.FlagFilters.ContainsKey(argName))
            {
                QAToolGlobals.FlagFilter filter = QAToolGlobals.FlagFilters[argName];
                if (filter.enabled)
                {
                    object argValue = entry.args[argName];

                    switch (filter.op)
                    {
                        case QAToolGlobals.FilterOperator.Ignore:
                            return entry;

                        case QAToolGlobals.FilterOperator.Equal:
                            return Equals(argValue, filter.value) ? entry : null;

                        case QAToolGlobals.FilterOperator.NotEqual:
                            return !Equals(argValue, filter.value) ? entry : null;

                        case QAToolGlobals.FilterOperator.GreaterThan:
                            return Compare(argValue, filter.value) > 0 ? entry : null;

                        case QAToolGlobals.FilterOperator.GreaterThanOrEqual:
                            return Compare(argValue, filter.value) >= 0 ? entry : null;

                        case QAToolGlobals.FilterOperator.LessThan:
                            return Compare(argValue, filter.value) < 0 ? entry : null;

                        case QAToolGlobals.FilterOperator.LessThanOrEqual:
                            return Compare(argValue, filter.value) <= 0 ? entry : null;

                        default:
                            break;
                    }
                }
            }
        }

        return entry;
    }

    private static int Compare(object a, object b)
    {
        if (a == null || b == null)
            throw new InvalidOperationException("Cannot compare null values.");

        // Normalize both to the same type (use the filter value's type as target)
        try
        {
            Type targetType = b.GetType();
            object normalizedA = Convert.ChangeType(a, targetType);

            if (normalizedA is IComparable comparable)
                return comparable.CompareTo(b);
        }
        catch { /* fall through */ }

        throw new InvalidOperationException($"Cannot compare types {a?.GetType().Name} and {b?.GetType().Name}.");
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
