using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace QATool
{
    public class QAToolFlagWindow : EditorWindow
    {
        private string inputText = "";
        private int selectedTypeIndex = 0;
        private readonly string[] typeLabels = { "String", "Int", "Float", "Bool" };
        private readonly Type[] typeValues = { typeof(string), typeof(int), typeof(float), typeof(bool) };
        private Dictionary<string, Type> entries = new Dictionary<string, Type>();
        private Vector2 scrollPos;
        private void OnEnable()
        {
            entries = QAToolGlobals.flagTypes ?? new Dictionary<string, Type>();

        }
        public static void ShowWindow()
        {
            GetWindow<QAToolFlagWindow>("QA Tool Flags").Show();
        }
        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            inputText = EditorGUILayout.TextField(inputText, GUILayout.ExpandWidth(true));
            selectedTypeIndex = EditorGUILayout.Popup(selectedTypeIndex, typeLabels, GUILayout.Width(70));
            GUI.enabled = !string.IsNullOrWhiteSpace(inputText);
            if (GUILayout.Button("Add", GUILayout.Width(50)))
            {
                entries[inputText.Trim()] = typeValues[selectedTypeIndex];
                QAToolGlobals.flagTypes = entries;
                inputText = "";
                GUI.FocusControl(null);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
            DrawHorizontalLine();
            EditorGUILayout.Space(4);
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            var toDelete = new List<string>();
            foreach (var kvp in entries)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(kvp.Key, GUILayout.ExpandWidth(true));
                string typeName = QAToolGlobals.typeNameToString.TryGetValue(kvp.Value.Name, out var label) ? label : kvp.Value.Name;
                EditorGUILayout.LabelField($"[{typeName}]", GUILayout.Width(52));
                if (GUILayout.Button("✕", GUILayout.Width(24)))
                {
                    toDelete.Add(kvp.Key);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            foreach (var key in toDelete)
            {
                entries.Remove(key);
            }
            if (toDelete.Count > 0)
            {
                QAToolGlobals.flagTypes = entries;
            }

            // ── Clear All ──────────────────────────────────────────────
            EditorGUILayout.Space(4);
            DrawHorizontalLine();
            EditorGUILayout.Space(4);
            GUI.enabled = entries.Count > 0;
            if (GUILayout.Button("Clear All Flags"))
            {
                if (EditorUtility.DisplayDialog(
                        "Clear All Flags",
                        $"Are you sure you want to remove all {entries.Count} flag(s)? This cannot be undone.",
                        "Clear All",
                        "Cancel"))
                {
                    entries.Clear();
                    QAToolGlobals.flagTypes = null;
                }
            }
            GUI.enabled = true;
        }
        private static void DrawHorizontalLine()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}