using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

/// <summary>
/// Email response workflow controller.
/// Manages UI states: Detecting -> Generating -> Displaying
/// Activated by EmailCanvas "replying" state to immediately start recording.
/// </summary>
public class EmailRespond : MonoBehaviour
{
    [Header("Email Canvas Integration")]
    [SerializeField] private EmailCanvas emailCanvas;

    [Header("UI States")]
    [SerializeField] private GameObject DetectingUI;
    [SerializeField] private GameObject GeneratingUI;
    [SerializeField] private GameObject DisplayingUI;

    [Header("Text Displays")]
    [SerializeField] private TextMeshProUGUI transcriptionText;
    [SerializeField] private TextMeshProUGUI generatedEmailText;

    private TextMeshProUGUI emailContent;

    private SpeechRecognition speechRecognition;
    private bool isInitialized = false;
    private bool hasStartedReplyWorkflow = false;

    private IEnumerator Start()
    {
        // Wait a frame to ensure SpeechRecognition has initialized
        yield return null;

        // Get or create SpeechRecognition instance
        speechRecognition = FindFirstObjectByType<SpeechRecognition>();
        if (speechRecognition == null)
        {
            GameObject go = new GameObject("SpeechRecognition");
            speechRecognition = go.AddComponent<SpeechRecognition>();
            yield return null;
        }

        // Subscribe to events
        if (speechRecognition.OnRecordingStarted != null)
            speechRecognition.OnRecordingStarted.AddListener(OnRecordingStarted);
        if (speechRecognition.OnRecordingStopped != null)
            speechRecognition.OnRecordingStopped.AddListener(OnRecordingStopped);
        if (speechRecognition.OnTranscriptionComplete != null)
            speechRecognition.OnTranscriptionComplete.AddListener(OnTranscriptionComplete);
        if (speechRecognition.OnError != null)
            speechRecognition.OnError.AddListener(OnError);

        // Initialize all UIs as inactive
        HideAllUI();

        if (DisplayingUI != null)
        {
             Transform contentTransform = DisplayingUI.transform.Find("content");
             if(contentTransform != null) emailContent = contentTransform.GetComponent<TextMeshProUGUI>();
        }

        // Try to find emailCanvas if not assigned
        if (emailCanvas == null)
            emailCanvas = GetComponentInParent<EmailCanvas>();


        isInitialized = true;
        Debug.Log("[EmailRespond] Ready.");
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Check for Reply Trigger from EmailCanvas
        if (emailCanvas != null)
        {
            if (emailCanvas.replying && !hasStartedReplyWorkflow)
            {
                StartReplyWorkflow();
            }
            else if (!emailCanvas.replying && hasStartedReplyWorkflow)
            {
                 // Reset if we stopped replying
                 hasStartedReplyWorkflow = false;
                 HideAllUI();
                 // Optionally cancel recording if in progress?
            }
        }

        // Debug/Fallback controls
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard.yKey.wasPressedThisFrame) SpeechRecognition.StartRecording();
            if (keyboard.uKey.wasPressedThisFrame) SpeechRecognition.StopRecordingAndTranscribe();
        }
    }

    private void StartReplyWorkflow()
    {
        hasStartedReplyWorkflow = true;
        Debug.Log("[EmailRespond] Reply activated - Starting recording immediately...");
        
        // Skip confirmation, start recording immediately
        SpeechRecognition.StartRecording();
    }

    // Called by event
    private void OnRecordingStarted()
    {
        ShowDetectingUI(); 
        // Note: User requested "end recording" page immediately after swiping. 
        // "DetectingUI" seems to be the "Listening..." state which usually has the stop button/visuals. 
        Debug.Log("[EmailRespond] Detecting speech...");
    }

    // Called by event
    private void OnRecordingStopped()
    {
        ShowGeneratingUI();
        Debug.Log("[EmailRespond] Generating email response...");
    }

    private void OnTranscriptionComplete(string text)
    {
        UpdateTranscription(text);

        // Generate email will be called automatically by SpeechRecognition logic or manually here
        GenerateEmail.Generate(text, OnEmailGenerated);
    }

    private void OnEmailGenerated(string generatedEmail)
    {
        if (!string.IsNullOrEmpty(generatedEmail))
        {
            ShowDisplayingUI();
            UpdateGeneratedEmail(generatedEmail);
            Debug.Log($"[EmailRespond] Email displayed:\n{generatedEmail}");
        }
        else
        {
            // Keep previous UI or show error?
            Debug.LogError("[EmailRespond] Failed to generate email");
             // Maybe go back to detecting or show error state
        }
    }

    private void OnError(string error)
    {
        HideAllUI();
        Debug.LogError($"[EmailRespond] Error: {error}");
        hasStartedReplyWorkflow = false; // Reset to allow trying again
    }

    private void HideAllUI()
    {
        if (DetectingUI != null) DetectingUI.SetActive(false);
        if (GeneratingUI != null) GeneratingUI.SetActive(false);
        if (DisplayingUI != null) DisplayingUI.SetActive(false);
    }

    private void ShowDetectingUI()
    {
        SetActiveUI(DetectingUI);
    }

    private void ShowGeneratingUI()
    {
        SetActiveUI(GeneratingUI);
    }

    private void ShowDisplayingUI()
    {
        SetActiveUI(DisplayingUI);
    }

    private void SetActiveUI(GameObject activeUI)
    {
        if (DetectingUI != null) DetectingUI.SetActive(DetectingUI == activeUI);
        if (GeneratingUI != null) GeneratingUI.SetActive(GeneratingUI == activeUI);
        if (DisplayingUI != null) DisplayingUI.SetActive(DisplayingUI == activeUI);
    }

    private void UpdateTranscription(string text)
    {
        if (transcriptionText != null)
        {
            transcriptionText.text = $"You said: \"{text}\"";
        }
    }

    private void UpdateGeneratedEmail(string email)
    {
        if (generatedEmailText != null)
        {
            generatedEmailText.text = email;
        }
        
        if (emailContent != null)
        {
            emailContent.text = email;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (speechRecognition != null)
        {
            speechRecognition.OnRecordingStarted.RemoveListener(OnRecordingStarted);
            speechRecognition.OnRecordingStopped.RemoveListener(OnRecordingStopped);
            speechRecognition.OnTranscriptionComplete.RemoveListener(OnTranscriptionComplete);
            speechRecognition.OnError.RemoveListener(OnError);
        }
    }
}