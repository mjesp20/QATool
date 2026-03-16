using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace QATool
{
    public class QAToolTemporalFileWindow : EditorWindow
    {
        private static List<string> fileNames = new List<string>();
        private Vector2 scrollPosition;
        private int hoveredIndex = -1;

        public static void ShowWindow()
        {
            var window = GetWindow<QAToolTemporalFileWindow>("Player Files");
            window.minSize = new Vector2(250, 400);
            Refresh();
        }

        public static void Refresh()
        {
            fileNames = Directory.GetFiles(QAToolGlobals.folderPath)
                .Select(Path.GetFileName)
                .ToList();
        }

        void OnGUI()
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.DownArrow)
                {
                    QAToolWindow.SelectFile(Mathf.Min(QAToolWindow.activeFileIndex + 1, fileNames.Count - 1));
                    ScrollToSelected();
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.UpArrow)
                {
                    QAToolWindow.SelectFile(Mathf.Max(QAToolWindow.activeFileIndex - 1, 0));
                    ScrollToSelected();
                    Event.current.Use();
                }
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label($"{fileNames.Count} files", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
                Refresh();
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            for (int i = 0; i < fileNames.Count; i++)
            {
                bool isSelected = QAToolWindow.activeFileIndex == i;

                GUIStyle style = new GUIStyle(EditorStyles.label);
                style.padding = new RectOffset(8, 8, 4, 4);

                if (isSelected)
                {
                    style.normal.background = MakeTex(Color.cyan * 0.4f);
                    style.normal.textColor = Color.white;
                }
                else if (hoveredIndex == i)
                {
                    style.normal.background = MakeTex(Color.white * 0.1f);
                }

                Rect row = GUILayoutUtility.GetRect(
                    new GUIContent(fileNames[i]), style,
                    GUILayout.ExpandWidth(true)
                );

                if (Event.current.type == EventType.Repaint)
                    style.Draw(row, fileNames[i], false, false, isSelected, false);

                if (Event.current.type == EventType.MouseMove && row.Contains(Event.current.mousePosition))
                {
                    hoveredIndex = i;
                    Repaint();
                }

                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                {
                    QAToolWindow.SelectFile(i);

                    if (Event.current.clickCount == 2)
                        Close();

                    Repaint();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void ScrollToSelected()
        {
            float rowHeight = EditorGUIUtility.singleLineHeight + 8;
            float selectedY = QAToolWindow.activeFileIndex * rowHeight;
            float windowHeight = position.height - EditorGUIUtility.singleLineHeight;

            if (selectedY < scrollPosition.y)
                scrollPosition.y = selectedY;

            if (selectedY + rowHeight > scrollPosition.y + windowHeight)
                scrollPosition.y = selectedY + rowHeight - windowHeight;

            Repaint();
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}