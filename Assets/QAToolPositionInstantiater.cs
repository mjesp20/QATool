using System.Collections.Generic;
using UnityEngine;

public class QAToolPositionInstantiater : MonoBehaviour
{
    private List<Vector3> positions;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        positions = QAToolTelemetryLoader.LoadPositions("C:\\Users\\rasmu\\Documents\\QATool\\1.jsonl");

        foreach (Vector3 position in positions)
        {
            
            GameObject Cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Cube.transform.position = position;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
