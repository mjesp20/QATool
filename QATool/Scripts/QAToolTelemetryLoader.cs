    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using UnityEngine;
    using UnityEngine.UIElements;

namespace QATool
{

    public static class QAToolTelemetryLoader
    {




        static string folderPath = QAToolGlobals.folderPath;

#if UNITY_EDITOR
        #region Generic Functions
        public static List<List<QAToolTelemetryClass.Entry>> LoadFromFolder()
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            string[] filePaths = Directory.GetFiles(folderPath);
            if (filePaths.Length == 0)
            {
                return new List<List<QAToolTelemetryClass.Entry>>();
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

        static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Culture = System.Globalization.CultureInfo.InvariantCulture
        };
        public static QAToolTelemetryClass.Entry ParseLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            try
            {
                QAToolTelemetryClass.Entry entry = JsonConvert.DeserializeObject<QAToolTelemetryClass.Entry>(line,settings);
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
            foreach (KeyValuePair<string, object> arg in entry.args)
            {
                if (arg.Key == "note" || arg.Key == "event" || arg.Key == "prompt")
                {
                    continue;
                }

                var flags = QAToolGlobals.flagTypes ?? new Dictionary<string, Type>();


                if (!flags.ContainsKey(arg.Key))
                {
                    object normalized = QAToolGlobals.NormalizeType(arg.Value);
                    flags[arg.Key] = normalized != null ? normalized.GetType() : typeof(object);
                }


                QAToolGlobals.flagTypes = flags;

                if (!QAToolGlobals.FlagFilters.ContainsKey(arg.Key))
                    continue;

                QAToolGlobals.FlagFilter filter = QAToolGlobals.FlagFilters[arg.Key];

                if (!filter.enabled)
                    continue;

                // If filter value is null, skip comparison
                if (filter.value == null)
                    continue;

                if (arg.Value == null)
                    continue;

                object entryVal = QAToolGlobals.NormalizeType(arg.Value);
                object filterVal = QAToolGlobals.NormalizeType(filter.value);

                // Both must be IComparable for ordered comparisons
                IComparable comparable = entryVal as IComparable;

                bool pass = true;

                switch (filter.op)
                {
                    case QAToolGlobals.FilterOperator.Ignore:
                        pass = true;
                        break;

                    case QAToolGlobals.FilterOperator.Equal:
                        pass = entryVal?.Equals(filterVal) ?? false;
                        break;

                    case QAToolGlobals.FilterOperator.NotEqual:
                        pass = !(entryVal?.Equals(filterVal) ?? false);
                        break;

                    case QAToolGlobals.FilterOperator.GreaterThan:
                        pass = comparable != null && comparable.CompareTo(filterVal) > 0;
                        break;

                    case QAToolGlobals.FilterOperator.GreaterThanOrEqual:
                        pass = comparable != null && comparable.CompareTo(filterVal) >= 0;
                        break;

                    case QAToolGlobals.FilterOperator.LessThan:
                        pass = comparable != null && comparable.CompareTo(filterVal) < 0;
                        break;

                    case QAToolGlobals.FilterOperator.LessThanOrEqual:
                        pass = comparable != null && comparable.CompareTo(filterVal) <= 0;
                        break;

                    default:
                        pass = true;
                        break;
                }

                // If any active filter fails, discard the entry
                if (!pass) return null;
            }

            return entry;
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
            List<QAToolTelemetryClass.Entry> entries = new List<QAToolTelemetryClass.Entry>();

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
#endif
    }
}