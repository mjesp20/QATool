using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        if (!Directory.Exists(QAToolGlobals.folderPath))
        {
            Directory.CreateDirectory(QAToolGlobals.folderPath);
        }

        int highest = 0;
        foreach (string file in Directory.GetFiles(QAToolGlobals.folderPath, "*.jsonl"))
        {
            string filename = Path.GetFileNameWithoutExtension(file);
            if (int.TryParse(filename, out int num) && num > highest)
            {
                highest = num;
            }
        }
        highest++;
        filePath = Path.Combine(QAToolGlobals.folderPath, $"{highest}.jsonl");
    }
    void Start()
    {

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
                $" \"x\": {pos.x.ToString(CultureInfo.InvariantCulture)}," +
                $" \"y\": {pos.y.ToString(CultureInfo.InvariantCulture)}," +
                $" \"z\": {pos.z.ToString(CultureInfo.InvariantCulture)}" +
                $" }} }}";


            // Append line and automatically close the file
            File.AppendAllText(filePath, jsonLine + Environment.NewLine);
        }
    }
}
