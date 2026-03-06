using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using QATool



public class QAToolPlayerTracker : MonoBehaviour
{
    private float dataPointsPerSecond;
    private float timerFrequency = 1f;
    private float timer;
    private float playSessionDuration;
    private string filePath;
    private KeyCode keyCode;
    Button submitButton;

    void Awake()
    {
        dataPointsPerSecond = QAToolGlobals.dataPointsPerSecond;
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
        keyCode = (KeyCode)Enum.Parse(typeof(KeyCode), QAToolGlobals.feedbackKeyCode, true);
        timerFrequency = 1f / dataPointsPerSecond;
    }

    void Update()
    {
        float delta = Time.deltaTime;
        playSessionDuration += delta;
        timer += delta;

        if (timer >= timerFrequency)
        {
            timer -= timerFrequency;
            PrintJSON(QAToolJSONTypes.Movement, QAToolGlobals.flagValues);
        }

        if (Input.GetKeyDown(keyCode))
        {
            CreateFeedbackNotesWindow();
        }
    }

    public void PrintJSON(QAToolJSONTypes type, Dictionary<string, object> args = null)
    {
        var entry = new Dictionary<string, object>
        {
            { "PlayerPosition", new { transform.position.x, transform.position.y, transform.position.z } },
            { "type", type.ToString() },
            { "time", playSessionDuration },
            { "playerID", 1 },
            { "args", args ?? new Dictionary<string, object>() }
        };

        string jsonLine = JsonConvert.SerializeObject(entry);
        File.AppendAllText(filePath, jsonLine + Environment.NewLine);
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

        submitButton = new GameObject("Button").AddComponent<Button>();
        submitButton.AddComponent<RectTransform>();
        submitButton.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 300);
        submitButton.onClick.AddListener(() => { SubmitNote(inputField); });
        submitButton.AddComponent<Image>();
        submitButton.transform.parent = canvas.gameObject.transform;
    }

    public void SubmitNote(TMP_InputField inputField)
    {
        PrintJSON(QAToolJSONTypes.FeedbackNote, new Dictionary<string, object> { { "note", inputField.text } });
        Destroy(submitButton.gameObject);
        Destroy(inputField.gameObject);
    }
}