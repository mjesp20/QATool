using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static System.Runtime.CompilerServices.RuntimeHelpers;



public class QAToolPlayerTracker : MonoBehaviour
{
    [SerializeField]
    float trackEverySecond = 1;

    private float timer;
    private string filePath;
    private Vector3 pos;
    private KeyCode keyCode;

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
        keyCode = (KeyCode)System.Enum.Parse(typeof(KeyCode), QAToolGlobals.feedbackKeyCode, true);
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= trackEverySecond)
        {
            timer = 0;
            pos = transform.position;

            PrintJSON(JSONType.Movement, new Dictionary<string, object>
            {
                { "PlayerPosition", new Dictionary<string, object>
                    {
                        { "x", pos.x },
                        { "y", pos.y },
                        { "z", pos.z }
                    }
                }
            });
        }
        if (Input.GetKeyDown(keyCode))
        {
            CreateFeedbackNotesWindow();
        }
    }
    

    void PrintJSON(JSONType type, Dictionary<string, object> dict)
    {
        Dictionary<string,object> commonValues = new Dictionary<string, object>
        {
            { "type", type.ToString() },
            { "time", 1 },
            { "playerID",1 }
        };

        foreach (KeyValuePair<string,object> item in commonValues)
        {
            dict[item.Key] = item.Value;
        }

        string JSONLine = ParseJSON(dict);
        File.AppendAllText(filePath, JSONLine + Environment.NewLine);
    }

    string ParseJSON(Dictionary<string, object> dict)
    {
        List<string> values = new List<string>();
        foreach (KeyValuePair<string,object> item in dict)
        {
            string key = item.Key;
            string value = ObjectToString(item.Value);
            values.Add("{" + key + ":" + value + "}");
        }

        return "{" + string.Join(", ", values) + "}";
    }

    string ObjectToString(object obj)
    {
        return obj switch
        {
            Dictionary<string, object> nested => ParseJSON(nested),
            float f => f.ToString(CultureInfo.InvariantCulture),
            double d => d.ToString(CultureInfo.InvariantCulture),
            int i => i.ToString(),
            bool b => b.ToString().ToLower(),
            null => "null",
            _ => $"\"{obj}\""
        };
    }


    enum JSONType
    {
        Movement,
        FeedbackNote,
        Event
    }

    void CreateFeedbackNotesWindow()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            eventSystem = new GameObject("EventSystem").AddComponent<EventSystem>();
            eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = new GameObject("Canvas").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>();
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        TMP_InputField inputField = new GameObject("InputField").AddComponent<TMP_InputField>();
        inputField.transform.SetParent(canvas.transform, false);
        inputField.gameObject.AddComponent<Image>();
        inputField.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 150);
        inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;

        RectTransform textArea = new GameObject("Text Area").AddComponent<RectTransform>();
        textArea.transform.SetParent(inputField.transform, false);
        textArea.sizeDelta = new Vector2(500, 150);
        textArea.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
        placeholder.transform.SetParent(textArea.transform, false);
        placeholder.text = "Enter text...";
        placeholder.color = Color.black;

        TextMeshProUGUI text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 150);
        text.transform.SetParent(textArea.transform, false);
        text.color = Color.black;

        inputField.textViewport = textArea.GetComponent<RectTransform>();
        inputField.textComponent = text;
        inputField.placeholder = placeholder;

        Button button = new GameObject("Button").AddComponent<Button>();
        button.AddComponent<RectTransform>();
        button.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 300);
        button.onClick.AddListener(() => { SubmitNote(inputField); });
        button.AddComponent<Image>();
        button.transform.parent = canvas.gameObject.transform;
    }
    public void SubmitNote(TMP_InputField inputField)
    {
        print(inputField.text);
        PrintJSON(JSONType.Movement, new Dictionary<string, object>());
    }
}
