using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



public class QAToolPlayerTracker : MonoBehaviour
{
    [SerializeField]
    float trackEverySecond = 1;

    private float timer;
    private string filePath;
    private Vector3 pos;

    void Awake()
    {

        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string folderPath = Path.Combine(documentsPath, "QATool");
        filePath = Path.Combine(folderPath, $"{1}.jsonl");

        // Make sure the folder exists
        Directory.CreateDirectory(folderPath);

    }
    void Start()
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= trackEverySecond)
        {
            timer = 0;
            pos = transform.position;



            // Line you want to write (example JSON line)
            string jsonLine =
                $"{{ \"time\": \"{DateTime.Now:o}\"," +
                $" \"playerID\": \"1\"," +
                $" \"PlayerPosition\": {{" +
                $" \"x\": {pos.x}," +
                $" \"y\": {pos.y}," +
                $" \"z\": {pos.z}" +
                $" }} }}";


            // Append line and automatically close the file
            File.AppendAllText(filePath, jsonLine + Environment.NewLine);
        }
    }
}
