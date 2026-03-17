using Newtonsoft.Json;
using QATool;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;




public class QAToolPlayerTracker : MonoBehaviour
{
    private float dataPointsPerSecond;
    private float timerFrequency = 1f;
    private float timer;
    private float playSessionDuration;
    private string filePath;
    private KeyCode keyCode;
    Button submitButton;
    Rigidbody rigidBody;
    private static QAToolPlayerTracker instance;
    public static QAToolPlayerTracker Instance
    {
        set { instance = value; }
        get
        {
            if (instance == null)
            {
                throw new Exception("No PlayerTracker found");
            }
            return instance;
        }
    }

    bool showingFeedbackNoteWindow = false;


    void Awake()
    {
        instance = this;
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
        if (!TryGetComponent<Rigidbody>(out rigidBody))
        {
            throw new Exception("Player Must have RigidBody");
        } 
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

        if (Input.GetKeyDown(keyCode) && !showingFeedbackNoteWindow)
        {

            CreateFeedbackNotesWindow();
        }
    }

    public void PrintJSON(QAToolJSONTypes type, Dictionary<string, object> args = null)
    {
        var entry = new Dictionary<string, object>
        {
            { "PlayerPosition", new {
                x = transform.position.x.ToString("F4"),
                y = transform.position.y.ToString("F4"),
                z = transform.position.z.ToString("F4") }},
            { "PlayerVelocity", new {
                x = rigidBody.linearVelocity.x.ToString("F4"),
                y = rigidBody.linearVelocity.y.ToString("F4"),
                z = rigidBody.linearVelocity.z.ToString("F4") }},
            { "PlayerCamera", new {
                x = Camera.main.transform.rotation.x.ToString("F4"),
                y = Camera.main.transform.rotation.y.ToString("F4"),
                z = Camera.main.transform.rotation.z.ToString("F4") }},
            { "type", type.ToString() },
            { "time", playSessionDuration },
            { "playerID", 1 },
            { "args", args ?? new Dictionary<string, object>() }
        };
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Culture = System.Globalization.CultureInfo.InvariantCulture
        };

        string jsonLine = JsonConvert.SerializeObject(entry,settings);
        File.AppendAllText(filePath, jsonLine + Environment.NewLine);
    }

    GameObject feedbackPanel;

    public void CreateFeedbackNotesWindow(string prompt = null, Color? accentColor = null)
    {
        showingFeedbackNoteWindow = true;

        Color accent = accentColor ?? new Color(0f, 0.8f, 0.4f, 1f);
        
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

        // Dark background panel
        feedbackPanel = new GameObject("FeedbackPanel");
        feedbackPanel.transform.SetParent(canvas.transform, false);
        Image panelImage = feedbackPanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        RectTransform panelRT = feedbackPanel.GetComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(560, 320);
        panelRT.anchoredPosition = Vector2.zero;

        // Accent bar at top of panel
        GameObject accentBar = new GameObject("AccentBar");
        accentBar.transform.SetParent(feedbackPanel.transform, false);
        Image accentImage = accentBar.AddComponent<Image>();
        accentImage.color = accent;
        RectTransform accentRT = accentBar.GetComponent<RectTransform>();
        accentRT.sizeDelta = new Vector2(560, 4);
        accentRT.anchorMin = new Vector2(0.5f, 1f);
        accentRT.anchorMax = new Vector2(0.5f, 1f);
        accentRT.anchoredPosition = new Vector2(0, -2);

        // Prompt label
        if (prompt != null)
        {
            TextMeshProUGUI promptLabel = new GameObject("Prompt").AddComponent<TextMeshProUGUI>();
            promptLabel.transform.SetParent(feedbackPanel.transform, false);
            promptLabel.text = prompt;
            promptLabel.color = Color.white;
            promptLabel.fontSize = 18;
            promptLabel.fontStyle = FontStyles.Bold;
            promptLabel.alignment = TextAlignmentOptions.TopLeft;
            RectTransform promptRT = promptLabel.GetComponent<RectTransform>();
            promptRT.sizeDelta = new Vector2(500, 50);
            promptRT.anchoredPosition = new Vector2(0, 110);
        }

        // Input field
        TMP_InputField inputField = new GameObject("InputField").AddComponent<TMP_InputField>();
        inputField.transform.SetParent(feedbackPanel.transform, false);
        Image inputImage = inputField.gameObject.AddComponent<Image>();
        inputImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform inputRT = inputField.GetComponent<RectTransform>();
        inputRT.sizeDelta = new Vector2(500, 120);
        inputRT.anchoredPosition = new Vector2(0, 20);
        inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;

        RectTransform textArea = new GameObject("Text Area").AddComponent<RectTransform>();
        textArea.transform.SetParent(inputField.transform, false);
        textArea.sizeDelta = new Vector2(480, 110);
        textArea.gameObject.AddComponent<RectMask2D>();

        TextMeshProUGUI placeholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
        placeholder.transform.SetParent(textArea.transform, false);
        placeholder.text = "Type your response here...";
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.fontSize = 14;
        placeholder.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 110);

        TextMeshProUGUI text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        text.transform.SetParent(textArea.transform, false);
        text.color = Color.white;
        text.fontSize = 14;
        text.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 110);

        inputField.textViewport = textArea.GetComponent<RectTransform>();
        inputField.textComponent = text;
        inputField.placeholder = placeholder;
        
        StartCoroutine(FocusInputField(inputField));
        
        // Submit button
        submitButton = new GameObject("SubmitButton").AddComponent<Button>();
        submitButton.transform.SetParent(feedbackPanel.transform, false);
        Image buttonImage = submitButton.gameObject.AddComponent<Image>();
        buttonImage.color = accent;
        RectTransform buttonRT = submitButton.GetComponent<RectTransform>();
        buttonRT.sizeDelta = new Vector2(500, 40);
        buttonRT.anchoredPosition = new Vector2(0, -110);

        TextMeshProUGUI buttonText = new GameObject("ButtonText").AddComponent<TextMeshProUGUI>();
        buttonText.transform.SetParent(submitButton.transform, false);
        buttonText.text = "SUBMIT";
        buttonText.color = Color.white;
        buttonText.fontSize = 16;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 40);

        ColorBlock colors = submitButton.colors;
        colors.highlightedColor = new Color(accent.r + 0.1f, accent.g + 0.1f, accent.b + 0.1f, 1f);
        colors.pressedColor = new Color(accent.r - 0.2f, accent.g - 0.2f, accent.b - 0.2f, 1f);
        submitButton.colors = colors;

        submitButton.onClick.AddListener(() => { SubmitNote(inputField, prompt); });
    }
    float timeScale;
    private IEnumerator FocusInputField(TMP_InputField inputField)
    {
        timeScale = Time.timeScale;
        Time.timeScale = 0;
        yield return null;
        inputField.ActivateInputField();
        inputField.Select();
    }

    public void SubmitNote(TMP_InputField inputField, string prompt)
    {
        Dictionary<string, object> dict = new Dictionary<string, object> { { "note", inputField.text } };
        if (prompt != null)
        {
            dict["prompt"] = prompt;
        }

        PrintJSON(QAToolJSONTypes.FeedbackNote, dict);
        Destroy(feedbackPanel);
        showingFeedbackNoteWindow = false;
        Time.timeScale = timeScale;
    }


    public void QAToolEvent(Dictionary<string,object> dict)
    {
        QAToolGlobals.flagValues.ToList().ForEach(x => dict.Add(x.Key, x.Value));
        PrintJSON(QAToolJSONTypes.Event, dict);
    }
}