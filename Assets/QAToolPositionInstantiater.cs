using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class QAToolPositionInstantiater : MonoBehaviour
{
    private List<Vector3> positions;

    private string filePath;
    

    int i = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {

        filePath = Path.Combine(QAToolGlobals.folderPath, $"{1}.jsonl");

        positions = QAToolTelemetryLoader.LoadPositions(filePath);

        GameObject oldCube = null;
        GameObject newCube = null;
        foreach (Vector3 position in positions)
        {

            newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            newCube.transform.position = position;
            newCube.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            newCube.transform.name = i.ToString();
            newCube.GetComponent<Renderer>().material.color = new Color(0, 1, 0, 1);
            newCube.GetComponent<Collider>().enabled = false;
            i++;

            if (oldCube == null)
            {
                oldCube = newCube;
                continue;
            }
            else
            {
                GameObject line = new GameObject();
                LineRenderer lineRenderer = line.AddComponent<LineRenderer>();
                lineRenderer.SetPositions(new[] { oldCube.transform.position, newCube.transform.position });
                
            }

            oldCube = newCube;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
