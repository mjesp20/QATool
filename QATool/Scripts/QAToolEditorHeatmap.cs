#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace QATool
{
    [ExecuteAlways]
    public class QAToolEditorHeatmap : MonoBehaviour
    {
        [System.NonSerialized]
        Dictionary<Vector3Int, int> heatmap = new Dictionary<Vector3Int, int>();

        [System.NonSerialized]
        bool loaded = false;

        [System.NonSerialized]
        float lastCellSize = -1f;

        private float heatmapCellSize;
        private float heatmapOpacity;
        private float heatmapDrawThreshold;
        private float heatmapContrast;
        private float heatmapHeightOffset;
        private float heatmapPercentile;
        private float heatmapMinPercentile;
        private float heatmapMaxPercentile;


        private void OnEnable()
        {
            QAToolSceneValidator.forceValidate += OnValidate;
        }

        private void OnDisable()
        {
            QAToolSceneValidator.forceValidate -= OnValidate;
        }

        public void OnValidate()
        {
            heatmapCellSize = QAToolGlobals.heatmapCellSize;
            heatmapOpacity = QAToolGlobals.heatmapOpacity;
            heatmapContrast = QAToolGlobals.heatmapContrast;
            heatmapHeightOffset = QAToolGlobals.heatmapHeightOffset;
            heatmapMinPercentile = QAToolGlobals.heatmapMinPercentile;
            heatmapMaxPercentile = QAToolGlobals.heatmapMaxPercentile;

            if (!loaded || heatmapCellSize != lastCellSize)
            {
                LoadHeatmap();
                loaded = true;
                lastCellSize = heatmapCellSize;
            }

#if UNITY_EDITOR
            UnityEditor.SceneView.RepaintAll();
#endif
        }

        void LoadHeatmap()
        {
            if (!QAToolGlobals.showHeatMap) { return; }
            if (heatmap == null)
                heatmap = new Dictionary<Vector3Int, int>();

            heatmap.Clear();

            List<Vector3> positions = QAToolTelemetryLoader.GetAllEntries().Select(entry => entry.position.ToVector3()).ToList();


            //Debug.Log($"{positions.Count} positions");

            foreach (var position in positions)
            {
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(position.x / heatmapCellSize),
                    Mathf.FloorToInt(position.y / heatmapCellSize),
                    Mathf.FloorToInt(position.z / heatmapCellSize)
                );

                if (!heatmap.ContainsKey(cell))
                    heatmap[cell] = 0;
                heatmap[cell]++;
            }



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


            List<KeyValuePair<Vector3Int, int>> cells =
                new List<KeyValuePair<Vector3Int, int>>(heatmap);

            cells.Sort((a, b) =>
            {
                Vector3 centerA = new Vector3(
                    a.Key.x * heatmapCellSize + heatmapCellSize / 2f,
                    a.Key.y * heatmapCellSize + heatmapCellSize / 2f,
                    a.Key.z * heatmapCellSize + heatmapCellSize / 2f
                );

                Vector3 centerB = new Vector3(
                    b.Key.x * heatmapCellSize + heatmapCellSize / 2f,
                    b.Key.y * heatmapCellSize + heatmapCellSize / 2f,
                    b.Key.z * heatmapCellSize + heatmapCellSize / 2f
                );

                float distA = Vector3.Distance(cam.transform.position, centerA);
                float distB = Vector3.Distance(cam.transform.position, centerB);

                return distB.CompareTo(distA);
            });


            List<int> values = new List<int>();

            foreach (KeyValuePair<Vector3Int, int> kvp in cells)
            {
                values.Add(kvp.Value);
            }

            if (values.Count == 0)
                return;

            values.Sort();

            int totalCount = values.Count;

            int minIndex = Mathf.FloorToInt(heatmapMinPercentile * (totalCount - 1));
            int maxIndex = Mathf.FloorToInt(heatmapMaxPercentile * (totalCount - 1));

            minIndex = Mathf.Clamp(minIndex, 0, totalCount - 1);
            maxIndex = Mathf.Clamp(maxIndex, 0, totalCount - 1);

            int minThreshold = values[minIndex];
            int maxThreshold = values[maxIndex];


            int logMin = Mathf.Max(1, minThreshold);
            int logMax = Mathf.Max(logMin + 1, maxThreshold);

            foreach (KeyValuePair<Vector3Int, int> kvp in cells)
            {

                if (kvp.Value < minThreshold || kvp.Value > maxThreshold)
                    continue;

                float normalized =
                    Mathf.Log(kvp.Value - logMin + 2f) /
                    Mathf.Log(logMax - logMin + 2f);

                normalized = Mathf.Pow(normalized, heatmapContrast);

                Color color = Color.Lerp(Color.green, Color.red, normalized);

                Vector3 center = new Vector3(
                    kvp.Key.x * heatmapCellSize + heatmapCellSize / 2f,
                    kvp.Key.y * heatmapCellSize + heatmapCellSize / 2f,
                    kvp.Key.z * heatmapCellSize + heatmapCellSize / 2f
                );

                Gizmos.color = new Color(color.r, color.g, color.b, heatmapOpacity);
                Gizmos.DrawCube(center, Vector3.one * heatmapCellSize);
            }
        }
    }
}
#endif